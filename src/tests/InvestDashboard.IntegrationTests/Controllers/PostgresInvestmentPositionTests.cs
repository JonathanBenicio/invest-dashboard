using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Xunit;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class PostgresInvestmentPositionTests
{
    [RequiresPostgresFact]
    public async Task MigrationsAndPositionLedger_PersistBuyReplayAndSaleAcrossApiHosts()
    {
        var connectionString = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        Guid portfolioId;
        using (var factory = CustomWebApplicationFactory.CreatePostgres(connectionString))
        using (var client = factory.CreateClient())
        {
            portfolioId = await InvestmentPositionFlowTests.ExecutePositionFlowAsync(client);
        }

        using var restartedFactory = CustomWebApplicationFactory.CreatePostgres(connectionString);
        using var restartedClient = restartedFactory.CreateClient();
        restartedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));

        var response = await restartedClient.GetAsync($"/api/v1/portfolios/{portfolioId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var positions = body.RootElement.GetProperty("data").GetProperty("positions");
        positions.GetArrayLength().Should().Be(1);
        positions[0].GetProperty("quantity").GetDecimal().Should().Be(6m);
    }
}
