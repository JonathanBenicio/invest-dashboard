using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class ExtratoTesouroControllerTests
{
    [RequiresPostgresFact]
    public async Task MembroSomenteLeituraAcessaExtratoConfirmadoDaCarteiraCompartilhadaSemExporLotePendente()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var administrador = factory.CreateClient();
        administrador.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        using var membroLeitor = factory.CreateClient();
        membroLeitor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(FakeAuthProvider.SecondTestUserId,
                FakeAuthProvider.SecondTestSessionId, FakeAuthProvider.SecondTestEmail,
                FakeAuthProvider.SecondTestName));

        using var criacaoGrupo = await administrador.PostAsJsonAsync("/api/v1/grupos-carteiras",
            new { nome = "Grupo de leitura do extrato" });
        criacaoGrupo.StatusCode.Should().Be(HttpStatusCode.OK, await criacaoGrupo.Content.ReadAsStringAsync());
        using var grupoJson = JsonDocument.Parse(await criacaoGrupo.Content.ReadAsStringAsync());
        var grupoId = grupoJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        var membershipId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.GroupMembers.Add(new MembroGrupo(membershipId, grupoId,
                FakeAuthProvider.SecondTestUserId.ToString(), FakeAuthProvider.SecondTestEmail,
                FakeAuthProvider.SecondTestName, PapelGrupo.Consulta, DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        using var criacaoCarteira = await administrador.PostAsJsonAsync("/api/v1/portfolios", new
        {
            nome = "Carteira compartilhada para consulta",
            grupoId,
            instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),
            visibilidade = "PublicaDoGrupo"
        });
        criacaoCarteira.StatusCode.Should().Be(HttpStatusCode.Created,
            await criacaoCarteira.Content.ReadAsStringAsync());
        using var carteiraJson = JsonDocument.Parse(await criacaoCarteira.Content.ReadAsStringAsync());
        var carteiraId = carteiraJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        var ativoId = Guid.NewGuid();
        var loteAtivoId = Guid.NewGuid();
        var lotePendenteId = Guid.NewGuid();
        var dataCriacao = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        var compra = Movimento(new DateOnly(2026, 10, 1), "Compra", "Crédito", 2m, 100m, 10m, 200m,
            [new CampoPreviaTesouroDto("Natureza:", "Compra"), new CampoPreviaTesouroDto("Valor:", "200,00")]);
        var loteAtivo = Lote(loteAtivoId, carteiraId, FakeAuthProvider.TestUserId,
            new DateOnly(2026, 11, 14), new string('a', 64), Previa(carteiraId, [compra]),
            dataCriacao, dataCriacao.AddDays(1));
        var lotePendente = new LoteImportacaoTesouro(lotePendenteId, carteiraId,
            FakeAuthProvider.TestUserId, new DateOnly(2026, 11, 15), new string('b', 64), "html",
            JsonSerializer.Serialize(Previa(carteiraId, [compra])), dataCriacao.AddDays(2));
        var baseAtiva = new BasePosicaoTesouro(Guid.NewGuid(), loteAtivoId, carteiraId, ativoId,
            new DateOnly(2026, 11, 14), 2m, 200m, 100m);
        baseAtiva.Ativar();
        var eventoOficial = Evento(loteAtivoId, carteiraId, ativoId, 0, compra, dataCriacao.AddDays(1));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.FixedIncomeAssets.Add(new RendaFixa(ativoId, "TD2035", "Tesouro Prefixado 2035", 100m,
                dataCriacao, "PREFIXADO", 10m, new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "Tesouro Nacional", "TESOURO_DIRETO"));
            db.AddRange(loteAtivo, lotePendente, baseAtiva, eventoOficial);
            await db.SaveChangesAsync();
        }

        var rota = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/posicoes/{ativoId}/extrato";
        using var extratoLeitura = await membroLeitor.GetAsync(rota);
        extratoLeitura.StatusCode.Should().Be(HttpStatusCode.OK,
            await extratoLeitura.Content.ReadAsStringAsync());
        using var extratoJson = JsonDocument.Parse(await extratoLeitura.Content.ReadAsStringAsync());
        var extrato = extratoJson.RootElement.GetProperty("dados");
        extrato.GetProperty("baseAtiva").GetProperty("loteId").GetGuid().Should().Be(loteAtivoId);
        extrato.GetProperty("eventos").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("loteId").GetGuid().Should().Be(loteAtivoId);

        using var lotePendenteLeitura = await membroLeitor.GetAsync($"{rota}?loteId={lotePendenteId}");
        lotePendenteLeitura.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var lotePendenteDireto = await membroLeitor.GetAsync(
            $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{lotePendenteId}");
        lotePendenteDireto.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var loteConfirmadoDireto = await membroLeitor.GetAsync(
            $"/api/v1/portfolios/{carteiraId}/tesouro-direto/lotes/{loteAtivoId}");
        loteConfirmadoDireto.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            (await db.Set<LoteImportacaoTesouro>().CountAsync(item => item.CarteiraId == carteiraId)).Should().Be(2);
            (await db.Set<BasePosicaoTesouro>().CountAsync(item => item.CarteiraId == carteiraId)).Should().Be(1);
            (await db.Set<EventoOficialTesouro>().CountAsync(item => item.CarteiraId == carteiraId)).Should().Be(1);
            (await db.Set<LoteImportacaoTesouro>().SingleAsync(item => item.Id == loteAtivoId)).Estado.Should().Be("Confirmado");
            (await db.Set<LoteImportacaoTesouro>().SingleAsync(item => item.Id == lotePendenteId)).Estado.Should().Be("Pendente");
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.GroupMembers.Remove(await db.GroupMembers.SingleAsync(item => item.Id == membershipId));
            await db.SaveChangesAsync();
        }
        (await membroLeitor.GetAsync(rota)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [RequiresPostgresFact]
    public async Task Extrato_UsaComparacaoPersistidaParaConfirmarEncerramentoPorVenda()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var cliente = factory.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));

        var carteiraId = Guid.NewGuid();
        var ativoId = Guid.NewGuid();
        var loteAnteriorId = Guid.NewGuid();
        var loteAtualId = Guid.NewGuid();
        var criadoEmUtc = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        var venda = Movimento(new DateOnly(2026, 11, 12), "Venda", "Débito", 5m, 100m, null, 500m,
            [new CampoPreviaTesouroDto("Natureza:", "Venda"), new CampoPreviaTesouroDto("Valor bruto:", "500,00")]);
        var loteAnterior = Lote(loteAnteriorId, carteiraId, FakeAuthProvider.TestUserId,
            new DateOnly(2026, 11, 6), new string('d', 64), Previa(carteiraId, []), criadoEmUtc,
            criadoEmUtc.AddDays(1));
        var loteAtual = Lote(loteAtualId, carteiraId, FakeAuthProvider.TestUserId,
            new DateOnly(2026, 11, 14), new string('e', 64), Previa(carteiraId, [venda], apresentarPosicao: false),
            criadoEmUtc.AddDays(10), criadoEmUtc.AddDays(11));
        var baseAnterior = new BasePosicaoTesouro(Guid.NewGuid(), loteAnteriorId, carteiraId, ativoId,
            new DateOnly(2026, 11, 6), 5m, 500m, 100m);
        baseAnterior.Ativar();
        baseAnterior.Desativar();
        var baseEncerrada = new BasePosicaoTesouro(Guid.NewGuid(), loteAtualId, carteiraId, ativoId,
            new DateOnly(2026, 11, 14), 0m, 0m, 100m);
        baseEncerrada.Ativar();
        var comparacaoPersistida = new ComparacaoSnapshotTesouroDto(new string('f', 64),
            new DateOnly(2026, 11, 6), 5m, 0m,
            [new DivergenciaSnapshotTesouroDto("posicao_encerrada_por_venda", "posicao",
                "As vendas confirmadas zeraram a quantidade reconciliada.", "5", "0")], true);
        var revisao = new RevisaoSnapshotTesouro(Guid.NewGuid(), carteiraId, loteAtualId,
            FakeAuthProvider.TestUserId, ativoId, comparacaoPersistida.Hash,
            JsonSerializer.Serialize(comparacaoPersistida), "aceitar_nova_base", 0,
            criadoEmUtc.AddDays(11));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Portfolios.Add(new Carteira(carteiraId, FakeAuthProvider.TestUserId.ToString(), "Carteira sintética"));
            db.FixedIncomeAssets.Add(new RendaFixa(ativoId, "TD2035", "Tesouro Prefixado 2035", 100m,
                criadoEmUtc, "PREFIXADO", 10m, new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "Tesouro Nacional", "TESOURO_DIRETO"));
            db.AddRange(loteAnterior, loteAtual, baseAnterior, baseEncerrada, revisao);
            db.Set<EventoOficialTesouro>().Add(Evento(loteAtualId, carteiraId, ativoId, 0, venda, criadoEmUtc.AddDays(11)));
            await db.SaveChangesAsync();
        }

        using var resposta = await cliente.GetAsync($"/api/v1/portfolios/{carteiraId}/tesouro-direto/posicoes/{ativoId}/extrato");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var dados = json.RootElement.GetProperty("dados");
        dados.GetProperty("baseAtiva").GetProperty("quantidade").GetDecimal().Should().Be(0m);
        var resumo = dados.GetProperty("resumoUltimaImportacao");
        resumo.GetProperty("quantidadeAnterior").GetDecimal().Should().Be(5m);
        resumo.GetProperty("variacaoQuantidade").GetDecimal().Should().Be(-5m);
        var tipos = resumo.GetProperty("divergencias").EnumerateArray()
            .Select(item => item.GetProperty("tipo").GetString()).ToArray();
        tipos.Should().Contain("posicao_encerrada_por_venda");
        tipos.Should().NotContain("posicao_nao_observada");
    }

    [RequiresPostgresFact]
    public async Task Extrato_ExpõeBaseAtivaPaginadaEDeltaDaBaseAnteriorComIsolamento()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var cliente = factory.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        using var outroUsuario = factory.CreateClient();
        outroUsuario.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(FakeAuthProvider.SecondTestUserId,
                FakeAuthProvider.SecondTestSessionId, FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName));

        var carteiraId = Guid.NewGuid();
        var ativoId = Guid.NewGuid();
        var outroAtivoId = Guid.NewGuid();
        var loteAnteriorId = Guid.NewGuid();
        var loteAtivoId = Guid.NewGuid();
        var lotePendenteId = Guid.NewGuid();
        var criadoEmUtc = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        var criadoAtivoEmUtc = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
        var venda = Movimento(new DateOnly(2026, 11, 12), "Venda", "Débito", 1m, 100m, null, 1_000m,
            [new CampoPreviaTesouroDto("Natureza:", "Venda"), new CampoPreviaTesouroDto("Valor bruto:", "1.000,00")]);
        var taxa = Movimento(new DateOnly(2026, 11, 13), "Taxas", "Débito", null, null, null, 2m,
            [new CampoPreviaTesouroDto("Taxa da B3:", "2,00")]);
        var compraAnterior = Movimento(new DateOnly(2026, 10, 1), "Compra", "Crédito", 5m, 100m, 10m, 500m,
            [new CampoPreviaTesouroDto("Natureza:", "Compra"), new CampoPreviaTesouroDto("Valor:", "500,00")]);
        var previaAnterior = Previa(carteiraId, [compraAnterior]);
        var previaAtual = Previa(carteiraId, [compraAnterior, venda, taxa]);
        var movimentoOutroAtivo = Movimento(new DateOnly(2026, 11, 10), "Compra", "Crédito", 2m, 50m, 10m, 100m,
            [new CampoPreviaTesouroDto("Natureza:", "Compra"), new CampoPreviaTesouroDto("Valor:", "100,00")]);
        var loteAnterior = Lote(loteAnteriorId, carteiraId, FakeAuthProvider.TestUserId,
            new DateOnly(2026, 11, 6), new string('a', 64), previaAnterior, criadoEmUtc,
            criadoEmUtc.AddDays(1));
        var loteAtivo = Lote(loteAtivoId, carteiraId, FakeAuthProvider.TestUserId,
            new DateOnly(2026, 11, 14), new string('b', 64), previaAtual, criadoAtivoEmUtc,
            criadoAtivoEmUtc.AddDays(1));
        var lotePendente = new LoteImportacaoTesouro(lotePendenteId, carteiraId, FakeAuthProvider.TestUserId,
            new DateOnly(2026, 11, 15), new string('c', 64), "html", JsonSerializer.Serialize(previaAtual),
            criadoAtivoEmUtc.AddDays(2));

        var baseAnterior = new BasePosicaoTesouro(Guid.NewGuid(), loteAnteriorId, carteiraId, ativoId,
            new DateOnly(2026, 11, 6), 5m, 500m, 100m);
        baseAnterior.Ativar();
        baseAnterior.Desativar();
        var baseAtiva = new BasePosicaoTesouro(Guid.NewGuid(), loteAtivoId, carteiraId, ativoId,
            new DateOnly(2026, 11, 14), 4m, 400m, 100m);
        baseAtiva.Ativar();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Portfolios.Add(new Carteira(carteiraId, FakeAuthProvider.TestUserId.ToString(), "Carteira sintética"));
            db.FixedIncomeAssets.AddRange(
                new RendaFixa(ativoId, "TD2035", "Tesouro Prefixado 2035", 100m, criadoEmUtc,
                    "PREFIXADO", 10m, new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Tesouro Nacional", "TESOURO_DIRETO"),
                new RendaFixa(outroAtivoId, "TD2036", "Tesouro Prefixado 2036", 50m, criadoEmUtc,
                    "PREFIXADO", 10m, new DateTime(2036, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Tesouro Nacional", "TESOURO_DIRETO"));
            db.AddRange(loteAnterior, loteAtivo, lotePendente, baseAnterior, baseAtiva);
            db.Set<EventoOficialTesouro>().AddRange(
                Evento(loteAtivoId, carteiraId, ativoId, 0, compraAnterior, criadoAtivoEmUtc),
                Evento(loteAtivoId, carteiraId, outroAtivoId, 1, movimentoOutroAtivo, criadoAtivoEmUtc),
                Evento(loteAtivoId, carteiraId, ativoId, 2, venda, criadoAtivoEmUtc),
                Evento(loteAtivoId, carteiraId, ativoId, 3, taxa, criadoAtivoEmUtc),
                Evento(lotePendenteId, carteiraId, ativoId, 0, taxa, criadoAtivoEmUtc.AddDays(2)));
            await db.SaveChangesAsync();
        }

        var rota = $"/api/v1/portfolios/{carteiraId}/tesouro-direto/posicoes/{ativoId}/extrato";
        using var primeiraPagina = await cliente.GetAsync($"{rota}?limite=1");
        primeiraPagina.StatusCode.Should().Be(HttpStatusCode.OK);
        primeiraPagina.Headers.CacheControl!.NoStore.Should().BeTrue();
        using var primeiraJson = JsonDocument.Parse(await primeiraPagina.Content.ReadAsStringAsync());
        var dadosPrimeiraPagina = primeiraJson.RootElement.GetProperty("dados");
        dadosPrimeiraPagina.GetProperty("baseAtiva").GetProperty("loteId").GetGuid().Should().Be(loteAtivoId);
        dadosPrimeiraPagina.GetProperty("eventos").GetArrayLength().Should().Be(1);
        dadosPrimeiraPagina.GetProperty("eventos")[0].GetProperty("indiceEvento").GetInt32().Should().Be(0);
        dadosPrimeiraPagina.GetProperty("eventos")[0].GetProperty("origem").GetString().Should().Be("extrato_oficial_tesouro");
        dadosPrimeiraPagina.GetProperty("proximoIndice").GetInt32().Should().Be(0);
        var resumo = dadosPrimeiraPagina.GetProperty("resumoUltimaImportacao");
        resumo.GetProperty("loteAnteriorId").GetGuid().Should().Be(loteAnteriorId);
        resumo.GetProperty("quantidadeAnterior").GetDecimal().Should().Be(5m);
        resumo.GetProperty("quantidadeAtual").GetDecimal().Should().Be(4m);
        resumo.GetProperty("variacaoQuantidade").GetDecimal().Should().Be(-1m);
        resumo.GetProperty("divergencias").EnumerateArray()
            .Any(item => item.GetProperty("tipo").GetString() == "evento_novo").Should().BeTrue();

        using var segundaPagina = await cliente.GetAsync($"{rota}?loteId={loteAtivoId}&depoisDoIndice=0&limite=1");
        segundaPagina.StatusCode.Should().Be(HttpStatusCode.OK);
        using var segundaJson = JsonDocument.Parse(await segundaPagina.Content.ReadAsStringAsync());
        var dadosSegundaPagina = segundaJson.RootElement.GetProperty("dados");
        dadosSegundaPagina.GetProperty("eventos").GetArrayLength().Should().Be(1);
        dadosSegundaPagina.GetProperty("eventos")[0].GetProperty("indiceEvento").GetInt32().Should().Be(2);
        dadosSegundaPagina.GetProperty("proximoIndice").GetInt32().Should().Be(2);

        using var terceiraPagina = await cliente.GetAsync($"{rota}?loteId={loteAtivoId}&depoisDoIndice=2&limite=1");
        terceiraPagina.StatusCode.Should().Be(HttpStatusCode.OK);
        using var terceiraJson = JsonDocument.Parse(await terceiraPagina.Content.ReadAsStringAsync());
        var dadosTerceiraPagina = terceiraJson.RootElement.GetProperty("dados");
        dadosTerceiraPagina.GetProperty("eventos").GetArrayLength().Should().Be(1);
        dadosTerceiraPagina.GetProperty("eventos")[0].GetProperty("indiceEvento").GetInt32().Should().Be(3);
        dadosTerceiraPagina.GetProperty("eventos")[0].GetProperty("dados").GetProperty("dadosInterpretados")
            .GetProperty("natureza").GetString().Should().Be("Taxas");
        dadosTerceiraPagina.GetProperty("proximoIndice").ValueKind.Should().Be(JsonValueKind.Null);

        using var loteDesatualizado = await cliente.GetAsync($"{rota}?loteId={loteAnteriorId}");
        loteDesatualizado.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var respostaLotePendente = await cliente.GetAsync($"{rota}?loteId={lotePendenteId}");
        respostaLotePendente.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var semBase = await cliente.GetAsync($"/api/v1/portfolios/{carteiraId}/tesouro-direto/posicoes/{outroAtivoId}/extrato");
        semBase.StatusCode.Should().Be(HttpStatusCode.OK);
        using var semBaseJson = JsonDocument.Parse(await semBase.Content.ReadAsStringAsync());
        semBaseJson.RootElement.GetProperty("dados").GetProperty("baseAtiva").ValueKind.Should().Be(JsonValueKind.Null);
        semBaseJson.RootElement.GetProperty("dados").GetProperty("eventos").GetArrayLength().Should().Be(0);
        (await outroUsuario.GetAsync(rota)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static LoteImportacaoTesouro Lote(Guid id, Guid carteiraId, Guid usuarioId, DateOnly dataReferencia,
        string hash, PreviaTesouroDto previa, DateTime criadoEmUtc, DateTime confirmadoEmUtc)
    {
        var lote = new LoteImportacaoTesouro(id, carteiraId, usuarioId, dataReferencia, hash,
            "html", JsonSerializer.Serialize(previa), criadoEmUtc);
        lote.MarcarConfirmado(confirmadoEmUtc);
        return lote;
    }

    private static EventoOficialTesouro Evento(Guid loteId, Guid carteiraId, Guid ativoId, int indiceEvento,
        MovimentoPreviaTesouroDto movimento, DateTime criadoEmUtc) =>
        new(Guid.NewGuid(), loteId, carteiraId, ativoId, indiceEvento,
            JsonSerializer.Serialize(movimento), null, criadoEmUtc);

    private static MovimentoPreviaTesouroDto Movimento(DateOnly data, string natureza, string tipo,
        decimal? quantidade, decimal? preco, decimal? taxaAnual, decimal? valor,
        IReadOnlyList<CampoPreviaTesouroDto> campos) =>
        new(campos, new DadosMovimentoTesouroDto(data, natureza, tipo, quantidade, preco, taxaAnual,
            valor, []));

    private static PreviaTesouroDto Previa(Guid carteiraId, IReadOnlyList<MovimentoPreviaTesouroDto> movimentos,
        bool apresentarPosicao = true)
    {
        var posicoes = apresentarPosicao
            ? new[]
            {
                new PosicaoPreviaTesouroDto(1, new Dictionary<string, string> { ["título"] = "Tesouro Prefixado 2035" },
                    new DadosPosicaoTesouroDto(new DateOnly(2026, 11, 1), 5m, 100m, 500m, "10%", null,
                        null, 600m, null, null, null, null, null, null, null, []))
            }
            : [];

        return new PreviaTesouroDto(carteiraId, "html", "portal_tesouro_html", "Tesouro Prefixado 2035",
            "2035-01-01", null, posicoes, movimentos, [], [], true, true, true);
    }
}
