using System;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public class RendaFixa : Ativo
{
    public string Issuer { get; private set; } = string.Empty;
    public string Indexer { get; private set; }
    public decimal InterestRate { get; private set; }
    public DateTime MaturityDate { get; private set; }

    public RendaFixa(Guid id, string ticker, string name, decimal currentPrice, DateTime lastUpdatedUtc, string indexer, decimal interestRate, DateTime maturityDate, string issuer = "", string subtype = "RENDA_FIXA")
        : base(id, ticker, name, TipoAtivo.RendaFixa, currentPrice, lastUpdatedUtc, subtype)
    {
        if (string.IsNullOrWhiteSpace(indexer))
            throw new ArgumentException("Indexer cannot be null or empty", nameof(indexer));

        if (interestRate < 0)
            throw new ArgumentException("Interest rate cannot be negative", nameof(interestRate));

        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException("Issuer cannot be null or empty", nameof(issuer));

        Issuer = issuer.Trim();
        Indexer = indexer.Trim();
        InterestRate = interestRate;
        MaturityDate = maturityDate.Kind == DateTimeKind.Utc ? maturityDate : maturityDate.ToUniversalTime();
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private RendaFixa() { }
#pragma warning restore CS8618
}
