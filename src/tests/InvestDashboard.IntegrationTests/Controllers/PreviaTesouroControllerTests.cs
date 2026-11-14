using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class PreviaTesouroControllerTests
{
    // Documento sintético; não contém dados privados.
    private const string Html = """
        <div class="td-custom-meus-investimentos-titulo">
          <h1 class="td-meus-investimentos-detalhe__titulo_card">Tesouro Prefixado 2035</h1>
          <table><tr class="saldo-table-vencimento"><td><strong>VENCIMENTO: 01/01/2035</strong></td></tr></table>
          <div class="td-posicao-detalhada__info">
            <p class="td-posicao-detalhada__info__valor">Data: <span class="td-posicao-detalhada__info__data">02/03/2024</span></p>
            <p class="td-posicao-detalhada__info__valor">Campo adicional: <span class="td-posicao-detalhada__info__data">Sintético</span></p>
            <p class="td-posicao-detalhada__info__valor">Tipo: <span class="td-posicao-detalhada__info__data">Compra</span></p>
            <p class="td-posicao-detalhada__info__valor">Quantidade: <span class="td-posicao-detalhada__info__data">0,01</span></p>
            <p class="td-posicao-detalhada__info__valor">Preço (R$): <span class="td-posicao-detalhada__info__data">123,45</span></p>
            <p class="td-posicao-detalhada__info__valor">Taxa (% a.a.): <span class="td-posicao-detalhada__info__data">10,00</span></p>
            <p class="td-posicao-detalhada__info__valor">Valor (R$): <span class="td-posicao-detalhada__info__data">1,23</span></p>
          </div>
        </div>
        """;

    [Fact]
    public async Task SemAutenticacao_EscondeConteudo()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var resposta = await Postar(client, Guid.NewGuid(), Encoding.UTF8.GetBytes(Html), "extrato.html");
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CarteiraDeOutroUsuario_NaoPermitePrevia()
    {
        using var factory = new CustomWebApplicationFactory();
        using var titular = Autenticado(factory);
        var carteira = await CriarCarteira(titular);
        using var outro = factory.CreateClient();
        outro.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
                FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName));
        using var resposta = await Postar(outro, carteira, Encoding.UTF8.GetBytes(Html), "extrato.html");
        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("extrato.html")]
    [InlineData("extrato.htm")]
    public async Task Html_ExibePreviaSemGravarPosicoesOuMovimentos(string nome)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Autenticado(factory);
        var carteira = await CriarCarteira(client);
        using var resposta = await Postar(client, carteira, Encoding.UTF8.GetBytes(Html), nome);
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        resposta.Headers.CacheControl!.NoStore.Should().BeTrue();
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var dados = json.RootElement.GetProperty("dados");
        dados.GetProperty("carteiraId").GetGuid().Should().Be(carteira);
        dados.GetProperty("formato").GetString().Should().Be("html");
        dados.GetProperty("vencimento").GetString().Should().Be("2035-01-01");
        dados.GetProperty("movimentacoes").GetArrayLength().Should().Be(1);
        dados.GetProperty("podeConfirmar").GetBoolean().Should().BeFalse();
        dados.GetProperty("coberturaCompleta").GetBoolean().Should().BeFalse();

        using var carteiraResposta = await client.GetAsync($"/api/v1/portfolios/{carteira}");
        using var carteiraJson = JsonDocument.Parse(await carteiraResposta.Content.ReadAsStringAsync());
        carteiraJson.RootElement.GetProperty("dados").GetProperty("posicoes").GetArrayLength().Should().Be(0);
        using var transacoes = await client.GetAsync($"/api/v1/transactions/portfolio/{carteira}");
        using var transacoesJson = JsonDocument.Parse(await transacoes.Content.ReadAsStringAsync());
        transacoesJson.RootElement.GetProperty("dados").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Xlsx_ExibeCamposDePosicaoSemGravar()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Autenticado(factory);
        var carteira = await CriarCarteira(client);
        using var resposta = await Postar(client, carteira, CriarPlanilha(), "extrato.xlsx");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var dados = json.RootElement.GetProperty("dados");
        dados.GetProperty("formato").GetString().Should().Be("xlsx");
        dados.GetProperty("posicoes")[0].GetProperty("camposBrutos").GetProperty("quantidade").GetString().Should().Be("1,2");
        dados.GetProperty("movimentacoes").GetArrayLength().Should().Be(0);
        dados.GetProperty("podeConfirmar").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ArquivoInvalido_OuMultiplosArquivos_RetornaErroSemRefletirConteudo()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Autenticado(factory);
        var carteira = await CriarCarteira(client);
        using var invalido = await Postar(client, carteira, Encoding.UTF8.GetBytes("texto-privado-exemplo"), "extrato.html");
        invalido.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await invalido.Content.ReadAsStringAsync()).Should().NotContain("texto-privado-exemplo");
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(Html)), "arquivo", "primeiro.html");
        multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(Html)), "arquivo", "segundo.html");
        using var duplicado = await client.PostAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/previa", multipart);
        duplicado.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static HttpClient Autenticado(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        return client;
    }

    private static async Task<Guid> CriarCarteira(HttpClient client)
    {
        using var resposta = await client.PostAsJsonAsync("/api/v1/portfolios", new
        {
            instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"), nome = "Carteira sintética"
        });
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task<HttpResponseMessage> Postar(HttpClient client, Guid carteira, byte[] bytes, string nome)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(bytes), "arquivo", nome);
        return await client.PostAsync($"/api/v1/portfolios/{carteira}/tesouro-direto/previa", multipart);
    }

    private static byte[] CriarPlanilha()
    {
        using var stream = new MemoryStream();
        using (var documento = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbook = documento.AddWorkbookPart();
            workbook.Workbook = new Workbook();
            var folha = workbook.AddNewPart<WorksheetPart>();
            folha.Worksheet = new Worksheet(new SheetData(
                Linha(6, ["Tesouro Direto"]),
                Linha(8, ["", "Saldo", "% Alocação", "Valor aplicado", "Quantidade", "Disponível", "Vencimento"]),
                Linha(9, ["Tesouro Prefixado 2035", "R$ 1.100,00", "50%", "R$ 1.000,00", "1,2", "1,2", "01/01/2035"])));
            workbook.Workbook.AppendChild(new Sheets()).Append(new Sheet { Id = workbook.GetIdOfPart(folha), SheetId = 1U, Name = "Exemplo" });
        }
        return stream.ToArray();
    }

    private static Row Linha(uint numero, string[] valores) => new(valores.Select((valor, indice) => new Cell
    {
        CellReference = $"{(char)('A' + indice)}{numero}", DataType = CellValues.InlineString,
        InlineString = new InlineString(new Text(valor))
    })) { RowIndex = numero };
}
