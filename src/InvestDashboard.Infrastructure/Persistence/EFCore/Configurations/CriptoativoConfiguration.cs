using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class CriptoativoConfiguration : IEntityTypeConfiguration<Criptoativo>
{
    public void Configure(EntityTypeBuilder<Criptoativo> builder)
    {
        builder.Property(c => c.Network)
            .HasColumnName("network")
            .HasMaxLength(100);
    }
}
