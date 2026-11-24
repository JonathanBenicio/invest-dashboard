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
    public async Task HtmlCriaPosicaoInicial_ConfirmaSaldoBaseERegistraEventoOficial()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));

        var carteiraId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Portfolios.Add(new Carteira(carteiraId, FakeAuthProvider.TestUserId.ToString(), "Carteira sintética"));
            await db.SaveChangesAsync();
        }

        const string html = """
            <div class="td-custom-meus-investimentos-titulo">
              <h1 class="td-meus-investimentos-detalhe__titulo_card">Tesouro Prefixado 2035</h1>
              <table><tbody><tr class="saldo-table-vencimento"><th><strong>VENCIMENTO: 01/01/2035</strong></th></tr></tbody></table>
              <div class="td-posicao-detalhada__info">
                <p class="td-posicao-detalhada__info__valor">Data: <span class="td-posicao-detalhada__info__data">02/03/2024</span></p>
                <p class="td-posicao-detalhada__info__valor">Natureza: <span class="td-posicao-detalhada__info__data">Compra</span></p>
                <p class="td-posicao-detalhada__info__valor">Tipo: <span class="td-posicao-detalhada__info__data">Crédito</span></p>
                <p class="td-posicao-detalhada__info__valor">Quantidade: <span class="td-posicao-detalhada__info__data">1,00</span></p>
                <p class="td-posicao-detalhada__info__valor">Preço (R$): <span class="td-posicao-detalhada__info__data">22,00</span></p>
                <p class="td-posicao-detalhada__info__valor">Taxa (% a.a.): <span class="td-posicao-detalhada__info__data">-</span></p>
                <p class="td-posicao-detalhada__info__valor">Valor (R$): <span class="td-posicao-detalhada__info__data">22,00</span></p>
              </div>
              <table><tbody>
                <tr class="saldo-table-data-names"><td></td><td>DATA DA APLICAÇÃO</td><td>QUANTIDADE DE TÍTULOS</td><td>PREÇO DO TÍTULO NA APLICAÇÃO</td><td>VALOR INVESTIDO</td><td>RENTABILIDADE CONTRATADA</td></tr>
                <tr class="saldo-table-data-names"><td colspan="2">RENTABILIDADE CONTRATADA</td><td>VALOR BRUTO</td><td>TEMPO DA APLICAÇÃO</td><td>ALÍQUOTA</td><td colspan="2">IMPOSTO PREVISTO</td><td colspan="2">TAXA DEVIDA</td><td>VALOR LÍQUIDO</td></tr>
                <tr class="saldo-table-data-names"><td>ANUALIZADA</td><td>ACUMULADA</td><td>I.R.</td><td>IOF</td><td>B3</td><td>INSTITUIÇÃO FINANCEIRA</td></tr>
                <tr class="saldo-table-data-values"><td></td><td><span>02/03/2024</span></td><td><span>1,00</span></td><td><span>20,00</span></td><td><span>20,00</span></td><td><span>10,50%</span></td><td><span>10,50%</span></td><td><span>10,50%</span></td><td><span>22,00</span></td><td><span>1</span></td><td><span>22,50%</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>22,00</span></td></tr>
                <tr class="saldo-table-data-values"><td></td><td><span>01/03/2024</span></td><td><span>1,00</span></td><td><span>20,00</span></td><td><span>20,00</span></td><td><span>12,00%</span></td><td><span>12,00%</span></td><td><span>12,00%</span></td><td><span>22,00</span></td><td><span>2</span></td><td><span>22,50%</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>22,00</span></td></tr>
              </tbody></table>
            </div>
            """;

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(html)), "arquivo", "extrato-sintetico.html");
        using var upload = await client.PostAsync(
            $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes?dataReferencia=2024-03-03", multipart);
        upload.StatusCode.Should().Be(HttpStatusCode.Created, await upload.Content.ReadAsStringAsync());
        using var loteJson = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var dadosLote = loteJson.RootElement.GetProperty("dados");
        var loteId = dadosLote.GetProperty("id").GetGuid();
        var hashPrevia = dadosLote.GetProperty("hashPrevia").GetString()
            ?? throw new InvalidOperationException("A resposta não incluiu o hash da prévia.");
        var movimentoPrevia = dadosLote.GetProperty("previa").GetProperty("movimentacoes")[0];
        movimentoPrevia.GetProperty("campos").EnumerateArray()
            .Single(x => x.GetProperty("rotulo").GetString()!.StartsWith("Taxa", StringComparison.Ordinal))
            .GetProperty("valor").GetString().Should().Be("-");
        movimentoPrevia.GetProperty("dadosInterpretados").GetProperty("camposNaoInformados")
            .EnumerateArray().Select(x => x.GetString()).Should().Contain("taxaAnual");
        var loteRota = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteId}";

        using var conciliacao = await client.PostAsJsonAsync($"{loteRota}/conciliacao", new { ativoId = (Guid?)null });
        conciliacao.StatusCode.Should().Be(HttpStatusCode.OK, await conciliacao.Content.ReadAsStringAsync());
        using var conciliacaoJson = JsonDocument.Parse(await conciliacao.Content.ReadAsStringAsync());
        var dadosConciliacao = conciliacaoJson.RootElement.GetProperty("dados");
        dadosConciliacao.GetProperty("podeCriarPosicao").GetBoolean().Should().BeTrue();
        dadosConciliacao.GetProperty("importacaoInicialSemRevisao").GetBoolean().Should().BeTrue();
        var carteiraVersao = dadosConciliacao.GetProperty("carteiraVersao").GetInt32();

        using var confirmacao = await client.PostAsJsonAsync($"{loteRota}/confirmacao", new
        {
            ativoId = (Guid?)null,
            criarPosicao = true,
            carteiraVersao,
            importacaoInicialSemRevisao = true,
            hashPrevia,
            revisoes = Array.Empty<object>()
        });
        confirmacao.StatusCode.Should().Be(HttpStatusCode.OK, await confirmacao.Content.ReadAsStringAsync());
        using var resultado = JsonDocument.Parse(await confirmacao.Content.ReadAsStringAsync());
        resultado.RootElement.GetProperty("dados").GetProperty("estado").GetString().Should().Be("Confirmado");

        using var scopeFinal = factory.Services.CreateScope();
        var persistido = scopeFinal.ServiceProvider.GetRequiredService<InvestDbContext>();
        var ativoCriado = await persistido.Assets.OfType<RendaFixa>().SingleAsync(a => a.Subtype == "TESOURO_DIRETO");
        ativoCriado.Name.Should().Be("Tesouro Prefixado 2035");
        ativoCriado.Indexer.Should().Be("PREFIXADO");
        ativoCriado.InterestRate.Should().BeNull();
        var baseCriada = await persistido.Set<BasePosicaoTesouro>().SingleAsync(b => b.LoteId == loteId);
        baseCriada.Ativa.Should().BeTrue();
        baseCriada.Quantidade.Should().Be(2m);
        baseCriada.CustoTotal.Should().Be(40m);
        baseCriada.PrecoUnitario.Should().Be(22m);
        var carteira = await persistido.Portfolios.Include(x => x.Positions).SingleAsync(x => x.Id == carteiraId);
        carteira.Positions.Should().ContainSingle().Which.CurrentValue.Should().Be(44m);
        (await persistido.Transactions.Where(x => x.CarteiraId == carteiraId).ToListAsync()).Should().BeEmpty();
        var lotePersistido = await persistido.Set<LoteImportacaoTesouro>().SingleAsync(x => x.Id == loteId);
        var previaPersistida = JsonSerializer.Deserialize<PreviaTesouroDto>(lotePersistido.PreviaJson)!;
        previaPersistida.Posicoes.Select(x => x.DadosInterpretados!.RentabilidadeContratada).Should().Equal("10,50%", "12,00%");
        var eventos = await persistido.Set<EventoOficialTesouro>().Where(e => e.LoteId == loteId).ToListAsync();
        eventos.Should().ContainSingle().Which.AtivoId.Should().Be(ativoCriado.Id);
    }

    [RequiresPostgresFact]
    public async Task HtmlComPosicaoValidaESemMovimentacoesPermiteCriarBaseInicial()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));

        var carteiraId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Portfolios.Add(new Carteira(carteiraId, FakeAuthProvider.TestUserId.ToString(), "Carteira sem eventos"));
            await db.SaveChangesAsync();
        }

        const string html = """
            <div class="td-custom-meus-investimentos-titulo">
              <h1 class="td-meus-investimentos-detalhe__titulo_card">Tesouro Prefixado 2035</h1>
              <table><tbody><tr class="saldo-table-vencimento"><th><strong>VENCIMENTO: 01/01/2035</strong></th></tr></tbody></table>
              <table><tbody>
                <tr class="saldo-table-data-names"><td></td><td>DATA DA APLICAÇÃO</td><td>QUANTIDADE DE TÍTULOS</td><td>PREÇO DO TÍTULO NA APLICAÇÃO</td><td>VALOR INVESTIDO</td><td>RENTABILIDADE CONTRATADA</td></tr>
                <tr class="saldo-table-data-names"><td colspan="2">RENTABILIDADE CONTRATADA</td><td>VALOR BRUTO</td><td>TEMPO DA APLICAÇÃO</td><td>ALÍQUOTA</td><td colspan="2">IMPOSTO PREVISTO</td><td colspan="2">TAXA DEVIDA</td><td>VALOR LÍQUIDO</td></tr>
                <tr class="saldo-table-data-names"><td>ANUALIZADA</td><td>ACUMULADA</td><td>I.R.</td><td>IOF</td><td>B3</td><td>INSTITUIÇÃO FINANCEIRA</td></tr>
                <tr class="saldo-table-data-values"><td></td><td><span>02/03/2024</span></td><td><span>1,00</span></td><td><span>20,00</span></td><td><span>20,00</span></td><td><span>10,50%</span></td><td><span>10,50%</span></td><td><span>10,50%</span></td><td><span>22,00</span></td><td><span>1</span></td><td><span>22,50%</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>0,00</span></td><td><span>22,00</span></td></tr>
              </tbody></table>
            </div>
            """;

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(html)), "arquivo", "snapshot-sem-eventos.html");
        using var upload = await client.PostAsync($"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes?dataReferencia=2024-03-03", multipart);
        upload.StatusCode.Should().Be(HttpStatusCode.Created, await upload.Content.ReadAsStringAsync());
        using var uploadJson = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        var loteId = uploadJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        var rota = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteId}";

        using var conciliacao = await client.PostAsJsonAsync($"{rota}/conciliacao", new { ativoId = (Guid?)null });
        conciliacao.StatusCode.Should().Be(HttpStatusCode.OK, await conciliacao.Content.ReadAsStringAsync());
        using var conciliacaoJson = JsonDocument.Parse(await conciliacao.Content.ReadAsStringAsync());
        conciliacaoJson.RootElement.GetProperty("dados").GetProperty("eventos").GetArrayLength().Should().Be(0);
        var carteiraVersao = conciliacaoJson.RootElement.GetProperty("dados").GetProperty("carteiraVersao").GetInt32();

        using var confirmacao = await client.PostAsJsonAsync($"{rota}/confirmacao", new
        {
            ativoId = (Guid?)null,
            criarPosicao = true,
            carteiraVersao,
            revisoes = Array.Empty<object>()
        });
        confirmacao.StatusCode.Should().Be(HttpStatusCode.OK, await confirmacao.Content.ReadAsStringAsync());

        using var scopeFinal = factory.Services.CreateScope();
        var persistido = scopeFinal.ServiceProvider.GetRequiredService<InvestDbContext>();
        var ativo = await persistido.Assets.OfType<RendaFixa>().SingleAsync(x => x.Subtype == "TESOURO_DIRETO");
        var posicao = await persistido.AssetPositions.SingleAsync(x => x.AtivoId == ativo.Id);
        posicao.Quantity.Should().Be(1m);
        var basePosicao = await persistido.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == loteId);
        basePosicao.Ativa.Should().BeTrue();
    }

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
        using var loteAtual = await client.GetAsync(rota);
        using var loteAtualJson = JsonDocument.Parse(await loteAtual.Content.ReadAsStringAsync());
        var hashPrevia = loteAtualJson.RootElement.GetProperty("dados").GetProperty("hashPrevia").GetString()
            ?? throw new InvalidOperationException("A resposta não incluiu o hash da prévia.");
        var revisoes = new[] { new { indiceEvento = 0, revisao = 1 }, new { indiceEvento = 1, revisao = 1 } };
        using var comparacao = await client.PostAsJsonAsync($"{rota}/conciliacao", new { ativoId });
        comparacao.StatusCode.Should().Be(HttpStatusCode.OK);
        using var comparacaoJson = JsonDocument.Parse(await comparacao.Content.ReadAsStringAsync());
        var hash = comparacaoJson.RootElement.GetProperty("dados").GetProperty("comparacaoSnapshot").GetProperty("hash").GetString();
        var pedido = new { ativoId, carteiraVersao = versao, revisoes, hashDivergencias = hash, aceitarDivergencias = true };
        using var acessoIndevido = await outro.PostAsJsonAsync($"{rota}/confirmacao", pedido);
        acessoIndevido.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var semRevisoes = await client.PostAsJsonAsync($"{rota}/confirmacao", pedido);
        semRevisoes.IsSuccessStatusCode.Should().BeFalse();
        await VerificarPendente();

        using var revisaoCompra = await client.PostAsJsonAsync($"{rota}/revisoes",
            new { indiceEvento = 0, decisao = "vincular", ativoId, transacaoId = manualId, carteiraVersao = versao, revisaoAnterior = 0, hashPrevia });
        revisaoCompra.StatusCode.Should().Be(HttpStatusCode.OK);
        using var revisaoTaxa = await client.PostAsJsonAsync($"{rota}/revisoes",
            new { indiceEvento = 1, decisao = "sem_vinculo", ativoId, carteiraVersao = versao, revisaoAnterior = 0, hashPrevia });
        revisaoTaxa.StatusCode.Should().Be(HttpStatusCode.OK);
        using var semAceite = await client.PostAsJsonAsync($"{rota}/confirmacao", new { ativoId, carteiraVersao = versao, revisoes });
        semAceite.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var hashObsoleto = await client.PostAsJsonAsync($"{rota}/confirmacao", new { ativoId, carteiraVersao = versao, revisoes, hashDivergencias = new string('0', 64), aceitarDivergencias = true });
        hashObsoleto.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
        (await persistido.Set<RevisaoSnapshotTesouro>().CountAsync(x => x.LoteId == loteId)).Should().Be(1);
        var saldo = await persistido.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == loteId);
        saldo.Ativa.Should().BeTrue();
        saldo.DataReferencia.Should().Be(referencia);
        saldo.Quantidade.Should().Be(1m);
        (await persistido.Transactions.Select(t => t.Id).ToArrayAsync()).Should().BeEquivalentTo([manualId, posteriorId]);
        var carteiraConfirmada = await persistido.Portfolios.Include(c => c.Positions).SingleAsync(c => c.Id == carteiraId);
        var posicaoConfirmada = carteiraConfirmada.Positions.Single(p => p.AtivoId == ativoId);
        posicaoConfirmada.Quantity.Should().Be(2m);
        posicaoConfirmada.TotalCost.Should().Be(51m);

        var lotePosteriorId = Guid.NewGuid();
        var dataPosterior = new DateOnly(2024, 3, 5);
        var versaoPosterior = carteiraConfirmada.Version;
        var linhaPosterior = new DadosPosicaoTesouroDto(new DateOnly(2024, 3, 2), 2m, 20m, 51m, "10%", null, null, 52m,
            3, null, null, null, null, null, 52m, []);
        var compraPosterior = new DadosMovimentoTesouroDto(new DateOnly(2024, 3, 4), "Compra", "Crédito", 1m, 30m, null, 30m, []);
        var previaPosterior = new PreviaTesouroDto(carteiraId, "html", "portal_tesouro_html", titulo, "2035-01-01", null,
            [new(1, new Dictionary<string, string>(), linhaPosterior)], [new([], compraPosterior)], [], [], true, false, false);
        persistido.Add(new LoteImportacaoTesouro(lotePosteriorId, carteiraId, usuario, dataPosterior,
            new string('b', 64), "html", JsonSerializer.Serialize(previaPosterior), DateTime.UtcNow));
        await persistido.SaveChangesAsync();

        var rotaPosterior = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{lotePosteriorId}";
        using var lotePosteriorAtual = await client.GetAsync(rotaPosterior);
        using var lotePosteriorJson = JsonDocument.Parse(await lotePosteriorAtual.Content.ReadAsStringAsync());
        var hashPreviaPosterior = lotePosteriorJson.RootElement.GetProperty("dados").GetProperty("hashPrevia").GetString()
            ?? throw new InvalidOperationException("A resposta não incluiu o hash da prévia.");
        using var novaConciliacao = await client.PostAsJsonAsync($"{rotaPosterior}/conciliacao", new { ativoId });
        novaConciliacao.StatusCode.Should().Be(HttpStatusCode.OK);
        using var novaJson = JsonDocument.Parse(await novaConciliacao.Content.ReadAsStringAsync());
        var novaComparacao = novaJson.RootElement.GetProperty("dados").GetProperty("comparacaoSnapshot");
        novaComparacao.GetProperty("divergencias").EnumerateArray()
            .Any(x => x.GetProperty("tipo").GetString() == "evento_nao_observado").Should().BeTrue();
        var novoHash = novaComparacao.GetProperty("hash").GetString();
        using var semVinculo = await client.PostAsJsonAsync($"{rotaPosterior}/revisoes", new
        {
            indiceEvento = 0, decisao = "sem_vinculo", ativoId,
            carteiraVersao = versaoPosterior, revisaoAnterior = 0, hashPrevia = hashPreviaPosterior
        });
        semVinculo.StatusCode.Should().Be(HttpStatusCode.OK);
        using var semAceiteDivergencias = await client.PostAsJsonAsync($"{rotaPosterior}/confirmacao", new
        {
            ativoId, carteiraVersao = versaoPosterior, revisoes = new[] { new { indiceEvento = 0, revisao = 1 } }
        });
        semAceiteDivergencias.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using (var scopeBloqueio = factory.Services.CreateScope())
        {
            var dbBloqueio = scopeBloqueio.ServiceProvider.GetRequiredService<InvestDbContext>();
            (await dbBloqueio.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == loteId)).Ativa.Should().BeTrue();
            (await dbBloqueio.Set<RevisaoSnapshotTesouro>().AnyAsync(x => x.LoteId == lotePosteriorId)).Should().BeFalse();
        }
        using var novaConfirmacao = await client.PostAsJsonAsync($"{rotaPosterior}/confirmacao", new
        {
            ativoId, carteiraVersao = versaoPosterior, revisoes = new[] { new { indiceEvento = 0, revisao = 1 } },
            hashDivergencias = novoHash, aceitarDivergencias = true
        });
        novaConfirmacao.StatusCode.Should().Be(HttpStatusCode.OK, await novaConfirmacao.Content.ReadAsStringAsync());
        using var auditoria = await client.GetAsync($"{rotaPosterior}/revisao-snapshot");
        auditoria.StatusCode.Should().Be(HttpStatusCode.OK);
        using var acessoAuditoria = await outro.GetAsync($"{rotaPosterior}/revisao-snapshot");
        acessoAuditoria.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var scopeFinal = factory.Services.CreateScope();
        var dbFinal = scopeFinal.ServiceProvider.GetRequiredService<InvestDbContext>();
        (await dbFinal.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == loteId)).Ativa.Should().BeFalse();
        (await dbFinal.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == lotePosteriorId)).Ativa.Should().BeTrue();
        (await dbFinal.Transactions.Select(x => x.Id).ToArrayAsync()).Should().BeEquivalentTo([manualId, posteriorId]);
        var eventoPosterior = await dbFinal.Set<EventoOficialTesouro>().SingleAsync(x => x.LoteId == lotePosteriorId);
        eventoPosterior.TransacaoId.Should().BeNull();
        var revisaoSnapshot = await dbFinal.Set<RevisaoSnapshotTesouro>().SingleAsync(x => x.LoteId == lotePosteriorId);
        revisaoSnapshot.DivergenciasJson.Should().Contain(posteriorId.ToString("N"));

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
