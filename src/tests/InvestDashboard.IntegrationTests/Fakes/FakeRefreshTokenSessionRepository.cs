using System.Collections.Concurrent;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.Authentication;

namespace InvestDashboard.IntegrationTests.Fakes;

public sealed class FakeRefreshTokenSessionRepository : IRefreshTokenSessionRepository
{
    private readonly ConcurrentDictionary<Guid, RefreshTokenSession> _sessions = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _sessionLocks = new();

    public FakeRefreshTokenSessionRepository()
    {
        var now = DateTime.UtcNow;
        _sessions[FakeAuthProvider.TestSessionId] = new RefreshTokenSession(
            FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestUserId,
            FakeAuthProvider.TestEmail,
            FakeAuthProvider.TestName,
            "user",
            new string('A', 64),
            now.AddMinutes(-1),
            now.AddDays(7));
        _sessions[FakeAuthProvider.SecondTestSessionId] = new RefreshTokenSession(
            FakeAuthProvider.SecondTestSessionId,
            FakeAuthProvider.SecondTestUserId,
            FakeAuthProvider.SecondTestEmail,
            FakeAuthProvider.SecondTestName,
            "user",
            new string('B', 64),
            now.AddMinutes(-1),
            now.AddDays(7));
    }

    public Task<RefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session);
    }

    public Task AddAsync(RefreshTokenSession session, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryAdd(session.Id, session))
            throw new InvalidOperationException("Session already exists.");
        return Task.CompletedTask;
    }

    public Task<bool> IsActiveAsync(Guid id, Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.TryGetValue(id, out var session)
            && session.UserId == userId
            && session.IsActive(nowUtc));

    public async Task<bool> TryRotateAsync(
        Guid id,
        string currentHash,
        string nextHash,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var sessionLock = _sessionLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await sessionLock.WaitAsync(cancellationToken);
        try
        {
            return _sessions.TryGetValue(id, out var session)
                && session.RotateRefreshToken(currentHash, nextHash, nowUtc);
        }
        finally
        {
            sessionLock.Release();
        }
    }

    public Task RevokeAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(id, out var session))
            session.Revoke(nowUtc);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}
