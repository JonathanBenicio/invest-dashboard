using InvestDashboard.Domain.Aggregates.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public sealed class RevisaoSnapshotTesouroConfiguration : IEntityTypeConfiguration<RevisaoSnapshotTesouro>
{
    public void Configure(EntityTypeBuilder<RevisaoSnapshotTesouro> builder)
    {
        builder.ToTable("treasury_snapshot_reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CarteiraId).HasColumnName("portfolio_id").IsRequired();
        builder.Property(x => x.LoteId).HasColumnName("batch_id").IsRequired();
        builder.Property(x => x.AutorId).HasColumnName("author_id").IsRequired();
        builder.Property(x => x.AtivoId).HasColumnName("asset_id").IsRequired();
        builder.Property(x => x.HashComparacao).HasColumnName("comparison_hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.DivergenciasJson).HasColumnName("divergences_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Decisao).HasColumnName("decision").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CarteiraVersao).HasColumnName("portfolio_version").IsRequired();
        builder.Property(x => x.CriadoEmUtc).HasColumnName("created_at_utc").IsRequired();
        builder.HasOne<LoteImportacaoTesouro>().WithMany()
            .HasForeignKey(x => new { x.LoteId, x.CarteiraId })
            .HasPrincipalKey(x => new { x.Id, x.CarteiraId }).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.LoteId, x.HashComparacao }).IsUnique()
            .HasDatabaseName("ux_treasury_snapshot_review_hash");
        builder.HasIndex(x => new { x.CarteiraId, x.LoteId, x.CriadoEmUtc })
            .HasDatabaseName("ix_treasury_snapshot_review_audit");
    }
}
