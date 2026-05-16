using System;
using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Trading;

public class Transaction : AggregateRoot<Guid>
{
    public string UserId { get; private set; }
    public Guid PortfolioId { get; private set; }
    public Guid? AssetId { get; private set; }
    public string? Ticker { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal BrokerageFee { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public string? Notes { get; private set; }

    public decimal TotalAmount => Type switch
    {
        TransactionType.Buy => (Quantity * UnitPrice) + BrokerageFee,
        TransactionType.Sell => (Quantity * UnitPrice) - BrokerageFee,
        TransactionType.Deposit => Quantity,
        TransactionType.Withdrawal => Quantity,
        _ => throw new InvalidOperationException("Unknown transaction type")
    };

    public Transaction(
        Guid id,
        string userId,
        Guid portfolioId,
        Guid? assetId,
        string? ticker,
        TransactionType type,
        decimal quantity,
        decimal unitPrice,
        decimal brokerageFee,
        DateTime transactionDate,
        string? notes = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User Id cannot be null or empty", nameof(userId));

        if (portfolioId == Guid.Empty)
            throw new ArgumentException("Portfolio Id cannot be empty", nameof(portfolioId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity/Amount must be greater than zero", nameof(quantity));

        if (brokerageFee < 0)
            throw new ArgumentException("Brokerage fee cannot be negative", nameof(brokerageFee));

        if (transactionDate > DateTime.UtcNow.AddMinutes(5))
            throw new ArgumentException("Transaction date cannot be in the future", nameof(transactionDate));

        if (type is TransactionType.Buy or TransactionType.Sell)
        {
            if (assetId is null || assetId == Guid.Empty)
                throw new ArgumentException("Asset Id must be specified for Buy/Sell transactions", nameof(assetId));

            if (string.IsNullOrWhiteSpace(ticker))
                throw new ArgumentException("Ticker must be specified for Buy/Sell transactions", nameof(ticker));

            if (unitPrice <= 0)
                throw new ArgumentException("Unit price must be greater than zero for Buy/Sell transactions", nameof(unitPrice));
        }
        else // Deposit or Withdrawal
        {
            if (assetId is not null || ticker is not null)
                throw new ArgumentException("Asset Id and Ticker must be null for Deposit/Withdrawal cash transactions");

            if (unitPrice != 1)
                throw new ArgumentException("Unit price must be 1 for Deposit/Withdrawal transactions", nameof(unitPrice));
        }

        UserId = userId;
        PortfolioId = portfolioId;
        AssetId = assetId;
        Ticker = ticker?.Trim().ToUpperInvariant();
        Type = type;
        Quantity = quantity;
        UnitPrice = unitPrice;
        BrokerageFee = brokerageFee;
        TransactionDate = transactionDate.Kind == DateTimeKind.Utc ? transactionDate : transactionDate.ToUniversalTime();
        Notes = notes?.Trim();
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private Transaction() { }
#pragma warning restore CS8618
}
