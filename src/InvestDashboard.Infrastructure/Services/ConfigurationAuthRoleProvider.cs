using InvestDashboard.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace InvestDashboard.Infrastructure.Services;

public sealed class ConfigurationAuthRoleProvider(IConfiguration configuration) : IAuthRoleProvider
{
    public string GetRole(Guid userId)
    {
        var allowedAdminIds = configuration.GetSection("Auth:AdminUserIds").GetChildren()
            .Select(section => section.Value);

        return allowedAdminIds.Contains(userId.ToString(), StringComparer.OrdinalIgnoreCase)
            ? "admin"
            : "user";
    }
}
