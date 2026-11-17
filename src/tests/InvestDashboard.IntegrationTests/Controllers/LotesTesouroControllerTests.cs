using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.Portfolio;
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

    private static async Task<Guid> CriarCarteira(HttpClient client)
    {
        using var resposta = await client.PostAsJsonAsync("/api/v1/portfolios", new
        { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"), nome = "Carteira sintética" });
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
