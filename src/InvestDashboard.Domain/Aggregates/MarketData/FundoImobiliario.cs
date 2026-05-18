using System;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public class FundoImobiliario : Ativo
{
    public string Segment { get; private set; }

    public FundoImobiliario(Guid id, string ticker, string name, decimal currentPrice, DateTime lastUpdatedUtc, string segment)
        : base(id, ticker, name, TipoAtivo.FundoImobiliario, currentPrice, lastUpdatedUtc)
    {
        if (string.IsNullOrWhiteSpace(segment))
            throw new ArgumentException("Segment cannot be null or empty", nameof(segment));

        Segment = segment.Trim();
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private FundoImobiliario() { }
#pragma warning restore CS8618
}
