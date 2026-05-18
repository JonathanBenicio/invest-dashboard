using System;
using FluentAssertions;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Ativo;
using Xunit;

namespace InvestDashboard.UnitTests.Domain;

public class AtivoTests
{
    [Fact]
    public void Constructor_ShouldCreateStockAsset_WhenValidArgumentsProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var ticker = "WEGE3";
        var name = "Weg S.A.";
        var price = 38.50m;
        var now = DateTime.UtcNow;
        var sector = "Industrials";

        // Act
        var asset = new StockAsset(id, ticker, name, price, now, sector);

        // Assert
        asset.Id.Should().Be(id);
        asset.Ticker.Should().Be(ticker);
        asset.Name.Should().Be(name);
        asset.AssetType.Should().Be(TipoAtivo.Stock);
        asset.CurrentPrice.Should().Be(price);
        asset.LastUpdatedUtc.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        asset.Sector.Should().Be(sector);
    }

    [Theory]
    [InlineData("", "Weg S.A.", "Industrials", "Ticker cannot be null or empty")]
    [InlineData("WEGE3", "", "Industrials", "Name cannot be null or empty")]
    public void Constructor_ShouldThrowArgumentException_WhenStringFieldsAreInvalid(
        string ticker, string name, string sector, string expectedMessage)
    {
        // Act
        Action action = () => new StockAsset(Guid.NewGuid(), ticker, name, 10m, DateTime.UtcNow, sector);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage($"*{expectedMessage}*");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenPriceIsNegative()
    {
        // Act
        Action action = () => new StockAsset(Guid.NewGuid(), "WEGE3", "Weg S.A.", -1.5m, DateTime.UtcNow, "Industrials");

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Current price cannot be negative*");
    }

    [Fact]
    public void UpdatePrice_ShouldModifyPriceAndDate_WhenValid()
    {
        // Arrange
        var asset = new StockAsset(Guid.NewGuid(), "WEGE3", "Weg", 10m, DateTime.UtcNow, "Industrials");
        var newPrice = 12.50m;
        var updateTime = DateTime.UtcNow;

        // Act
        asset.UpdatePrice(newPrice, updateTime);

        // Assert
        asset.CurrentPrice.Should().Be(newPrice);
        asset.LastUpdatedUtc.Should().BeCloseTo(updateTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdatePrice_ShouldThrowArgumentException_WhenNewPriceIsNegative()
    {
        // Arrange
        var asset = new StockAsset(Guid.NewGuid(), "WEGE3", "Weg", 10m, DateTime.UtcNow, "Industrials");

        // Act
        Action action = () => asset.UpdatePrice(-0.01m, DateTime.UtcNow);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*New price cannot be negative*");
    }

    [Fact]
    public void UpdatePrice_ShouldThrowArgumentException_WhenDateIsInTheFuture()
    {
        // Arrange
        var asset = new StockAsset(Guid.NewGuid(), "WEGE3", "Weg", 10m, DateTime.UtcNow, "Industrials");
        var futureDate = DateTime.UtcNow.AddHours(1);

        // Act
        Action action = () => asset.UpdatePrice(15m, futureDate);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Updated date cannot be in the future*");
    }
}
