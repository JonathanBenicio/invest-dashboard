using System.Net;
using System.Text;
using FluentAssertions;
using InvestDashboard.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class BcbCdiBenchmarkProviderTests
{
    [Fact]
    public async Task GetCdiAsync_ParsesDailyRatesAndCompoundsBase100()
    {
        var handler = new CallbackHandler(request =>
        {
            request.RequestUri!.Query.Should().Contain("dataInicial=01%2F09%2F2026");
            request.RequestUri.Query.Should().Contain("dataFinal=02%2F09%2F2026");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "[{\"data\":\"01/09/2026\",\"valor\":\"0.05\"},{\"data\":\"02/09/2026\",\"valor\":\"0.10\"}]",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbCdiBenchmarkProvider(client, cache, TimeProvider.System);

        var result = await provider.GetCdiAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2));

        result.Pontos.Should().HaveCount(2);
        result.Pontos[0].TaxaDiariaPercentual.Should().Be(0.05m);
        result.Pontos[1].IndiceBase100.Should().Be(100.15005m);
        result.Origem.Should().Contain("SGS série 12");
    }

    [Fact]
    public async Task GetCdiAsync_MapsTransportFailuresToUnavailable()
    {
        using var client = new HttpClient(new CallbackHandler(_ => throw new HttpRequestException("Network unavailable.")));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbCdiBenchmarkProvider(client, cache, TimeProvider.System);

        var action = () => provider.GetCdiAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2));

        await action.Should().ThrowAsync<InvestDashboard.Application.Exceptions.MarketDataUnavailableException>();
    }

    [Fact]
    public async Task GetCdiAsync_MapsTimeoutsToUnavailable()
    {
        using var client = new HttpClient(new CallbackHandler(_ => throw new TaskCanceledException("Request timed out.")));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbCdiBenchmarkProvider(client, cache, TimeProvider.System);

        var action = () => provider.GetCdiAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2));

        await action.Should().ThrowAsync<InvestDashboard.Application.Exceptions.MarketDataUnavailableException>();
    }

    [Fact]
    public async Task GetCdiAsync_MapsInvalidJsonToUnavailable()
    {
        using var client = new HttpClient(new CallbackHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json", Encoding.UTF8, "application/json")
        }));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbCdiBenchmarkProvider(client, cache, TimeProvider.System);

        var action = () => provider.GetCdiAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2));

        await action.Should().ThrowAsync<InvestDashboard.Application.Exceptions.MarketDataUnavailableException>();
    }

    private sealed class CallbackHandler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(callback(request));
    }
}
