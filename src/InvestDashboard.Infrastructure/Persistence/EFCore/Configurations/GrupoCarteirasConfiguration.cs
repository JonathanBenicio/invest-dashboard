using InvestDashboard.Domain.Aggregates.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public sealed class GrupoCarteirasConfiguration : IEntityTypeConfiguration<GrupoCarteiras>
{
    public void Configure(EntityTypeBuilder<GrupoCarteiras> builder)
    {
        builder.ToTable("portfolio_groups");
        builder.HasKey(group => group.Id);
        builder.Property(group => group.Id).HasColumnName("id");
        builder.Property(group => group.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(group => group.CreatedByUserId).HasColumnName("created_by_user_id").HasMaxLength(100).IsRequired();
        builder.Property(group => group.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.HasIndex(group => group.CreatedByUserId).HasDatabaseName("idx_portfolio_groups_created_by");
    }
}

public sealed class MembroGrupoConfiguration : IEntityTypeConfiguration<MembroGrupo>
{
    public void Configure(EntityTypeBuilder<MembroGrupo> builder)
    {
        builder.ToTable("portfolio_group_members");
        builder.HasKey(member => member.Id);
        builder.Property(member => member.Id).HasColumnName("id");
        builder.Property(member => member.GrupoId).HasColumnName("group_id").IsRequired();
        builder.Property(member => member.UsuarioId).HasColumnName("user_id").HasMaxLength(100).IsRequired();
        builder.Property(member => member.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(member => member.Nome).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(member => member.Papel).HasColumnName("role").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(member => member.Ativo).HasColumnName("active").IsRequired();
        builder.Property(member => member.IngressouEmUtc).HasColumnName("joined_at_utc").IsRequired();
        builder.HasIndex(member => new { member.GrupoId, member.UsuarioId }).IsUnique().HasDatabaseName("ux_group_member_user");
        builder.HasIndex(member => new { member.GrupoId, member.Ativo }).HasDatabaseName("idx_group_members_active");
        builder.HasOne<GrupoCarteiras>().WithMany().HasForeignKey(member => member.GrupoId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ConviteGrupoConfiguration : IEntityTypeConfiguration<ConviteGrupo>
{
    public void Configure(EntityTypeBuilder<ConviteGrupo> builder)
    {
        builder.ToTable("portfolio_group_invitations");
        builder.HasKey(invite => invite.Id);
        builder.Property(invite => invite.Id).HasColumnName("id");
        builder.Property(invite => invite.GrupoId).HasColumnName("group_id").IsRequired();
        builder.Property(invite => invite.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(invite => invite.Papel).HasColumnName("role").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(invite => invite.CriadoPorUsuarioId).HasColumnName("invited_by_user_id").HasMaxLength(100).IsRequired();
        builder.Property(invite => invite.CriadoEmUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(invite => invite.ExpiraEmUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(invite => invite.AceitoEmUtc).HasColumnName("accepted_at_utc");
        builder.HasIndex(invite => new { invite.GrupoId, invite.Email, invite.AceitoEmUtc }).HasDatabaseName("idx_group_invitations_pending");
        builder.HasOne<GrupoCarteiras>().WithMany().HasForeignKey(invite => invite.GrupoId).OnDelete(DeleteBehavior.Cascade);
    }
}
