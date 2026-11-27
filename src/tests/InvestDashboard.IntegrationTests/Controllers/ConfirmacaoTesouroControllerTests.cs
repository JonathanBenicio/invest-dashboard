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
    public async Task ReimportarUmTituloPreservaOutroTituloDaMesmaCarteira()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = Autenticar(factory);

        var carteiraId = Guid.NewGuid();
        var ativoAlvoId = Guid.NewGuid();
        var outroAtivoId = Guid.NewGuid();
        var loteAlvoAnteriorId = Guid.NewGuid();
        var loteOutroAnteriorId = Guid.NewGuid();
        var loteNovoId = Guid.NewGuid();
        var usuarioId = FakeAuthProvider.TestUserId;
        var dataAplicacao = new DateOnly(2024, 3, 1);
        var dataBase = new DateOnly(2024, 3, 2);
        var dataNova = new DateOnly(2024, 3, 3);
        var instante = new DateTime(2024, 3, 3, 12, 0, 0, DateTimeKind.Utc);

        var tituloAlvo = new RendaFixa(ativoAlvoId, "TD2035", "Tesouro Prefixado 2035", 22m,
            instante, "PREFIXADO", 10m, new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "Tesouro Nacional", "TESOURO_DIRETO");
        var outroTitulo = new RendaFixa(outroAtivoId, "TD2040", "Tesouro Prefixado 2040", 31m,
            instante, "PREFIXADO", 11m, new DateTime(2040, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "Tesouro Nacional", "TESOURO_DIRETO");
        var carteira = new Carteira(carteiraId, usuarioId.ToString(), "Carteira com dois títulos Tesouro");
        var compraAlvo = new Transacao(Guid.NewGuid(), usuarioId.ToString(), carteiraId, ativoAlvoId,
            tituloAlvo.Ticker, TipoTransacao.Buy, 1m, 20m, 0m, new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc));
        var compraOutro = new Transacao(Guid.NewGuid(), usuarioId.ToString(), carteiraId, outroAtivoId,
            outroTitulo.Ticker, TipoTransacao.Buy, 2m, 30m, 0m, new DateTime(2024, 3, 1, 13, 0, 0, DateTimeKind.Utc));
        carteira.ProcessTransaction(compraAlvo, tituloAlvo.CurrentPrice, TipoAtivo.RendaFixa);
        carteira.ProcessTransaction(compraOutro, outroTitulo.CurrentPrice, TipoAtivo.RendaFixa);

        var posicaoAlvoHtml = new DadosPosicaoTesouroDto(dataAplicacao, 1m, 20m, 20m,
            "10,00%", null, null, 22m, 1, null, null, null, null, null, 22m, []);
        var posicaoOutroHtml = new DadosPosicaoTesouroDto(dataAplicacao, 2m, 30m, 60m,
            "11,00%", null, null, 31m, 1, null, null, null, null, null, 62m, []);
        var previaAlvoAnterior = CriarPreviaTesouro(carteiraId, "Tesouro Prefixado 2035", "2035-01-01", posicaoAlvoHtml);
        var previaOutroAnterior = CriarPreviaTesouro(carteiraId, "Tesouro Prefixado 2040", "2040-01-01", posicaoOutroHtml);
        var loteAlvoAnterior = CriarLoteTesouro(loteAlvoAnteriorId, carteiraId, usuarioId,
            dataBase, "a", previaAlvoAnterior, instante);
        var loteOutroAnterior = CriarLoteTesouro(loteOutroAnteriorId, carteiraId, usuarioId,
            dataBase, "b", previaOutroAnterior, instante);
        var baseAlvoAnterior = new BasePosicaoTesouro(Guid.NewGuid(), loteAlvoAnteriorId, carteiraId,
            ativoAlvoId, dataBase, 1m, 20m, 22m);
        var baseOutroAnterior = new BasePosicaoTesouro(Guid.NewGuid(), loteOutroAnteriorId, carteiraId,
            outroAtivoId, dataBase, 2m, 60m, 31m);
        baseAlvoAnterior.Ativar();
        baseOutroAnterior.Ativar();

        // O novo HTML cobre apenas o título Prefixado 2035. Não contém movimentações.
        var previaNova = CriarPreviaTesouro(carteiraId, "Tesouro Prefixado 2035", "2035-01-01", posicaoAlvoHtml);
        var loteNovo = CriarLoteTesouro(loteNovoId, carteiraId, usuarioId,
            dataNova, "c", previaNova, instante.AddHours(1), confirmado: false);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.AddRange(carteira, tituloAlvo, outroTitulo, compraAlvo, compraOutro,
                loteAlvoAnterior, loteOutroAnterior, baseAlvoAnterior, baseOutroAnterior, loteNovo);
            await db.SaveChangesAsync();
        }

        var rota = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteNovoId}";
        using var conciliacao = await client.PostAsJsonAsync($"{rota}/conciliacao", new { ativoId = ativoAlvoId });
        conciliacao.StatusCode.Should().Be(HttpStatusCode.OK, await conciliacao.Content.ReadAsStringAsync());
        using var conciliacaoJson = JsonDocument.Parse(await conciliacao.Content.ReadAsStringAsync());
        var dadosConciliacao = conciliacaoJson.RootElement.GetProperty("dados");
        dadosConciliacao.GetProperty("podeConfirmar").GetBoolean().Should().BeTrue();
        var carteiraVersao = dadosConciliacao.GetProperty("carteiraVersao").GetInt32();
        var hashDivergencias = dadosConciliacao.GetProperty("comparacaoSnapshot").GetProperty("hash").GetString();

        using var confirmacao = await client.PostAsJsonAsync($"{rota}/confirmacao", new
        {
            ativoId = ativoAlvoId,
            carteiraVersao,
            revisoes = Array.Empty<object>(),
            hashDivergencias,
            aceitarDivergencias = true
        });
        confirmacao.StatusCode.Should().Be(HttpStatusCode.OK, await confirmacao.Content.ReadAsStringAsync());

        using var verificacao = factory.Services.CreateScope();
        var persistido = verificacao.ServiceProvider.GetRequiredService<InvestDbContext>();
        var basesAtivas = await persistido.Set<BasePosicaoTesouro>()
            .Where(baseAtiva => baseAtiva.CarteiraId == carteiraId && baseAtiva.Ativa)
            .ToDictionaryAsync(baseAtiva => baseAtiva.AtivoId);
        basesAtivas.Should().ContainKey(ativoAlvoId).And.ContainKey(outroAtivoId);
        basesAtivas[ativoAlvoId].LoteId.Should().Be(loteNovoId);
        basesAtivas[outroAtivoId].LoteId.Should().Be(loteOutroAnteriorId);
        basesAtivas[outroAtivoId].DataReferencia.Should().Be(dataBase);
        basesAtivas[outroAtivoId].Quantidade.Should().Be(2m);
        basesAtivas[outroAtivoId].CustoTotal.Should().Be(60m);

        var posicoes = await persistido.AssetPositions.Where(posicao => posicao.CarteiraId == carteiraId)
            .ToDictionaryAsync(posicao => posicao.AtivoId);
        posicoes.Should().ContainKey(ativoAlvoId).And.ContainKey(outroAtivoId);
        posicoes[ativoAlvoId].Quantity.Should().Be(1m);
        posicoes[outroAtivoId].Quantity.Should().Be(2m);
        posicoes[outroAtivoId].TotalCost.Should().Be(60m);
        posicoes[outroAtivoId].CurrentPrice.Should().Be(31m);
        (await persistido.Set<LoteImportacaoTesouro>().SingleAsync(lote => lote.Id == loteOutroAnteriorId))
            .Estado.Should().Be("Confirmado");
        (await persistido.Set<LoteImportacaoTesouro>().SingleAsync(lote => lote.Id == loteNovoId))
            .Estado.Should().Be("Confirmado");
    }

    private static PreviaTesouroDto CriarPreviaTesouro(Guid carteiraId, string titulo, string vencimento,
        DadosPosicaoTesouroDto posicao)
    {
        return new PreviaTesouroDto(carteiraId, "html", "portal_tesouro_html", titulo, vencimento, null,
            [new(1, new Dictionary<string, string>(), posicao)], [], [], [], true, false, false,
            SecaoPosicoesReconhecida: true);
    }

    private static LoteImportacaoTesouro CriarLoteTesouro(Guid loteId, Guid carteiraId, Guid usuarioId,
        DateOnly dataReferencia, string caractereHash, PreviaTesouroDto previa, DateTime criadoEmUtc,
        bool confirmado = true)
    {
        var lote = new LoteImportacaoTesouro(loteId, carteiraId, usuarioId, dataReferencia,
            new string(caractereHash[0], 64), "html", JsonSerializer.Serialize(previa), criadoEmUtc);
        if (confirmado) lote.MarcarConfirmado(criadoEmUtc.AddMinutes(1));
        return lote;
    }

    [RequiresPostgresFact]
    public async Task FalhaAoInserirNovaBase_RestauraBaseAnteriorELotePendente()
    {
        var setup = await CriarBaseAtivaParaReimportacaoAsync();
        await using var database = setup.Database;
        using var factory = setup.Factory;
        using var client = Autenticar(factory);
        var rota = $"/api/v1/portfolios/{setup.CarteiraId}/tesouro-direto/lotes/{setup.LoteNovoId}";
        var (carteiraVersao, hashPrevia, hashDivergencias) = await PrepararConfirmacaoAsync(client, rota, setup.AtivoId);

        await using (var scopeTrigger = factory.Services.CreateAsyncScope())
        {
            var db = scopeTrigger.ServiceProvider.GetRequiredService<InvestDbContext>();
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION fail_treasury_position_base_insert() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'Falha injetada pelo teste de rollback' USING ERRCODE = 'P0001';
                END;
                $$;
                CREATE TRIGGER fail_treasury_position_base_insert
                BEFORE INSERT ON treasury_position_bases
                FOR EACH ROW EXECUTE FUNCTION fail_treasury_position_base_insert();
                """);
        }

        using var resposta = await client.PostAsJsonAsync($"{rota}/confirmacao", new
        {
            ativoId = setup.AtivoId,
            carteiraVersao,
            revisoes = Array.Empty<object>(),
            hashDivergencias,
            aceitarDivergencias = true
        });
        resposta.StatusCode.Should().Be(HttpStatusCode.InternalServerError, await resposta.Content.ReadAsStringAsync());

        using var scopeVerificacao = factory.Services.CreateScope();
        var persistido = scopeVerificacao.ServiceProvider.GetRequiredService<InvestDbContext>();
        var baseAnterior = await persistido.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == setup.LoteBaseId);
        baseAnterior.Ativa.Should().BeTrue();
        (await persistido.Set<BasePosicaoTesouro>().AnyAsync(x => x.LoteId == setup.LoteNovoId)).Should().BeFalse();
        (await persistido.Set<LoteImportacaoTesouro>().SingleAsync(x => x.Id == setup.LoteNovoId)).Estado.Should().Be("Pendente");
        (await persistido.Set<EventoOficialTesouro>().AnyAsync(x => x.LoteId == setup.LoteNovoId)).Should().BeFalse();
        (await persistido.Set<RevisaoSnapshotTesouro>().AnyAsync(x => x.LoteId == setup.LoteNovoId)).Should().BeFalse();
    }

    [RequiresPostgresFact]
    public async Task ConfirmacoesConcorrentes_DuasRequisiçõesNaoCriamBasesAtivasDuplicadas()
    {
        var barreira = new TesouroSnapshotConfirmationReadBarrier();
        var setup = await CriarBaseAtivaParaReimportacaoAsync(barreira);
        await using var database = setup.Database;
        using var factory = setup.Factory;
        using var client = Autenticar(factory);
        var rota = $"/api/v1/portfolios/{setup.CarteiraId}/tesouro-direto/lotes/{setup.LoteNovoId}";
        var (carteiraVersao, hashPrevia, hashDivergencias) = await PrepararConfirmacaoAsync(client, rota, setup.AtivoId);
        var pedido = new
        {
            ativoId = setup.AtivoId,
            carteiraVersao,
            revisoes = Array.Empty<object>(),
            hashDivergencias,
            aceitarDivergencias = true
        };

        barreira.Armar(2);
        var primeira = client.PostAsJsonAsync($"{rota}/confirmacao", pedido);
        var segunda = client.PostAsJsonAsync($"{rota}/confirmacao", pedido);
        HttpResponseMessage[] respostas;
        try
        {
            await barreira.EsperarParticipantesAsync(CancellationToken.None);
        }
        finally
        {
            barreira.Liberar();
        }
        respostas = await Task.WhenAll(primeira, segunda);
        try
        {
            respostas.Select(x => x.StatusCode).Should().Contain(HttpStatusCode.OK);
            respostas.Select(x => x.StatusCode).Should().OnlyContain(x =>
                x == HttpStatusCode.OK || x == HttpStatusCode.Conflict,
                string.Join(" | ", await Task.WhenAll(respostas.Select(x => x.Content.ReadAsStringAsync()))));
        }
        finally
        {
            foreach (var resposta in respostas) resposta.Dispose();
        }

        using var scopeVerificacao = factory.Services.CreateScope();
        var persistido = scopeVerificacao.ServiceProvider.GetRequiredService<InvestDbContext>();
        (await persistido.Set<BasePosicaoTesouro>().CountAsync(x => x.CarteiraId == setup.CarteiraId && x.AtivoId == setup.AtivoId && x.Ativa))
            .Should().Be(1);
        (await persistido.Set<BasePosicaoTesouro>().CountAsync(x => x.LoteId == setup.LoteNovoId)).Should().Be(1);
        (await persistido.Set<LoteImportacaoTesouro>().SingleAsync(x => x.Id == setup.LoteNovoId)).Estado.Should().Be("Confirmado");
    }

    [RequiresPostgresFact]
    public async Task Confirmacao_VendaIntegralEncerraPosicaoEPermiteConsultarDetalhes()
    {
        var setup = await CriarBaseAtivaParaVendaIntegralAsync(secaoPosicoesReconhecida: true);
        await using var database = setup.Database;
        using var factory = setup.Factory;
        using var client = Autenticar(factory);
        var rota = $"/api/v1/portfolios/{setup.CarteiraId}/tesouro-direto/lotes/{setup.LoteNovoId}";

        var consulta = await ConsultarEncerramentoAsync(client, rota, setup.AtivoId);
        consulta.PodeEncerrar.Should().BeTrue();
        consulta.PodeConfirmar.Should().BeTrue();
        consulta.PosicaoPropostaDisponivel.Should().BeTrue();
        consulta.QuantidadeProposta.Should().Be(0m);
        consulta.TiposDivergencia.Should().Contain("posicao_encerrada_por_venda");
        await RegistrarRevisoesAsync(client, rota, setup, consulta, vincularVendaManual: false);
        consulta = await ConsultarEncerramentoAsync(client, rota, setup.AtivoId);

        using var confirmacao = await ConfirmarEncerramentoAsync(client, rota, setup, consulta);
        confirmacao.StatusCode.Should().Be(HttpStatusCode.OK, await confirmacao.Content.ReadAsStringAsync());

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            var baseAnterior = await db.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == setup.LoteBaseId);
            var baseFechada = await db.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == setup.LoteNovoId);
            baseAnterior.Ativa.Should().BeFalse();
            baseFechada.Ativa.Should().BeTrue();
            baseFechada.Quantidade.Should().Be(0m);
            baseFechada.CustoTotal.Should().Be(0m);
            (await db.Set<LoteImportacaoTesouro>().SingleAsync(x => x.Id == setup.LoteNovoId)).Estado.Should().Be("Confirmado");
            (await db.Set<EventoOficialTesouro>().CountAsync(x => x.LoteId == setup.LoteNovoId)).Should().Be(2);
            (await db.Set<RevisaoSnapshotTesouro>().SingleAsync(x => x.LoteId == setup.LoteNovoId))
                .DivergenciasJson.Should().Contain("posicao_encerrada_por_venda");
            var posicao = await db.AssetPositions.SingleAsync(x => x.AtivoId == setup.AtivoId && x.CarteiraId == setup.CarteiraId);
            posicao.Quantity.Should().Be(0m);
            (await db.Transactions.Select(x => x.Id).ToArrayAsync()).Should().Equal(setup.CompraInicialId);
        }

        using var detalhes = await client.GetAsync($"/api/v1/investments/{setup.PosicaoId}");
        detalhes.StatusCode.Should().Be(HttpStatusCode.OK, await detalhes.Content.ReadAsStringAsync());
        using var detalhesJson = JsonDocument.Parse(await detalhes.Content.ReadAsStringAsync());
        detalhesJson.RootElement.GetProperty("dados").GetProperty("situacao").GetString().Should().Be("closed");
        detalhesJson.RootElement.GetProperty("dados").GetProperty("quantidade").GetDecimal().Should().Be(0m);
    }

    [RequiresPostgresFact]
    public async Task Confirmacao_VendaIntegralPreservaCompraManualPosteriorAReferencia()
    {
        var setup = await CriarBaseAtivaParaVendaIntegralAsync(secaoPosicoesReconhecida: true,
            incluirCompraPosterior: true);
        await using var database = setup.Database;
        using var factory = setup.Factory;
        using var client = Autenticar(factory);
        var rota = $"/api/v1/portfolios/{setup.CarteiraId}/tesouro-direto/lotes/{setup.LoteNovoId}";
        var consulta = await ConsultarEncerramentoAsync(client, rota, setup.AtivoId);
        consulta.PodeEncerrar.Should().BeTrue();
        await RegistrarRevisoesAsync(client, rota, setup, consulta, vincularVendaManual: false);
        consulta = await ConsultarEncerramentoAsync(client, rota, setup.AtivoId);

        using var confirmacao = await ConfirmarEncerramentoAsync(client, rota, setup, consulta);
        confirmacao.StatusCode.Should().Be(HttpStatusCode.OK, await confirmacao.Content.ReadAsStringAsync());

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            var saldo = await db.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == setup.LoteNovoId);
            saldo.Quantidade.Should().Be(0m);
            var transacaoPosterior = await db.Transactions.SingleAsync(x => x.Id == setup.CompraPosteriorId);
            transacaoPosterior.Quantity.Should().Be(0.5m);
            (await db.Portfolios.Include(x => x.Positions).SingleAsync(x => x.Id == setup.CarteiraId))
                .Positions.Single(x => x.AtivoId == setup.AtivoId).Quantity.Should().Be(0.5m);
        }

        using var detalhes = await client.GetAsync($"/api/v1/investments/{setup.PosicaoId}");
        detalhes.StatusCode.Should().Be(HttpStatusCode.OK, await detalhes.Content.ReadAsStringAsync());
        using var detalhesJson = JsonDocument.Parse(await detalhes.Content.ReadAsStringAsync());
        detalhesJson.RootElement.GetProperty("dados").GetProperty("situacao").GetString().Should().Be("open");
        detalhesJson.RootElement.GetProperty("dados").GetProperty("quantidade").GetDecimal().Should().Be(0.5m);
    }

    [RequiresPostgresFact]
    public async Task Confirmacao_NaoEncerraQuandoSecaoDePosicoesHtmlEstaIncompleta()
    {
        var setup = await CriarBaseAtivaParaVendaIntegralAsync(secaoPosicoesReconhecida: false);
        await using var database = setup.Database;
        using var factory = setup.Factory;
        using var client = Autenticar(factory);
        var rota = $"/api/v1/portfolios/{setup.CarteiraId}/tesouro-direto/lotes/{setup.LoteNovoId}";

        var consulta = await ConsultarEncerramentoAsync(client, rota, setup.AtivoId);
        consulta.PodeEncerrar.Should().BeFalse();
        consulta.PodeConfirmar.Should().BeFalse();
        consulta.PosicaoPropostaDisponivel.Should().BeFalse();
        consulta.Motivo.Should().Contain("seção de posições");
        await RegistrarRevisoesAsync(client, rota, setup, consulta, vincularVendaManual: false);
        consulta = await ConsultarEncerramentoAsync(client, rota, setup.AtivoId);

        using var confirmacao = await ConfirmarEncerramentoAsync(client, rota, setup, consulta);
        confirmacao.StatusCode.Should().Be(HttpStatusCode.Conflict, await confirmacao.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
        (await db.Set<BasePosicaoTesouro>().SingleAsync(x => x.LoteId == setup.LoteBaseId)).Ativa.Should().BeTrue();
        (await db.Set<BasePosicaoTesouro>().AnyAsync(x => x.LoteId == setup.LoteNovoId)).Should().BeFalse();
        (await db.Set<LoteImportacaoTesouro>().SingleAsync(x => x.Id == setup.LoteNovoId)).Estado.Should().Be("Pendente");
    }

    [RequiresPostgresFact]
    public async Task Confirmacao_ExigeVinculoDeVendaManualAntesDeEncerrar()
    {
        var setup = await CriarBaseAtivaParaVendaIntegralAsync(secaoPosicoesReconhecida: true,
            incluirVendaManual: true);
        await using var database = setup.Database;
        using var factory = setup.Factory;
        using var client = Autenticar(factory);
        var rota = $"/api/v1/portfolios/{setup.CarteiraId}/tesouro-direto/lotes/{setup.LoteNovoId}";

        var consulta = await ConsultarEncerramentoAsync(client, rota, setup.AtivoId);
        consulta.PodeEncerrar.Should().BeFalse();
        consulta.Motivo.Should().Contain("manuais sem vínculo");
        await RegistrarRevisoesAsync(client, rota, setup, consulta, vincularVendaManual: true);
        consulta = await ConsultarEncerramentoAsync(client, rota, setup.AtivoId);
        consulta.PodeEncerrar.Should().BeTrue();

        using var confirmacao = await ConfirmarEncerramentoAsync(client, rota, setup, consulta);
        confirmacao.StatusCode.Should().Be(HttpStatusCode.OK, await confirmacao.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
        var eventoVenda = await db.Set<EventoOficialTesouro>().SingleAsync(x => x.LoteId == setup.LoteNovoId && x.IndiceEvento == 1);
        eventoVenda.TransacaoId.Should().Be(setup.VendaManualId);
        (await db.AssetPositions.SingleAsync(x => x.AtivoId == setup.AtivoId && x.CarteiraId == setup.CarteiraId))
            .Quantity.Should().Be(0m);
    }

    private sealed record SetupBaseTesouro(IsolatedPostgresDatabase Database, CustomWebApplicationFactory Factory,
        Guid CarteiraId, Guid AtivoId, Guid LoteBaseId, Guid LoteNovoId);
    private sealed record SetupVendaTesouro(IsolatedPostgresDatabase Database, CustomWebApplicationFactory Factory,
        Guid CarteiraId, Guid AtivoId, Guid LoteBaseId, Guid LoteNovoId, Guid PosicaoId,
        Guid CompraInicialId, Guid? VendaManualId, Guid? CompraPosteriorId);
    private sealed record ConsultaEncerramento(int CarteiraVersao, string HashPrevia, string HashComparacao,
        bool PodeEncerrar, bool PodeConfirmar, bool PosicaoPropostaDisponivel, decimal? QuantidadeProposta,
        string Motivo, IReadOnlyList<string> TiposDivergencia);

    private static HttpClient Autenticar(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        return client;
    }

    private static async Task<SetupBaseTesouro> CriarBaseAtivaParaReimportacaoAsync(
        TesouroSnapshotConfirmationReadBarrier? barreira = null)
    {
        var database = await IsolatedPostgresDatabase.CreateAsync(Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!);
        var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString, barreira);
        try
        {
            var carteiraId = Guid.NewGuid();
            var ativoId = Guid.NewGuid();
            var loteBaseId = Guid.NewGuid();
            var loteNovoId = Guid.NewGuid();
            var usuario = FakeAuthProvider.TestUserId;
            var dataAplicacao = new DateOnly(2024, 3, 1);
            var dataBase = new DateOnly(2024, 3, 2);
            const string titulo = "Tesouro Prefixado 2035";
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            var carteira = new Carteira(carteiraId, usuario.ToString(), "Carteira sintética");
            var ativo = new RendaFixa(ativoId, "TESOURO_TESTE", titulo, 22m, DateTime.UtcNow, "PREFIXADO", null,
                new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Tesouro Nacional", "TESOURO_DIRETO");
            var compra = new Transacao(Guid.NewGuid(), usuario.ToString(), carteiraId, ativoId, ativo.Ticker,
                TipoTransacao.Buy, 1m, 20m, 0m, new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc));
            carteira.ProcessTransaction(compra, 22m, TipoAtivo.RendaFixa);
            var linhaBase = new DadosPosicaoTesouroDto(dataAplicacao, 1m, 20m, 20m, "10%", null, null, 22m,
                1, null, null, null, null, null, 22m, []);
            var previaBase = new PreviaTesouroDto(carteiraId, "html", "portal_tesouro_html", titulo, "2035-01-01", null,
                [new(1, new Dictionary<string, string>(), linhaBase)], [], [], [], true, false, false);
            var loteBase = new LoteImportacaoTesouro(loteBaseId, carteiraId, usuario, dataBase,
                new string('a', 64), "html", JsonSerializer.Serialize(previaBase), DateTime.UtcNow);
            loteBase.MarcarConfirmado(DateTime.UtcNow);
            var baseAtiva = new BasePosicaoTesouro(Guid.NewGuid(), loteBaseId, carteiraId, ativoId, dataBase, 1m, 20m, 22m);
            baseAtiva.Ativar();
            var linhaNova = new DadosPosicaoTesouroDto(dataAplicacao, 1m, 20m, 20m, "10%", null, null, 22m,
                1, null, null, null, null, null, 22m, []);
            var previaNova = new PreviaTesouroDto(carteiraId, "html", "portal_tesouro_html", titulo, "2035-01-01", null,
                [new(1, new Dictionary<string, string>(), linhaNova)], [], [], [], true, false, false);
            var loteNovo = new LoteImportacaoTesouro(loteNovoId, carteiraId, usuario, new DateOnly(2024, 3, 3),
                new string('b', 64), "html", JsonSerializer.Serialize(previaNova), DateTime.UtcNow);
            db.AddRange(carteira, ativo, compra, loteBase, baseAtiva, loteNovo);
            await db.SaveChangesAsync();
            return new SetupBaseTesouro(database, factory, carteiraId, ativoId, loteBaseId, loteNovoId);
        }
        catch
        {
            factory.Dispose();
            await database.DisposeAsync();
            throw;
        }
    }

    private static async Task<SetupVendaTesouro> CriarBaseAtivaParaVendaIntegralAsync(
        bool secaoPosicoesReconhecida, bool incluirVendaManual = false, bool incluirCompraPosterior = false)
    {
        var database = await IsolatedPostgresDatabase.CreateAsync(Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!);
        var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        try
        {
            var carteiraId = Guid.NewGuid();
            var ativoId = Guid.NewGuid();
            var loteBaseId = Guid.NewGuid();
            var loteNovoId = Guid.NewGuid();
            var usuario = FakeAuthProvider.TestUserId;
            var dataAplicacao = new DateOnly(2024, 3, 1);
            var dataBase = new DateOnly(2024, 3, 2);
            var dataReferencia = new DateOnly(2024, 3, 3);
            const string titulo = "Tesouro Prefixado 2035";
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            var carteira = new Carteira(carteiraId, usuario.ToString(), "Carteira de venda integral");
            var ativo = new RendaFixa(ativoId, "TESOURO_VENDA", titulo, 22m, DateTime.UtcNow, "PREFIXADO", null,
                new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Tesouro Nacional", "TESOURO_DIRETO");
            var compraInicialId = Guid.NewGuid();
            var compraInicial = new Transacao(compraInicialId, usuario.ToString(), carteiraId, ativoId, ativo.Ticker,
                TipoTransacao.Buy, 1m, 20m, 0m, new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc));
            carteira.ProcessTransaction(compraInicial, 22m, TipoAtivo.RendaFixa);
            var posicaoId = carteira.Positions.Single(x => x.AtivoId == ativoId).Id;

            Transacao? transacaoVendaManual = null;
            if (incluirVendaManual)
            {
                transacaoVendaManual = new Transacao(Guid.NewGuid(), usuario.ToString(), carteiraId, ativoId, ativo.Ticker,
                    TipoTransacao.Sell, 1m, 22m, 0m, new DateTime(2024, 3, 3, 12, 0, 0, DateTimeKind.Utc));
                carteira.ProcessTransaction(transacaoVendaManual, 22m, TipoAtivo.RendaFixa);
            }

            Transacao? transacaoCompraPosterior = null;
            if (incluirCompraPosterior)
            {
                transacaoCompraPosterior = new Transacao(Guid.NewGuid(), usuario.ToString(), carteiraId, ativoId, ativo.Ticker,
                    TipoTransacao.Buy, 0.5m, 30m, 0m, new DateTime(2024, 3, 4, 12, 0, 0, DateTimeKind.Utc));
                carteira.ProcessTransaction(transacaoCompraPosterior, 30m, TipoAtivo.RendaFixa);
            }

            var posicao = new DadosPosicaoTesouroDto(dataAplicacao, 1m, 20m, 20m, "10,50%", null, null,
                22m, 1, null, null, null, null, null, 22m, []);
            var movimentoCompra = new DadosMovimentoTesouroDto(dataAplicacao, "Compra", "Crédito", 1m, 20m,
                10.5m, 20m, []);
            var previaBase = new PreviaTesouroDto(carteiraId, "html", "portal_tesouro_html", titulo,
                "2035-01-01", null, [new(1, new Dictionary<string, string>(), posicao)],
                [new([], movimentoCompra)], [], [], true, false, false, SecaoPosicoesReconhecida: true);
            var loteBase = new LoteImportacaoTesouro(loteBaseId, carteiraId, usuario, dataBase,
                new string('c', 64), "html", JsonSerializer.Serialize(previaBase), DateTime.UtcNow);
            loteBase.MarcarConfirmado(DateTime.UtcNow);
            var baseAtiva = new BasePosicaoTesouro(Guid.NewGuid(), loteBaseId, carteiraId, ativoId,
                dataBase, 1m, 20m, 22m);
            baseAtiva.Ativar();

            var dadosVenda = new DadosMovimentoTesouroDto(dataReferencia, "Venda", "Débito", 1m, 22m,
                null, 22m, [], ["taxaAnual"]);
            var previaNova = new PreviaTesouroDto(carteiraId, "html", "portal_tesouro_html", titulo,
                "2035-01-01", null, [], [new([], movimentoCompra), new([], dadosVenda)], [], [], true, false,
                false, SecaoPosicoesReconhecida: secaoPosicoesReconhecida);
            var loteNovo = new LoteImportacaoTesouro(loteNovoId, carteiraId, usuario, dataReferencia,
                new string('d', 64), "html", JsonSerializer.Serialize(previaNova), DateTime.UtcNow);
            db.AddRange(carteira, ativo, compraInicial, loteBase, baseAtiva, loteNovo);
            if (transacaoVendaManual is not null) db.Add(transacaoVendaManual);
            if (transacaoCompraPosterior is not null) db.Add(transacaoCompraPosterior);
            await db.SaveChangesAsync();
            return new SetupVendaTesouro(database, factory, carteiraId, ativoId, loteBaseId, loteNovoId,
                posicaoId, compraInicialId, transacaoVendaManual?.Id, transacaoCompraPosterior?.Id);
        }
        catch
        {
            factory.Dispose();
            await database.DisposeAsync();
            throw;
        }
    }

    private static async Task<ConsultaEncerramento> ConsultarEncerramentoAsync(HttpClient client, string rota,
        Guid ativoId)
    {
        using var lote = await client.GetAsync(rota);
        lote.StatusCode.Should().Be(HttpStatusCode.OK, await lote.Content.ReadAsStringAsync());
        using var loteJson = JsonDocument.Parse(await lote.Content.ReadAsStringAsync());
        var hashPrevia = loteJson.RootElement.GetProperty("dados").GetProperty("hashPrevia").GetString()!;

        using var resposta = await client.PostAsJsonAsync($"{rota}/conciliacao", new { ativoId });
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var dados = json.RootElement.GetProperty("dados");
        var encerramento = dados.GetProperty("encerramentoPosicao");
        var comparacao = dados.GetProperty("comparacaoSnapshot");
        var tipos = comparacao.GetProperty("divergencias").EnumerateArray()
            .Select(item => item.GetProperty("tipo").GetString()!).ToArray();
        var quantidadeProposta = comparacao.GetProperty("quantidadeProposta");
        return new ConsultaEncerramento(dados.GetProperty("carteiraVersao").GetInt32(), hashPrevia,
            comparacao.GetProperty("hash").GetString()!, encerramento.GetProperty("podeEncerrar").GetBoolean(),
            dados.GetProperty("podeConfirmar").GetBoolean(), dados.GetProperty("posicaoPropostaDisponivel").GetBoolean(),
            quantidadeProposta.ValueKind == JsonValueKind.Null ? null : quantidadeProposta.GetDecimal(),
            encerramento.GetProperty("motivo").GetString()!, tipos);
    }

    private static async Task RegistrarRevisoesAsync(HttpClient client, string rota, SetupVendaTesouro setup,
        ConsultaEncerramento consulta, bool vincularVendaManual)
    {
        using var revisaoCompra = await client.PostAsJsonAsync($"{rota}/revisoes", new
        {
            indiceEvento = 0,
            decisao = "sem_vinculo",
            ativoId = setup.AtivoId,
            carteiraVersao = consulta.CarteiraVersao,
            revisaoAnterior = 0,
            hashPrevia = consulta.HashPrevia
        });
        revisaoCompra.StatusCode.Should().Be(HttpStatusCode.OK, await revisaoCompra.Content.ReadAsStringAsync());

        using var revisaoVenda = await client.PostAsJsonAsync($"{rota}/revisoes", new
        {
            indiceEvento = 1,
            decisao = vincularVendaManual ? "vincular" : "sem_vinculo",
            ativoId = setup.AtivoId,
            transacaoId = vincularVendaManual ? setup.VendaManualId : null,
            carteiraVersao = consulta.CarteiraVersao,
            revisaoAnterior = 0,
            hashPrevia = consulta.HashPrevia
        });
        revisaoVenda.StatusCode.Should().Be(HttpStatusCode.OK, await revisaoVenda.Content.ReadAsStringAsync());
    }

    private static Task<HttpResponseMessage> ConfirmarEncerramentoAsync(HttpClient client, string rota,
        SetupVendaTesouro setup, ConsultaEncerramento consulta) =>
        client.PostAsJsonAsync($"{rota}/confirmacao", new
        {
            ativoId = setup.AtivoId,
            criarPosicao = false,
            carteiraVersao = consulta.CarteiraVersao,
            revisoes = new[] { new { indiceEvento = 0, revisao = 1 }, new { indiceEvento = 1, revisao = 1 } },
            hashDivergencias = consulta.HashComparacao,
            aceitarDivergencias = true
        });

    private static async Task<(int CarteiraVersao, string HashPrevia, string? HashDivergencias)> PrepararConfirmacaoAsync(
        HttpClient client, string rota, Guid ativoId)
    {
        using var lote = await client.GetAsync(rota);
        lote.StatusCode.Should().Be(HttpStatusCode.OK, await lote.Content.ReadAsStringAsync());
        using var loteJson = JsonDocument.Parse(await lote.Content.ReadAsStringAsync());
        var hashPrevia = loteJson.RootElement.GetProperty("dados").GetProperty("hashPrevia").GetString()
            ?? throw new InvalidOperationException("A resposta não incluiu o hash da prévia.");
        using var consulta = await client.PostAsJsonAsync($"{rota}/conciliacao", new { ativoId });
        consulta.StatusCode.Should().Be(HttpStatusCode.OK, await consulta.Content.ReadAsStringAsync());
        using var consultaJson = JsonDocument.Parse(await consulta.Content.ReadAsStringAsync());
        var dados = consultaJson.RootElement.GetProperty("dados");
        return (dados.GetProperty("carteiraVersao").GetInt32(), hashPrevia,
            dados.GetProperty("comparacaoSnapshot").GetProperty("hash").GetString());
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
