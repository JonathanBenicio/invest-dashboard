using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.PortfolioId)
            .HasColumnName("portfolio_id")
            .IsRequired();

        builder.Property(t => t.AssetId)
            .HasColumnName("asset_id");

        builder.Property(t => t.Ticker)
            .HasColumnName("ticker")
            .HasMaxLength(20);

        builder.Property(t => t.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(t => t.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(t => t.BrokerageFee)
            .HasColumnName("brokerage_fee")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(t => t.TransactionDate)
            .HasColumnName("transaction_date")
            .IsRequired();

        builder.Property(t => t.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("idx_transactions_user_id");

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(t => t.AssetId)
            .OnDelete(DeleteBehavior.Restrict); // restrict so assets aren't accidentally deleted if transactions exist

        builder.HasOne<Portfolio>()
            .WithMany()
            .HasForeignKey(t => t.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
