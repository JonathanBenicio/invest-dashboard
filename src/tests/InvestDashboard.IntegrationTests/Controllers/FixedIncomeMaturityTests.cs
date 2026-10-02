using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Infrastructure.Services;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class FixedIncomeMaturityTests
{
    [Fact]
    public async Task MaturityMark_PreservesValueAndLedger_InMemory()
    {
        using var factory = new CustomWebApplicationFactory();
        await AssertMaturityFlowAsync(factory);
    }

    [RequiresPostgresFact]
    public async Task MaturityMark_PersistsAcrossApiHosts_Postgres()
    {
        var connectionString = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connectionString);
        Guid positionId;

        using (var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString))
            positionId = await AssertMaturityFlowAsync(factory);

        using var restartedFactory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = CreateAuthenticatedClient(restartedFactory);
        using var response = await client.GetAsync($"/api/v1/investments/{positionId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("dados").GetProperty("situacao").GetString().Should().Be("matured");
    }

    private static async Task<Guid> AssertMaturityFlowAsync(CustomWebApplicationFactory factory)
    {
        using var client = CreateAuthenticatedClient(factory);
        using var portfolioResponse = await client.PostAsJsonAsync("/api/v1/portfolios", new { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),  nome = "Vencimento sem caixa" });
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var portfolioBody = JsonDocument.Parse(await portfolioResponse.Content.ReadAsStringAsync());
        var portfolioId = portfolioBody.RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        using var purchaseResponse = await client.PostAsJsonAsync("/api/v1/investments/fixed-income", new
        {
            carteiraId = portfolioId,
            nome = "CDB vencimento",
            subtipo = "CDB",
            emissor = "Banco de teste",
            valorPrincipal = 5000m,
            valorExtrato = 5075m,
            taxaJuros = 110m,
            indexador = "CDI",
            dataCompra = DateTime.UtcNow.AddDays(-30),
            dataVencimento = DateTime.UtcNow.AddDays(1),
            chaveIdempotencia = Guid.NewGuid()
        });
        purchaseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var purchaseBody = JsonDocument.Parse(await purchaseResponse.Content.ReadAsStringAsync());
        var positionId = purchaseBody.RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var updater = scope.ServiceProvider.GetRequiredService<AtualizadorVencimentosService>();
            (await updater.MarkDueAsync(DateTime.UtcNow.AddDays(2))).Should().Be(1);
            (await updater.MarkDueAsync(DateTime.UtcNow.AddDays(2))).Should().Be(0);
        }

        using var positionResponse = await client.GetAsync($"/api/v1/investments/{positionId}");
        positionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var positionBody = JsonDocument.Parse(await positionResponse.Content.ReadAsStringAsync());
        var position = positionBody.RootElement.GetProperty("dados");
        position.GetProperty("situacao").GetString().Should().Be("matured");
        position.GetProperty("valorAtual").GetDecimal().Should().Be(5075m);
        position.GetProperty("quantidade").GetDecimal().Should().Be(5000m);

        using var summaryResponse = await client.GetAsync("/api/v1/investments/summary");
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var summaryBody = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        summaryBody.RootElement.GetProperty("dados").GetProperty("totalRendaFixa").GetDecimal().Should().Be(5075m);

        using var transactionsResponse = await client.GetAsync($"/api/v1/transactions/portfolio/{portfolioId}");
        transactionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var transactionsBody = JsonDocument.Parse(await transactionsResponse.Content.ReadAsStringAsync());
        transactionsBody.RootElement.GetProperty("dados").GetArrayLength().Should().Be(1);
        return positionId;
    }

    private static HttpClient CreateAuthenticatedClient(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        return client;
    }
}
