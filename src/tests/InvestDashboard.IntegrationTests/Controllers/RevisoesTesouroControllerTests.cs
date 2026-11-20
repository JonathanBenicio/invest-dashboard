using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
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

public sealed class RevisoesTesouroControllerTests
{
    [RequiresPostgresFact]
    public async Task HistoricoImutavelEvitaAssociacaoDuplaEProtegeCarteira()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        using var outro = factory.CreateClient();
        outro.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeAuthProvider.GenerateJwtForUser(FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId, FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName));
        var carteiraId = Guid.NewGuid(); var ativoId = Guid.NewGuid(); var loteId = Guid.NewGuid(); var manualId = Guid.NewGuid(); var outroManualId = Guid.NewGuid(); var terceiroManualId = Guid.NewGuid();
        int versao;
        string hashPrevia;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            var carteira = new Carteira(carteiraId, FakeAuthProvider.TestUserId.ToString(), "Carteira sintética");
            var ativo = new RendaFixa(ativoId, "TESTE01", "Título sintético", 20m, DateTime.UtcNow, "PREFIXADO", 10m, DateTime.UtcNow.AddYears(5), "Tesouro");
            var manual = new Transacao(manualId, FakeAuthProvider.TestUserId.ToString(), carteiraId, ativoId, ativo.Ticker, TipoTransacao.Buy, 1m, 20m, 0m, new DateTime(2024, 3, 2, 12, 0, 0, DateTimeKind.Utc));
            var outroManual = new Transacao(outroManualId, FakeAuthProvider.TestUserId.ToString(), carteiraId, ativoId, ativo.Ticker, TipoTransacao.Buy, 1m, 20m, 0m, new DateTime(2024, 3, 2, 12, 0, 0, DateTimeKind.Utc));
            var terceiroManual = new Transacao(terceiroManualId, FakeAuthProvider.TestUserId.ToString(), carteiraId, ativoId, ativo.Ticker, TipoTransacao.Buy, 1m, 20m, 0m, new DateTime(2024, 3, 2, 12, 0, 0, DateTimeKind.Utc));
            carteira.ProcessTransaction(manual, 20m, TipoAtivo.RendaFixa);
            carteira.ProcessTransaction(outroManual, 20m, TipoAtivo.RendaFixa);
            carteira.ProcessTransaction(terceiroManual, 20m, TipoAtivo.RendaFixa);
            versao = carteira.Version;
            var movimento = new DadosMovimentoTesouroDto(new DateOnly(2024, 3, 2), "Compra", "Crédito", 1m, 20m, null, 20m, []);
            var taxa = new DadosMovimentoTesouroDto(new DateOnly(2024, 3, 2), "Taxas", "Débito", null, null, null, 1m, []);
            var previa = new PreviaTesouroDto(carteiraId, "html", "teste", "Título", "2035-01-01", null, [],
            [new([], movimento), new([], movimento), new([], taxa), new([], movimento), new([], movimento),
             new([], movimento with { Data = null }), new([], movimento with { Data = new DateOnly(2024, 3, 4) }),
             new([], movimento with { Quantidade = null }), new([], movimento with { Preco = null }),
             new([], movimento with { CamposNaoInterpretados = ["natureza"] }),
             new([], movimento with { Data = new DateOnly(2024, 3, 1), Quantidade = 2m, Preco = 30m })], [], [], true, false, false);
            var previaJson = JsonSerializer.Serialize(previa);
            hashPrevia = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(previaJson))).ToLowerInvariant();
            var lote = new LoteImportacaoTesouro(loteId, carteiraId, FakeAuthProvider.TestUserId, new DateOnly(2024, 3, 3), new string('a', 64), "html", previaJson, DateTime.UtcNow);
            db.AddRange(carteira, ativo, manual, outroManual, terceiroManual, lote);
            await db.SaveChangesAsync();
        }
        var url = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteId}/revisoes";
        object Revisao(int indice, string decisao, Guid? transacao, int anterior, int? carteiraVersao = null) => new
        { indiceEvento = indice, decisao, ativoId, transacaoId = transacao, carteiraVersao = carteiraVersao ?? versao, revisaoAnterior = anterior, hashPrevia };
        (await outro.PostAsJsonAsync(url, Revisao(0, "vincular", manualId, 0))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync(url, Revisao(0, "vincular", manualId, 0, versao + 1))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsJsonAsync(url, Revisao(2, "vincular", manualId, 0))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        foreach (var indice in new[] { 5, 6, 7, 8, 9 })
            (await client.PostAsJsonAsync(url, Revisao(indice, "vincular", terceiroManualId, 0))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync(url, Revisao(5, "sem_vinculo", null, 0))).StatusCode.Should().Be(HttpStatusCode.OK);
        using var divergente = await client.PostAsJsonAsync(url, Revisao(10, "vincular", terceiroManualId, 0));
        divergente.StatusCode.Should().Be(HttpStatusCode.OK, await divergente.Content.ReadAsStringAsync());
        using (var jsonDivergente = JsonDocument.Parse(await divergente.Content.ReadAsStringAsync()))
            jsonDivergente.RootElement.GetProperty("dados").GetProperty("divergencias").EnumerateArray().Select(x => x.GetString())
                .Should().BeEquivalentTo(["data", "quantidade", "preco"]);
        using var historicoDivergente = await client.GetAsync($"{url}/10");
        using (var jsonHistoricoDivergente = JsonDocument.Parse(await historicoDivergente.Content.ReadAsStringAsync()))
            jsonHistoricoDivergente.RootElement.GetProperty("dados")[0].GetProperty("divergencias").EnumerateArray().Select(x => x.GetString())
                .Should().BeEquivalentTo(["data", "quantidade", "preco"]);
        using var primeira = await client.PostAsJsonAsync(url, Revisao(0, "vincular", manualId, 0));
        primeira.StatusCode.Should().Be(HttpStatusCode.OK, await primeira.Content.ReadAsStringAsync());
        primeira.Headers.CacheControl!.NoStore.Should().BeTrue();
        (await client.PostAsJsonAsync(url, Revisao(0, "vincular", manualId, 0))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsJsonAsync(url, Revisao(1, "vincular", manualId, 0))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsJsonAsync(url, Revisao(0, "sem_vinculo", null, 1))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync(url, Revisao(1, "vincular", manualId, 0))).StatusCode.Should().Be(HttpStatusCode.OK);
        var simultaneas = await Task.WhenAll(client.PostAsJsonAsync(url, Revisao(3, "vincular", outroManualId, 0)),
            client.PostAsJsonAsync(url, Revisao(4, "vincular", outroManualId, 0)));
        var descricoes = await Task.WhenAll(simultaneas.Select(r => r.Content.ReadAsStringAsync()));
        simultaneas.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict], string.Join(" | ", descricoes));
        foreach (var item in simultaneas) item.Dispose();
        using var respostaHistorico = await client.GetAsync($"{url}/0");
        using var historico = JsonDocument.Parse(await respostaHistorico.Content.ReadAsStringAsync());
        historico.RootElement.GetProperty("dados").GetArrayLength().Should().Be(2);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            (await db.Set<DecisaoConciliacaoTesouro>().CountAsync(x => x.LoteId == loteId)).Should().Be(6);
            (await db.Set<LoteImportacaoTesouro>().SingleAsync(x => x.Id == loteId)).Estado.Should().Be("Pendente");
            (await db.Transactions.SingleAsync(x => x.Id == manualId)).Quantity.Should().Be(1m);
        }
    }
}
