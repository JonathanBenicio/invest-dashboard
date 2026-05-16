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

public class HistoricalPriceRepository : IHistoricalPriceRepository
{
    private readonly InvestDbContext _context;

    public HistoricalPriceRepository(InvestDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<HistoricalPrice>> GetByAssetIdAsync(
        Guid assetId, 
        DateTime? fromDate = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.HistoricalPrices
            .AsNoTracking()
            .Where(hp => hp.AssetId == assetId);

        if (fromDate.HasValue)
        {
            var utcFromDate = fromDate.Value.Kind == DateTimeKind.Utc ? fromDate.Value : fromDate.Value.ToUniversalTime();
            query = query.Where(hp => hp.Date >= utcFromDate);
        }

        return await query
            .OrderBy(hp => hp.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(HistoricalPrice historicalPrice, CancellationToken cancellationToken = default)
    {
        if (historicalPrice is null)
            throw new ArgumentNullException(nameof(historicalPrice));

        await _context.HistoricalPrices.AddAsync(historicalPrice, cancellationToken);
    }
}
