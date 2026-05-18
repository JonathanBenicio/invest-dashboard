using System;
using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Trading;

public class Transacao : AggregateRoot<Guid>
{
    public string UserId { get; private set; }
    public Guid CarteiraId { get; private set; }
    public Guid? AtivoId { get; private set; }
    public string? Ticker { get; private set; }
    public TipoTransacao Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal BrokerageFee { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public string? Notes { get; private set; }

    public decimal TotalAmount => Type switch
    {
        TipoTransacao.Buy => (Quantity * UnitPrice) + BrokerageFee,
        TipoTransacao.Sell => (Quantity * UnitPrice) - BrokerageFee,
        TipoTransacao.Deposit => Quantity,
        TipoTransacao.Withdrawal => Quantity,
        _ => throw new InvalidOperationException("Unknown transaction type")
    };

    public Transacao(
        Guid id,
        string userId,
        Guid carteiraId,
        Guid? ativoId,
        string? ticker,
        TipoTransacao type,
        decimal quantity,
        decimal unitPrice,
        decimal brokerageFee,
        DateTime transactionDate,
        string? notes = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User Id cannot be null or empty", nameof(userId));

        if (carteiraId == Guid.Empty)
            throw new ArgumentException("Carteira Id cannot be empty", nameof(carteiraId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity/Amount must be greater than zero", nameof(quantity));

        if (brokerageFee < 0)
            throw new ArgumentException("Brokerage fee cannot be negative", nameof(brokerageFee));

        if (transactionDate > DateTime.UtcNow.AddMinutes(5))
            throw new ArgumentException("Transaction date cannot be in the future", nameof(transactionDate));

        if (type is TipoTransacao.Buy or TipoTransacao.Sell)
        {
            if (ativoId is null || ativoId == Guid.Empty)
                throw new ArgumentException("Ativo Id must be specified for Buy/Sell transactions", nameof(ativoId));

            if (string.IsNullOrWhiteSpace(ticker))
                throw new ArgumentException("Ticker must be specified for Buy/Sell transactions", nameof(ticker));

            if (unitPrice <= 0)
                throw new ArgumentException("Unit price must be greater than zero for Buy/Sell transactions", nameof(unitPrice));
        }
        else
        {
            if (ativoId is not null || ticker is not null)
                throw new ArgumentException("Ativo Id and Ticker must be null for Deposit/Withdrawal cash transactions");

            if (unitPrice != 1)
                throw new ArgumentException("Unit price must be 1 for Deposit/Withdrawal transactions", nameof(unitPrice));
        }

        UserId = userId;
        CarteiraId = carteiraId;
        AtivoId = ativoId;
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
    private Transacao() { }
#pragma warning restore CS8618
}
