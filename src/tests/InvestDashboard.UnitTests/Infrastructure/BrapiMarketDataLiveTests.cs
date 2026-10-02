using FluentAssertions;
using InvestDashboard.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class BrapiMarketDataLiveTests
{
    [BrapiLiveFact]
    public async Task PublicQuoteHistoryAndSearch_AreMappedByTheProductionClient()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var configuration = new ConfigurationManager();
        configuration.AddEnvironmentVariables();
        var client = new BrapiMarketDataClient(http, configuration, NullLogger<BrapiMarketDataClient>.Instance);

        var quotes = await client.GetQuotesAsync(["PETR4"]);
        quotes.Should().ContainSingle(item => item.Symbol == "PETR4" && item.Price > 0);

        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = end.AddDays(-14);
        var history = await client.GetDailyHistoryAsync(["PETR4"], start, end);
        history.Should().NotBeEmpty();
        history.Should().OnlyContain(point => point.Symbol == "PETR4" && point.Price > 0 && point.DateUtc >= start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var search = await client.SearchAsync("PETR4");
        search.Should().Contain(item => item.Symbol == "PETR4");
    }
}

public sealed class BrapiLiveFactAttribute : FactAttribute
{
    public BrapiLiveFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("BRAPI_LIVE_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
            Skip = "Set BRAPI_LIVE_TESTS=true only for explicit live-provider validation.";
    }
}
