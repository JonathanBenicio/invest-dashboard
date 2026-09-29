using InvestDashboard.Domain.Aggregates.Authentication;

namespace InvestDashboard.Application.Interfaces;

public interface IRefreshTokenSessionRepository
{
    Task<RefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshTokenSession session, CancellationToken cancellationToken = default);
    Task<bool> IsActiveAsync(Guid id, Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<bool> TryRotateAsync(Guid id, string currentHash, string nextHash, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
