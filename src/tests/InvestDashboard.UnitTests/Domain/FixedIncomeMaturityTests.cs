using FluentAssertions;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.UnitTests.Domain;

public sealed class FixedIncomeMaturityTests
{
    [Fact]
    public void MarkingMaturity_IsIdempotentAndPreservesPositionValue()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var maturityDate = new DateTime(2026, 9, 29, 15, 0, 0, DateTimeKind.Utc);
        var asset = new RendaFixa(Guid.NewGuid(), "CDBTESTE", "CDB de teste", 1m,
            maturityDate.AddYears(-1), "CDI", 110m, maturityDate, "Banco de teste", "CDB");
        var position = new PosicaoInvestimento(Guid.NewGuid(), Guid.NewGuid(), asset.Id,
            asset.Ticker, TipoAtivo.RendaFixa, 1.04m);
        position.UpdateMarketAsset(asset);
        position.AddShares(1000m, 1m, 0m);
        var before = new DateTime(2026, 9, 29, 2, 59, 0, DateTimeKind.Utc);
        var due = new DateTime(2026, 9, 29, 3, 0, 0, DateTimeKind.Utc);

        position.TryMarkMatured(before, timeZone).Should().BeFalse();
        position.MaturedAtUtc.Should().BeNull();

        position.TryMarkMatured(due, timeZone).Should().BeTrue();
        position.TryMarkMatured(due.AddHours(1), timeZone).Should().BeFalse();
        position.MaturedAtUtc.Should().Be(due);
        position.Quantity.Should().Be(1000m);
        position.TotalCost.Should().Be(1000m);
        position.CurrentValue.Should().Be(1040m);
    }
}
