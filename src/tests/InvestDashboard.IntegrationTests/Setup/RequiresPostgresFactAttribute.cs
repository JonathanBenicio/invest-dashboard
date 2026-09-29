using Xunit;

namespace InvestDashboard.IntegrationTests.Setup;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiresPostgresFactAttribute : FactAttribute
{
    public RequiresPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")))
            Skip = "Requires the PostgreSQL service configured by the CI integration job.";
    }
}
