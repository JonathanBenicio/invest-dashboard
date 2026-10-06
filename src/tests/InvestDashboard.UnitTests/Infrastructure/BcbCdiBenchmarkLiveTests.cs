using FluentAssertions;
using InvestDashboard.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class BcbCdiBenchmarkLiveTests
{
    [BcbCdiLiveFact]
    public async Task PublicSgsSeries_IsMappedAndComposedByProductionProvider()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbCdiBenchmarkProvider(client, cache, TimeProvider.System);
        var end = DateOnly.FromDateTime(DateTime.UtcNow);

        var serie = await provider.GetCdiAsync(end.AddDays(-14), end);

        serie.Pontos.Should().NotBeEmpty();
        serie.Pontos.Should().OnlyContain(point => point.TaxaDiariaPercentual > 0 && point.IndiceBase100 >= 100);
        serie.Pontos.Should().BeInAscendingOrder(point => point.Data);
    }
}

public sealed class BcbCdiLiveFactAttribute : FactAttribute
{
    public BcbCdiLiveFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("BCB_CDI_LIVE_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
            Skip = "Set BCB_CDI_LIVE_TESTS=true only for explicit live-provider validation.";
    }
}
