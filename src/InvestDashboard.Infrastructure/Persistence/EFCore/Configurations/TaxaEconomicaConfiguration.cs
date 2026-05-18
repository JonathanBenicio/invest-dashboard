using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class TaxaEconomicaConfiguration : IEntityTypeConfiguration<TaxaEconomica>
{
    public void Configure(EntityTypeBuilder<TaxaEconomica> builder)
    {
        builder.ToTable("economic_rates");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.CurrentValue)
            .HasColumnName("current_value")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(r => r.PreviousValue)
            .HasColumnName("previous_value")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.Source)
            .HasColumnName("source")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.LastUpdate)
            .HasColumnName("last_update")
            .IsRequired();

        builder.HasIndex(r => r.Symbol)
            .HasDatabaseName("idx_economic_rates_symbol");
    }
}
