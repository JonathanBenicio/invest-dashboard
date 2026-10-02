using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class TaxaEconomicaConfiguration : IEntityTypeConfiguration<TaxaEconomica>
{
    public void Configure(EntityTypeBuilder<TaxaEconomica> builder)
    {
        builder.ToTable("economic_rates");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.CurrentValue)
            .HasColumnName("current_value")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(r => r.PreviousValue)
            .HasColumnName("previous_value")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.Source)
            .HasColumnName("source")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.LastUpdate)
            .HasColumnName("last_update")
            .IsRequired();

        builder.Property(r => r.GroupId).HasColumnName("group_id");
        builder.Property(r => r.Unit).HasColumnName("unit").HasMaxLength(80).IsRequired();
        builder.Property(r => r.Periodicity).HasColumnName("periodicity").HasMaxLength(80).IsRequired();
        builder.Property(r => r.ReferenceDate).HasColumnName("reference_date").IsRequired();
        builder.Property(r => r.UpdatedByUserId).HasColumnName("updated_by_user_id").HasMaxLength(100).IsRequired();

        builder.HasIndex(r => new { r.GroupId, r.Symbol }).HasDatabaseName("idx_economic_rates_group_symbol");
        builder.HasOne<GrupoCarteiras>().WithMany().HasForeignKey(r => r.GroupId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TaxaEconomicaHistoricoConfiguration : IEntityTypeConfiguration<TaxaEconomicaHistorico>
{
    public void Configure(EntityTypeBuilder<TaxaEconomicaHistorico> builder)
    {
        builder.ToTable("economic_rate_history");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.TaxaId).HasColumnName("rate_id").IsRequired();
        builder.Property(item => item.ValorAnterior).HasColumnName("previous_value").HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(item => item.ValorNovo).HasColumnName("new_value").HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(item => item.UnidadeAnterior).HasColumnName("previous_unit").HasMaxLength(80);
        builder.Property(item => item.UnidadeNova).HasColumnName("new_unit").HasMaxLength(80);
        builder.Property(item => item.PeriodicidadeAnterior).HasColumnName("previous_periodicity").HasMaxLength(80);
        builder.Property(item => item.PeriodicidadeNova).HasColumnName("new_periodicity").HasMaxLength(80);
        builder.Property(item => item.Origem).HasColumnName("source").HasMaxLength(100).IsRequired();
        builder.Property(item => item.DataReferencia).HasColumnName("reference_date").IsRequired();
        builder.Property(item => item.ResponsavelUserId).HasColumnName("updated_by_user_id").HasMaxLength(100).IsRequired();
        builder.Property(item => item.AtualizadoEmUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasIndex(item => new { item.TaxaId, item.AtualizadoEmUtc }).HasDatabaseName("idx_economic_rate_history_rate_date");
        builder.HasOne<TaxaEconomica>().WithMany().HasForeignKey(item => item.TaxaId).OnDelete(DeleteBehavior.Cascade);
    }
}
