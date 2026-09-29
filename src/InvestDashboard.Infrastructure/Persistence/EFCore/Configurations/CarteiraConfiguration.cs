using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class CarteiraConfiguration : IEntityTypeConfiguration<Carteira>
{
    public void Configure(EntityTypeBuilder<Carteira> builder)
    {
        builder.ToTable("portfolios");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.Version)
            .HasColumnName("version")
            .HasDefaultValue(0)
            .IsConcurrencyToken();

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(p => p.Balance)
            .HasColumnName("balance")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(p => p.RealizedGain)
            .HasColumnName("realized_gain")
            .HasColumnType("numeric(18,4)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(p => p.RealizedCostBasis)
            .HasColumnName("realized_cost_basis")
            .HasColumnType("numeric(18,4)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("idx_portfolios_user_id");

        // Map private collection field to the navigation property
        builder.HasMany(p => p.Positions)
            .WithOne()
            .HasForeignKey(pos => pos.CarteiraId)
            .OnDelete(DeleteBehavior.Cascade);

        var navigation = builder.Metadata.FindNavigation(nameof(Carteira.Positions));
        navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
