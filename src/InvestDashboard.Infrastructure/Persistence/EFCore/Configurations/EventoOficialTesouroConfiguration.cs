using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.MarketData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public sealed class EventoOficialTesouroConfiguration : IEntityTypeConfiguration<EventoOficialTesouro>
{
    public void Configure(EntityTypeBuilder<EventoOficialTesouro> builder)
    {
        builder.ToTable("treasury_official_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LoteId).HasColumnName("batch_id").IsRequired();
        builder.Property(x => x.CarteiraId).HasColumnName("portfolio_id").IsRequired();
        builder.Property(x => x.AtivoId).HasColumnName("asset_id").IsRequired();
        builder.Property(x => x.IndiceEvento).HasColumnName("event_index").IsRequired();
        builder.Property(x => x.EventoJson).HasColumnName("event_json").IsRequired();
        builder.Property(x => x.TransacaoId).HasColumnName("transaction_id");
        builder.Property(x => x.CriadoEmUtc).HasColumnName("created_at_utc").IsRequired();
        builder.HasOne<LoteImportacaoTesouro>().WithMany()
            .HasForeignKey(x => new { x.LoteId, x.CarteiraId })
            .HasPrincipalKey(x => new { x.Id, x.CarteiraId }).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Ativo>().WithMany().HasForeignKey(x => x.AtivoId).OnDelete(DeleteBehavior.Restrict);
        // Transação é referência histórica, revalidada pela confirmação; não
        // criar FK que destrua o evento ou impeça a revisão do histórico.
        builder.HasIndex(x => new { x.LoteId, x.IndiceEvento }).IsUnique().HasDatabaseName("ux_treasury_official_event_batch_index");
        builder.HasIndex(x => new { x.CarteiraId, x.AtivoId, x.LoteId }).HasDatabaseName("ix_treasury_official_event_portfolio_asset");
    }
}
