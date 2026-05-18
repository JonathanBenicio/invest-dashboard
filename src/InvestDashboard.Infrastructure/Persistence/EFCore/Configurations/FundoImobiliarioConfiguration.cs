using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class FundoImobiliarioConfiguration : IEntityTypeConfiguration<FundoImobiliario>
{
    public void Configure(EntityTypeBuilder<FundoImobiliario> builder)
    {
        builder.Property(f => f.Segment)
            .HasColumnName("segment")
            .HasMaxLength(100);
    }
}
