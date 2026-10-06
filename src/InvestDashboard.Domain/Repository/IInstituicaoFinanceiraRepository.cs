using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Domain.Repository;

public interface IInstituicaoFinanceiraRepository
{
    Task<IReadOnlyList<InstituicaoFinanceira>> GetCatalogoAsync(Guid? grupoId, CancellationToken cancellationToken = default);
    Task<InstituicaoFinanceira?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InstituicaoFinanceira?> GetByNameAsync(Guid? grupoId, string nomeNormalizado, CancellationToken cancellationToken = default);
    Task AddAsync(InstituicaoFinanceira instituicao, CancellationToken cancellationToken = default);
}
