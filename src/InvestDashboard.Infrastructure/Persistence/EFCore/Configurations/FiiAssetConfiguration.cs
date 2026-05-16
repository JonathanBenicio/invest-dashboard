using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class FiiAssetConfiguration : IEntityTypeConfiguration<FiiAsset>
{
    public void Configure(EntityTypeBuilder<FiiAsset> builder)
    {
        builder.Property(f => f.Segment)
            .HasColumnName("segment")
            .HasMaxLength(100);
    }
}
