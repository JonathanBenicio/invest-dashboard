using InvestDashboard.Domain.Aggregates.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public sealed class DecisaoConciliacaoTesouroConfiguration : IEntityTypeConfiguration<DecisaoConciliacaoTesouro>
{
    public void Configure(EntityTypeBuilder<DecisaoConciliacaoTesouro> builder)
    {
        builder.ToTable("treasury_reconciliation_decisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CarteiraId).HasColumnName("portfolio_id").IsRequired();
        builder.Property(x => x.LoteId).HasColumnName("batch_id").IsRequired();
        builder.Property(x => x.AutorId).HasColumnName("author_id").IsRequired();
        builder.Property(x => x.AtivoId).HasColumnName("asset_id");
        builder.Property(x => x.IndiceEvento).HasColumnName("event_index").IsRequired();
        builder.Property(x => x.Revisao).HasColumnName("revision").IsRequired();
        builder.Property(x => x.Decisao).HasColumnName("decision").HasMaxLength(40).IsRequired();
        builder.Property(x => x.TransacaoId).HasColumnName("transaction_id");
        builder.Property(x => x.CarteiraVersao).HasColumnName("portfolio_version").IsRequired();
        builder.Property(x => x.CriadoEmUtc).HasColumnName("created_at_utc").IsRequired();
        builder.HasOne<LoteImportacaoTesouro>().WithMany()
            .HasForeignKey(x => new { x.LoteId, x.CarteiraId })
            .HasPrincipalKey(x => new { x.Id, x.CarteiraId }).OnDelete(DeleteBehavior.Cascade);
        // Identificadores são referências históricas, não FKs: uma revisão
        // pendente não pode bloquear exclusão de ativo ou transação. O serviço
        // revalida a existência e a titularidade antes de usar uma revisão.
        builder.HasIndex(x => new { x.LoteId, x.IndiceEvento, x.Revisao }).IsUnique()
            .HasDatabaseName("ux_treasury_reconciliation_decision_revision");
        builder.HasIndex(x => new { x.LoteId, x.IndiceEvento, x.CriadoEmUtc })
            .HasDatabaseName("ix_treasury_reconciliation_decision_audit");
    }
}
