using System;
using System.Linq;
using FluentAssertions;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using Xunit;

namespace InvestDashboard.UnitTests.Domain;

public class PortfolioTests
{
    private readonly string _userId = "user_123";
    private readonly Guid _portfolioId = Guid.NewGuid();

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var portfolio = new Portfolio(_portfolioId, _userId, "My Portfolio", 100m);

        // Assert
        portfolio.Id.Should().Be(_portfolioId);
        portfolio.UserId.Should().Be(_userId);
        portfolio.Name.Should().Be("My Portfolio");
        portfolio.Balance.Should().Be(100m);
        portfolio.Positions.Should().BeEmpty();
        portfolio.TotalAssetsValue.Should().Be(0);
        portfolio.TotalValue.Should().Be(100m);
    }

    [Fact]
    public void ProcessTransaction_ShouldIncreaseBalance_OnDeposit()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _userId, "My Portfolio", 500m);
        var deposit = new Transaction(
            Guid.NewGuid(), _userId, _portfolioId, null, null, TransactionType.Deposit, 1500m, 1m, 0m, DateTime.UtcNow);

        // Act
        portfolio.ProcessTransaction(deposit, 0m, AssetType.Stock);

        // Assert
        portfolio.Balance.Should().Be(2000m);
    }

    [Fact]
    public void ProcessTransaction_ShouldDecreaseBalance_OnWithdrawal()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _userId, "My Portfolio", 2000m);
        var withdrawal = new Transaction(
            Guid.NewGuid(), _userId, _portfolioId, null, null, TransactionType.Withdrawal, 500m, 1m, 0m, DateTime.UtcNow);

        // Act
        portfolio.ProcessTransaction(withdrawal, 0m, AssetType.Stock);

        // Assert
        portfolio.Balance.Should().Be(1500m);
    }

    [Fact]
    public void ProcessTransaction_ShouldThrow_WhenWithdrawalExceedsBalance()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _userId, "My Portfolio", 100m);
        var withdrawal = new Transaction(
            Guid.NewGuid(), _userId, _portfolioId, null, null, TransactionType.Withdrawal, 150m, 1m, 0m, DateTime.UtcNow);

        // Act
        Action action = () => portfolio.ProcessTransaction(withdrawal, 0m, AssetType.Stock);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient cash balance*");
    }

    [Fact]
    public void ProcessTransaction_ShouldCalculateAverageCostAndCreatePosition_OnBuy()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _userId, "My Portfolio", 10000m);
        var assetId = Guid.NewGuid();
        var ticker = "WEGE3";
        var buyTx = new Transaction(
            Guid.NewGuid(), _userId, _portfolioId, assetId, ticker, TransactionType.Buy, 100m, 35m, 10m, DateTime.UtcNow);

        // Act
        portfolio.ProcessTransaction(buyTx, 35m, AssetType.Stock);

        // Assert
        portfolio.Balance.Should().Be(10000m - ((100 * 35) + 10)); // 10000 - 3510 = 6490
        portfolio.Positions.Should().HaveCount(1);

        var position = portfolio.Positions.First();
        position.AssetId.Should().Be(assetId);
        position.Ticker.Should().Be(ticker);
        position.AssetType.Should().Be(AssetType.Stock);
        position.Quantity.Should().Be(100m);
        position.AverageCost.Should().Be(35.10m); // 3510 / 100 = 35.10
        position.TotalCost.Should().Be(3510m);
        position.CurrentPrice.Should().Be(35m);
        position.CurrentValue.Should().Be(3500m);
        position.TotalReturnAmount.Should().Be(-10m); // 3500 - 3510
    }

    [Fact]
    public void ProcessTransaction_ShouldRecalculateAverageCost_OnConsecutiveBuys()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _userId, "My Portfolio", 20000m);
        var assetId = Guid.NewGuid();
        var ticker = "WEGE3";

        var buy1 = new Transaction(Guid.NewGuid(), _userId, _portfolioId, assetId, ticker, TransactionType.Buy, 100m, 30m, 10m, DateTime.UtcNow);
        var buy2 = new Transaction(Guid.NewGuid(), _userId, _portfolioId, assetId, ticker, TransactionType.Buy, 50m, 40m, 5m, DateTime.UtcNow);

        // Act
        portfolio.ProcessTransaction(buy1, 30m, AssetType.Stock);
        portfolio.ProcessTransaction(buy2, 40m, AssetType.Stock);

        // Assert
        // Buy 1: Cost = 3010, Qty = 100. Avg = 30.10
        // Buy 2: Cost = 2005, Qty = 50.
        // Total Qty = 150. Total Cost = 3010 + 2005 = 5015. Avg = 5015 / 150 = 33.4333...
        var position = portfolio.Positions.First();
        position.Quantity.Should().Be(150m);
        position.TotalCost.Should().Be(5015m);
        position.AverageCost.Should().BeApproximately(33.4333m, 0.0001m);
    }

    [Fact]
    public void ProcessTransaction_ShouldReduceSharesButKeepAverageCostUnchanged_OnSell()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _userId, "My Portfolio", 10000m);
        var assetId = Guid.NewGuid();
        var ticker = "WEGE3";

        var buy = new Transaction(Guid.NewGuid(), _userId, _portfolioId, assetId, ticker, TransactionType.Buy, 100m, 30m, 10m, DateTime.UtcNow);
        var sell = new Transaction(Guid.NewGuid(), _userId, _portfolioId, assetId, ticker, TransactionType.Sell, 40m, 35m, 5m, DateTime.UtcNow);

        // Act
        portfolio.ProcessTransaction(buy, 30m, AssetType.Stock);
        portfolio.ProcessTransaction(sell, 35m, AssetType.Stock);

        // Assert
        // Buy: Balance becomes 10000 - 3010 = 6990. Position: Qty = 100, Avg = 30.10, TotalCost = 3010.
        // Sell: Revenue = (40 * 35) - 5 = 1395. Balance becomes 6990 + 1395 = 8385.
        // Position: Qty becomes 60. Avg remains 30.10. TotalCost = 60 * 30.10 = 1806.
        portfolio.Balance.Should().Be(8385m);
        
        var position = portfolio.Positions.First();
        position.Quantity.Should().Be(60m);
        position.AverageCost.Should().Be(30.10m);
        position.TotalCost.Should().Be(1806m);
    }

    [Fact]
    public void ProcessTransaction_ShouldResetPositionToZero_OnFullyClosedPosition()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _userId, "My Portfolio", 10000m);
        var assetId = Guid.NewGuid();
        var ticker = "WEGE3";

        var buy = new Transaction(Guid.NewGuid(), _userId, _portfolioId, assetId, ticker, TransactionType.Buy, 100m, 30m, 0m, DateTime.UtcNow);
        var sell = new Transaction(Guid.NewGuid(), _userId, _portfolioId, assetId, ticker, TransactionType.Sell, 100m, 35m, 0m, DateTime.UtcNow);

        // Act
        portfolio.ProcessTransaction(buy, 30m, AssetType.Stock);
        portfolio.ProcessTransaction(sell, 35m, AssetType.Stock);

        // Assert
        var position = portfolio.Positions.First();
        position.Quantity.Should().Be(0);
        position.AverageCost.Should().Be(0);
        position.TotalCost.Should().Be(0);
    }

    [Fact]
    public void RemovePositionAndRevertTransactions_ShouldReconstructBalanceAndRemovePosition()
    {
        // Arrange
        var portfolio = new Portfolio(_portfolioId, _userId, "My Portfolio", 10000m);
        var assetId = Guid.NewGuid();
        var ticker = "WEGE3";

        var buy1 = new Transaction(Guid.NewGuid(), _userId, _portfolioId, assetId, ticker, TransactionType.Buy, 100m, 30m, 10m, DateTime.UtcNow);
        var buy2 = new Transaction(Guid.NewGuid(), _userId, _portfolioId, assetId, ticker, TransactionType.Buy, 50m, 40m, 5m, DateTime.UtcNow);
        var sell = new Transaction(Guid.NewGuid(), _userId, _portfolioId, assetId, ticker, TransactionType.Sell, 30m, 45m, 10m, DateTime.UtcNow);

        // Act
        portfolio.ProcessTransaction(buy1, 30m, AssetType.Stock);
        portfolio.ProcessTransaction(buy2, 40m, AssetType.Stock);
        portfolio.ProcessTransaction(sell, 45m, AssetType.Stock);

        var transactions = new List<Transaction> { buy1, buy2, sell };

        // We verify cash balance before revert
        // Balance = 10000 - ((100 * 30) + 10) - ((50 * 40) + 5) + ((30 * 45) - 10)
        // Balance = 10000 - 3010 - 2005 + 1340 = 6325
        portfolio.Balance.Should().Be(6325m);
        portfolio.Positions.Should().NotBeEmpty();

        portfolio.RemovePositionAndRevertTransactions(assetId, transactions);

        // Assert: cash restored back to 10000m and position removed!
        portfolio.Balance.Should().Be(10000m);
        portfolio.Positions.Should().BeEmpty();
    }
}
