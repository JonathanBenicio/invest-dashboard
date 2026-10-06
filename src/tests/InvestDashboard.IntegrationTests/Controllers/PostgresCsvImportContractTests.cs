using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class PostgresCsvImportContractTests
{
    [RequiresPostgresFact]
    public async Task ImportedRows_PersistStockAndFixedIncomeAndRejectInvalidRow()
    {
        var connectionString = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connectionString);
        Guid portfolioId;
        Guid stockTransactionId;

        using (var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString))
        using (var client = CreateAuthenticatedClient(factory))
        {
            portfolioId = await CreatePortfolioAsync(client);
            var stock = CreateStockRow(portfolioId);
            stockTransactionId = await PostTransactionAsync(client, stock, HttpStatusCode.Created);

            var replayedStockId = await PostTransactionAsync(client, stock, HttpStatusCode.Created);
            replayedStockId.Should().Be(stockTransactionId);

            await PostTransactionAsync(client, CreateFixedIncomeRow(portfolioId), HttpStatusCode.Created);
            await PostTransactionAsync(client, CreateInvalidRow(portfolioId), HttpStatusCode.BadRequest);
        }

        using var restartedFactory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var restartedClient = CreateAuthenticatedClient(restartedFactory);
        var transactionsResponse = await restartedClient.GetAsync($"/api/v1/transactions/portfolio/{portfolioId}");
        transactionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var transactionsJson = JsonDocument.Parse(await transactionsResponse.Content.ReadAsStringAsync());
        var transactions = transactionsJson.RootElement.GetProperty("dados");
        transactions.GetArrayLength().Should().Be(2);
        transactions.EnumerateArray().Select(item => item.GetProperty("ticker").GetString())
            .Should().BeEquivalentTo("PETR4", "RFABC1");
        transactions.EnumerateArray().Count(item => item.GetProperty("id").GetGuid() == stockTransactionId)
            .Should().Be(1);

        var positionsResponse = await restartedClient.GetAsync($"/api/v1/investments?carteiraId={portfolioId}");
        positionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var positionsJson = JsonDocument.Parse(await positionsResponse.Content.ReadAsStringAsync());
        var positions = positionsJson.RootElement.GetProperty("dados");
        positions.GetArrayLength().Should().Be(2);
        positions.EnumerateArray().Single(item => item.GetProperty("ticker").GetString() == "PETR4")
            .GetProperty("quantidade").GetDecimal().Should().Be(10m);
        positions.EnumerateArray().Single(item => item.GetProperty("ticker").GetString() == "RFABC1")
            .GetProperty("valorAtual").GetDecimal().Should().Be(1010m);
    }

    private static HttpClient CreateAuthenticatedClient(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        return client;
    }

    private static async Task<Guid> CreatePortfolioAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/portfolios", new { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),  nome = "CSV import contract" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task<Guid> PostTransactionAsync(
        HttpClient client,
        object transaction,
        HttpStatusCode expectedStatus)
    {
        var response = await client.PostAsJsonAsync("/api/v1/transactions", transaction);
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expectedStatus, "API returned {0}", content);
        if (expectedStatus != HttpStatusCode.Created)
            return Guid.Empty;

        using var json = JsonDocument.Parse(content);
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static object CreateStockRow(Guid portfolioId) => new
    {
        carteiraId = portfolioId,
        ticker = "PETR4",
        tipo = "Buy",
        classeAtivo = "ACAO",
        nome = "Petrobras",
        setor = "Energia",
        quantidade = 10m,
        precoUnitario = 35.50m,
        taxas = 1m,
        dataTransacao = DateTime.UtcNow.AddDays(-1),
        chaveIdempotencia = Guid.NewGuid()
    };

    private static object CreateFixedIncomeRow(Guid portfolioId) => new
    {
        carteiraId = portfolioId,
        ticker = "RFABC1",
        tipo = "Buy",
        classeAtivo = "RENDA_FIXA",
        nome = "CDB",
        emissor = "Banco de teste",
        subtipo = "CDB",
        indexador = "CDI",
        taxaJuros = 110m,
        dataVencimento = DateTime.UtcNow.AddYears(2),
        valorInicialExtrato = 1010m,
        quantidade = 1000m,
        precoUnitario = 1m,
        taxas = 0m,
        dataTransacao = DateTime.UtcNow.AddDays(-1),
        chaveIdempotencia = Guid.NewGuid()
    };

    private static object CreateInvalidRow(Guid portfolioId) => new
    {
        carteiraId = portfolioId,
        ticker = "BAD1",
        tipo = "Buy",
        classeAtivo = "ACAO",
        quantidade = 0m,
        precoUnitario = 10m,
        taxas = 0m,
        dataTransacao = DateTime.UtcNow.AddDays(-1),
        chaveIdempotencia = Guid.NewGuid()
    };
}
