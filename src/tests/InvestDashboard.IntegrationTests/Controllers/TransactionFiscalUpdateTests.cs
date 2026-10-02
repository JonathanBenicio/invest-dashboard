using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class TransactionFiscalUpdateTests
{
    [Theory]
    [InlineData("ACAO", "WEGE3")]
    [InlineData("FII", "HGLG11")]
    public async Task UpdatingBuyToTaxableSale_RequiresAndPersistsFiscalModality(string assetClass, string ticker)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);
        var portfolioId = await CreatePortfolioAsync(client);
        await CreateBuyAsync(client, portfolioId, assetClass, ticker, DateTime.UtcNow.AddDays(-2));
        var transactionId = await CreateBuyAsync(client, portfolioId, assetClass, ticker, DateTime.UtcNow.AddDays(-1));

        using var rejected = await client.PatchAsJsonAsync($"/api/v1/transactions/{transactionId}", UpdateSale());

        rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var unchangedResponse = await client.GetAsync($"/api/v1/transactions/portfolio/{portfolioId}");
        using var unchangedJson = JsonDocument.Parse(await unchangedResponse.Content.ReadAsStringAsync());
        unchangedJson.RootElement.GetProperty("dados").EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == transactionId)
            .GetProperty("tipo").GetString().Should().Be("Buy");

        using var updated = await client.PatchAsJsonAsync($"/api/v1/transactions/{transactionId}", UpdateSale("Comum"));

        var updatedContent = await updated.Content.ReadAsStringAsync();
        updated.StatusCode.Should().Be(HttpStatusCode.OK, "API returned {0}", updatedContent);
        using var updatedJson = JsonDocument.Parse(updatedContent);
        updatedJson.RootElement.GetProperty("dados").GetProperty("tipo").GetString().Should().Be("Sell");
        updatedJson.RootElement.GetProperty("dados").GetProperty("modalidadeFiscal").GetString().Should().Be("Comum");
    }

    private static object UpdateSale(string? modalidadeFiscal = null) => new
    {
        tipo = "Sell",
        quantidade = 1m,
        precoUnitario = 12m,
        taxas = 0.4m,
        modalidadeFiscal,
        dataTransacao = DateTime.UtcNow,
        observacoes = "Venda editada"
    };

    private static async Task<Guid> CreatePortfolioAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/portfolios", new
        {
            nome = "Atualização fiscal",
            instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001")
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateBuyAsync(
        HttpClient client, Guid portfolioId, string assetClass, string ticker, DateTime transactionDate)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId = portfolioId,
            ticker,
            tipo = "Buy",
            classeAtivo = assetClass,
            nome = $"Teste {ticker}",
            setor = "Outros",
            quantidade = 2m,
            precoUnitario = 10m,
            taxas = 0.2m,
            dataTransacao = transactionDate,
            chaveIdempotencia = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static HttpClient CreateClient(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        return client;
    }
}
