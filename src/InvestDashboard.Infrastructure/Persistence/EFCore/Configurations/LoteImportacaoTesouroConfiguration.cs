using InvestDashboard.Domain.Aggregates.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public sealed class LoteImportacaoTesouroConfiguration : IEntityTypeConfiguration<LoteImportacaoTesouro>
{
    public void Configure(EntityTypeBuilder<LoteImportacaoTesouro> builder)
    {
        builder.ToTable("tesouro_import_batches");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.Id, x.CarteiraId });
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CarteiraId).HasColumnName("portfolio_id").IsRequired();
        builder.Property(x => x.UsuarioId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.DataReferencia).HasColumnName("reference_date").IsRequired();
        builder.Property(x => x.HashConteudo).HasColumnName("content_hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Formato).HasColumnName("format").HasMaxLength(8).IsRequired();
        builder.Property(x => x.PreviaJson).HasColumnName("preview_json").IsRequired();
        builder.Property(x => x.CriadoEmUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.Estado).HasColumnName("state").HasMaxLength(16).IsConcurrencyToken().IsRequired();
        builder.Property(x => x.ConfirmadoEmUtc).HasColumnName("confirmed_at_utc");
        builder.HasOne<Carteira>().WithMany().HasForeignKey(x => x.CarteiraId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CarteiraId, x.HashConteudo, x.DataReferencia }).IsUnique()
            .HasDatabaseName("ux_tesouro_import_batch_idempotency");
    }
}
