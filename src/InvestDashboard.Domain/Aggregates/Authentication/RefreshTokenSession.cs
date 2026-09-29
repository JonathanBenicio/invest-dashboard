using InvestDashboard.Domain.Common;
using System.Security.Cryptography;

namespace InvestDashboard.Domain.Aggregates.Authentication;

public sealed class RefreshTokenSession : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Role { get; private set; } = "user";
    public string RefreshTokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    public RefreshTokenSession(Guid id, Guid userId, string email, string name, string role, string refreshTokenHash, DateTime createdAtUtc, DateTime expiresAtUtc)
        : base(id)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User Id cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(refreshTokenHash)) throw new ArgumentException("Token hash is required", nameof(refreshTokenHash));
        if (expiresAtUtc <= createdAtUtc) throw new ArgumentException("Session expiry must follow creation", nameof(expiresAtUtc));

        UserId = userId;
        Email = email.Trim();
        Name = name.Trim();
        Role = role;
        RefreshTokenHash = refreshTokenHash;
        CreatedAtUtc = EnsureUtc(createdAtUtc);
        ExpiresAtUtc = EnsureUtc(expiresAtUtc);
    }

    public void Revoke(DateTime nowUtc)
    {
        RevokedAtUtc ??= EnsureUtc(nowUtc);
    }

    public bool RotateRefreshToken(string currentHash, string nextHash, DateTime nowUtc)
    {
        if (!IsActive(nowUtc) || !CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(RefreshTokenHash), Convert.FromHexString(currentHash)))
            return false;

        if (string.IsNullOrWhiteSpace(nextHash))
            throw new ArgumentException("Token hash is required", nameof(nextHash));

        RefreshTokenHash = nextHash;
        return true;
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

#pragma warning disable CS8618
    private RefreshTokenSession() { }
#pragma warning restore CS8618
}
