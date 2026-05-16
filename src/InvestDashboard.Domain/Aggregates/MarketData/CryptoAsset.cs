using System;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public class CryptoAsset : Asset
{
    public string Network { get; private set; }

    public CryptoAsset(Guid id, string ticker, string name, decimal currentPrice, DateTime lastUpdatedUtc, string network)
        : base(id, ticker, name, AssetType.Crypto, currentPrice, lastUpdatedUtc)
    {
        if (string.IsNullOrWhiteSpace(network))
            throw new ArgumentException("Network cannot be null or empty", nameof(network));

        Network = network.Trim();
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private CryptoAsset() { }
#pragma warning restore CS8618
}
