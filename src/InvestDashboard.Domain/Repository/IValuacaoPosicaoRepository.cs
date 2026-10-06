using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Domain.Repository;

public interface IValuacaoPosicaoRepository
{
    Task AddAsync(ValuacaoPosicao valuation, CancellationToken cancellationToken = default);
    Task<ValuacaoPosicao?> GetLatestAsync(Guid positionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ValuacaoPosicao>> GetByPositionIdAsync(Guid positionId, DateTime? fromDate = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ValuacaoPosicao>> GetByPositionIdsAsync(IReadOnlyCollection<Guid> positionIds,
        DateTime? fromDate = null, CancellationToken cancellationToken = default);
}
