using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class StockAssetConfiguration : IEntityTypeConfiguration<StockAsset>
{
    public void Configure(EntityTypeBuilder<StockAsset> builder)
    {
        builder.Property(s => s.Sector)
            .HasColumnName("sector")
            .HasMaxLength(100);
    }
}
