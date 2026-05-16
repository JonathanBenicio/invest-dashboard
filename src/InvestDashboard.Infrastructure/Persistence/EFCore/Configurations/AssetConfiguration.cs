using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.Ticker)
            .HasColumnName("ticker")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(a => a.Ticker)
            .IsUnique();

        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.AssetType)
            .HasColumnName("asset_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.CurrentPrice)
            .HasColumnName("current_price")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(a => a.LastUpdatedUtc)
            .HasColumnName("last_updated_utc")
            .IsRequired();

        // TPH Inheritance configuration
        builder.HasDiscriminator<AssetType>(a => a.AssetType)
            .HasValue<StockAsset>(AssetType.Stock)
            .HasValue<FiiAsset>(AssetType.Fii)
            .HasValue<CryptoAsset>(AssetType.Crypto)
            .HasValue<FixedIncomeAsset>(AssetType.FixedIncome);
    }
}
