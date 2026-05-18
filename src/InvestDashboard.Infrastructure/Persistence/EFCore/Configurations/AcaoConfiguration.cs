using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InvestDashboard.Domain.Aggregates.MarketData;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public class AcaoConfiguration : IEntityTypeConfiguration<Acao>
{
    public void Configure(EntityTypeBuilder<Acao> builder)
    {
        builder.Property(s => s.Sector)
            .HasColumnName("sector")
            .HasMaxLength(100);
    }
}
