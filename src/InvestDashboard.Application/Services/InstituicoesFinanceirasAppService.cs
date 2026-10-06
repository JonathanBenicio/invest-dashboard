using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services;

public sealed class InstituicoesFinanceirasAppService(
    IInstituicaoFinanceiraRepository instituicoes,
    IGrupoCarteirasRepository grupos,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IInstituicoesFinanceirasAppService
{
    public async Task<IReadOnlyList<InstituicaoFinanceiraDto>> ListarAsync(
        Guid? grupoId, string usuarioId, CancellationToken cancellationToken = default)
    {
        await ValidarAcessoAoGrupoAsync(grupoId, usuarioId, cancellationToken);
        return (await instituicoes.GetCatalogoAsync(grupoId, cancellationToken))
            .Select(Mapear)
            .ToList();
    }

    public async Task<InstituicaoFinanceiraDto> CriarOutraAsync(
        Guid grupoId, string nome, string usuarioId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Informe o nome da instituição.", nameof(nome));
        var membro = await ValidarAcessoAoGrupoAsync(grupoId, usuarioId, cancellationToken);
        if (membro!.Papel == PapelGrupo.Consulta)
            throw new UnauthorizedAccessException("Usuários Consulta não podem cadastrar instituições no grupo.");

        var normalizedName = InstituicaoFinanceira.NormalizeName(nome);
        var existing = await instituicoes.GetByNameAsync(grupoId, normalizedName, cancellationToken);
        if (existing is not null) return Mapear(existing);

        var globalMatch = await instituicoes.GetByNameAsync(null, normalizedName, cancellationToken);
        if (globalMatch is not null) return Mapear(globalMatch);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var custom = new InstituicaoFinanceira(
            Guid.NewGuid(), nome, CategoriaInstituicaoFinanceira.Outra, grupoId, now);
        await instituicoes.AddAsync(custom, cancellationToken);
        await unitOfWork.SaveChangesAsync();
        return Mapear(custom);
    }

    private async Task<MembroGrupo?> ValidarAcessoAoGrupoAsync(Guid? grupoId, string usuarioId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(usuarioId)) throw new UnauthorizedAccessException("É necessário entrar na conta.");
        if (!grupoId.HasValue) return null;
        var membro = await grupos.GetMemberAsync(grupoId.Value, usuarioId, cancellationToken);
        if (membro is null || !membro.Ativo)
            throw new UnauthorizedAccessException("É necessário ser membro ativo do grupo.");
        return membro;
    }

    private static InstituicaoFinanceiraDto Mapear(InstituicaoFinanceira instituicao) =>
        new(instituicao.Id, instituicao.Nome, instituicao.Categoria.ToString(), instituicao.GrupoId, instituicao.Personalizada);
}
