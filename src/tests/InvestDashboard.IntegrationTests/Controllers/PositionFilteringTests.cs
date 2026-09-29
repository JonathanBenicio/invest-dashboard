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

        var portfolioResponse = await client.PostAsJsonAsync("/api/v1/portfolios", new { name = "Filter positions" });
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var portfolioJson = JsonDocument.Parse(await portfolioResponse.Content.ReadAsStringAsync());
        var portfolioId = portfolioJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        await RegisterTransactionAsync(client, portfolioId, "PETR4", "Petróleo", "Energia", "Buy", 2m, 10m);
        await RegisterTransactionAsync(client, portfolioId, "PETR4", "Petróleo", "Energia", "Sell", 2m, 12m);
        await RegisterTransactionAsync(client, portfolioId, "ABCD3", "Alpha", "Varejo", "Buy", 1m, 10m);
        await RegisterTransactionAsync(client, portfolioId, "WEGE3", "Weg", "Industrial", "Buy", 1m, 20m);

        var filtered = await client.GetAsync(
            $"/api/v1/investments?portfolioId={portfolioId}&type=variable_income&subtype=ACAO&sector=energia&status=closed&search=petr&page=1&pageSize=1");

        filtered.StatusCode.Should().Be(HttpStatusCode.OK);
        using var filteredJson = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
        var filteredRoot = filteredJson.RootElement;
        var filteredPositions = filteredRoot.GetProperty("data");
        filteredRoot.GetProperty("pagination").GetProperty("totalCount").GetInt32().Should().Be(1);
        filteredPositions.GetArrayLength().Should().Be(1);
        filteredPositions[0].GetProperty("ticker").GetString().Should().Be("PETR4");
        filteredPositions[0].GetProperty("status").GetString().Should().Be("closed");

        var paginated = await client.GetAsync(
            $"/api/v1/investments?portfolioId={portfolioId}&status=open&sortBy=currentValue&sortOrder=desc&page=1&pageSize=1");

        paginated.StatusCode.Should().Be(HttpStatusCode.OK);
        using var paginatedJson = JsonDocument.Parse(await paginated.Content.ReadAsStringAsync());
        var paginatedRoot = paginatedJson.RootElement;
        var page = paginatedRoot.GetProperty("data");
        paginatedRoot.GetProperty("pagination").GetProperty("totalCount").GetInt32().Should().Be(2);
        paginatedRoot.GetProperty("pagination").GetProperty("page").GetInt32().Should().Be(1);
        paginatedRoot.GetProperty("pagination").GetProperty("pageSize").GetInt32().Should().Be(1);
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
            portfolioId,
            ticker,
            type,
            assetClass = "ACAO",
            name,
            sector,
            quantity,
            unitPrice,
            fees = 0m,
            transactionDate = DateTime.UtcNow,
            idempotencyKey = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
