using FluentAssertions;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.Infrastructure.Persistence.RepositoryImpl;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class TesouroImportPersistenceTests
{
    [RequiresPostgresFact]
    public async Task Lotes_SaoPersistidosComIsolamentoDeAutorEChaveIdempotente()
    {
        await using var database = await IsolatedPostgresDatabase.CreateAsync(Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!);
        var options = new DbContextOptionsBuilder<InvestDbContext>().UseNpgsql(database.ConnectionString).Options;
        var carteira = Guid.NewGuid();
        var autor = Guid.NewGuid();
        var outro = Guid.NewGuid();
        var outraCarteira = Guid.NewGuid();
        var ativo = Guid.NewGuid();
        var data = new DateOnly(2026, 1, 2);
        var hash = new string('a', 64);
        var id = Guid.NewGuid();
        var criado = DateTime.UtcNow;
        await using (var db = new InvestDbContext(options))
        {
            await db.Database.MigrateAsync();
            db.Portfolios.Add(new Carteira(carteira, autor.ToString(), "Carteira sintética"));
            db.Portfolios.Add(new Carteira(outraCarteira, autor.ToString(), "Outra carteira sintética"));
            db.FixedIncomeAssets.Add(new RendaFixa(ativo, "TESOURO_TESTE", "Título sintético", 100, DateTime.UtcNow,
                "PREFIXADO", 10, DateTime.UtcNow.AddYears(5), "Tesouro"));
            await db.SaveChangesAsync();
            var repo = new RepositorioLoteImportacaoTesouro(db);
            await repo.AdicionarAsync(new LoteImportacaoTesouro(id, carteira, autor, data, hash, "html", "{}", criado));
            await repo.SalvarAsync();
            db.Set<EventoOficialTesouro>().AddRange(
                new EventoOficialTesouro(Guid.NewGuid(), id, carteira, ativo, 0, "{}", null, criado),
                new EventoOficialTesouro(Guid.NewGuid(), id, carteira, ativo, 1, "{}", null, criado));
            await db.SaveChangesAsync();
        }
        await using (var db = new InvestDbContext(options))
        {
            var eventos = new EventoOficialTesouroRepository(db);
            (await eventos.ListarAsync(carteira, id, 0, 1)).Should().ContainSingle().Which.IndiceEvento.Should().Be(1);
            (await eventos.ListarAsync(outraCarteira, id, -1, 100)).Should().BeEmpty();
            var repo = new RepositorioLoteImportacaoTesouro(db);
            (await repo.BuscarPorIdAsync(carteira, outro, id)).Should().BeNull();
            (await repo.ListarPorCarteiraAsync(carteira, outro)).Should().BeEmpty();
            (await repo.BuscarPorChaveAsync(carteira, autor, hash, data))!.Id.Should().Be(id);
            await repo.AdicionarAsync(new LoteImportacaoTesouro(Guid.NewGuid(), carteira, autor, data, hash, "html", "{}", criado));
            await FluentActions.Invoking(() => repo.SalvarAsync()).Should().ThrowAsync<DbUpdateException>();
        }
        await using (var db = new InvestDbContext(options))
        {
            var repo = new RepositorioLoteImportacaoTesouro(db);
            await repo.AdicionarAsync(new LoteImportacaoTesouro(Guid.NewGuid(), carteira, outro, data, hash, "html", "{}", criado));
            await FluentActions.Invoking(() => repo.SalvarAsync()).Should().ThrowAsync<DbUpdateException>();
        }
        await using (var db = new InvestDbContext(options))
        {
            var repo = new RepositorioLoteImportacaoTesouro(db);
            await repo.AdicionarAsync(new LoteImportacaoTesouro(Guid.NewGuid(), carteira, outro, data, new string('b', 64), "html", "{}", criado));
            await repo.SalvarAsync();
            (await repo.ListarPorCarteiraAsync(carteira, outro)).Should().ContainSingle();
        }
        await using (var db = new InvestDbContext(options))
        {
            db.Set<BasePosicaoTesouro>().Add(new BasePosicaoTesouro(Guid.NewGuid(), id, outraCarteira, ativo, data, 1, 100, 100));
            await FluentActions.Invoking(() => db.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
        }
    }
}
