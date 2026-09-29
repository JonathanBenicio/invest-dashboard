using InvestDashboard.Domain.Aggregates.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public sealed class RefreshTokenSessionConfiguration : IEntityTypeConfiguration<RefreshTokenSession>
{
    public void Configure(EntityTypeBuilder<RefreshTokenSession> builder)
    {
        builder.ToTable("auth_sessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).HasColumnName("id");
        builder.Property(session => session.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(session => session.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(session => session.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(session => session.Role).HasColumnName("role").HasMaxLength(20).IsRequired();
        builder.Property(session => session.RefreshTokenHash).HasColumnName("refresh_token_hash").HasMaxLength(64).IsRequired();
        builder.Property(session => session.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(session => session.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(session => session.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.HasIndex(session => session.RefreshTokenHash).IsUnique().HasDatabaseName("ux_auth_sessions_refresh_token_hash");
        builder.HasIndex(session => new { session.UserId, session.ExpiresAtUtc }).HasDatabaseName("ix_auth_sessions_user_expiry");
    }
}
