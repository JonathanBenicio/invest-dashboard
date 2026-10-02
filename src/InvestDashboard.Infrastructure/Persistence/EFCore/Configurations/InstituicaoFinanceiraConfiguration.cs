using InvestDashboard.Domain.Aggregates.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestDashboard.Infrastructure.Persistence.EFCore.Configurations;

public sealed class InstituicaoFinanceiraConfiguration : IEntityTypeConfiguration<InstituicaoFinanceira>
{
    public void Configure(EntityTypeBuilder<InstituicaoFinanceira> builder)
    {
        builder.ToTable("financial_institutions");
        builder.HasKey(institution => institution.Id);
        builder.Property(institution => institution.Id).HasColumnName("id");
        builder.Property(institution => institution.Nome).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(institution => institution.NomeNormalizado).HasColumnName("normalized_name").HasMaxLength(120).IsRequired();
        builder.Property(institution => institution.Categoria).HasColumnName("category").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(institution => institution.GrupoId).HasColumnName("group_id");
        builder.Property(institution => institution.CriadoEmUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(institution => institution.AtualizadoEmUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasOne<GrupoCarteiras>().WithMany().HasForeignKey(institution => institution.GrupoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(institution => new { institution.GrupoId, institution.NomeNormalizado }).IsUnique()
            .HasFilter("group_id IS NOT NULL").HasDatabaseName("ux_financial_institutions_group_name");
        builder.HasIndex(institution => institution.NomeNormalizado).IsUnique()
            .HasFilter("group_id IS NULL").HasDatabaseName("ux_financial_institutions_global_name");

        builder.HasData(CatalogoInstituicoesFinanceiras.Entries);
    }
}

public static class CatalogoInstituicoesFinanceiras
{
    private static readonly DateTime SeedDate = new(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc);

    public static readonly InstituicaoFinanceira[] Entries =
    [
        Global("10000000-0000-4000-8000-000000000001", "Banco do Brasil", CategoriaInstituicaoFinanceira.Banco),
        Global("10000000-0000-4000-8000-000000000002", "Bradesco", CategoriaInstituicaoFinanceira.Banco),
        Global("10000000-0000-4000-8000-000000000003", "Caixa", CategoriaInstituicaoFinanceira.Banco),
        Global("10000000-0000-4000-8000-000000000004", "Itaú", CategoriaInstituicaoFinanceira.Banco),
        Global("10000000-0000-4000-8000-000000000005", "Nubank", CategoriaInstituicaoFinanceira.Banco),
        Global("10000000-0000-4000-8000-000000000006", "Santander", CategoriaInstituicaoFinanceira.Banco),
        Global("10000000-0000-4000-8000-000000000007", "Banco Inter", CategoriaInstituicaoFinanceira.Banco),
        Global("10000000-0000-4000-8000-000000000008", "Ágora", CategoriaInstituicaoFinanceira.Corretora),
        Global("10000000-0000-4000-8000-000000000009", "Clear", CategoriaInstituicaoFinanceira.Corretora),
        Global("10000000-0000-4000-8000-000000000010", "Genial", CategoriaInstituicaoFinanceira.Corretora),
        Global("10000000-0000-4000-8000-000000000011", "Nu Invest", CategoriaInstituicaoFinanceira.Corretora),
        Global("10000000-0000-4000-8000-000000000012", "Rico", CategoriaInstituicaoFinanceira.Corretora),
        Global("10000000-0000-4000-8000-000000000013", "XP Investimentos", CategoriaInstituicaoFinanceira.Corretora),
        Global("10000000-0000-4000-8000-000000000014", "BTG Pactual DTVM", CategoriaInstituicaoFinanceira.DTVM),
        Global("10000000-0000-4000-8000-000000000015", "BTG Pactual", CategoriaInstituicaoFinanceira.Banco)
    ];

    private static InstituicaoFinanceira Global(string id, string nome, CategoriaInstituicaoFinanceira categoria) =>
        new(Guid.Parse(id), nome, categoria, null, SeedDate);
}
