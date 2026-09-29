using System.Net;
using System.Text;
using FluentAssertions;
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
