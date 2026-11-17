using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class ConfirmacaoTesouroControllerTests
{
    [RequiresPostgresFact]
    public async Task Confirmacao_RevalidaRevisoesEPreservaMovimentacaoPosteriorSemDuplicarEventos()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        using var outro = factory.CreateClient();
        outro.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
                FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName));

        var carteiraId = Guid.NewGuid();
        var ativoId = Guid.NewGuid();
        var loteId = Guid.NewGuid();
        var manualId = Guid.NewGuid();
        var posteriorId = Guid.NewGuid();
        var usuario = FakeAuthProvider.TestUserId;
        var referencia = new DateOnly(2024, 3, 3);
        const string titulo = "Tesouro Prefixado 2035";
        int versao;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            var carteira = new Carteira(carteiraId, usuario.ToString(), "Carteira sintética");
            var ativo = new RendaFixa(ativoId, "TESOURO_TESTE", titulo, 20m, DateTime.UtcNow, "PREFIXADO", 10m,
                new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Tesouro", "TESOURO_DIRETO");
            var manual = new Transacao(manualId, usuario.ToString(), carteiraId, ativoId, ativo.Ticker,
                TipoTransacao.Buy, 1m, 20m, 0m, new DateTime(2024, 3, 2, 12, 0, 0, DateTimeKind.Utc));
            var posterior = new Transacao(posteriorId, usuario.ToString(), carteiraId, ativoId, ativo.Ticker,
                TipoTransacao.Buy, 1m, 30m, 0m, new DateTime(2024, 3, 4, 12, 0, 0, DateTimeKind.Utc));
            carteira.ProcessTransaction(manual, 20m, TipoAtivo.RendaFixa);
            carteira.ProcessTransaction(posterior, 30m, TipoAtivo.RendaFixa);
            versao = carteira.Version;

            var diaCompra = new DateOnly(2024, 3, 2);
            var posicao = new DadosPosicaoTesouroDto(diaCompra, 1m, 20m, 21m, "10%", null, null, 22m,
                1, null, null, null, null, null, 22m, []);
            var compra = new DadosMovimentoTesouroDto(diaCompra, "Compra", "Crédito", 1m, 20m, null, 20m, []);
            var taxa = new DadosMovimentoTesouroDto(diaCompra, "Taxas", "Débito", null, null, null, 1m, [],
                ["quantidade", "preco", "taxaAnual"]);
            var previa = new PreviaTesouroDto(carteiraId, "html", "portal_tesouro_html", titulo, "2035-01-01", null,
                [new(1, new Dictionary<string, string>(), posicao)], [new([], compra), new([], taxa)],
                [], [], true, false, false);
            var lote = new LoteImportacaoTesouro(loteId, carteiraId, usuario, referencia, new string('a', 64),
                "html", JsonSerializer.Serialize(previa), DateTime.UtcNow);
            db.AddRange(carteira, ativo, manual, posterior, lote);
            await db.SaveChangesAsync();
        }

        var rota = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteId}";
        var revisoes = new[] { new { indiceEvento = 0, revisao = 1 }, new { indiceEvento = 1, revisao = 1 } };
        var pedido = new { ativoId, carteiraVersao = versao, revisoes };
        using var acessoIndevido = await outro.PostAsJsonAsync($"{rota}/confirmacao", pedido);
        acessoIndevido.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var semRevisoes = await client.PostAsJsonAsync($"{rota}/confirmacao", pedido);
        semRevisoes.IsSuccessStatusCode.Should().BeFalse();
        await VerificarPendente();

        using var revisaoCompra = await client.PostAsJsonAsync($"{rota}/revisoes",
            new { indiceEvento = 0, decisao = "vincular", ativoId, transacaoId = manualId, carteiraVersao = versao, revisaoAnterior = 0 });
        revisaoCompra.StatusCode.Should().Be(HttpStatusCode.OK);
        using var revisaoTaxa = await client.PostAsJsonAsync($"{rota}/revisoes",
            new { indiceEvento = 1, decisao = "sem_vinculo", ativoId, carteiraVersao = versao, revisaoAnterior = 0 });
        revisaoTaxa.StatusCode.Should().Be(HttpStatusCode.OK);
        using var versaoObsoleta = await client.PostAsJsonAsync($"{rota}/confirmacao",
            new { ativoId, carteiraVersao = versao + 1, revisoes });
        versaoObsoleta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var revisaoObsoleta = await client.PostAsJsonAsync($"{rota}/confirmacao",
            new { ativoId, carteiraVersao = versao, revisoes = new[] { revisoes[0], new { indiceEvento = 1, revisao = 2 } } });
        revisaoObsoleta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await VerificarPendente();

        var simultaneas = await Task.WhenAll(client.PostAsJsonAsync($"{rota}/confirmacao", pedido),
            client.PostAsJsonAsync($"{rota}/confirmacao", pedido));
        try
        {
            var respostas = await Task.WhenAll(simultaneas.Select(r => r.Content.ReadAsStringAsync()));
            var detalhe = string.Join(" | ", respostas);
            simultaneas.Select(r => r.StatusCode).Should().Contain(HttpStatusCode.OK, detalhe);
            simultaneas.Select(r => r.StatusCode).Should().OnlyContain(
                status => status == HttpStatusCode.OK || status == HttpStatusCode.Conflict, detalhe);
        }
        finally
        {
            foreach (var resposta in simultaneas) resposta.Dispose();
        }
        using var repeticao = await client.PostAsJsonAsync($"{rota}/confirmacao", pedido);
        repeticao.StatusCode.Should().Be(HttpStatusCode.OK, await repeticao.Content.ReadAsStringAsync());

        using var verificacao = factory.Services.CreateScope();
        var persistido = verificacao.ServiceProvider.GetRequiredService<InvestDbContext>();
        var loteConfirmado = await persistido.Set<LoteImportacaoTesouro>().SingleAsync(x => x.Id == loteId);
        loteConfirmado.Estado.Should().Be("Confirmado");
        var eventos = await persistido.Set<EventoOficialTesouro>().Where(x => x.LoteId == loteId)
            .OrderBy(x => x.IndiceEvento).ToListAsync();
        eventos.Select(x => x.TransacaoId).Should().Equal(manualId, null);
        var saldo = await persistido.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == loteId);
        saldo.Ativa.Should().BeTrue();
        saldo.DataReferencia.Should().Be(referencia);
        saldo.Quantidade.Should().Be(1m);
        (await persistido.Transactions.Select(t => t.Id).ToArrayAsync()).Should().BeEquivalentTo([manualId, posteriorId]);
        var carteiraConfirmada = await persistido.Portfolios.Include(c => c.Positions).SingleAsync(c => c.Id == carteiraId);
        var posicaoConfirmada = carteiraConfirmada.Positions.Single(p => p.AtivoId == ativoId);
        posicaoConfirmada.Quantity.Should().Be(2m);
        posicaoConfirmada.TotalCost.Should().Be(51m);

        async Task VerificarPendente()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            (await db.Set<LoteImportacaoTesouro>().SingleAsync(x => x.Id == loteId)).Estado.Should().Be("Pendente");
            (await db.Set<EventoOficialTesouro>().AnyAsync(x => x.LoteId == loteId)).Should().BeFalse();
            (await db.Set<BasePosicaoTesouro>().AnyAsync(x => x.LoteId == loteId)).Should().BeFalse();
        }
    }
}
