using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services;

public sealed class GrupoCarteirasAppService(
    IGrupoCarteirasRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IInvitationEmailSender invitationEmailSender) : IGrupoCarteirasAppService
{
    public async Task<GrupoCarteirasDto> CriarAsync(string nome, string usuarioId, string email, string nomeUsuario, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(email))
            throw new UnauthorizedAccessException("A sessão não contém uma identidade de usuário válida.");

        var group = new GrupoCarteiras(Guid.NewGuid(), nome, usuarioId, timeProvider.GetUtcNow().UtcDateTime);
        var admin = new MembroGrupo(Guid.NewGuid(), group.Id, usuarioId, email, nomeUsuario, PapelGrupo.Admin, timeProvider.GetUtcNow().UtcDateTime);
        await repository.AddGroupAsync(group, cancellationToken);
        await repository.AddMemberAsync(admin, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new GrupoCarteirasDto(group.Id, group.Name, "Admin");
    }

    public async Task<IReadOnlyList<GrupoCarteirasDto>> ListarAsync(string usuarioId, CancellationToken cancellationToken = default) =>
        (await repository.GetGroupsForUserAsync(usuarioId, cancellationToken))
        .Select(item => new GrupoCarteirasDto(item.Group.Id, item.Group.Name, item.Membership.Papel.ToString())).ToList();

    public async Task<IReadOnlyList<MembroGrupoDto>?> ListarMembrosAsync(Guid grupoId, string usuarioId, CancellationToken cancellationToken = default)
    {
        var caller = await repository.GetMemberAsync(grupoId, usuarioId, cancellationToken);
        if (caller is null || !caller.Ativo || caller.Papel != PapelGrupo.Admin) return null;
        return (await repository.GetMembersAsync(grupoId, cancellationToken))
            .Select(member => new MembroGrupoDto(member.Id, member.UsuarioId, member.Email, member.Nome, member.Papel.ToString(), member.Ativo)).ToList();
    }

    public async Task<bool?> AlterarPapelAsync(Guid grupoId, Guid membroId, string papel, string usuarioId, CancellationToken cancellationToken = default)
    {
        var caller = await repository.GetMemberAsync(grupoId, usuarioId, cancellationToken);
        if (caller is null || !caller.Ativo || caller.Papel != PapelGrupo.Admin) return null;
        if (!Enum.TryParse<PapelGrupo>(papel, true, out var role) || !Enum.IsDefined(role)) return false;
        var member = await repository.GetMemberByIdAsync(grupoId, membroId, cancellationToken);
        if (member is null) return false;
        if (member.Papel == PapelGrupo.Admin && role != PapelGrupo.Admin && await repository.CountActiveAdminsAsync(grupoId, cancellationToken) <= 1) return false;
        member.AlterarPapel(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool?> DesativarAsync(Guid grupoId, Guid membroId, string usuarioId, CancellationToken cancellationToken = default)
    {
        var caller = await repository.GetMemberAsync(grupoId, usuarioId, cancellationToken);
        if (caller is null || !caller.Ativo || caller.Papel != PapelGrupo.Admin) return null;
        var member = await repository.GetMemberByIdAsync(grupoId, membroId, cancellationToken);
        if (member is null) return false;
        if (member.Papel == PapelGrupo.Admin && await repository.CountActiveAdminsAsync(grupoId, cancellationToken) <= 1) return false;
        member.Desativar();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ResultadoConviteGrupoDto?> ConvidarAsync(Guid grupoId, string email, string papel, string usuarioId, CancellationToken cancellationToken = default)
    {
        var caller = await repository.GetMemberAsync(grupoId, usuarioId, cancellationToken);
        if (caller is null || !caller.Ativo || caller.Papel != PapelGrupo.Admin) return null;
        if (!Enum.TryParse<PapelGrupo>(papel, true, out var role) || !Enum.IsDefined(role))
            throw new ArgumentException("Papel de grupo inválido.");
        if (string.IsNullOrWhiteSpace(email) || email.Length > 320 || !email.Contains('@'))
            throw new ArgumentException("E-mail de convite inválido.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (await repository.GetPendingInvitationAsync(grupoId, email, now, cancellationToken) is not null)
            throw new InvalidOperationException("Já existe um convite pendente para esse e-mail.");
        var invitationId = Guid.NewGuid();
        var emailSent = await invitationEmailSender.SendInvitationAsync(email.Trim(), grupoId, invitationId, cancellationToken);
        await repository.AddInvitationAsync(new ConviteGrupo(
            invitationId, grupoId, email, role, usuarioId, now, now.AddHours(1)), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ResultadoConviteGrupoDto(emailSent, emailSent
            ? "O Supabase enviou um link de convite; ele expira em uma hora."
            : "O e-mail já tem conta confirmada. A pessoa pode aceitar o convite na tela Grupos e usuários.");
    }

    public async Task<IReadOnlyList<ConvitePendenteGrupoDto>> ListarConvitesPendentesAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return [];
        return (await repository.GetPendingInvitationsForEmailAsync(email.Trim(), timeProvider.GetUtcNow().UtcDateTime, cancellationToken))
            .Select(item => new ConvitePendenteGrupoDto(item.Invitation.Id, item.Group.Id, item.Group.Name,
                item.Invitation.Papel.ToString(), item.Invitation.ExpiraEmUtc)).ToList();
    }

    public async Task<bool?> AceitarConviteAsync(Guid grupoId, Guid conviteId, string usuarioId, string email, string nome, CancellationToken cancellationToken = default)
    {
        var invitation = await repository.GetInvitationAsync(grupoId, conviteId, cancellationToken);
        if (invitation is null || !invitation.EstaPendente(timeProvider.GetUtcNow().UtcDateTime)) return false;
        if (!string.Equals(invitation.Email, email, StringComparison.OrdinalIgnoreCase)) return null;
        var member = await repository.GetMemberAsync(grupoId, usuarioId, cancellationToken);
        if (member is null)
            await repository.AddMemberAsync(new MembroGrupo(Guid.NewGuid(), grupoId, usuarioId, email, nome, invitation.Papel,
                timeProvider.GetUtcNow().UtcDateTime), cancellationToken);
        else
        {
            if (member.Ativo && member.Papel == PapelGrupo.Admin && invitation.Papel != PapelGrupo.Admin &&
                await repository.CountActiveAdminsAsync(grupoId, cancellationToken) <= 1) return false;
            member.AlterarPapel(invitation.Papel);
            member.Reativar();
        }
        invitation.Aceitar(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
