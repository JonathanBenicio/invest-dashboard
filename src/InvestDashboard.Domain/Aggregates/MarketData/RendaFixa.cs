using System;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public class RendaFixa : Ativo
{
    public string Indexer { get; private set; }
    public decimal InterestRate { get; private set; }
    public DateTime MaturityDate { get; private set; }

    public RendaFixa(Guid id, string ticker, string name, decimal currentPrice, DateTime lastUpdatedUtc, string indexer, decimal interestRate, DateTime maturityDate)
        : base(id, ticker, name, TipoAtivo.RendaFixa, currentPrice, lastUpdatedUtc)
    {
        if (string.IsNullOrWhiteSpace(indexer))
            throw new ArgumentException("Indexer cannot be null or empty", nameof(indexer));

        if (interestRate < 0)
            throw new ArgumentException("Interest rate cannot be negative", nameof(interestRate));

        Indexer = indexer.Trim();
        InterestRate = interestRate;
        MaturityDate = maturityDate.Kind == DateTimeKind.Utc ? maturityDate : maturityDate.ToUniversalTime();
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private RendaFixa() { }
#pragma warning restore CS8618
}
