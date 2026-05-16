using System;
using System.Collections.Generic;
using System.Linq;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

public class Portfolio : AggregateRoot<Guid>
{
    private readonly List<AssetPosition> _positions = new();

    public string UserId { get; private set; }
    public string Name { get; private set; }
    public decimal Balance { get; private set; }

    public IReadOnlyCollection<AssetPosition> Positions => _positions.AsReadOnly();

    public decimal TotalAssetsValue => _positions.Sum(p => p.CurrentValue);
    public decimal TotalAssetsCost => _positions.Sum(p => p.TotalCost);
    public decimal TotalValue => TotalAssetsValue + Balance;
    public decimal TotalReturnAmount => TotalAssetsValue - TotalAssetsCost;
    public decimal TotalReturnPercentage => TotalAssetsCost > 0 ? (TotalReturnAmount / TotalAssetsCost) * 100 : 0;

    public Portfolio(Guid id, string userId, string name, decimal initialBalance = 0)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User Id cannot be null or empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Portfolio name cannot be null or empty", nameof(name));

        if (initialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative", nameof(initialBalance));

        UserId = userId;
        Name = name.Trim();
        Balance = initialBalance;
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private Portfolio() { }
#pragma warning restore CS8618

    public void ProcessTransaction(Transaction transaction, decimal currentAssetPrice, AssetType assetType)
    {
        if (transaction is null)
            throw new ArgumentNullException(nameof(transaction));

        if (transaction.UserId != UserId)
            throw new InvalidOperationException("Transaction does not belong to the owner of this portfolio");

        switch (transaction.Type)
        {
            case TransactionType.Deposit:
                Balance += transaction.Quantity;
                break;

            case TransactionType.Withdrawal:
                if (Balance < transaction.Quantity)
                    throw new InvalidOperationException($"Insufficient cash balance. Required: {transaction.Quantity}, Available: {Balance}");

                Balance -= transaction.Quantity;
                break;

            case TransactionType.Buy:
                {
                    decimal totalCost = transaction.TotalAmount;
                    if (Balance < totalCost)
                        throw new InvalidOperationException($"Insufficient cash balance to execute buy. Required: {totalCost}, Available: {Balance}");

                    Balance -= totalCost;

                    if (transaction.AssetId is null)
                        throw new InvalidOperationException("Asset ID is required for a buy transaction");

                    if (string.IsNullOrWhiteSpace(transaction.Ticker))
                        throw new InvalidOperationException("Ticker is required for a buy transaction");

                    var position = _positions.FirstOrDefault(p => p.AssetId == transaction.AssetId);
                    if (position is null)
                    {
                        position = new AssetPosition(
                            Guid.NewGuid(),
                            Id,
                            transaction.AssetId.Value,
                            transaction.Ticker,
                            assetType,
                            currentAssetPrice
                        );
                        _positions.Add(position);
                    }

                    position.AddShares(transaction.Quantity, transaction.UnitPrice, transaction.BrokerageFee);
                    break;
                }

            case TransactionType.Sell:
                {
                    if (transaction.AssetId is null)
                        throw new InvalidOperationException("Asset ID is required for a sell transaction");

                    var position = _positions.FirstOrDefault(p => p.AssetId == transaction.AssetId);
                    if (position is null || position.Quantity < transaction.Quantity)
                        throw new InvalidOperationException($"Insufficient shares held to execute sell. Required: {transaction.Quantity}, Available: {(position is null ? 0 : position.Quantity)}");

                    position.RemoveShares(transaction.Quantity, transaction.UnitPrice, transaction.BrokerageFee, out _);
                    Balance += transaction.TotalAmount;

                    // Proactively clean up closed empty positions if desired, but retaining them with 0 qty can be helpful for history.
                    // Let's keep them so the UI can display closed positions if requested, but only active ones contribute to sums.
                    break;
                }

            default:
                throw new InvalidOperationException("Unknown transaction type");
        }
    }

    public void UpdateAssetPrice(Guid assetId, decimal newPrice)
    {
        if (assetId == Guid.Empty)
            throw new ArgumentException("Asset Id cannot be empty", nameof(assetId));

        var position = _positions.FirstOrDefault(p => p.AssetId == assetId);
        if (position is not null)
        {
            position.UpdateCurrentPrice(newPrice);
        }
    }

    public void RemovePositionAndRevertTransactions(Guid assetId, IEnumerable<Transaction> transactions)
    {
        var position = _positions.FirstOrDefault(p => p.AssetId == assetId);
        if (position is null)
            return;

        foreach (var transaction in transactions.Where(t => t.AssetId == assetId))
        {
            if (transaction.Type == TransactionType.Buy)
            {
                Balance += transaction.TotalAmount;
            }
            else if (transaction.Type == TransactionType.Sell)
            {
                Balance -= transaction.TotalAmount;
            }
        }

        _positions.Remove(position);
    }
}
