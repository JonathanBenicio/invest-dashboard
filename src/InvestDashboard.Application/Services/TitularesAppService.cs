using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services;

public sealed class TitularesAppService(
    ITitularCarteiraRepository titulares,
    IGrupoCarteirasRepository grupos,
    ICarteiraRepository carteiras,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<TitularCarteiraDto>> ListarAsync(Guid grupoId, string usuarioId)
    {
        var member = await RequireMemberAsync(grupoId, usuarioId);
        var profiles = await titulares.ListarAsync(grupoId);
        if (member.Papel == PapelGrupo.Admin) return profiles.Select(Map).ToArray();
        var visible = new HashSet<Guid>();
        var count = await carteiras.CountByUserIdAsync(usuarioId, grupoId);
        for (var skip = 0; skip < count; skip += 100)
            foreach (var wallet in await carteiras.GetByUserIdPageAsync(usuarioId, skip, 100, grupoId))
                if (wallet.TitularId.HasValue) visible.Add(wallet.TitularId.Value);
        return profiles.Where(profile => profile.UsuarioId == usuarioId || visible.Contains(profile.Id))
            .Select(profile => new TitularCarteiraDto(profile.Id, profile.GrupoId, profile.Nome, profile.Parentesco,
                profile.UsuarioId == usuarioId ? usuarioId : null)).ToArray();
    }

    public async Task<TitularCarteiraDto> SalvarAsync(Guid grupoId, Guid? id, SalvarTitularCarteiraDto request, string usuarioId)
    {
        var member = await RequireMemberAsync(grupoId, usuarioId);
        if (member.Papel != PapelGrupo.Admin) throw new UnauthorizedAccessException("Somente Admin pode gerenciar titulares.");
        var linkedUser = string.IsNullOrWhiteSpace(request.UsuarioId) ? null : request.UsuarioId.Trim();
        if (linkedUser is not null)
        {
            await RequireMemberAsync(grupoId, linkedUser);
            var existingLink = await titulares.GetByUserIdAsync(grupoId, linkedUser);
            if (existingLink is not null && existingLink.Id != id)
                throw new ArgumentException("Este membro já está vinculado a outro perfil de titular.");
        }
        TitularCarteira profile;
        if (id.HasValue)
        {
            profile = await titulares.GetByIdInGroupAsync(id.Value, grupoId)
                ?? throw new KeyNotFoundException("Titular não encontrado neste grupo.");
            profile.AtualizarDados(request.Nome, linkedUser, request.Parentesco, clock.GetUtcNow().UtcDateTime);
            titulares.Update(profile);
        }
        else
        {
            profile = new TitularCarteira(Guid.NewGuid(), grupoId, request.Nome, linkedUser, request.Parentesco, clock.GetUtcNow().UtcDateTime);
            await titulares.AddAsync(profile);
        }
        await unitOfWork.SaveChangesAsync();
        return Map(profile);
    }

    private async Task<MembroGrupo> RequireMemberAsync(Guid grupoId, string usuarioId)
    {
        var member = await grupos.GetMemberAsync(grupoId, usuarioId);
        if (member is null || !member.Ativo) throw new UnauthorizedAccessException("É necessário ser membro ativo do grupo.");
        return member;
    }

    private static TitularCarteiraDto Map(TitularCarteira profile) =>
        new(profile.Id, profile.GrupoId, profile.Nome, profile.Parentesco, profile.UsuarioId);
}
