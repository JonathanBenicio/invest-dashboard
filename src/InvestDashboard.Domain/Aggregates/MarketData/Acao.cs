using System;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public class Acao : Ativo
{
    public string Sector { get; private set; }

    public Acao(Guid id, string ticker, string name, decimal currentPrice, DateTime lastUpdatedUtc, string sector)
        : base(id, ticker, name, TipoAtivo.Acao, currentPrice, lastUpdatedUtc)
    {
        if (string.IsNullOrWhiteSpace(sector))
            throw new ArgumentException("Sector cannot be null or empty", nameof(sector));

        Sector = sector.Trim();
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private Acao() { }
#pragma warning restore CS8618
}
