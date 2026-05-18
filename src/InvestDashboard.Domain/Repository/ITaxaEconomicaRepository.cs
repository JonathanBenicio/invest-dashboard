using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Domain.Repository;

public interface ITaxaEconomicaRepository
{
    Task<TaxaEconomica?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TaxaEconomica>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TaxaEconomica taxa, CancellationToken cancellationToken = default);
    void Update(TaxaEconomica taxa);
    void Delete(TaxaEconomica taxa);
}
