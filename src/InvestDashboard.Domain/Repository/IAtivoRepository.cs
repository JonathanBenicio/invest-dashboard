using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Domain.Repository;

public interface IAtivoRepository
{
    Task<Ativo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Ativo?> GetByTickerAsync(string ticker, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Ativo>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Ativo ativo, CancellationToken cancellationToken = default);
    void Update(Ativo ativo);
    void Delete(Ativo ativo);
}
