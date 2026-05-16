using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class CryptoAssetConfiguration : IEntityTypeConfiguration<CryptoAsset>
{
    public void Configure(EntityTypeBuilder<CryptoAsset> builder)
    {
        builder.Property(c => c.Network)
            .HasColumnName("network")
            .HasMaxLength(100);
    }
}
