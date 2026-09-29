using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InvestDashboard.WebAPI.Health;

public sealed class DatabaseHealthCheck(InvestDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await database.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database connection failed.");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Database connection failed.");
        }
    }
}
