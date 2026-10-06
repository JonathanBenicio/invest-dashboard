using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class BrapiMarketDataClientTests
{
    [Fact]
    public async Task GetQuotesAsync_UsesMarketTimestampInsteadOfRequestTimestamp()
    {
        using var httpClient = CreateHttpClient(request =>
        {
            var content = request.RequestUri!.AbsolutePath.EndsWith("/crypto", StringComparison.Ordinal)
                ? """
                  {"coins":[{"coin":"BTC","coinName":"Bitcoin","currency":"BRL","regularMarketPrice":500000,"regularMarketTime":"2025-06-09T12:25:00.000Z"}],"requestedAt":"2025-06-09T12:30:00.000Z"}
                  """
                : """
                  {"results":[{"symbol":"PETR4","data":{"shortName":"Petrobras PN","currency":"BRL","regularMarketPrice":40.5,"regularMarketTime":"2025-06-09T12:20:00.000Z"}}],"requestedAt":"2025-06-09T12:30:00.000Z"}
                  """;

            return JsonResponse(content);
        });
        var client = CreateMarketDataClient(httpClient);

        var quotes = await client.GetQuotesAsync(["PETR4", "BTC"]);

        quotes.Should().HaveCount(2);
        var stockQuote = quotes.Single(quote => quote.Symbol == "PETR4");
        stockQuote.ObservedAtUtc.Should().Be(DateTime.Parse("2025-06-09T12:20:00.000Z").ToUniversalTime());
        stockQuote.Source.Should().Be("brapi");

        var cryptoQuote = quotes.Single(quote => quote.Symbol == "BTC");
        cryptoQuote.ObservedAtUtc.Should().Be(DateTime.Parse("2025-06-09T12:25:00.000Z").ToUniversalTime());
        cryptoQuote.Subtype.Should().Be("CRYPTO");
    }

    [Fact]
    public async Task GetQuotesAsync_OmitsPricesWithoutMarketTimestamp()
    {
        using var httpClient = CreateHttpClient(request =>
        {
            var content = request.RequestUri!.AbsolutePath.EndsWith("/crypto", StringComparison.Ordinal)
                ? """
                  {"coins":[{"coin":"BTC","coinName":"Bitcoin","currency":"BRL","regularMarketPrice":500000}],"requestedAt":"2025-06-09T12:30:00.000Z"}
                  """
                : """
                  {"results":[{"symbol":"PETR4","data":{"shortName":"Petrobras PN","currency":"BRL","regularMarketPrice":40.5}}],"requestedAt":"2025-06-09T12:30:00.000Z"}
                  """;

            return JsonResponse(content);
        });
        var client = CreateMarketDataClient(httpClient);

        var quotes = await client.GetQuotesAsync(["PETR4", "BTC"]);

        quotes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetQuotesAsync_DoesNotInventAQuoteWhenTheSymbolIsMissing()
    {
        using var httpClient = CreateHttpClient(_ => JsonResponse("{\"results\":[]}"));
        var client = CreateMarketDataClient(httpClient);

        var quotes = await client.GetQuotesAsync(["PETR4"]);

        quotes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDailyHistoryAsync_UsesAvailableAdjustedCloseWithoutRequiringProRawPrices()
    {
        var firstDate = DateTimeOffset.Parse("2025-06-09T00:00:00Z").ToUnixTimeSeconds();
        var secondDate = DateTimeOffset.Parse("2025-06-10T00:00:00Z").ToUnixTimeSeconds();
        string? requestedQuery = null;
        using var httpClient = CreateHttpClient(request =>
        {
            requestedQuery = request.RequestUri!.Query;
            var payload = new
            {
                results = new[]
                {
                    new
                    {
                        symbol = "PETR4",
                        data = new
                        {
                            historicalDataPrice = new object[]
                            {
                                new { date = firstDate, close = 40.5m, adjustedClose = 40.25m },
                                new { date = secondDate, close = 41.0m, adjustedClose = (decimal?)null }
                            }
                        }
                    }
                }
            };
            return JsonResponse(JsonSerializer.Serialize(payload));
        });
        var client = CreateMarketDataClient(httpClient);

        var history = await client.GetDailyHistoryAsync(
            ["PETR4"],
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 10));

        history.Should().HaveCount(2);
        history[0].Price.Should().Be(40.25m);
        history[1].Price.Should().Be(41.0m);
        history[0].IsAdjusted.Should().BeTrue();
        history[1].IsAdjusted.Should().BeFalse();
        history[0].DateUtc.Should().Be(DateTimeOffset.FromUnixTimeSeconds(firstDate).UtcDateTime);
        requestedQuery.Should().Contain("startDate=2025-06-01");
        requestedQuery.Should().NotContain("includeRaw=true");
    }

    [Fact]
    public async Task GetDailyHistoryAsync_ReturnsEmptyWhenTheProviderHasNoHistory()
    {
        using var httpClient = CreateHttpClient(_ => JsonResponse("""
            {"results":[{"symbol":"PETR4","data":{"historicalDataPrice":[]}}]}
            """));
        var client = CreateMarketDataClient(httpClient);

        var history = await client.GetDailyHistoryAsync(
            ["PETR4"],
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 10));

        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDailyHistoryAsync_UsesCryptoHistoryAndAppliesTheRequestedDateRange()
    {
        var firstDate = DateTimeOffset.Parse("2025-06-09T00:00:00Z").ToUnixTimeSeconds();
        var secondDate = DateTimeOffset.Parse("2025-06-10T00:00:00Z").ToUnixTimeSeconds();
        var outsideDate = DateTimeOffset.Parse("2025-06-11T00:00:00Z").ToUnixTimeSeconds();
        string? requestedPathAndQuery = null;
        using var httpClient = CreateHttpClient(request =>
        {
            requestedPathAndQuery = request.RequestUri!.PathAndQuery;
            return JsonResponse(JsonSerializer.Serialize(new
            {
                coins = new[]
                {
                    new
                    {
                        coin = "BTC",
                        historicalDataPrice = new[]
                        {
                            new { date = firstDate, close = 100m },
                            new { date = secondDate, close = 110m },
                            new { date = outsideDate, close = 120m }
                        }
                    }
                }
            }));
        });
        var client = CreateMarketDataClient(httpClient);

        var history = await client.GetDailyHistoryAsync(
            ["BTC"],
            new DateOnly(2025, 6, 9),
            new DateOnly(2025, 6, 10));

        history.Should().HaveCount(2);
        history.Select(point => point.Price).Should().Equal(100m, 110m);
        requestedPathAndQuery.Should().Contain("/v2/crypto?");
        requestedPathAndQuery.Should().Contain("coin=BTC");
        requestedPathAndQuery.Should().Contain("interval=1d");
    }

    [Fact]
    public async Task GetQuotesAsync_BatchesMoreThanOneHundredSymbolsWithoutDroppingTheRemainder()
    {
        var batchSizes = new List<int>();
        using var httpClient = CreateHttpClient(request =>
        {
            var query = Uri.UnescapeDataString(request.RequestUri!.Query);
            var symbols = query[(query.IndexOf("symbols=", StringComparison.Ordinal) + "symbols=".Length)..]
                .Split('&')[0].Split(',');
            batchSizes.Add(symbols.Length);
            var results = symbols.Select(symbol => new
            {
                symbol,
                data = new { regularMarketPrice = 1m, regularMarketTime = "2025-06-09T12:00:00Z" }
            });
            return JsonResponse(JsonSerializer.Serialize(new { results }));
        });
        var client = CreateMarketDataClient(httpClient);
        var symbols = Enumerable.Range(0, 101).Select(index => $"TEST{index}3").ToArray();

        var quotes = await client.GetQuotesAsync(symbols);

        quotes.Should().HaveCount(101);
        batchSizes.Should().Equal(100, 1);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task GetQuotesAsync_MapsProviderFailuresToUnavailable(HttpStatusCode statusCode)
    {
        using var httpClient = CreateHttpClient(_ => new HttpResponseMessage(statusCode));
        var client = CreateMarketDataClient(httpClient);

        var action = () => client.GetQuotesAsync(["PETR4"]);

        await action.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task GetDailyHistoryAsync_MapsProviderFailuresToUnavailable(HttpStatusCode statusCode)
    {
        using var httpClient = CreateHttpClient(_ => new HttpResponseMessage(statusCode));
        var client = CreateMarketDataClient(httpClient);

        var action = () => client.GetDailyHistoryAsync(
            ["PETR4"],
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 10));

        await action.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Fact]
    public async Task SearchAsync_MapsBrapiAssetResults()
    {
        string? requestedPathAndQuery = null;
        using var httpClient = CreateHttpClient(request =>
        {
            requestedPathAndQuery = request.RequestUri!.PathAndQuery;
            return JsonResponse("""
                {"stocks":[{"stock":"HGLG11","name":"CSHG Logística FII","sector":"Imóveis Industriais","type":"fund","subType":"fii"}]}
                """);
        });
        var client = CreateMarketDataClient(httpClient);

        var results = await client.SearchAsync("HGLG");

        results.Should().ContainSingle();
        results[0].Symbol.Should().Be("HGLG11");
        results[0].Name.Should().Be("CSHG Logística FII");
        results[0].Subtype.Should().Be("FII");
        requestedPathAndQuery.Should().Contain("/quote/list?search=HGLG");
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyWhenNoTickerMatches()
    {
        using var httpClient = CreateHttpClient(_ => JsonResponse("{\"stocks\":[]}"));
        var client = CreateMarketDataClient(httpClient);

        var results = await client.SearchAsync("NOTREAL");

        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task SearchAsync_MapsProviderFailureStatusesToUnavailable(HttpStatusCode statusCode)
    {
        using var httpClient = CreateHttpClient(_ => new HttpResponseMessage(statusCode));
        var client = CreateMarketDataClient(httpClient);

        var action = () => client.SearchAsync("PETR");

        await action.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Fact]
    public async Task SearchAsync_MapsTransportFailureToUnavailable()
    {
        using var httpClient = CreateHttpClient(_ => throw new HttpRequestException("Network unavailable."));
        var client = CreateMarketDataClient(httpClient);

        var action = () => client.SearchAsync("PETR");

        await action.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Fact]
    public async Task SearchAsync_MapsTimeoutToUnavailable()
    {
        using var httpClient = CreateHttpClient(_ => throw new TaskCanceledException("Request timed out."));
        var client = CreateMarketDataClient(httpClient);

        var action = () => client.SearchAsync("PETR");

        await action.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Fact]
    public async Task SearchAsync_MapsInvalidJsonToUnavailable()
    {
        using var httpClient = CreateHttpClient(_ => JsonResponse("not-json"));
        var client = CreateMarketDataClient(httpClient);

        var action = () => client.SearchAsync("PETR");

        await action.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) =>
        new(new StubHttpMessageHandler(responseFactory));

    private static BrapiMarketDataClient CreateMarketDataClient(HttpClient httpClient)
    {
        var configuration = new ConfigurationManager
        {
            ["MarketData:BaseUrl"] = "https://brapi.test/api/"
        };
        return new BrapiMarketDataClient(httpClient, configuration, NullLogger<BrapiMarketDataClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responseFactory(request));
    }
}
