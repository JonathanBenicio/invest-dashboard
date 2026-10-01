using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class FixedIncomeStatementValuationTests
{
    [Fact]
    public async Task StatementValuation_UpdatesPositionAndHistory_InMemory()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        var ids = await RegisterAndValuePositionAsync(client);
        await AssertValuedPositionAndHistoryAsync(client, ids.PortfolioId, ids.PositionId);
    }

    [RequiresPostgresFact]
    public async Task StatementValuation_PersistsPositionAndHistoryAcrossApiHosts_Postgres()
    {
        var connectionString = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connectionString);
        (Guid PortfolioId, Guid PositionId) ids;

        using (var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString))
        using (var client = CreateAuthenticatedClient(factory))
            ids = await RegisterAndValuePositionAsync(client);

        using var restartedFactory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var restartedClient = CreateAuthenticatedClient(restartedFactory);
        await AssertValuedPositionAndHistoryAsync(restartedClient, ids.PortfolioId, ids.PositionId);
    }

    private static HttpClient CreateAuthenticatedClient(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        return client;
    }

    private static async Task<(Guid PortfolioId, Guid PositionId)> RegisterAndValuePositionAsync(HttpClient client)
    {
        var portfolioResponse = await client.PostAsJsonAsync(
            "/api/v1/portfolios", new { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),  nome = "Fixed income statement" });
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var portfolioBody = JsonDocument.Parse(await portfolioResponse.Content.ReadAsStringAsync());
        var portfolioId = portfolioBody.RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        var purchaseDate = DateTime.UtcNow.AddMonths(-1);
        var purchaseResponse = await client.PostAsJsonAsync("/api/v1/investments/fixed-income", new
        {
            carteiraId = portfolioId,
            nome = "CDB extrato",
            subtipo = "CDB",
            emissor = "Banco de teste",
            valorPrincipal = 5000m,
            valorExtrato = 5075m,
            taxaJuros = 110m,
            indexador = "CDI",
            dataCompra = purchaseDate,
            dataVencimento = DateTime.UtcNow.AddYears(1),
            chaveIdempotencia = Guid.NewGuid()
        });
        purchaseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var purchaseBody = JsonDocument.Parse(await purchaseResponse.Content.ReadAsStringAsync());
        var positionId = purchaseBody.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        purchaseBody.RootElement.GetProperty("dados").GetProperty("valorAtual").GetDecimal().Should().Be(5075m);

        var valuationDate = DateTime.UtcNow;
        var valuationResponse = await client.PostAsJsonAsync(
            $"/api/v1/investments/{positionId}/valuations",
            new { valorTotal = 5200m, data = valuationDate });
        valuationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var valuationBody = JsonDocument.Parse(await valuationResponse.Content.ReadAsStringAsync());
        valuationBody.RootElement.GetProperty("dados").GetProperty("valorAtual").GetDecimal().Should().Be(5200m);

        return (portfolioId, positionId);
    }

    private static async Task AssertValuedPositionAndHistoryAsync(HttpClient client, Guid portfolioId, Guid positionId)
    {
        var portfolioResponse = await client.GetAsync($"/api/v1/portfolios/{portfolioId}");
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var portfolioBody = JsonDocument.Parse(await portfolioResponse.Content.ReadAsStringAsync());
        var positions = portfolioBody.RootElement.GetProperty("dados").GetProperty("posicoes");
        positions.GetArrayLength().Should().Be(1);
        positions[0].GetProperty("id").GetGuid().Should().Be(positionId);
        positions[0].GetProperty("valorAtual").GetDecimal().Should().Be(5200m);

        var historyResponse = await client.GetAsync($"/api/v1/investments/{positionId}/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var historyBody = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
        var history = historyBody.RootElement.GetProperty("dados");
        history.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
        history.EnumerateArray().Should().Contain(item =>
            item.GetProperty("origem").GetString() == "statement" &&
            item.GetProperty("preco").GetDecimal() == 1.04m);

        var transactionsResponse = await client.GetAsync($"/api/v1/transactions/portfolio/{portfolioId}");
        transactionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var transactionsBody = JsonDocument.Parse(await transactionsResponse.Content.ReadAsStringAsync());
        transactionsBody.RootElement.GetProperty("dados").GetArrayLength().Should().Be(1);
    }
}
