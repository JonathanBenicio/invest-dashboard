using System;
using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.MarketData;

public class PrecoHistorico : Entity<Guid>
{
    public Guid AtivoId { get; private set; }
    public decimal Price { get; private set; }
    public DateTime Date { get; private set; }

    public PrecoHistorico(Guid id, Guid ativoId, decimal price, DateTime date)
        : base(id)
    {
        if (ativoId == Guid.Empty)
            throw new ArgumentException("Ativo Id cannot be empty", nameof(ativoId));

        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        AtivoId = ativoId;
        Price = price;
        Date = date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private PrecoHistorico() { }
#pragma warning restore CS8618
}
