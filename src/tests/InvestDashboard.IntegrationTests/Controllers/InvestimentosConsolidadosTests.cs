using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class InvestimentosConsolidadosTests
{
    [Fact]
    public async Task PosicoesSemGrupoPermanecemIndividuaisMesmoQuandoCompartilhamOAtivo()
    {
        using var factory = new CustomWebApplicationFactory();
        using var cliente = CriarCliente(factory, FakeAuthProvider.TestUserId,
            FakeAuthProvider.TestSessionId, FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        var agora = DateTime.UtcNow;
        var ativo = new Acao(Guid.NewGuid(), "SOLO1", "Ação sem grupo", 10m, agora, "Tecnologia");
        var carteiraA = new Carteira(Guid.NewGuid(), FakeAuthProvider.TestUserId.ToString(), "Carteira independente A");
        var carteiraB = new Carteira(Guid.NewGuid(), FakeAuthProvider.TestUserId.ToString(), "Carteira independente B");
        var posicaoA = new PosicaoInvestimento(Guid.NewGuid(), carteiraA.Id, ativo.Id, ativo.Ticker,
            TipoAtivo.Acao, ativo.CurrentPrice);
        var posicaoB = new PosicaoInvestimento(Guid.NewGuid(), carteiraB.Id, ativo.Id, ativo.Ticker,
            TipoAtivo.Acao, ativo.CurrentPrice);
        posicaoA.AddShares(2m, 10m, 0m, agora.AddDays(-2));
        posicaoB.AddShares(3m, 11m, 0m, agora.AddDays(-1));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Portfolios.AddRange(carteiraA, carteiraB);
            db.StockAssets.Add(ativo);
            db.AssetPositions.AddRange(posicaoA, posicaoB);
            await db.SaveChangesAsync();
        }

        using var resposta = await cliente.GetAsync("/api/v1/investments/consolidated?tipo=variable_income&busca=SOLO1");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var posicoes = json.RootElement.GetProperty("dados").EnumerateArray().ToArray();

        posicoes.Should().HaveCount(2);
        posicoes.Should().OnlyContain(posicao => posicao.GetProperty("escopo").GetString() == "carteira");
        posicoes.Should().OnlyContain(posicao => posicao.GetProperty("grupoId").ValueKind == JsonValueKind.Null);
        posicoes.Select(posicao => posicao.GetProperty("carteiraId").GetGuid()).Distinct().Should().HaveCount(2);
        posicoes.Select(posicao => posicao.GetProperty("quantidade").GetDecimal()).Should().BeEquivalentTo([2m, 3m]);
    }

    [Fact]
    public async Task ConsolidaPosicoesAntesDaPaginacaoEIsolaGrupoTitularCarteiraEInstituicao()
    {
        using var factory = new CustomWebApplicationFactory();
        using var administrador = CriarCliente(factory, FakeAuthProvider.TestUserId,
            FakeAuthProvider.TestSessionId, FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        using var membroParcial = CriarCliente(factory, FakeAuthProvider.SecondTestUserId,
            FakeAuthProvider.SecondTestSessionId, FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName);

        var grupo = await CriarGrupo(administrador, "Grupo para consolidação");
        var outroGrupo = await CriarGrupo(administrador, "Outro grupo");
        await AdicionarMembro(factory, grupo);
        var titularA = await CriarTitular(administrador, grupo, "Titular A", FakeAuthProvider.SecondTestUserId.ToString());
        var titularB = await CriarTitular(administrador, grupo, "Titular B");
        var carteiraA = await CriarCarteira(administrador, grupo, titularA, "Carteira A",
            "10000000-0000-4000-8000-000000000001", "Particular");
        var carteiraB = await CriarCarteira(administrador, grupo, titularB, "Carteira B",
            "10000000-0000-4000-8000-000000000004", "Particular");
        var carteiraOutroGrupo = await CriarCarteira(administrador, outroGrupo, null, "Carteira do outro grupo",
            "10000000-0000-4000-8000-000000000001");
        await Comprar(administrador, carteiraA, "DUPL1", 2m, 10m);
        await Comprar(administrador, carteiraB, "DUPL1", 3m, 11m);
        await Comprar(administrador, carteiraA, "OUTR1", 4m, 12m);
        await Comprar(administrador, carteiraOutroGrupo, "DUPL1", 5m, 13m);

        using var paginaUm = await administrador.GetAsync(
            $"/api/v1/investments/consolidated?grupoId={grupo}&tipo=variable_income&pagina=1&itensPorPagina=1");
        paginaUm.StatusCode.Should().Be(HttpStatusCode.OK, await paginaUm.Content.ReadAsStringAsync());
        using var paginaUmJson = JsonDocument.Parse(await paginaUm.Content.ReadAsStringAsync());
        paginaUmJson.RootElement.GetProperty("paginacao").GetProperty("totalItens").GetInt32().Should().Be(2);

        using var paginaDois = await administrador.GetAsync(
            $"/api/v1/investments/consolidated?grupoId={grupo}&tipo=variable_income&pagina=2&itensPorPagina=1");
        using var paginaDoisJson = JsonDocument.Parse(await paginaDois.Content.ReadAsStringAsync());
        var linhaDUPL1 = new[]
        {
            paginaUmJson.RootElement.GetProperty("dados")[0],
            paginaDoisJson.RootElement.GetProperty("dados")[0]
        }.Single(linha => linha.GetProperty("ticker").GetString() == "DUPL1");
        var ativoId = linhaDUPL1.GetProperty("ativoId").GetGuid();
        linhaDUPL1.GetProperty("grupoId").GetGuid().Should().Be(grupo);
        linhaDUPL1.GetProperty("quantidade").GetDecimal().Should().Be(5m);
        linhaDUPL1.GetProperty("quantidadeCarteiras").GetInt32().Should().Be(2);
        linhaDUPL1.GetProperty("totalInvestido").GetDecimal().Should().Be(53m);
        linhaDUPL1.GetProperty("valorAtual").GetDecimal().Should().Be(50m);
        linhaDUPL1.GetProperty("ganho").GetDecimal().Should().Be(-3m);
        linhaDUPL1.GetProperty("datasAvaliacaoCompletas").GetBoolean().Should().BeTrue();
        linhaDUPL1.GetProperty("datasAvaliacaoDiferentes").GetBoolean().Should().BeFalse();
        linhaDUPL1.GetProperty("dataAvaliacaoMaisAntiga").GetDateTime().Date
            .Should().Be(linhaDUPL1.GetProperty("dataAvaliacaoMaisRecente").GetDateTime().Date);

        using var detalhe = await administrador.GetAsync($"/api/v1/investments/consolidated/{grupo}/{ativoId}");
        detalhe.StatusCode.Should().Be(HttpStatusCode.OK, await detalhe.Content.ReadAsStringAsync());
        using var detalheJson = JsonDocument.Parse(await detalhe.Content.ReadAsStringAsync());
        detalheJson.RootElement.GetProperty("dados").GetProperty("carteiras").GetArrayLength().Should().Be(2);
        detalheJson.RootElement.GetProperty("dados").GetProperty("consolidado")
            .GetProperty("quantidade").GetDecimal().Should().Be(5m);
        detalheJson.RootElement.GetProperty("dados").GetProperty("carteiras").EnumerateArray()
            .Should().OnlyContain(carteira => carteira.GetProperty("dataAvaliacao").ValueKind != JsonValueKind.Null);

        using var titularList = await administrador.GetAsync(
            $"/api/v1/investments/consolidated?grupoId={grupo}&titularId={titularA}&tipo=variable_income&busca=DUPL1");
        using var titularJson = JsonDocument.Parse(await titularList.Content.ReadAsStringAsync());
        titularJson.RootElement.GetProperty("dados").EnumerateArray().Single()
            .GetProperty("quantidade").GetDecimal().Should().Be(2m);

        using var carteiraList = await administrador.GetAsync(
            $"/api/v1/investments/consolidated?grupoId={grupo}&carteiraId={carteiraB}&tipo=variable_income");
        using var carteiraJson = JsonDocument.Parse(await carteiraList.Content.ReadAsStringAsync());
        carteiraJson.RootElement.GetProperty("dados").EnumerateArray().Single()
            .GetProperty("escopo").GetString().Should().Be("carteira");
        carteiraJson.RootElement.GetProperty("dados").EnumerateArray().Single()
            .GetProperty("quantidade").GetDecimal().Should().Be(3m);

        using var porBanco = await administrador.GetAsync(
            $"/api/v1/investments/consolidated/{grupo}/{ativoId}?instituicaoFinanceiraId=10000000-0000-4000-8000-000000000001");
        using var porBancoJson = JsonDocument.Parse(await porBanco.Content.ReadAsStringAsync());
        porBancoJson.RootElement.GetProperty("dados").GetProperty("carteiras").GetArrayLength().Should().Be(1);
        porBancoJson.RootElement.GetProperty("dados").GetProperty("consolidado")
            .GetProperty("quantidade").GetDecimal().Should().Be(2m);

        using var todosGrupos = await administrador.GetAsync(
            "/api/v1/investments/consolidated?tipo=variable_income&itensPorPagina=50");
        using var todosGruposJson = JsonDocument.Parse(await todosGrupos.Content.ReadAsStringAsync());
        todosGruposJson.RootElement.GetProperty("dados").EnumerateArray()
            .Where(linha => linha.GetProperty("ticker").GetString() == "DUPL1")
            .Select(linha => linha.GetProperty("grupoId").GetGuid()).Distinct().Should().HaveCount(2);

        using var parcialmenteVisivel = await membroParcial.GetAsync($"/api/v1/investments/consolidated/{grupo}/{ativoId}");
        parcialmenteVisivel.StatusCode.Should().Be(HttpStatusCode.OK);
        using var parcialJson = JsonDocument.Parse(await parcialmenteVisivel.Content.ReadAsStringAsync());
        parcialJson.RootElement.GetProperty("dados").GetProperty("consolidado").GetProperty("quantidade").GetDecimal().Should().Be(2m);
        var carteirasVisiveis = parcialJson.RootElement.GetProperty("dados").GetProperty("carteiras")
            .EnumerateArray().Select(wallet => wallet.GetProperty("carteiraId").GetGuid()).ToArray();
        carteirasVisiveis.Should().ContainSingle().Which.Should().Be(carteiraA);
        carteirasVisiveis.Should().NotContain(carteiraB);
    }

    [Fact]
    public async Task ListaEDetalheConsolidadosFiltramPosicoesEncerradasEIncluemTodasSobDemanda()
    {
        using var factory = new CustomWebApplicationFactory();
        using var cliente = CriarCliente(factory, FakeAuthProvider.TestUserId,
            FakeAuthProvider.TestSessionId, FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        var grupo = await CriarGrupo(cliente, "Grupo com posição encerrada");
        var carteira = await CriarCarteira(cliente, grupo, null, "Carteira encerrada",
            "10000000-0000-4000-8000-000000000001");
        await Comprar(cliente, carteira, "FIMGR1", 2m, 10m);
        await Vender(cliente, carteira, "FIMGR1", 2m, 12m);
        await Comprar(cliente, carteira, "ATIV1", 1m, 20m);

        using var padrao = await cliente.GetAsync(
            $"/api/v1/investments/consolidated?grupoId={grupo}&tipo=variable_income");
        padrao.StatusCode.Should().Be(HttpStatusCode.OK, await padrao.Content.ReadAsStringAsync());
        using var padraoJson = JsonDocument.Parse(await padrao.Content.ReadAsStringAsync());
        padraoJson.RootElement.GetProperty("dados").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("ticker").GetString().Should().Be("ATIV1");

        using var encerradas = await cliente.GetAsync(
            $"/api/v1/investments/consolidated?grupoId={grupo}&tipo=variable_income&busca=FIMGR1&situacao=closed");
        encerradas.StatusCode.Should().Be(HttpStatusCode.OK, await encerradas.Content.ReadAsStringAsync());
        using var encerradasJson = JsonDocument.Parse(await encerradas.Content.ReadAsStringAsync());
        var encerrada = encerradasJson.RootElement.GetProperty("dados").EnumerateArray().Single();
        var ativoId = encerrada.GetProperty("ativoId").GetGuid();
        encerrada.GetProperty("situacao").GetString().Should().Be("closed");
        encerrada.GetProperty("quantidade").GetDecimal().Should().Be(0m);

        using var todas = await cliente.GetAsync(
            $"/api/v1/investments/consolidated?grupoId={grupo}&tipo=variable_income&situacao=all");
        using var todasJson = JsonDocument.Parse(await todas.Content.ReadAsStringAsync());
        todasJson.RootElement.GetProperty("dados").EnumerateArray().Should().HaveCount(2);

        using var detalhePadrao = await cliente.GetAsync($"/api/v1/investments/consolidated/{grupo}/{ativoId}");
        detalhePadrao.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var detalheEncerrado = await cliente.GetAsync(
            $"/api/v1/investments/consolidated/{grupo}/{ativoId}?situacao=closed");
        detalheEncerrado.StatusCode.Should().Be(HttpStatusCode.OK,
            await detalheEncerrado.Content.ReadAsStringAsync());
        using var detalheJson = JsonDocument.Parse(await detalheEncerrado.Content.ReadAsStringAsync());
        detalheJson.RootElement.GetProperty("dados").GetProperty("consolidado")
            .GetProperty("situacao").GetString().Should().Be("closed");
        detalheJson.RootElement.GetProperty("dados").GetProperty("carteiras").EnumerateArray()
            .Should().ContainSingle().Which.GetProperty("quantidade").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task ConsolidadoIndicaDataIndisponivelQuandoRendaFixaNaoPossuiFonteDeAvaliacao()
    {
        using var factory = new CustomWebApplicationFactory();
        using var cliente = CriarCliente(factory, FakeAuthProvider.TestUserId,
            FakeAuthProvider.TestSessionId, FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        var grupo = await CriarGrupo(cliente, "Grupo com avaliação desconhecida");
        var titular = await CriarTitular(cliente, grupo, "Titular CDB");
        var carteira = await CriarCarteira(cliente, grupo, titular, "Carteira CDB",
            "10000000-0000-4000-8000-000000000001");

        using var criacao = await cliente.PostAsJsonAsync("/api/v1/investments/fixed-income", new
        {
            carteiraId = carteira,
            nome = "CDB sem data de avaliação",
            subtipo = "CDB",
            emissor = "Banco sintético",
            valorPrincipal = 1_000m,
            valorExtrato = 1_050m,
            taxaJuros = 10m,
            indexador = "CDI",
            dataCompra = DateTime.UtcNow.AddDays(-10),
            dataVencimento = DateTime.UtcNow.AddYears(1),
            chaveIdempotencia = Guid.NewGuid(),
            convencao = "365 dias corridos"
        });
        criacao.StatusCode.Should().Be(HttpStatusCode.Created, await criacao.Content.ReadAsStringAsync());

        using var lista = await cliente.GetAsync(
            $"/api/v1/investments/consolidated?grupoId={grupo}&tipo=fixed_income&busca=CDB%20sem%20data");
        lista.StatusCode.Should().Be(HttpStatusCode.OK, await lista.Content.ReadAsStringAsync());
        using var listaJson = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
        var resumo = listaJson.RootElement.GetProperty("dados").EnumerateArray().Single();
        resumo.GetProperty("datasAvaliacaoCompletas").GetBoolean().Should().BeFalse();
        resumo.GetProperty("datasAvaliacaoDiferentes").GetBoolean().Should().BeFalse();
        resumo.GetProperty("dataAvaliacaoMaisAntiga").ValueKind.Should().Be(JsonValueKind.Null);
        resumo.GetProperty("dataAvaliacaoMaisRecente").ValueKind.Should().Be(JsonValueKind.Null);

        var ativoId = resumo.GetProperty("ativoId").GetGuid();
        using var detalhe = await cliente.GetAsync($"/api/v1/investments/consolidated/{grupo}/{ativoId}");
        detalhe.StatusCode.Should().Be(HttpStatusCode.OK, await detalhe.Content.ReadAsStringAsync());
        using var detalheJson = JsonDocument.Parse(await detalhe.Content.ReadAsStringAsync());
        var contribuicao = detalheJson.RootElement.GetProperty("dados").GetProperty("carteiras").EnumerateArray().Single();
        contribuicao.GetProperty("dataAvaliacao").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [RequiresPostgresFact]
    public async Task ConsolidadoSinalizaDatasDeAvaliacaoDiferentesEntreBasesDoMesmoTesouro()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var cliente = CriarCliente(factory, FakeAuthProvider.TestUserId,
            FakeAuthProvider.TestSessionId, FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        var grupo = await CriarGrupo(cliente, "Grupo com bases de Tesouro em datas distintas");
        var carteiraA = await CriarCarteira(cliente, grupo, null, "Carteira 10 de novembro",
            "10000000-0000-4000-8000-000000000001");
        var carteiraB = await CriarCarteira(cliente, grupo, null, "Carteira 11 de novembro",
            "10000000-0000-4000-8000-000000000001");
        var ativoId = Guid.NewGuid();
        var ativo = new RendaFixa(ativoId, "TD2035", "Tesouro Prefixado 2035", 100m,
            new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc), "PREFIXADO", 10m,
            new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Tesouro Nacional", "TESOURO_DIRETO");
        var dataAplicacao = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var posicaoA = new PosicaoInvestimento(Guid.NewGuid(), carteiraA, ativoId, ativo.Ticker,
            TipoAtivo.RendaFixa, 100m);
        posicaoA.AddShares(2m, 100m, 0m, dataAplicacao);
        var posicaoB = new PosicaoInvestimento(Guid.NewGuid(), carteiraB, ativoId, ativo.Ticker,
            TipoAtivo.RendaFixa, 100m);
        posicaoB.AddShares(3m, 100m, 0m, dataAplicacao);
        var loteAId = Guid.NewGuid();
        var loteBId = Guid.NewGuid();
        var instante = new DateTime(2026, 11, 12, 12, 0, 0, DateTimeKind.Utc);
        var loteA = new LoteImportacaoTesouro(loteAId, carteiraA, FakeAuthProvider.TestUserId,
            new DateOnly(2026, 11, 10), new string('a', 64), "html", "{}", instante);
        loteA.MarcarConfirmado(instante.AddHours(1));
        var loteB = new LoteImportacaoTesouro(loteBId, carteiraB, FakeAuthProvider.TestUserId,
            new DateOnly(2026, 11, 11), new string('b', 64), "html", "{}", instante);
        loteB.MarcarConfirmado(instante.AddHours(1));
        var baseA = new BasePosicaoTesouro(Guid.NewGuid(), loteAId, carteiraA, ativoId,
            new DateOnly(2026, 11, 10), 2m, 200m, 100m);
        var baseB = new BasePosicaoTesouro(Guid.NewGuid(), loteBId, carteiraB, ativoId,
            new DateOnly(2026, 11, 11), 3m, 300m, 100m);
        baseA.Ativar();
        baseB.Ativar();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.FixedIncomeAssets.Add(ativo);
            db.AssetPositions.AddRange(posicaoA, posicaoB);
            db.AddRange(loteA, loteB, baseA, baseB);
            await db.SaveChangesAsync();
        }

        using var resposta = await cliente.GetAsync(
            $"/api/v1/investments/consolidated?grupoId={grupo}&tipo=fixed_income&busca=TD2035");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var consolidado = json.RootElement.GetProperty("dados").EnumerateArray().Single();
        consolidado.GetProperty("quantidade").GetDecimal().Should().Be(5m);
        consolidado.GetProperty("datasAvaliacaoDiferentes").GetBoolean().Should().BeTrue();
        consolidado.GetProperty("datasAvaliacaoCompletas").GetBoolean().Should().BeTrue();
        consolidado.GetProperty("dataAvaliacaoMaisAntiga").GetDateTime().Date
            .Should().Be(new DateTime(2026, 11, 10));
        consolidado.GetProperty("dataAvaliacaoMaisRecente").GetDateTime().Date
            .Should().Be(new DateTime(2026, 11, 11));
    }

    [Fact]
    public async Task DatasCompletasDeVencimentoDiferentesNoMesmoAnoPermanecemEmLinhasSeparadas()
    {
        using var factory = new CustomWebApplicationFactory();
        using var cliente = CriarCliente(factory, FakeAuthProvider.TestUserId,
            FakeAuthProvider.TestSessionId, FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        var grupo = await CriarGrupo(cliente, "Grupo com vencimentos distintos");
        var carteira = await CriarCarteira(cliente, grupo, null, "Carteira de renda fixa",
            "10000000-0000-4000-8000-000000000001");

        foreach (var vencimento in new[] { new DateTime(2031, 6, 30), new DateTime(2031, 12, 31) })
        {
            using var criacao = await cliente.PostAsJsonAsync("/api/v1/investments/fixed-income", new
            {
                carteiraId = carteira,
                nome = "CDB sintético mesmo emissor",
                subtipo = "CDB",
                emissor = "Banco sintético",
                valorPrincipal = 1_000m,
                valorExtrato = 1_000m,
                taxaJuros = 10m,
                indexador = "CDI",
                dataCompra = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
                dataVencimento = vencimento,
                chaveIdempotencia = Guid.NewGuid(),
                convencao = "365 dias corridos"
            });
            criacao.StatusCode.Should().Be(HttpStatusCode.Created, await criacao.Content.ReadAsStringAsync());
        }

        using var resposta = await cliente.GetAsync(
            $"/api/v1/investments/consolidated?grupoId={grupo}&tipo=fixed_income&emissor=Banco%20sint%C3%A9tico");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var linhas = json.RootElement.GetProperty("dados").EnumerateArray().ToArray();
        linhas.Should().HaveCount(2);
        linhas.Select(linha => linha.GetProperty("dataVencimento").GetDateTime().Date)
            .Should().BeEquivalentTo([new DateTime(2031, 6, 30), new DateTime(2031, 12, 31)]);
    }

    [Fact]
    public async Task AcoesDoMesmoTickerCompradasEmAnosDiferentesContinuamConsolidadas()
    {
        using var factory = new CustomWebApplicationFactory();
        using var cliente = CriarCliente(factory, FakeAuthProvider.TestUserId,
            FakeAuthProvider.TestSessionId, FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        var grupo = await CriarGrupo(cliente, "Grupo com compras em anos diferentes");
        var carteiraA = await CriarCarteira(cliente, grupo, null, "Carteira da primeira compra",
            "10000000-0000-4000-8000-000000000001");
        var carteiraB = await CriarCarteira(cliente, grupo, null, "Carteira da segunda compra",
            "10000000-0000-4000-8000-000000000001");
        await Comprar(cliente, carteiraA, "YEAR1", 2m, 10m, new DateTime(2021, 3, 15, 12, 0, 0, DateTimeKind.Utc));
        await Comprar(cliente, carteiraB, "YEAR1", 3m, 12m, new DateTime(2025, 8, 20, 12, 0, 0, DateTimeKind.Utc));

        using var resposta = await cliente.GetAsync(
            $"/api/v1/investments/consolidated?grupoId={grupo}&tipo=variable_income&busca=YEAR1");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var linhas = json.RootElement.GetProperty("dados").EnumerateArray().ToArray();
        linhas.Should().ContainSingle();
        linhas[0].GetProperty("quantidade").GetDecimal().Should().Be(5m);
        linhas[0].GetProperty("quantidadeCarteiras").GetInt32().Should().Be(2);
    }

    private static HttpClient CriarCliente(CustomWebApplicationFactory factory, Guid userId, Guid sessionId,
        string email, string nome)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(userId, sessionId, email, nome));
        return client;
    }

    private static async Task<Guid> CriarGrupo(HttpClient client, string nome)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/grupos-carteiras", new { nome });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CriarTitular(HttpClient client, Guid grupoId, string nome, string? usuarioId = null)
    {
        using var response = await client.PostAsJsonAsync($"/api/v1/grupos-carteiras/{grupoId}/titulares", new { nome, usuarioId });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CriarCarteira(HttpClient client, Guid grupoId, Guid? titularId,
        string nome, string instituicaoId, string visibilidade = "PublicaDoGrupo")
    {
        using var response = await client.PostAsJsonAsync("/api/v1/portfolios", new
        {
            nome,
            grupoId,
            titularId,
            instituicaoFinanceiraId = Guid.Parse(instituicaoId),
            visibilidade
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task AdicionarMembro(CustomWebApplicationFactory factory, Guid grupoId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
        db.GroupMembers.Add(new MembroGrupo(Guid.NewGuid(), grupoId, FakeAuthProvider.SecondTestUserId.ToString(),
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName, PapelGrupo.Consulta, DateTime.UtcNow));
        await db.SaveChangesAsync();
    }

    private static async Task Comprar(HttpClient client, Guid carteiraId, string ticker, decimal quantidade,
        decimal preco, DateTime? dataTransacao = null)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId,
            ticker,
            tipo = "Buy",
            classeAtivo = "ACAO",
            nome = ticker,
            setor = "Tecnologia",
            quantidade,
            precoUnitario = preco,
            taxas = 0m,
            dataTransacao = dataTransacao ?? DateTime.UtcNow,
            chaveIdempotencia = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    private static async Task Vender(HttpClient client, Guid carteiraId, string ticker, decimal quantidade, decimal preco)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId,
            ticker,
            tipo = "Sell",
            modalidadeFiscal = "Comum",
            classeAtivo = "ACAO",
            quantidade,
            precoUnitario = preco,
            taxas = 0m,
            dataTransacao = DateTime.UtcNow.AddMinutes(1),
            chaveIdempotencia = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }
}
