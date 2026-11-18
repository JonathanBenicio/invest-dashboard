using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class TesouroTaxasIsolamentoTests
{
    [Fact]
    public async Task TaxaLegadaCompartilhadaNaoApareceNemAlimentaProjecao_InMemory()
    {
        using var factory = new CustomWebApplicationFactory();
        await ValidarIsolamentoAsync(factory);
    }

    [RequiresPostgresFact]
    public async Task TaxaLegadaCompartilhadaNaoApareceNemAlimentaProjecao_Postgres()
    {
        var connection = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connection);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        await ValidarIsolamentoAsync(factory);
    }

    private static async Task ValidarIsolamentoAsync(CustomWebApplicationFactory factory)
    {
        using var primeiro = CriarCliente(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        using var segundo = CriarCliente(factory, FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName);

        var primeiraCarteira = new Carteira(Guid.NewGuid(), FakeAuthProvider.TestUserId.ToString(), "Carteira A");
        var segundaCarteira = new Carteira(Guid.NewGuid(), FakeAuthProvider.SecondTestUserId.ToString(), "Carteira B");
        var agora = DateTime.UtcNow;
        var observado = agora.AddDays(-1);
        // Dado legado: uma taxa contratada está gravada no instrumento compartilhado.
        var tesouro = new RendaFixa(Guid.NewGuid(), "TDTESTE", "Tesouro Prefixado sintético", 100m, agora,
            "PREFIXADO", 12m, agora.AddYears(2), "Tesouro Nacional", "TESOURO_DIRETO", convention: "365 dias corridos");
        var cdb = new RendaFixa(Guid.NewGuid(), "CDBTESTE", "CDB sintético", 100m, agora,
            "PREFIXADO", 10m, agora.AddYears(2), "Banco", "CDB", convention: "365 dias corridos");

        var primeiraPosicao = CriarPosicao(primeiraCarteira.Id, tesouro, observado);
        var segundaPosicao = CriarPosicao(segundaCarteira.Id, tesouro, observado);
        var posicaoCdb = CriarPosicao(primeiraCarteira.Id, cdb, observado);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Portfolios.AddRange(primeiraCarteira, segundaCarteira);
            db.Assets.AddRange(tesouro, cdb);
            db.AssetPositions.AddRange(primeiraPosicao, segundaPosicao, posicaoCdb);
            db.PositionValuations.AddRange(
                CriarAvaliacao(primeiraPosicao.Id, observado),
                CriarAvaliacao(segundaPosicao.Id, observado),
                CriarAvaliacao(posicaoCdb.Id, observado));
            await db.SaveChangesAsync();
        }

        foreach (var (cliente, id) in new[] { (primeiro, primeiraPosicao.Id), (segundo, segundaPosicao.Id) })
        {
            using var resposta = await cliente.GetAsync($"/api/v1/investments/{id}");
            resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var dados = json.RootElement.GetProperty("dados");
            dados.GetProperty("ativoId").GetGuid().Should().Be(tesouro.Id);
            dados.GetProperty("taxaJuros").ValueKind.Should().Be(JsonValueKind.Null);

            using var projecaoResposta = await cliente.GetAsync($"/api/v1/investments/{id}/projecao-renda-fixa");
            projecaoResposta.StatusCode.Should().Be(HttpStatusCode.OK, await projecaoResposta.Content.ReadAsStringAsync());
            using var projecaoJson = JsonDocument.Parse(await projecaoResposta.Content.ReadAsStringAsync());
            var projecao = projecaoJson.RootElement.GetProperty("dados");
            projecao.GetProperty("valorProjetadoBruto").ValueKind.Should().Be(JsonValueKind.Null);
            projecao.GetProperty("estadoProjecao").GetString().Should().Be("sem_premissa");
        }

        using var acessoIndevido = await segundo.GetAsync($"/api/v1/investments/{primeiraPosicao.Id}");
        acessoIndevido.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var projecaoIndevida = await segundo.GetAsync($"/api/v1/investments/{primeiraPosicao.Id}/projecao-renda-fixa");
        projecaoIndevida.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var respostaCdb = await primeiro.GetAsync($"/api/v1/investments/{posicaoCdb.Id}");
        respostaCdb.StatusCode.Should().Be(HttpStatusCode.OK);
        using var cdbJson = JsonDocument.Parse(await respostaCdb.Content.ReadAsStringAsync());
        cdbJson.RootElement.GetProperty("dados").GetProperty("taxaJuros").GetDecimal().Should().Be(10m);
        using var projecaoCdb = await primeiro.GetAsync($"/api/v1/investments/{posicaoCdb.Id}/projecao-renda-fixa");
        projecaoCdb.StatusCode.Should().Be(HttpStatusCode.OK);
        using var cdbProjecaoJson = JsonDocument.Parse(await projecaoCdb.Content.ReadAsStringAsync());
        cdbProjecaoJson.RootElement.GetProperty("dados").GetProperty("valorProjetadoBruto").GetDecimal().Should().BeGreaterThan(100m);
    }

    private static PosicaoInvestimento CriarPosicao(Guid carteiraId, RendaFixa ativo, DateTime data)
    {
        var posicao = new PosicaoInvestimento(Guid.NewGuid(), carteiraId, ativo.Id, ativo.Ticker, TipoAtivo.RendaFixa, 100m);
        posicao.AddShares(1m, 90m, 0m, data);
        return posicao;
    }

    private static ValuacaoPosicao CriarAvaliacao(Guid posicaoId, DateTime data) =>
        new(Guid.NewGuid(), posicaoId, data, 100m, 1m, data);

    private static HttpClient CriarCliente(CustomWebApplicationFactory factory, Guid usuarioId, Guid sessaoId,
        string email, string nome)
    {
        var cliente = factory.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(usuarioId, sessaoId, email, nome));
        return cliente;
    }
}
