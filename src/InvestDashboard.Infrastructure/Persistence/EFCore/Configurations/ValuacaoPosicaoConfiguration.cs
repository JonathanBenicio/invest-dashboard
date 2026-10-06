using InvestDashboard.Domain.Aggregates.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public sealed class ValuacaoPosicaoConfiguration : IEntityTypeConfiguration<ValuacaoPosicao>
{
    public void Configure(EntityTypeBuilder<ValuacaoPosicao> builder)
    {
        builder.ToTable("position_valuations");
        builder.HasKey(valuation => valuation.Id);
        builder.Property(valuation => valuation.Id).HasColumnName("id");
        builder.Property(valuation => valuation.PosicaoId).HasColumnName("position_id").IsRequired();
        builder.Property(valuation => valuation.DataObservadaUtc).HasColumnName("observed_at_utc").IsRequired();
        builder.Property(valuation => valuation.PrecoUnitario).HasColumnName("unit_price")
            .HasColumnType("numeric(18,8)").IsRequired();
        builder.Property(valuation => valuation.QuantidadeObservada).HasColumnName("observed_quantity")
            .HasColumnType("numeric(18,8)").IsRequired();
        builder.Property(valuation => valuation.RegistradaEmUtc).HasColumnName("recorded_at_utc").IsRequired();
        builder.HasIndex(valuation => new
            { valuation.PosicaoId, valuation.DataObservadaUtc, valuation.RegistradaEmUtc, valuation.Id })
            .HasDatabaseName("idx_position_valuations_latest");
        builder.HasOne<PosicaoInvestimento>().WithMany().HasForeignKey(valuation => valuation.PosicaoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
