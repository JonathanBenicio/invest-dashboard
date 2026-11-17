using FluentAssertions;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class DecisaoConciliacaoTesouroPersistenceTests
{
    [RequiresPostgresFact]
    public async Task RevisoesSaoAuditaveisSemVincularLoteDeOutraCarteira()
    {
        await using var database = await IsolatedPostgresDatabase.CreateAsync(Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!);
        var options = new DbContextOptionsBuilder<InvestDbContext>().UseNpgsql(database.ConnectionString).Options;
        var carteira = Guid.NewGuid(); var outraCarteira = Guid.NewGuid(); var autor = Guid.NewGuid(); var lote = Guid.NewGuid();
        await using (var db = new InvestDbContext(options))
        {
            await db.Database.MigrateAsync();
            db.Portfolios.AddRange(new Carteira(carteira, autor.ToString(), "Sintética"), new Carteira(outraCarteira, autor.ToString(), "Outra"));
            db.Set<LoteImportacaoTesouro>().Add(new LoteImportacaoTesouro(lote, carteira, autor, new DateOnly(2026, 1, 2), new string('a', 64), "html", "{}", DateTime.UtcNow));
            await db.SaveChangesAsync();
            db.Set<DecisaoConciliacaoTesouro>().Add(new DecisaoConciliacaoTesouro(Guid.NewGuid(), carteira, lote, autor, null, 0, 1, "sem_vinculo", null, 1, DateTime.UtcNow));
            db.Set<DecisaoConciliacaoTesouro>().Add(new DecisaoConciliacaoTesouro(Guid.NewGuid(), carteira, lote, autor, Guid.NewGuid(), 0, 2, "vincular", Guid.NewGuid(), 1, DateTime.UtcNow));
            await db.SaveChangesAsync();
            (await db.Set<DecisaoConciliacaoTesouro>().CountAsync()).Should().Be(2);
        }
        await using (var db = new InvestDbContext(options))
        {
            db.Set<DecisaoConciliacaoTesouro>().Add(new DecisaoConciliacaoTesouro(Guid.NewGuid(), carteira, lote, autor, null, 0, 2, "sem_vinculo", null, 1, DateTime.UtcNow));
            await FluentActions.Invoking(() => db.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
        }
        await using (var db = new InvestDbContext(options))
        {
            db.Set<DecisaoConciliacaoTesouro>().Add(new DecisaoConciliacaoTesouro(Guid.NewGuid(), outraCarteira, lote, autor, null, 1, 1, "sem_vinculo", null, 1, DateTime.UtcNow));
            await FluentActions.Invoking(() => db.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
        }
    }
}
