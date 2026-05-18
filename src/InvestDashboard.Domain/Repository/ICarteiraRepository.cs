using System;
using System.Threading;
using System.Threading.Tasks;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Domain.Repository;

public interface ICarteiraRepository
{
    Task<Carteira?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Carteira?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Carteira carteira, CancellationToken cancellationToken = default);
    void Update(Carteira carteira);
    void Delete(Carteira carteira);
}
