using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class LotesTesouroControllerTests
{
    private const string Html = """
      <div class="td-custom-meus-investimentos-titulo">
       <h1 class="td-meus-investimentos-detalhe__titulo_card">Tesouro Prefixado 2035</h1>
       <table><tr class="saldo-table-vencimento"><td><strong>VENCIMENTO: 01/01/2035</strong></td></tr></table>
       <div class="td-posicao-detalhada__info">
        <p class="td-posicao-detalhada__info__valor">Data: <span class="td-posicao-detalhada__info__data">02/03/2024</span></p>
        <p class="td-posicao-detalhada__info__valor">Natureza: <span class="td-posicao-detalhada__info__data">Compra</span></p>
        <p class="td-posicao-detalhada__info__valor">Tipo: <span class="td-posicao-detalhada__info__data">Crédito</span></p>
        <p class="td-posicao-detalhada__info__valor">Quantidade: <span class="td-posicao-detalhada__info__data">0,01</span></p>
        <p class="td-posicao-detalhada__info__valor">Preço (R$): <span class="td-posicao-detalhada__info__data">123,45</span></p>
        <p class="td-posicao-detalhada__info__valor">Taxa (% a.a.): <span class="td-posicao-detalhada__info__data">10,00</span></p>
        <p class="td-posicao-detalhada__info__valor">Valor (R$): <span class="td-posicao-detalhada__info__data">1,23</span></p>
       </div>
      </div>
      """;

    private static HttpClient Autenticado(CustomWebApplicationFactory factory, bool outro = false)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", outro
            ? FakeAuthProvider.GenerateJwtForUser(FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
                FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName)
            : FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        return client;
    }

    private static async Task<Guid> CriarCarteira(HttpClient client,
        string instituicaoFinanceiraId = "10000000-0000-4000-8000-000000000001",
        string nome = "Carteira sintética")
    {
        using var resposta = await client.PostAsJsonAsync("/api/v1/portfolios", new
        { instituicaoFinanceiraId = Guid.Parse(instituicaoFinanceiraId), nome });
        resposta.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> Enviar(HttpClient client, Guid carteira, string query, string conteudo = Html)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(conteudo)), "arquivo", "extrato.html");
        return await client.PostAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/lotes{query}", multipart);
    }

    private static string HtmlComReferencia(string referencia) =>
        Html.Replace("<h1 class=\"td-meus-investimentos-detalhe__titulo_card\">Tesouro Prefixado 2035</h1>",
            $"<h1 class=\"td-meus-investimentos-detalhe__titulo_card\">Tesouro Prefixado 2035</h1><h2>{referencia}</h2>");

    private static string HtmlComInstituicao(string html, string instituicao) => html.Replace(
        "<div class=\"td-custom-meus-investimentos-titulo\">",
        "<div class=\"td-custom-meus-investimentos-titulo\"><div class=\"td-meus-investimentos-detalhe-box\">" +
        "<div class=\"td-meus-investimentos-deatlhe-instituicao\"><p class=\"td-meus-investimentos-deatlhe-instituicao__info\">" +
        $"<span class=\"td-meus-investimentos-deatlhe-instituicao__nome\">{instituicao}</span></p></div></div>",
        StringComparison.Ordinal);

    [RequiresPostgresFact]
    public async Task ExcluiPreviaPendenteEApagaRevisoesAssociadas()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = Autenticado(factory);
        var carteiraId = await CriarCarteira(client);
        var loteId = Guid.NewGuid();
        var ativoId = Guid.NewGuid();
        var criado = DateTime.UtcNow;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            var lote = new LoteImportacaoTesouro(loteId, carteiraId, FakeAuthProvider.TestUserId,
                new DateOnly(2026, 11, 24), new string('a', 64), "html", "{}", criado);
            db.Add(lote);
            db.Add(new DecisaoConciliacaoTesouro(Guid.NewGuid(), carteiraId, loteId, FakeAuthProvider.TestUserId,
                ativoId, 0, 1, "sem_vinculo", null, 1, criado));
            db.Add(new RevisaoSnapshotTesouro(Guid.NewGuid(), carteiraId, loteId, FakeAuthProvider.TestUserId,
                ativoId, new string('b', 64), "{}", "aceitar_nova_base", 1, criado));
            await db.SaveChangesAsync();
        }

        using var resposta = await client.DeleteAsync($"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteId}");

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var obter = await client.GetAsync($"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteId}");
        obter.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var scopeFinal = factory.Services.CreateScope();
        var persistido = scopeFinal.ServiceProvider.GetRequiredService<InvestDbContext>();
        (await persistido.Set<DecisaoConciliacaoTesouro>().CountAsync(x => x.LoteId == loteId)).Should().Be(0);
        (await persistido.Set<RevisaoSnapshotTesouro>().CountAsync(x => x.LoteId == loteId)).Should().Be(0);
    }

    [RequiresPostgresFact]
    public async Task ExcluirPreviaExigeAutorizacaoEApenasLotePendente()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = Autenticado(factory);
        using var outro = Autenticado(factory, outro: true);
        var carteiraId = await CriarCarteira(client);
        var pendenteId = Guid.NewGuid();
        var confirmadoId = Guid.NewGuid();
        var criado = DateTime.UtcNow.AddMinutes(-1);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Add(new LoteImportacaoTesouro(pendenteId, carteiraId, FakeAuthProvider.TestUserId,
                new DateOnly(2026, 11, 23), new string('c', 64), "html", "{}", criado));
            var confirmado = new LoteImportacaoTesouro(confirmadoId, carteiraId, FakeAuthProvider.TestUserId,
                new DateOnly(2026, 11, 24), new string('d', 64), "html", "{}", criado);
            confirmado.MarcarConfirmado(DateTime.UtcNow);
            db.Add(confirmado);
            await db.SaveChangesAsync();
        }

        using var negado = await outro.DeleteAsync($"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{pendenteId}");
        negado.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var loteConfirmado = await client.DeleteAsync($"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{confirmadoId}");
        loteConfirmado.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var confirmarPendente = await client.DeleteAsync($"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{pendenteId}");
        confirmarPendente.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var loteMantido = await client.GetAsync($"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{confirmadoId}");
        loteMantido.StatusCode.Should().Be(HttpStatusCode.OK);
        using var loteJson = JsonDocument.Parse(await loteMantido.Content.ReadAsStringAsync());
        loteJson.RootElement.GetProperty("dados").GetProperty("estado").GetString().Should().Be("Confirmado");
    }

    [RequiresPostgresFact]
    public async Task ExcluirPreviaConcorrenteComConfirmacaoDeveConcluirUmaUnicaOperacao()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = Autenticado(factory);
        var carteiraId = Guid.NewGuid();
        var loteId = Guid.NewGuid();
        var previa = new PreviaTesouroDto(carteiraId, "html", "portal_tesouro_html",
            "Tesouro Prefixado 2035", "2035-01-01", null,
            [new PosicaoPreviaTesouroDto(1, new Dictionary<string, string>(),
                new DadosPosicaoTesouroDto(new DateOnly(2024, 3, 2), 1m, 20m, 20m, "10,50%",
                    null, null, 22m, 1, null, null, null, null, null, 22m, []))],
            [], [], [], true, false, false, SecaoPosicoesReconhecida: true);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Add(new Carteira(carteiraId, FakeAuthProvider.TestUserId.ToString(), "Carteira concorrente"));
            db.Add(new LoteImportacaoTesouro(loteId, carteiraId, FakeAuthProvider.TestUserId,
                new DateOnly(2024, 3, 3), new string('e', 64), "html", JsonSerializer.Serialize(previa), DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        var lotePath = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteId}";
        using var lote = await client.GetAsync(lotePath);
        lote.StatusCode.Should().Be(HttpStatusCode.OK);
        using var loteJson = JsonDocument.Parse(await lote.Content.ReadAsStringAsync());
        var hashPrevia = loteJson.RootElement.GetProperty("dados").GetProperty("hashPrevia").GetString();

        using var conciliacao = await client.PostAsJsonAsync($"{lotePath}/conciliacao", new { ativoId = (Guid?)null });
        conciliacao.StatusCode.Should().Be(HttpStatusCode.OK, await conciliacao.Content.ReadAsStringAsync());
        using var conciliacaoJson = JsonDocument.Parse(await conciliacao.Content.ReadAsStringAsync());
        var conciliacaoDados = conciliacaoJson.RootElement.GetProperty("dados");
        var carteiraVersao = conciliacaoDados.GetProperty("carteiraVersao").GetInt32();
        conciliacaoDados.GetProperty("criacaoPosicaoSemRevisao").GetBoolean().Should().BeTrue();
        var pedidoConfirmacao = new
        {
            ativoId = (Guid?)null,
            criarPosicao = true,
            carteiraVersao,
            revisoes = Array.Empty<object>(),
            criacaoPosicaoSemRevisao = true,
            importacaoInicialSemRevisao = true,
            hashPrevia
        };

        var exclusao = client.DeleteAsync(lotePath);
        var confirmacao = client.PostAsJsonAsync($"{lotePath}/confirmacao", pedidoConfirmacao);
        await Task.WhenAll(exclusao, confirmacao);

        var statusExclusao = exclusao.Result.StatusCode;
        var statusConfirmacao = confirmacao.Result.StatusCode;
        (statusExclusao == HttpStatusCode.NoContent &&
             statusConfirmacao is HttpStatusCode.NotFound or HttpStatusCode.Conflict ||
         statusExclusao == HttpStatusCode.Conflict && statusConfirmacao == HttpStatusCode.OK).Should().BeTrue(
            $"Expected only delete or confirmation to win, got DELETE {statusExclusao} and confirm {statusConfirmacao}.");

        using var scopeFinal = factory.Services.CreateScope();
        var persistido = scopeFinal.ServiceProvider.GetRequiredService<InvestDbContext>();
        var existeLote = await persistido.Set<LoteImportacaoTesouro>().AnyAsync(x => x.Id == loteId);
        var baseAtiva = await persistido.Set<BasePosicaoTesouro>().AnyAsync(x => x.LoteId == loteId && x.Ativa);
        if (statusExclusao == HttpStatusCode.NoContent)
        {
            statusConfirmacao.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Conflict);
            existeLote.Should().BeFalse();
            baseAtiva.Should().BeFalse();
        }
        else
        {
            statusConfirmacao.Should().Be(HttpStatusCode.OK);
            existeLote.Should().BeTrue();
            baseAtiva.Should().BeTrue();
        }
    }

    [Fact]
    public async Task BloqueiaImportacaoQuandoInstituicaoDoHtmlDivergeDaCarteira()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Autenticado(factory);
        var carteiraBancoBrasil = await CriarCarteira(client);
        var html = HtmlComReferencia("Posição em Novembro/2026");
        html = HtmlComInstituicao(html, "XP Investimentos CCTVM S/A.");

        using var divergente = await Enviar(client, carteiraBancoBrasil, "?dataReferencia=2026-11-14", html);

        divergente.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using (var erro = JsonDocument.Parse(await divergente.Content.ReadAsStringAsync()))
        {
            erro.RootElement.GetProperty("title").GetString().Should().Be("Instituição do extrato diverge da carteira");
            erro.RootElement.GetProperty("codigo").GetString().Should().Be("tesouro_instituicao_diverge_da_carteira");
            erro.RootElement.GetProperty("detail").GetString().Should().Contain("Escolha outra carteira");
        }

        using var lotesBloqueados = await client.GetAsync($"/api/v1/portfolios/{carteiraBancoBrasil}/tesouro-direto/lotes");
        using var listaBloqueada = JsonDocument.Parse(await lotesBloqueados.Content.ReadAsStringAsync());
        listaBloqueada.RootElement.GetProperty("dados").GetArrayLength().Should().Be(0);

        var carteiraXp = await CriarCarteira(client, "10000000-0000-4000-8000-000000000013", "Carteira XP");
        using var compativel = await Enviar(client, carteiraXp, "?dataReferencia=2026-11-14", html);

        compativel.StatusCode.Should().Be(HttpStatusCode.Created, await compativel.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CriaRetomaERepeteSemAlterarCarteira()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Autenticado(factory);
        var carteira = await CriarCarteira(client);
        using var inicial = await Enviar(client, carteira, "?dataReferencia=2026-11-14");
        inicial.StatusCode.Should().Be(HttpStatusCode.Created, await inicial.Content.ReadAsStringAsync());
        inicial.Headers.CacheControl!.NoStore.Should().BeTrue();
        using var json = JsonDocument.Parse(await inicial.Content.ReadAsStringAsync());
        var dados = json.RootElement.GetProperty("dados");
        var lote = dados.GetProperty("id").GetGuid();
        dados.GetProperty("estado").GetString().Should().Be("Pendente");
        dados.GetProperty("previa").GetProperty("podeConfirmar").GetBoolean().Should().BeFalse();
        using var repetido = await Enviar(client, carteira, "?dataReferencia=2026-11-14");
        repetido.StatusCode.Should().Be(HttpStatusCode.OK);
        using var repetidoJson = JsonDocument.Parse(await repetido.Content.ReadAsStringAsync());
        repetidoJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid().Should().Be(lote);
        using var recuperado = await client.GetAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/lotes/{lote}");
        recuperado.StatusCode.Should().Be(HttpStatusCode.OK);
        recuperado.Headers.CacheControl!.NoStore.Should().BeTrue();
        using var lista = await client.GetAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/lotes");
        using var listaJson = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
        listaJson.RootElement.GetProperty("dados").GetArrayLength().Should().Be(1);
        listaJson.RootElement.GetProperty("dados")[0].TryGetProperty("previa", out _).Should().BeFalse();
        using var transacoes = await client.GetAsync($"/api/v1/transactions/portfolio/{carteira}");
        using var transacoesJson = JsonDocument.Parse(await transacoes.Content.ReadAsStringAsync());
        transacoesJson.RootElement.GetProperty("dados").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task RejeitaDataForaDoMesEAnoDoHtmlSemCriarLote()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Autenticado(factory);
        var carteira = await CriarCarteira(client);
        var html = HtmlComReferencia("Posição em Novembro/2026");

        using var incompatibilidade = await Enviar(client, carteira, "?dataReferencia=2026-10-31", html);

        incompatibilidade.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using (var problema = JsonDocument.Parse(await incompatibilidade.Content.ReadAsStringAsync()))
        {
            problema.RootElement.GetProperty("title").GetString().Should().Be("Data de referência incompatível");
            problema.RootElement.GetProperty("detail").GetString()
                .Should().Be("O extrato informa posição em novembro de 2026. Escolha uma data desse mês e ano.");
            problema.RootElement.GetProperty("codigo").GetString()
                .Should().Be("tesouro_data_referencia_mes_ano_incompativel");
        }

        using var listaVazia = await client.GetAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/lotes");
        using var listaJson = JsonDocument.Parse(await listaVazia.Content.ReadAsStringAsync());
        listaJson.RootElement.GetProperty("dados").GetArrayLength().Should().Be(0);

        using var compativel = await Enviar(client, carteira, "?dataReferencia=2026-11-30", html);
        compativel.StatusCode.Should().Be(HttpStatusCode.Created, await compativel.Content.ReadAsStringAsync());
        using var resposta = JsonDocument.Parse(await compativel.Content.ReadAsStringAsync());
        var previa = resposta.RootElement.GetProperty("dados").GetProperty("previa");
        previa.GetProperty("mesReferencia").GetInt32().Should().Be(11);
        previa.GetProperty("anoReferencia").GetInt32().Should().Be(2026);
        previa.GetProperty("instituicaoHtml").ValueKind.Should().Be(JsonValueKind.Null);
        previa.GetProperty("secaoPosicoesReconhecida").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task RejeitaDataIncompativelAntesDeRetomarLotePendenteComMesmoConteudo()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Autenticado(factory);
        var carteira = await CriarCarteira(client);
        var html = HtmlComReferencia("Posição em Novembro/2026");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(html))).ToLowerInvariant();
        var loteId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepositorioLoteImportacaoTesouro>();
            await repo.AdicionarAsync(new LoteImportacaoTesouro(loteId, carteira, FakeAuthProvider.TestUserId,
                new DateOnly(2026, 10, 31), hash, "html", "{}", DateTime.UtcNow));
            await repo.SalvarAsync();
        }

        using var incompatibilidade = await Enviar(client, carteira, "?dataReferencia=2026-10-31", html);

        incompatibilidade.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var lista = await client.GetAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/lotes");
        using var json = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
        var dados = json.RootElement.GetProperty("dados");
        dados.GetArrayLength().Should().Be(1);
        var existente = dados[0];
        existente.GetProperty("id").GetGuid().Should().Be(loteId);
        existente.GetProperty("estado").GetString().Should().Be("Pendente");
        existente.GetProperty("dataReferencia").GetString().Should().Be("2026-10-31");
    }

    [Fact]
    public async Task ExigeDataEIsolaCarteiraEAutor()
    {
        using var factory = new CustomWebApplicationFactory();
        using var titular = Autenticado(factory);
        using var outro = Autenticado(factory, true);
        var carteira = await CriarCarteira(titular);
        using var semData = await Enviar(titular, carteira, "");
        semData.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var semAcesso = await Enviar(outro, carteira, "?dataReferencia=2026-11-14");
        semAcesso.StatusCode.Should().Be(HttpStatusCode.NotFound, await semAcesso.Content.ReadAsStringAsync());
        using var inicial = await Enviar(titular, carteira, "?dataReferencia=2026-11-14");
        using var json = JsonDocument.Parse(await inicial.Content.ReadAsStringAsync());
        var lote = json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        using var bloqueado = await outro.GetAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/lotes/{lote}");
        bloqueado.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RejeitaArquivoInvalidoESemAutenticacaoSemEcoarConteudo()
    {
        using var factory = new CustomWebApplicationFactory();
        using var titular = Autenticado(factory);
        using var anonimo = factory.CreateClient();
        var carteira = await CriarCarteira(titular);
        using var semSessao = await Enviar(anonimo, carteira, "?dataReferencia=2026-11-14");
        semSessao.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var invalido = await Enviar(titular, carteira, "?dataReferencia=2026-11-14", "documento sintético inválido");
        invalido.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await invalido.Content.ReadAsStringAsync()).Should().NotContain("documento sintético inválido");
        using var lista = await titular.GetAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/lotes");
        using var json = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("dados").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListaPaginadaLimitaResultadosESomenteDevolveMetadadosDoAutor()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Autenticado(factory);
        var carteira = await CriarCarteira(client);
        using (var scope = factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepositorioLoteImportacaoTesouro>();
            for (var i = 0; i < 52; i++)
                await repo.AdicionarAsync(new LoteImportacaoTesouro(Guid.NewGuid(), carteira, FakeAuthProvider.TestUserId,
                    new DateOnly(2026, 1, 1).AddDays(i), i.ToString("x64"), "html", "{\"conteudoPrivado\":\"sintético\"}",
                    DateTime.UnixEpoch.AddDays(i)));
            await repo.AdicionarAsync(new LoteImportacaoTesouro(Guid.NewGuid(), carteira, FakeAuthProvider.SecondTestUserId,
                new DateOnly(2026, 3, 1), new string('f', 64), "html", "{}", DateTime.UtcNow));
            await repo.SalvarAsync();
        }
        using var primeira = await client.GetAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/lotes");
        primeira.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await primeira.Content.ReadAsStringAsync();
        corpo.Should().NotContain("conteudoPrivado").And.NotContain("sintético");
        using var json = JsonDocument.Parse(corpo);
        json.RootElement.GetProperty("dados").GetArrayLength().Should().Be(50);
        json.RootElement.GetProperty("dados")[0].GetProperty("dataReferencia").GetString().Should().Be("2026-02-21");
        using var segunda = await client.GetAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/lotes?pagina=2");
        using var segundaJson = JsonDocument.Parse(await segunda.Content.ReadAsStringAsync());
        segundaJson.RootElement.GetProperty("dados").GetArrayLength().Should().Be(2);
        using var invalida = await client.GetAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/lotes?pagina=0");
        invalida.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
