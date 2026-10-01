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
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connectionString);
        Guid portfolioId;
        using (var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString))
        using (var client = factory.CreateClient())
        {
            portfolioId = await InvestmentPositionFlowTests.ExecutePositionFlowAsync(client);
        }

        using var restartedFactory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var restartedClient = restartedFactory.CreateClient();
        restartedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));

        var response = await restartedClient.GetAsync($"/api/v1/portfolios/{portfolioId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var positions = body.RootElement.GetProperty("dados").GetProperty("posicoes");
        positions.GetArrayLength().Should().Be(1);
        positions[0].GetProperty("quantidade").GetDecimal().Should().Be(6m);
    }
}
