using System;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public class RendaFixa : Ativo
{
    public string Issuer { get; private set; } = string.Empty;
    public string Indexer { get; private set; }
    public decimal InterestRate { get; private set; }
    public DateTime MaturityDate { get; private set; }
    public string? Liquidity { get; private set; }
    public string? Convention { get; private set; }

    public RendaFixa(Guid id, string ticker, string name, decimal currentPrice, DateTime lastUpdatedUtc, string indexer, decimal interestRate, DateTime maturityDate, string issuer = "", string subtype = "RENDA_FIXA", string? liquidity = null, string? convention = null)
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
        Liquidity = NormalizeOptional(liquidity, nameof(liquidity));
        Convention = NormalizeOptional(convention, nameof(convention));
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private RendaFixa() { }
#pragma warning restore CS8618

    private static string? NormalizeOptional(string? value, string parameterName)
    {
        var normalized = value?.Trim();
        if (normalized?.Length > 80) throw new ArgumentOutOfRangeException(parameterName, "Value cannot exceed 80 characters.");
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
