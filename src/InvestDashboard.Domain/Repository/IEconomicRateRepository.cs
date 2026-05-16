using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Domain.Repository;

public interface IEconomicRateRepository
{
    Task<EconomicRate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<EconomicRate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(EconomicRate rate, CancellationToken cancellationToken = default);
    void Update(EconomicRate rate);
    void Delete(EconomicRate rate);
}
