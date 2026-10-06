using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public class PrecoHistoricoRepository : IPrecoHistoricoRepository
{
    private readonly InvestDbContext _context;

    public PrecoHistoricoRepository(InvestDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<PrecoHistorico>> GetByAtivoIdAsync(
        Guid assetId, 
        DateTime? fromDate = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.HistoricalPrices
            .AsNoTracking()
            .Where(hp => hp.AtivoId == assetId && hp.Source != "statement");

        if (fromDate.HasValue)
        {
            var utcFromDate = fromDate.Value.Kind == DateTimeKind.Utc ? fromDate.Value : fromDate.Value.ToUniversalTime();
            query = query.Where(hp => hp.Date >= utcFromDate);
        }

        return await query
            .OrderBy(hp => hp.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PrecoHistorico>> GetByAtivoIdsAsync(
        IReadOnlyCollection<Guid> assetIds,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        if (assetIds.Count == 0)
            return Array.Empty<PrecoHistorico>();

        var query = _context.HistoricalPrices.AsNoTracking()
            .Where(price => assetIds.Contains(price.AtivoId) && price.Source != "statement");

        if (fromDate.HasValue)
        {
            var utcFromDate = fromDate.Value.Kind == DateTimeKind.Utc
                ? fromDate.Value
                : fromDate.Value.ToUniversalTime();
            query = query.Where(price => price.Date >= utcFromDate);
        }

        return await query.OrderBy(price => price.Date).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PrecoHistorico historicalPrice, CancellationToken cancellationToken = default)
    {
        if (historicalPrice is null)
            throw new ArgumentNullException(nameof(historicalPrice));

        await _context.HistoricalPrices.AddAsync(historicalPrice, cancellationToken);
    }
}
