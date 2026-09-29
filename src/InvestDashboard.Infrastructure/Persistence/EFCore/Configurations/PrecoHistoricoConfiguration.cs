using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class PrecoHistoricoConfiguration : IEntityTypeConfiguration<PrecoHistorico>
{
    public void Configure(EntityTypeBuilder<PrecoHistorico> builder)
    {
        builder.ToTable("historical_prices");

        builder.HasKey(hp => hp.Id);
        builder.Property(hp => hp.Id)
            .HasColumnName("id");

        builder.Property(hp => hp.AtivoId)
            .HasColumnName("asset_id")
            .IsRequired();

        builder.Property(hp => hp.Price)
            .HasColumnName("price")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(hp => hp.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(hp => hp.Source)
            .HasColumnName("source")
            .HasMaxLength(20)
            .HasDefaultValue("legacy")
            .IsRequired();

        builder.Property(hp => hp.IsAdjusted)
            .HasColumnName("is_adjusted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(hp => new { hp.AtivoId, hp.Date })
            .HasDatabaseName("idx_historical_prices_asset_date");

        builder.HasOne<Ativo>()
            .WithMany()
            .HasForeignKey(hp => hp.AtivoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
