using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public sealed class BasePosicaoTesouroConfiguration : IEntityTypeConfiguration<BasePosicaoTesouro>
{
    public void Configure(EntityTypeBuilder<BasePosicaoTesouro> b)
    {
        b.ToTable("treasury_position_bases");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.LoteId).HasColumnName("batch_id").IsRequired();
        b.Property(x => x.CarteiraId).HasColumnName("portfolio_id").IsRequired();
        b.Property(x => x.AtivoId).HasColumnName("asset_id").IsRequired();
        b.Property(x => x.DataReferencia).HasColumnName("reference_date").IsRequired();
        b.Property(x => x.Quantidade).HasColumnName("quantity").HasColumnType("numeric(18,8)").IsRequired();
        b.Property(x => x.CustoTotal).HasColumnName("total_cost").HasColumnType("numeric(18,8)").IsRequired();
        b.Property(x => x.PrecoUnitario).HasColumnName("unit_price").HasColumnType("numeric(18,8)").IsRequired();
        b.Property(x => x.Ativa).HasColumnName("active").IsRequired();
        b.HasIndex(x => new { x.CarteiraId, x.AtivoId }).IsUnique().HasFilter("active = TRUE")
            .HasDatabaseName("ux_treasury_position_base_active");
        b.HasOne<Carteira>().WithMany().HasForeignKey(x => x.CarteiraId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Ativo>().WithMany().HasForeignKey(x => x.AtivoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LoteImportacaoTesouro>().WithMany().HasForeignKey(x => new { x.LoteId, x.CarteiraId })
            .HasPrincipalKey(x => new { x.Id, x.CarteiraId }).OnDelete(DeleteBehavior.Cascade);
    }
}
