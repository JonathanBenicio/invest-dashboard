using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class RendaFixaConfiguration : IEntityTypeConfiguration<RendaFixa>
{
    public void Configure(EntityTypeBuilder<RendaFixa> builder)
    {
        builder.Property(f => f.Indexer)
            .HasColumnName("indexer")
            .HasMaxLength(50);

        builder.Property(f => f.InterestRate)
            .HasColumnName("interest_rate")
            .HasColumnType("numeric(18,4)");

        builder.Property(f => f.MaturityDate)
            .HasColumnName("maturity_date");
    }
}
