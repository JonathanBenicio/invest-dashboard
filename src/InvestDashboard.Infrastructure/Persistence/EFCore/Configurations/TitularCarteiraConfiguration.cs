using InvestDashboard.Domain.Aggregates.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public sealed class TitularCarteiraConfiguration : IEntityTypeConfiguration<TitularCarteira>
{
    public void Configure(EntityTypeBuilder<TitularCarteira> builder)
    {
        builder.ToTable("portfolio_holders");
        builder.HasKey(holder => holder.Id);
        builder.Property(holder => holder.Id).HasColumnName("id");
        builder.Property(holder => holder.GrupoId).HasColumnName("group_id").IsRequired();
        builder.Property(holder => holder.Nome).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(holder => holder.NomeNormalizado).HasColumnName("normalized_name").HasMaxLength(160).IsRequired();
        builder.Property(holder => holder.UsuarioId).HasColumnName("user_id").HasMaxLength(100);
        builder.Property(holder => holder.Parentesco).HasColumnName("relationship").HasMaxLength(80);
        builder.Property(holder => holder.CriadoEmUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(holder => holder.AtualizadoEmUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasIndex(holder => new { holder.GrupoId, holder.NomeNormalizado })
            .HasFilter("user_id IS NULL").HasDatabaseName("idx_portfolio_holders_group_unlinked_name");
        builder.HasIndex(holder => new { holder.GrupoId, holder.UsuarioId })
            .IsUnique().HasFilter("user_id IS NOT NULL").HasDatabaseName("ux_portfolio_holders_group_user");
        builder.HasOne<GrupoCarteiras>().WithMany().HasForeignKey(holder => holder.GrupoId).OnDelete(DeleteBehavior.Cascade);
    }
}
