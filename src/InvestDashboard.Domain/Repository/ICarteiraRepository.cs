using System;
using System.Threading;
using System.Threading.Tasks;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Domain.Repository;

public interface ICarteiraRepository
{
    Task<Carteira?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Carteira?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<Carteira?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Carteira>> GetByUserIdPageAsync(string userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<(Carteira Portfolio, PosicaoInvestimento Position)?> GetPositionByIdForUserAsync(Guid positionId, string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Carteira carteira, CancellationToken cancellationToken = default);
    void AddPosition(PosicaoInvestimento position);
    void Update(Carteira carteira);
    void Delete(Carteira carteira);
}
