using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class AssetPositionConfiguration : IEntityTypeConfiguration<AssetPosition>
{
    public void Configure(EntityTypeBuilder<AssetPosition> builder)
    {
        builder.ToTable("asset_positions");

        builder.HasKey(ap => ap.Id);
        builder.Property(ap => ap.Id)
            .HasColumnName("id");

        builder.Property(ap => ap.PortfolioId)
            .HasColumnName("portfolio_id")
            .IsRequired();

        builder.Property(ap => ap.AssetId)
            .HasColumnName("asset_id")
            .IsRequired();

        builder.Property(ap => ap.Ticker)
            .HasColumnName("ticker")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ap => ap.AssetType)
            .HasColumnName("asset_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ap => ap.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(ap => ap.AverageCost)
            .HasColumnName("average_cost")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(ap => ap.TotalCost)
            .HasColumnName("total_cost")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(ap => ap.CurrentPrice)
            .HasColumnName("current_price")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.HasIndex(ap => ap.PortfolioId)
            .HasDatabaseName("idx_asset_positions_portfolio_id");

        builder.HasIndex(ap => ap.AssetId)
            .HasDatabaseName("idx_asset_positions_asset_id");

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(ap => ap.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
