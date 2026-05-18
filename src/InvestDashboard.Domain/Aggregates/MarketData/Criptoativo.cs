using System;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public class Criptoativo : Ativo
{
    public string Network { get; private set; }

    public Criptoativo(Guid id, string ticker, string name, decimal currentPrice, DateTime lastUpdatedUtc, string network)
        : base(id, ticker, name, TipoAtivo.Criptoativo, currentPrice, lastUpdatedUtc)
    {
        if (string.IsNullOrWhiteSpace(network))
            throw new ArgumentException("Network cannot be null or empty", nameof(network));

        Network = network.Trim();
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private Criptoativo() { }
#pragma warning restore CS8618
}
