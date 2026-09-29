using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class PortfolioTenantIsolationTests
{
    [Fact]
    public async Task PortfolioAndPositions_AreIsolatedBetweenAuthenticatedUsers_InMemory()
    {
        using var factory = new CustomWebApplicationFactory();
        await AssertPortfolioIsolationAsync(factory);
    }

    [RequiresPostgresFact]
    public async Task PortfolioAndPositions_AreIsolatedBetweenAuthenticatedUsers_Postgres()
    {
        var connectionString = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connectionString);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        await AssertPortfolioIsolationAsync(factory);
    }

    private static async Task AssertPortfolioIsolationAsync(CustomWebApplicationFactory factory)
    {
        using var ownerClient = CreateClient(
            factory,
            FakeAuthProvider.TestUserId,
            FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail,
            FakeAuthProvider.TestName);
        using var otherClient = CreateClient(
            factory,
            FakeAuthProvider.SecondTestUserId,
            FakeAuthProvider.SecondTestSessionId,
            FakeAuthProvider.SecondTestEmail,
            FakeAuthProvider.SecondTestName);

        var portfolioId = await CreatePortfolioAsync(ownerClient);
        await RegisterStockPurchaseAsync(ownerClient, portfolioId);
        var positionId = await GetOnlyPositionIdAsync(ownerClient, portfolioId);

        var otherPortfolios = await otherClient.GetAsync("/api/v1/portfolios");
        otherPortfolios.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var body = JsonDocument.Parse(await otherPortfolios.Content.ReadAsStringAsync()))
        {
            body.RootElement.GetProperty("data").GetArrayLength().Should().Be(0);
            body.RootElement.GetProperty("pagination").GetProperty("totalCount").GetInt32().Should().Be(0);
        }

        (await otherClient.GetAsync($"/api/v1/portfolios/{portfolioId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/portfolios/{portfolioId}/summary")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/portfolios/{portfolioId}/history?fromDate=2025-01-01&toDate=2025-01-02"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/investments?portfolioId={portfolioId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/investments/summary?portfolioId={portfolioId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/investments/{positionId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/investments/{positionId}/transactions")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var foreignPortfolioUpdate = await otherClient.PatchAsJsonAsync(
            $"/api/v1/portfolios/{portfolioId}",
            new { name = "Stolen portfolio name" });
        foreignPortfolioUpdate.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var foreignValuation = await otherClient.PostAsJsonAsync(
            $"/api/v1/investments/{positionId}/valuations",
            new { totalValue = 1_000_000m, date = DateTime.UtcNow });
        foreignValuation.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var foreignTransaction = await otherClient.PostAsJsonAsync(
            "/api/v1/transactions",
            new
            {
                portfolioId,
                ticker = "PETR4",
                type = "Buy",
                assetClass = "ACAO",
                quantity = 1m,
                unitPrice = 10m,
                fees = 0m,
                transactionDate = DateTime.UtcNow.AddDays(-1),
                idempotencyKey = Guid.NewGuid()
            });
        foreignTransaction.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.DeleteAsync($"/api/v1/investments/{positionId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownerPortfolio = await ownerClient.GetAsync($"/api/v1/portfolios/{portfolioId}");
        ownerPortfolio.StatusCode.Should().Be(HttpStatusCode.OK);
        using var ownerBody = JsonDocument.Parse(await ownerPortfolio.Content.ReadAsStringAsync());
        var ownerData = ownerBody.RootElement.GetProperty("data");
        ownerData.GetProperty("name").GetString().Should().Be("Private to owner");
        var ownerPositions = ownerData.GetProperty("positions");
        ownerPositions.GetArrayLength().Should().Be(1);
        ownerPositions[0].GetProperty("currentValue").GetDecimal().Should().Be(80m);
    }

    private static HttpClient CreateClient(
        CustomWebApplicationFactory factory,
        Guid userId,
        Guid sessionId,
        string email,
        string name)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwtForUser(userId, sessionId, email, name));
        return client;
    }

    private static async Task<Guid> CreatePortfolioAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/portfolios", new { name = "Private to owner" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static async Task RegisterStockPurchaseAsync(HttpClient client, Guid portfolioId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            portfolioId,
            ticker = "PETR4",
            type = "Buy",
            assetClass = "ACAO",
            name = "Petrobras",
            sector = "Energia",
            quantity = 2m,
            unitPrice = 40m,
            fees = 0m,
            transactionDate = DateTime.UtcNow.AddDays(-1),
            idempotencyKey = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task<Guid> GetOnlyPositionIdAsync(HttpClient client, Guid portfolioId)
    {
        var response = await client.GetAsync($"/api/v1/investments?portfolioId={portfolioId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var positions = body.RootElement.GetProperty("data");
        positions.GetArrayLength().Should().Be(1);
        return positions[0].GetProperty("id").GetGuid();
    }
}
