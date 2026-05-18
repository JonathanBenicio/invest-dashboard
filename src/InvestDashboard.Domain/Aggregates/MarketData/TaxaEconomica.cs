using System;
using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public class TaxaEconomica : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Symbol { get; private set; }
    public decimal CurrentValue { get; private set; }
    public decimal PreviousValue { get; private set; }
    public decimal Variation => CurrentValue - PreviousValue;
    public string Description { get; private set; }
    public string Source { get; private set; }
    public DateTime LastUpdate { get; private set; }

    public TaxaEconomica(
        Guid id,
        string name,
        string symbol,
        decimal currentValue,
        decimal previousValue,
        string description,
        string source,
        DateTime lastUpdate)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or empty", nameof(description));
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or empty", nameof(source));

        Name = name.Trim();
        Symbol = symbol.Trim().ToUpperInvariant();
        CurrentValue = currentValue;
        PreviousValue = previousValue;
        Description = description.Trim();
        Source = source.Trim();
        LastUpdate = lastUpdate;
    }

#pragma warning disable CS8618
    private TaxaEconomica() { }
#pragma warning restore CS8618

    public void UpdateValue(decimal newValue, DateTime updateDate)
    {
        PreviousValue = CurrentValue;
        CurrentValue = newValue;
        LastUpdate = updateDate;
    }
}
