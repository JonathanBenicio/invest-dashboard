using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class RendaFixaConfiguration : IEntityTypeConfiguration<RendaFixa>
{
    public void Configure(EntityTypeBuilder<RendaFixa> builder)
    {
        builder.Property(asset => asset.Issuer)
            .HasColumnName("issuer")
            .HasMaxLength(200)
            .HasDefaultValue("Desconhecido")
            .IsRequired();
        builder.Property(f => f.Indexer)
            .HasColumnName("indexer")
            .HasMaxLength(50);

        builder.Property(f => f.InterestRate)
            .HasColumnName("interest_rate")
            .HasColumnType("numeric(18,4)");

        builder.Property(f => f.MaturityDate)
            .HasColumnName("maturity_date");
        builder.Property(f => f.Liquidity).HasColumnName("liquidity").HasMaxLength(80);
        builder.Property(f => f.Convention).HasColumnName("convention").HasMaxLength(80);
    }
}
