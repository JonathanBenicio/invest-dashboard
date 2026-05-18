using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class AtivoConfiguration : IEntityTypeConfiguration<Ativo>
{
    public void Configure(EntityTypeBuilder<Ativo> builder)
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

        builder.Property(a => a.TipoAtivo)
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
        builder.HasDiscriminator<TipoAtivo>(a => a.TipoAtivo)
            .HasValue<Acao>(TipoAtivo.Acao)
            .HasValue<FundoImobiliario>(TipoAtivo.FundoImobiliario)
            .HasValue<Criptoativo>(TipoAtivo.Criptoativo)
            .HasValue<RendaFixa>(TipoAtivo.RendaFixa);
    }
}
