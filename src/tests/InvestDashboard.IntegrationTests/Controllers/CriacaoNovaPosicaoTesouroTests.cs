using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Services;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class CriacaoNovaPosicaoTesouroTests
{
    private const string Html = """
        <div class="td-custom-meus-investimentos-titulo">
          <h1 class="td-meus-investimentos-detalhe__titulo_card">Tesouro Prefixado 2035</h1>
          <table><tbody><tr class="saldo-table-vencimento"><th><strong>VENCIMENTO: 01/01/2035</strong></th></tr></tbody></table>
          <div class="td-posicao-detalhada__info">
            <p class="td-posicao-detalhada__info__valor">Data: <span class="td-posicao-detalhada__info__data">02/03/2024</span></p>
            <p class="td-posicao-detalhada__info__valor">Natureza: <span class="td-posicao-detalhada__info__data">Compra</span></p>
            <p class="td-posicao-detalhada__info__valor">Tipo: <span class="td-posicao-detalhada__info__data">Crédito</span></p>
            <p class="td-posicao-detalhada__info__valor">Quantidade: <span class="td-posicao-detalhada__info__data">1,00</span></p>
            <p class="td-posicao-detalhada__info__valor">Preço (R$): <span class="td-posicao-detalhada__info__data">22,00</span></p>
            <p class="td-posicao-detalhada__info__valor">Taxa (% a.a.): <span class="td-posicao-detalhada__info__data">10,50</span></p>
            <p class="td-posicao-detalhada__info__valor">Valor (R$): <span class="td-posicao-detalhada__info__data">22,00</span></p>
          </div>
          <table><tbody>
            <tr class="saldo-table-data-names"><td></td><td>DATA DA APLICAÇÃO</td><td>QUANTIDADE DE TÍTULOS</td><td>PREÇO DO TÍTULO NA APLICAÇÃO</td><td>VALOR INVESTIDO</td><td>RENTABILIDADE CONTRATADA</td></tr>
            <tr class="saldo-table-data-names"><td colspan="2">RENTABILIDADE CONTRATADA</td><td>VALOR BRUTO</td><td>TEMPO DA APLICAÇÃO</td><td>ALÍQUOTA</td><td colspan="2">IMPOSTO PREVISTO</td><td colspan="2">TAXA DEVIDA</td><td>VALOR LÍQUIDO</td></tr>
            <tr class="saldo-table-data-names"><td>ANUALIZADA</td><td>ACUMULADA</td><td>I.R.</td><td>IOF</td><td>B3</td><td>INSTITUIÇÃO FINANCEIRA</td></tr>
            <tr class="saldo-table-data-values"><td></td><td><span>02/03/2024</span></td><td><span>1,00</span></td><td><span>20,00</span></td><td><span>20,00</span></td><td><span>10,50%</span></td><td><span>10,50%</span></td><td><span>10,50%</span></td><td><span>22,00</span></td><td><span>1</span></td><td><span>22,50%</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>22,00</span></td></tr>
          </tbody></table>
        </div>
        """;

    [RequiresPostgresFact]
    public async Task CriaNovoTituloSemRevisoesMesmoComOutrasPosicoesERevisoesAntigas()
    {
        await using var database = await IsolatedPostgresDatabase.CreateAsync(
            Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = CriarCliente(factory);
        var carteiraId = Guid.NewGuid();
        var ativoCdb = new RendaFixa(Guid.NewGuid(), "CDB_TESTE", "CDB sintético", 10m, DateTime.UtcNow,
            "CDI", 10m, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Banco sintético", "CDB");
        var compraCdb = new Transacao(Guid.NewGuid(), FakeAuthProvider.TestUserId.ToString(), carteiraId,
            ativoCdb.Id, ativoCdb.Ticker, TipoTransacao.Buy, 1m, 10m, 0m,
            new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc));
        var carteira = new Carteira(carteiraId, FakeAuthProvider.TestUserId.ToString(), "Carteira com outros ativos");
        carteira.ProcessTransaction(compraCdb, 10m, TipoAtivo.RendaFixa);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.AddRange(carteira, ativoCdb, compraCdb);
            await db.SaveChangesAsync();
        }

        using var upload = await EnviarHtmlAsync(client, carteiraId);
        upload.StatusCode.Should().Be(HttpStatusCode.Created, await upload.Content.ReadAsStringAsync());
        using var uploadJson = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var dadosLote = uploadJson.RootElement.GetProperty("dados");
        var loteId = dadosLote.GetProperty("id").GetGuid();
        var hashPrevia = dadosLote.GetProperty("hashPrevia").GetString()!;
        var rotaLote = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteId}";

        using var conciliacao = await client.PostAsJsonAsync($"{rotaLote}/conciliacao", new { ativoId = (Guid?)null });
        conciliacao.StatusCode.Should().Be(HttpStatusCode.OK, await conciliacao.Content.ReadAsStringAsync());
        using var conciliacaoJson = JsonDocument.Parse(await conciliacao.Content.ReadAsStringAsync());
        var dadosConciliacao = conciliacaoJson.RootElement.GetProperty("dados");
        dadosConciliacao.GetProperty("podeCriarPosicao").GetBoolean().Should().BeTrue();
        dadosConciliacao.GetProperty("importacaoInicialSemRevisao").GetBoolean().Should().BeFalse();
        dadosConciliacao.GetProperty("criacaoPosicaoSemRevisao").GetBoolean().Should().BeTrue();
        var carteiraVersao = dadosConciliacao.GetProperty("carteiraVersao").GetInt32();

        // Representa escolhas gravadas por uma versão anterior do fluxo. A criação direta ignora essas decisões
        // para os efeitos do novo lote, sem apagá-las da auditoria.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Add(new DecisaoConciliacaoTesouro(Guid.NewGuid(), carteiraId, loteId,
                FakeAuthProvider.TestUserId, null, 0, 1, "sem_vinculo", null, carteiraVersao, DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        using var confirmacao = await client.PostAsJsonAsync($"{rotaLote}/confirmacao", new
        {
            ativoId = (Guid?)null,
            criarPosicao = true,
            criacaoPosicaoSemRevisao = true,
            carteiraVersao,
            hashPrevia,
            revisoes = Array.Empty<object>()
        });
        confirmacao.StatusCode.Should().Be(HttpStatusCode.OK, await confirmacao.Content.ReadAsStringAsync());

        using var escopoFinal = factory.Services.CreateScope();
        var persistido = escopoFinal.ServiceProvider.GetRequiredService<InvestDbContext>();
        var posicoes = await persistido.AssetPositions.Where(x => x.CarteiraId == carteiraId).ToListAsync();
        posicoes.Should().HaveCount(2);
        (await persistido.Transactions.Where(x => x.CarteiraId == carteiraId).ToListAsync()).Should().ContainSingle()
            .Which.Id.Should().Be(compraCdb.Id);
        (await persistido.Set<EventoOficialTesouro>().Where(x => x.LoteId == loteId).ToListAsync()).Should().ContainSingle();
        (await persistido.Set<DecisaoConciliacaoTesouro>().Where(x => x.LoteId == loteId).ToListAsync()).Should().ContainSingle();
        (await persistido.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == loteId)).Quantidade.Should().Be(1m);
    }

    [RequiresPostgresFact]
    public async Task BloqueiaCriacaoDiretaQuandoRevisaoAntigaVinculaOperacaoManualDeOutroAtivo()
    {
        await using var database = await IsolatedPostgresDatabase.CreateAsync(
            Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = CriarCliente(factory);
        var carteiraId = Guid.NewGuid();
        var ativoCdb = new RendaFixa(Guid.NewGuid(), "CDB_TESTE", "CDB sintético", 10m, DateTime.UtcNow,
            "CDI", 10m, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Banco sintético", "CDB");
        var compraCdb = new Transacao(Guid.NewGuid(), FakeAuthProvider.TestUserId.ToString(), carteiraId,
            ativoCdb.Id, ativoCdb.Ticker, TipoTransacao.Buy, 1m, 10m, 0m,
            new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc));
        var carteira = new Carteira(carteiraId, FakeAuthProvider.TestUserId.ToString(), "Carteira com outro ativo");
        carteira.ProcessTransaction(compraCdb, 10m, TipoAtivo.RendaFixa);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.AddRange(carteira, ativoCdb, compraCdb);
            await db.SaveChangesAsync();
        }

        using var upload = await EnviarHtmlAsync(client, carteiraId);
        upload.StatusCode.Should().Be(HttpStatusCode.Created, await upload.Content.ReadAsStringAsync());
        using var uploadJson = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var lote = uploadJson.RootElement.GetProperty("dados");
        var loteId = lote.GetProperty("id").GetGuid();
        var hashPrevia = lote.GetProperty("hashPrevia").GetString()!;
        var rotaLote = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteId}";

        using var conciliacao = await client.PostAsJsonAsync($"{rotaLote}/conciliacao", new { ativoId = (Guid?)null });
        conciliacao.StatusCode.Should().Be(HttpStatusCode.OK, await conciliacao.Content.ReadAsStringAsync());
        using var conciliacaoJson = JsonDocument.Parse(await conciliacao.Content.ReadAsStringAsync());
        var dadosConciliacao = conciliacaoJson.RootElement.GetProperty("dados");
        dadosConciliacao.GetProperty("criacaoPosicaoSemRevisao").GetBoolean().Should().BeTrue();
        var carteiraVersao = dadosConciliacao.GetProperty("carteiraVersao").GetInt32();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Add(new DecisaoConciliacaoTesouro(Guid.NewGuid(), carteiraId, loteId,
                FakeAuthProvider.TestUserId, ativoCdb.Id, 0, 1, "vincular", compraCdb.Id,
                carteiraVersao, DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        using var confirmacao = await client.PostAsJsonAsync($"{rotaLote}/confirmacao", new
        {
            ativoId = (Guid?)null,
            criarPosicao = true,
            criacaoPosicaoSemRevisao = true,
            carteiraVersao,
            hashPrevia,
            revisoes = Array.Empty<object>()
        });

        confirmacao.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var erro = JsonDocument.Parse(await confirmacao.Content.ReadAsStringAsync());
        erro.RootElement.GetProperty("mensagem").GetString().Should().Contain("operação manual de outra posição")
            .And.Contain("exclua esta prévia e envie o HTML novamente")
            .And.Contain("a operação precisa ser conciliada antes de criar o título");

        using var escopoFinal = factory.Services.CreateScope();
        var persistido = escopoFinal.ServiceProvider.GetRequiredService<InvestDbContext>();
        (await persistido.Set<LoteImportacaoTesouro>().SingleAsync(x => x.Id == loteId)).Estado.Should().Be("Pendente");
        (await persistido.AssetPositions.Where(x => x.CarteiraId == carteiraId).ToListAsync()).Should().ContainSingle()
            .Which.AtivoId.Should().Be(ativoCdb.Id);
        (await persistido.Transactions.Where(x => x.CarteiraId == carteiraId).ToListAsync()).Should().ContainSingle()
            .Which.Id.Should().Be(compraCdb.Id);
        (await persistido.Set<BasePosicaoTesouro>().AnyAsync(x => x.LoteId == loteId)).Should().BeFalse();
        (await persistido.Set<EventoOficialTesouro>().AnyAsync(x => x.LoteId == loteId)).Should().BeFalse();
    }

    [RequiresPostgresFact]
    public async Task BloqueiaCriacaoDiretaQuandoHaOperacaoManualAnteriorDoNovoTitulo()
    {
        await using var database = await IsolatedPostgresDatabase.CreateAsync(
            Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = CriarCliente(factory);
        var carteiraId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Add(new Carteira(carteiraId, FakeAuthProvider.TestUserId.ToString(), "Carteira vazia"));
            await db.SaveChangesAsync();
        }
        using var upload = await EnviarHtmlAsync(client, carteiraId);
        upload.StatusCode.Should().Be(HttpStatusCode.Created, await upload.Content.ReadAsStringAsync());
        using var uploadJson = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var lote = uploadJson.RootElement.GetProperty("dados");
        var loteId = lote.GetProperty("id").GetGuid();
        var previa = JsonSerializer.Deserialize<PreviaTesouroDto>(lote.GetProperty("previa").GetRawText())!;
        var candidato = ConfirmacaoTesouro.CriarAtivoInicial(previa, 20m)!;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Add(candidato);
            db.Add(new Transacao(Guid.NewGuid(), FakeAuthProvider.TestUserId.ToString(), carteiraId,
                candidato.Id, candidato.Ticker, TipoTransacao.Buy, 1m, 20m, 0m,
                new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc)));
            await db.SaveChangesAsync();
        }

        using var reconciliacao = await client.PostAsJsonAsync(
            $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteId}/conciliacao", new { ativoId = (Guid?)null });
        reconciliacao.StatusCode.Should().Be(HttpStatusCode.OK, await reconciliacao.Content.ReadAsStringAsync());
        using var resposta = JsonDocument.Parse(await reconciliacao.Content.ReadAsStringAsync());
        var dados = resposta.RootElement.GetProperty("dados");
        dados.GetProperty("podeCriarPosicao").GetBoolean().Should().BeFalse();
        dados.GetProperty("criacaoPosicaoSemRevisao").GetBoolean().Should().BeFalse();
        dados.GetProperty("motivosCriacaoPosicao").EnumerateArray().Select(x => x.GetString())
            .Should().Contain(x => x!.Contains("operações manuais deste título"));
    }

    private static HttpClient CriarCliente(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        return client;
    }

    private static async Task<HttpResponseMessage> EnviarHtmlAsync(HttpClient client, Guid carteiraId)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(Html)), "arquivo", "tesouro-sintetico.html");
        return await client.PostAsync($"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes?dataReferencia=2024-03-03", multipart);
    }
}
