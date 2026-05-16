using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Domain.Repository;

public interface IHistoricalPriceRepository
{
    Task<IReadOnlyList<HistoricalPrice>> GetByAssetIdAsync(Guid assetId, DateTime? fromDate = null, CancellationToken cancellationToken = default);
    Task AddAsync(HistoricalPrice historicalPrice, CancellationToken cancellationToken = default);
}
