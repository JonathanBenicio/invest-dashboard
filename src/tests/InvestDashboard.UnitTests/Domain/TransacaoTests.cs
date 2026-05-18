using System;
using FluentAssertions;
using InvestDashboard.Domain.Aggregates.Transacao;
using Xunit;

namespace InvestDashboard.UnitTests.Domain;

public class TransacaoTests
{
    [Fact]
    public void Constructor_ShouldCreateBuyTransaction_WithCorrectComputedTotalAmount()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user_abc123";
        var portfolioId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var ticker = "VALE3";
        var type = TipoTransacao.Buy;
        var qty = 100m;
        var price = 60.00m;
        var fee = 5.00m;
        var date = DateTime.UtcNow;

        // Act
        var tx = new Transacao(id, userId, portfolioId, assetId, ticker, type, qty, price, fee, date);

        // Assert
        tx.Id.Should().Be(id);
        tx.UserId.Should().Be(userId);
        tx.CarteiraId.Should().Be(portfolioId);
        tx.AtivoId.Should().Be(assetId);
        tx.Ticker.Should().Be(ticker);
        tx.Type.Should().Be(type);
        tx.Quantity.Should().Be(qty);
        tx.UnitPrice.Should().Be(price);
        tx.BrokerageFee.Should().Be(fee);
        tx.TransactionDate.Should().BeCloseTo(date, TimeSpan.FromSeconds(1));
        tx.TotalAmount.Should().Be((qty * price) + fee); // (100 * 60) + 5 = 6005
    }

    [Fact]
    public void Constructor_ShouldCreateSellTransaction_WithCorrectComputedTotalAmount()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user_abc123";
        var portfolioId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var ticker = "VALE3";
        var type = TipoTransacao.Sell;
        var qty = 50m;
        var price = 70.00m;
        var fee = 2.50m;
        var date = DateTime.UtcNow;

        // Act
        var tx = new Transacao(id, userId, portfolioId, assetId, ticker, type, qty, price, fee, date);

        // Assert
        tx.TotalAmount.Should().Be((qty * price) - fee); // (50 * 70) - 2.5 = 3497.5
    }

    [Fact]
    public void Constructor_ShouldCreateDepositTransaction_WithUnitPriceOfOne()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user_abc123";
        var portfolioId = Guid.NewGuid();
        var type = TipoTransacao.Deposit;
        var amount = 1500m;
        var date = DateTime.UtcNow;

        // Act
        var tx = new Transacao(id, userId, portfolioId, null, null, type, amount, 1.00m, 0m, date);

        // Assert
        tx.TotalAmount.Should().Be(amount);
        tx.CarteiraId.Should().Be(portfolioId);
        tx.AtivoId.Should().BeNull();
        tx.Ticker.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenBuyTransactionIsMissingAssetDetails()
    {
        // Act
        Action action = () => new Transacao(
            Guid.NewGuid(), "user_123", Guid.NewGuid(), null, null, TipoTransacao.Buy, 10m, 15m, 0m, DateTime.UtcNow);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Asset Id must be specified*");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenDepositTransactionContainsAssetDetails()
    {
        // Act
        Action action = () => new Transacao(
            Guid.NewGuid(), "user_123", Guid.NewGuid(), Guid.NewGuid(), "WEGE3", TipoTransacao.Deposit, 1000m, 1.00m, 0m, DateTime.UtcNow);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Asset Id and Ticker must be null for Deposit/Withdrawal*");
    }
}
