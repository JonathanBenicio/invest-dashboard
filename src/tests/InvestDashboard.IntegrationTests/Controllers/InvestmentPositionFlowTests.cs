using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Xunit;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class InvestmentPositionFlowTests
{
    [Fact]
    public async Task BuyReplayAndSell_KeepOnePositionAndRejectOversell()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        await ExecutePositionFlowAsync(client);
    }

    internal static async Task<Guid> ExecutePositionFlowAsync(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));

        var portfolioResponse = await client.PostAsJsonAsync("/api/v1/portfolios", new { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),  nome = "Position flow" });
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var portfolioJson = JsonDocument.Parse(await portfolioResponse.Content.ReadAsStringAsync());
        var portfolioId = portfolioJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        var idempotencyKey = Guid.NewGuid();
        var buy = new
        {
            carteiraId = portfolioId,
            ticker = "WEGE3",
            tipo = "Buy",
            classeAtivo = "ACAO",
            nome = "WEG",
            setor = "Industrial",
            quantidade = 10m,
            precoUnitario = 10m,
            taxas = 1m,
            dataTransacao = DateTime.UtcNow.AddDays(-1),
            chaveIdempotencia = idempotencyKey
        };

        var firstBuy = await client.PostAsJsonAsync("/api/v1/transactions", buy);
        var firstBuyContent = await firstBuy.Content.ReadAsStringAsync();
        firstBuy.StatusCode.Should().Be(HttpStatusCode.Created, "API returned {0}", firstBuyContent);
        using var firstBuyJson = JsonDocument.Parse(firstBuyContent);
        var transactionId = firstBuyJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        var replay = await client.PostAsJsonAsync("/api/v1/transactions", buy);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        using var replayJson = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
        replayJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid().Should().Be(transactionId);

        var sell = new
        {
            carteiraId = portfolioId,
            ticker = "WEGE3",
            tipo = "Sell",
            modalidadeFiscal = "Comum",
            classeAtivo = "ACAO",
            quantidade = 4m,
            precoUnitario = 12m,
            taxas = 0.4m,
            dataTransacao = DateTime.UtcNow,
            chaveIdempotencia = Guid.NewGuid()
        };
        var sellResponse = await client.PostAsJsonAsync("/api/v1/transactions", sell);
        sellResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var positionsResponse = await client.GetAsync($"/api/v1/investments?carteiraId={portfolioId}");
        positionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var positionsJson = JsonDocument.Parse(await positionsResponse.Content.ReadAsStringAsync());
        var positions = positionsJson.RootElement.GetProperty("dados");
        positions.GetArrayLength().Should().Be(1);
        positions[0].GetProperty("quantidade").GetDecimal().Should().Be(6m);
        positions[0].GetProperty("valorAtual").GetDecimal().Should().Be(60m);

        var oversell = new
        {
            carteiraId = portfolioId,
            ticker = "WEGE3",
            tipo = "Sell",
            modalidadeFiscal = "Comum",
            classeAtivo = "ACAO",
            quantidade = 7m,
            precoUnitario = 12m,
            taxas = 0m,
            dataTransacao = DateTime.UtcNow.AddMinutes(1),
            chaveIdempotencia = Guid.NewGuid()
        };
        var oversellResponse = await client.PostAsJsonAsync("/api/v1/transactions", oversell);
        oversellResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var historyRangeStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var historyRangeEnd = DateOnly.FromDateTime(DateTime.UtcNow);
        var portfolioHistoryResponse = await client.GetAsync(
            $"/api/v1/portfolios/{portfolioId}/history?dataDe={historyRangeStart:yyyy-MM-dd}&dataAte={historyRangeEnd:yyyy-MM-dd}");
        portfolioHistoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var portfolioHistoryJson = JsonDocument.Parse(await portfolioHistoryResponse.Content.ReadAsStringAsync());
        var portfolioHistory = portfolioHistoryJson.RootElement.GetProperty("dados");
        portfolioHistory.GetArrayLength().Should().BeGreaterThan(0);
        portfolioHistory[0].GetProperty("estaCompleto").GetBoolean().Should().BeFalse();
        portfolioHistory[0].GetProperty("valorTotal").ValueKind.Should().Be(JsonValueKind.Null);
        portfolioHistory[0].GetProperty("tickersAusentes")[0].GetString().Should().Be("WEGE3");

        var historyResponse = await client.GetAsync($"/api/v1/investments/{positions[0].GetProperty("id").GetGuid()}/transactions");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var historyJson = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
        historyJson.RootElement.GetProperty("dados").GetArrayLength().Should().Be(2);

        return portfolioId;
    }
}
