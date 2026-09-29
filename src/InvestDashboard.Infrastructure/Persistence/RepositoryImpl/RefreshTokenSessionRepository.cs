using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.Authentication;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public sealed class RefreshTokenSessionRepository(InvestDbContext context) : IRefreshTokenSessionRepository
{
    public Task<RefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.RefreshTokenSessions.SingleOrDefaultAsync(session => session.Id == id, cancellationToken);

    public async Task AddAsync(RefreshTokenSession session, CancellationToken cancellationToken = default) =>
        await context.RefreshTokenSessions.AddAsync(session, cancellationToken);

    public Task<bool> IsActiveAsync(Guid id, Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        context.RefreshTokenSessions.AnyAsync(
            session => session.Id == id && session.UserId == userId && session.RevokedAtUtc == null && session.ExpiresAtUtc > nowUtc,
            cancellationToken);

    public async Task<bool> TryRotateAsync(
        Guid id,
        string currentHash,
        string nextHash,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var updated = await context.RefreshTokenSessions
            .Where(session => session.Id == id
                && session.RefreshTokenHash == currentHash
                && session.RevokedAtUtc == null
                && session.ExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(session => session.RefreshTokenHash, nextHash),
                cancellationToken);

        return updated == 1;
    }

    public async Task RevokeAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await context.RefreshTokenSessions
            .Where(session => session.Id == id && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(session => session.RevokedAtUtc, nowUtc),
                cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
