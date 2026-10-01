using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class TransacaoConfiguration : IEntityTypeConfiguration<Transacao>
{
    public void Configure(EntityTypeBuilder<Transacao> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.CarteiraId)
            .HasColumnName("portfolio_id")
            .IsRequired();

        builder.Property(t => t.TitularId)
            .HasColumnName("holder_profile_id");

        builder.Property(t => t.AtivoId)
            .HasColumnName("asset_id");

        builder.Property(t => t.Ticker)
            .HasColumnName("ticker")
            .HasMaxLength(20);

        builder.Property(t => t.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(t => t.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(t => t.BrokerageFee)
            .HasColumnName("brokerage_fee")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(t => t.IdempotencyKey)
            .HasColumnName("idempotency_key");

        builder.Property(t => t.RealizedGain)
            .HasColumnName("realized_gain")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(t => t.RealizedCostBasis)
            .HasColumnName("realized_cost_basis")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(t => t.ModalidadeFiscal)
            .HasColumnName("tax_modality")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ModalidadeFiscal.NaoInformada)
            .IsRequired();

        builder.Property(t => t.TransactionDate)
            .HasColumnName("transaction_date")
            .IsRequired();

        builder.Property(t => t.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("idx_transactions_user_id");

        builder.HasIndex(t => new { t.UserId, t.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("ux_transactions_user_idempotency_key");

        builder.HasOne<Ativo>()
            .WithMany()
            .HasForeignKey(t => t.AtivoId)
            .OnDelete(DeleteBehavior.Restrict); // restrict so assets aren't accidentally deleted if transactions exist

        builder.HasOne<Carteira>()
            .WithMany()
            .HasForeignKey(t => t.CarteiraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TitularCarteira>()
            .WithMany()
            .HasForeignKey(t => t.TitularId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TitularId).HasDatabaseName("idx_transactions_holder_profile_id");
    }
}
