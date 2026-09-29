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

        var portfolioResponse = await client.PostAsJsonAsync("/api/v1/portfolios", new { name = "Position flow" });
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var portfolioJson = JsonDocument.Parse(await portfolioResponse.Content.ReadAsStringAsync());
        var portfolioId = portfolioJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var idempotencyKey = Guid.NewGuid();
        var buy = new
        {
            portfolioId,
            ticker = "WEGE3",
            type = "Buy",
            assetClass = "ACAO",
            name = "WEG",
            sector = "Industrial",
            quantity = 10m,
            unitPrice = 10m,
            fees = 1m,
            transactionDate = DateTime.UtcNow.AddDays(-1),
            idempotencyKey
        };

        var firstBuy = await client.PostAsJsonAsync("/api/v1/transactions", buy);
        var firstBuyContent = await firstBuy.Content.ReadAsStringAsync();
        firstBuy.StatusCode.Should().Be(HttpStatusCode.Created, "API returned {0}", firstBuyContent);
        using var firstBuyJson = JsonDocument.Parse(firstBuyContent);
        var transactionId = firstBuyJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var replay = await client.PostAsJsonAsync("/api/v1/transactions", buy);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        using var replayJson = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
        replayJson.RootElement.GetProperty("data").GetProperty("id").GetGuid().Should().Be(transactionId);

        var sell = new
        {
            portfolioId,
            ticker = "WEGE3",
            type = "Sell",
            assetClass = "ACAO",
            quantity = 4m,
            unitPrice = 12m,
            fees = 0.4m,
            transactionDate = DateTime.UtcNow,
            idempotencyKey = Guid.NewGuid()
        };
        var sellResponse = await client.PostAsJsonAsync("/api/v1/transactions", sell);
        sellResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var positionsResponse = await client.GetAsync($"/api/v1/investments?portfolioId={portfolioId}");
        positionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var positionsJson = JsonDocument.Parse(await positionsResponse.Content.ReadAsStringAsync());
        var positions = positionsJson.RootElement.GetProperty("data");
        positions.GetArrayLength().Should().Be(1);
        positions[0].GetProperty("quantity").GetDecimal().Should().Be(6m);
        positions[0].GetProperty("currentValue").GetDecimal().Should().Be(60m);

        var oversell = new
        {
            portfolioId,
            ticker = "WEGE3",
            type = "Sell",
            assetClass = "ACAO",
            quantity = 7m,
            unitPrice = 12m,
            fees = 0m,
            transactionDate = DateTime.UtcNow.AddMinutes(1),
            idempotencyKey = Guid.NewGuid()
        };
        var oversellResponse = await client.PostAsJsonAsync("/api/v1/transactions", oversell);
        oversellResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var historyRangeStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var historyRangeEnd = DateOnly.FromDateTime(DateTime.UtcNow);
        var portfolioHistoryResponse = await client.GetAsync(
            $"/api/v1/portfolios/{portfolioId}/history?fromDate={historyRangeStart:yyyy-MM-dd}&toDate={historyRangeEnd:yyyy-MM-dd}");
        portfolioHistoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var portfolioHistoryJson = JsonDocument.Parse(await portfolioHistoryResponse.Content.ReadAsStringAsync());
        var portfolioHistory = portfolioHistoryJson.RootElement.GetProperty("data");
        portfolioHistory.GetArrayLength().Should().BeGreaterThan(0);
        portfolioHistory[0].GetProperty("isComplete").GetBoolean().Should().BeFalse();
        portfolioHistory[0].GetProperty("totalValue").ValueKind.Should().Be(JsonValueKind.Null);
        portfolioHistory[0].GetProperty("missingTickers")[0].GetString().Should().Be("WEGE3");

        var historyResponse = await client.GetAsync($"/api/v1/investments/{positions[0].GetProperty("id").GetGuid()}/transactions");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var historyJson = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
        historyJson.RootElement.GetProperty("data").GetArrayLength().Should().Be(2);

        return portfolioId;
    }
}
