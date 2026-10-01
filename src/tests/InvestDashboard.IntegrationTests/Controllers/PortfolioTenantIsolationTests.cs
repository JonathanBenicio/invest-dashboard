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
            body.RootElement.GetProperty("dados").GetArrayLength().Should().Be(0);
            body.RootElement.GetProperty("paginacao").GetProperty("totalItens").GetInt32().Should().Be(0);
        }

        (await otherClient.GetAsync($"/api/v1/portfolios/{portfolioId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/portfolios/{portfolioId}/summary")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/portfolios/{portfolioId}/history?dataDe=2025-01-01&dataAte=2025-01-02"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/investments?carteiraId={portfolioId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/investments/summary?carteiraId={portfolioId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/investments/{positionId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.GetAsync($"/api/v1/investments/{positionId}/transactions")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var foreignPortfolioUpdate = await otherClient.PatchAsJsonAsync(
            $"/api/v1/portfolios/{portfolioId}",
            new { nome = "Stolen portfolio name" });
        foreignPortfolioUpdate.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var foreignValuation = await otherClient.PostAsJsonAsync(
            $"/api/v1/investments/{positionId}/valuations",
            new { valorTotal = 1_000_000m, data = DateTime.UtcNow });
        foreignValuation.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var foreignTransaction = await otherClient.PostAsJsonAsync(
            "/api/v1/transactions",
            new
            {
                carteiraId = portfolioId,
                ticker = "PETR4",
                tipo = "Buy",
                classeAtivo = "ACAO",
                quantidade = 1m,
                precoUnitario = 10m,
                taxas = 0m,
                dataTransacao = DateTime.UtcNow.AddDays(-1),
                chaveIdempotencia = Guid.NewGuid()
            });
        foreignTransaction.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.DeleteAsync($"/api/v1/investments/{positionId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownerPortfolio = await ownerClient.GetAsync($"/api/v1/portfolios/{portfolioId}");
        ownerPortfolio.StatusCode.Should().Be(HttpStatusCode.OK);
        using var ownerBody = JsonDocument.Parse(await ownerPortfolio.Content.ReadAsStringAsync());
        var ownerData = ownerBody.RootElement.GetProperty("dados");
        ownerData.GetProperty("nome").GetString().Should().Be("Private to owner");
        var ownerPositions = ownerData.GetProperty("posicoes");
        ownerPositions.GetArrayLength().Should().Be(1);
        ownerPositions[0].GetProperty("valorAtual").GetDecimal().Should().Be(80m);
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
        var response = await client.PostAsJsonAsync("/api/v1/portfolios", new { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),  nome = "Private to owner" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task RegisterStockPurchaseAsync(HttpClient client, Guid portfolioId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId = portfolioId,
            ticker = "PETR4",
            tipo = "Buy",
            classeAtivo = "ACAO",
            nome = "Petrobras",
            setor = "Energia",
            quantidade = 2m,
            precoUnitario = 40m,
            taxas = 0m,
            dataTransacao = DateTime.UtcNow.AddDays(-1),
            chaveIdempotencia = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task<Guid> GetOnlyPositionIdAsync(HttpClient client, Guid portfolioId)
    {
        var response = await client.GetAsync($"/api/v1/investments?carteiraId={portfolioId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var positions = body.RootElement.GetProperty("dados");
        positions.GetArrayLength().Should().Be(1);
        return positions[0].GetProperty("id").GetGuid();
    }
}
