using System;
using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public class HistoricalPrice : Entity<Guid>
{
    public Guid AssetId { get; private set; }
    public decimal Price { get; private set; }
    public DateTime Date { get; private set; }

    public HistoricalPrice(Guid id, Guid assetId, decimal price, DateTime date)
        : base(id)
    {
        if (assetId == Guid.Empty)
            throw new ArgumentException("Asset Id cannot be empty", nameof(assetId));

        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        AssetId = assetId;
        Price = price;
        Date = date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private HistoricalPrice() { }
#pragma warning restore CS8618
}
