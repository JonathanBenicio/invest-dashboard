using System;
using System.Threading;
using System.Threading.Tasks;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Domain.Repository;

public interface IPortfolioRepository
{
    Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Portfolio?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Portfolio portfolio, CancellationToken cancellationToken = default);
    void Update(Portfolio portfolio);
    void Delete(Portfolio portfolio);
}
