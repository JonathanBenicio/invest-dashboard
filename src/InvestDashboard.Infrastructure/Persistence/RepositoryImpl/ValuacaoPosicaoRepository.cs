using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public sealed class ValuacaoPosicaoRepository(InvestDbContext context) : IValuacaoPosicaoRepository
{
    public async Task AddAsync(ValuacaoPosicao valuation, CancellationToken cancellationToken = default) =>
        await context.PositionValuations.AddAsync(valuation, cancellationToken);

    public Task<ValuacaoPosicao?> GetLatestAsync(Guid positionId, CancellationToken cancellationToken = default) =>
        context.PositionValuations.AsNoTracking()
            .Where(valuation => valuation.PosicaoId == positionId)
            .OrderByDescending(valuation => valuation.DataObservadaUtc)
            .ThenByDescending(valuation => valuation.RegistradaEmUtc)
            .ThenByDescending(valuation => valuation.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ValuacaoPosicao>> GetByPositionIdAsync(Guid positionId, DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
        => await GetByPositionIdsAsync([positionId], fromDate, cancellationToken);

    public async Task<IReadOnlyList<ValuacaoPosicao>> GetByPositionIdsAsync(IReadOnlyCollection<Guid> positionIds,
        DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        if (positionIds.Count == 0) return [];
        var query = context.PositionValuations.AsNoTracking()
            .Where(valuation => positionIds.Contains(valuation.PosicaoId));
        if (fromDate.HasValue)
            query = query.Where(valuation => valuation.DataObservadaUtc >= fromDate.Value);
        return await query.OrderBy(valuation => valuation.DataObservadaUtc)
            .ThenBy(valuation => valuation.RegistradaEmUtc)
            .ThenBy(valuation => valuation.Id)
            .ToListAsync(cancellationToken);
    }
}
