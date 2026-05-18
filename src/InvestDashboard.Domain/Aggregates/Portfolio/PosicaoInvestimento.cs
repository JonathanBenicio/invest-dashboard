using System;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

public class PosicaoInvestimento : Entity<Guid>
{
    public Guid CarteiraId { get; private set; }
    public Guid AtivoId { get; private set; }
    public string Ticker { get; private set; }
    public TipoAtivo TipoAtivo { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal AverageCost { get; private set; }
    public decimal TotalCost { get; private set; }
    public decimal CurrentPrice { get; private set; }

    public decimal CurrentValue => Quantity * CurrentPrice;
    public decimal TotalReturnAmount => CurrentValue - TotalCost;
    public decimal TotalReturnPercentage => TotalCost > 0 ? (TotalReturnAmount / TotalCost) * 100 : 0;

    public PosicaoInvestimento(Guid id, Guid carteiraId, Guid ativoId, string ticker, TipoAtivo tipoAtivo, decimal currentPrice)
        : base(id)
    {
        if (carteiraId == Guid.Empty)
            throw new ArgumentException("Carteira Id cannot be empty", nameof(carteiraId));

        if (ativoId == Guid.Empty)
            throw new ArgumentException("Ativo Id cannot be empty", nameof(ativoId));

        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Ticker cannot be null or empty", nameof(ticker));

        if (currentPrice < 0)
            throw new ArgumentException("Current price cannot be negative", nameof(currentPrice));

        CarteiraId = carteiraId;
        AtivoId = ativoId;
        Ticker = ticker.Trim().ToUpperInvariant();
        TipoAtivo = tipoAtivo;
        CurrentPrice = currentPrice;
        Quantity = 0;
        AverageCost = 0;
        TotalCost = 0;
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private PosicaoInvestimento() { }
#pragma warning restore CS8618

    public void AddShares(decimal qty, decimal price, decimal fee)
    {
        if (qty <= 0)
            throw new ArgumentException("Quantity to add must be greater than zero", nameof(qty));

        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(price));

        if (fee < 0)
            throw new ArgumentException("Fee cannot be negative", nameof(fee));

        decimal transactionCost = (qty * price) + fee;
        decimal newQuantity = Quantity + qty;
        decimal newTotalCost = TotalCost + transactionCost;

        AverageCost = newTotalCost / newQuantity;
        Quantity = newQuantity;
        TotalCost = newTotalCost;
    }

    public void RemoveShares(decimal qty, decimal price, decimal fee, out decimal realizedGain)
    {
        if (qty <= 0)
            throw new ArgumentException("Quantity to remove must be greater than zero", nameof(qty));

        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(price));

        if (fee < 0)
            throw new ArgumentException("Fee cannot be negative", nameof(fee));

        if (qty > Quantity)
            throw new InvalidOperationException($"Cannot sell {qty} units. Only {Quantity} units are held in this position.");

        decimal transactionRevenue = (qty * price) - fee;
        decimal costOfSharesSold = qty * AverageCost;
        realizedGain = transactionRevenue - costOfSharesSold;

        decimal newQuantity = Quantity - qty;
        Quantity = newQuantity;

        if (Quantity == 0)
        {
            AverageCost = 0;
            TotalCost = 0;
        }
        else
        {
            TotalCost = Quantity * AverageCost;
        }
    }

    public void UpdateCurrentPrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        CurrentPrice = price;
    }
}
