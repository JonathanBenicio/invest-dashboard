using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

public sealed class GrupoCarteiras : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public GrupoCarteiras(Guid id, string name, string createdByUserId, DateTime createdAtUtc) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Group name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(createdByUserId)) throw new ArgumentException("Creator is required.", nameof(createdByUserId));
        Name = name.Trim();
        CreatedByUserId = createdByUserId.Trim();
        CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
    }

#pragma warning disable CS8618
    private GrupoCarteiras() { }
#pragma warning restore CS8618
}
