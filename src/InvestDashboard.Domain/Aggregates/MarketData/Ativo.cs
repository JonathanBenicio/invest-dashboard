using System;
using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public abstract class Ativo : AggregateRoot<Guid>
{
    public string Ticker { get; private set; }
    public string Name { get; private set; }
    public TipoAtivo TipoAtivo { get; private set; }
    public decimal CurrentPrice { get; private set; }
    public DateTime LastUpdatedUtc { get; private set; }

    protected Ativo(Guid id, string ticker, string name, TipoAtivo tipoAtivo, decimal currentPrice, DateTime lastUpdatedUtc)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Ticker cannot be null or empty", nameof(ticker));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        if (currentPrice < 0)
            throw new ArgumentException("Current price cannot be negative", nameof(currentPrice));

        Ticker = ticker.Trim().ToUpperInvariant();
        Name = name.Trim();
        TipoAtivo = tipoAtivo;
        CurrentPrice = currentPrice;
        LastUpdatedUtc = lastUpdatedUtc.Kind == DateTimeKind.Utc ? lastUpdatedUtc : lastUpdatedUtc.ToUniversalTime();
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    protected Ativo() { }
#pragma warning restore CS8618

    public void UpdatePrice(decimal newPrice, DateTime updatedUtc)
    {
        if (newPrice < 0)
            throw new ArgumentException("New price cannot be negative", nameof(newPrice));

        if (updatedUtc > DateTime.UtcNow.AddMinutes(5)) // Allow slight clock drift
            throw new ArgumentException("Updated date cannot be in the future", nameof(updatedUtc));

        CurrentPrice = newPrice;
        LastUpdatedUtc = updatedUtc.Kind == DateTimeKind.Utc ? updatedUtc : updatedUtc.ToUniversalTime();
    }
}
