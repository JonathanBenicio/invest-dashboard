namespace InvestDashboard.Application.Interfaces;

public interface IAccessTokenIssuer
{
    string Issue(Guid userId, Guid sessionId, string email, string name, string role, DateTime expiresAtUtc);
    string? GetConfigurationError();
}
