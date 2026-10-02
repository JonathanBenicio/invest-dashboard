using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Xunit;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class PositionFilteringTests
{
    [Fact]
    public async Task CombinedFiltersAndPagination_ReturnExpectedPositions_InMemory()
    {
        using var factory = new CustomWebApplicationFactory();
        await AssertPositionFiltersAsync(factory);
    }

    [RequiresPostgresFact]
    public async Task CombinedFiltersAndPagination_ReturnExpectedPositions_Postgres()
    {
        var connectionString = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connectionString);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        await AssertPositionFiltersAsync(factory);
    }

    private static async Task AssertPositionFiltersAsync(CustomWebApplicationFactory factory)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));

        var portfolioResponse = await client.PostAsJsonAsync("/api/v1/portfolios", new { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),  nome = "Filter positions" });
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var portfolioJson = JsonDocument.Parse(await portfolioResponse.Content.ReadAsStringAsync());
        var portfolioId = portfolioJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        await RegisterTransactionAsync(client, portfolioId, "PETR4", "Petróleo", "Energia", "Buy", 2m, 10m);
        await RegisterTransactionAsync(client, portfolioId, "PETR4", "Petróleo", "Energia", "Sell", 2m, 12m);
        await RegisterTransactionAsync(client, portfolioId, "ABCD3", "Alpha", "Varejo", "Buy", 1m, 10m);
        await RegisterTransactionAsync(client, portfolioId, "WEGE3", "Weg", "Industrial", "Buy", 1m, 20m);

        var filtered = await client.GetAsync(
            $"/api/v1/investments?carteiraId={portfolioId}&tipo=variable_income&subtipo=ACAO&setor=energia&situacao=closed&busca=petr&pagina=1&itensPorPagina=1");

        filtered.StatusCode.Should().Be(HttpStatusCode.OK);
        using var filteredJson = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        var filteredRoot = filteredJson.RootElement;
        var filteredPositions = filteredRoot.GetProperty("dados");
        filteredRoot.GetProperty("paginacao").GetProperty("totalItens").GetInt32().Should().Be(1);
        filteredPositions.GetArrayLength().Should().Be(1);
        filteredPositions[0].GetProperty("ticker").GetString().Should().Be("PETR4");
        filteredPositions[0].GetProperty("situacao").GetString().Should().Be("closed");

        var paginated = await client.GetAsync(
            $"/api/v1/investments?carteiraId={portfolioId}&situacao=open&ordenarPor=valorAtual&ordem=desc&pagina=1&itensPorPagina=1");

        paginated.StatusCode.Should().Be(HttpStatusCode.OK);
        using var paginatedJson = JsonDocument.Parse(await paginated.Content.ReadAsStringAsync());
        var paginatedRoot = paginatedJson.RootElement;
        var page = paginatedRoot.GetProperty("dados");
        paginatedRoot.GetProperty("paginacao").GetProperty("totalItens").GetInt32().Should().Be(2);
        paginatedRoot.GetProperty("paginacao").GetProperty("pagina").GetInt32().Should().Be(1);
        paginatedRoot.GetProperty("paginacao").GetProperty("itensPorPagina").GetInt32().Should().Be(1);
        page.GetArrayLength().Should().Be(1);
        page[0].GetProperty("ticker").GetString().Should().Be("WEGE3");
    }

    private static async Task RegisterTransactionAsync(
        HttpClient client,
        Guid portfolioId,
        string ticker,
        string name,
        string sector,
        string type,
        decimal quantity,
        decimal unitPrice)
    {
        var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId = portfolioId,
            ticker,
            tipo = type,
            modalidadeFiscal = type == "Sell" ? "Comum" : null,
            classeAtivo = "ACAO",
            nome = name,
            setor = sector,
            quantidade = quantity,
            precoUnitario = unitPrice,
            taxas = 0m,
            dataTransacao = DateTime.UtcNow,
            chaveIdempotencia = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
