using System.Net;
using System.Text;
using FluentAssertions;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class BcbIndicadoresProviderTests
{
    [Fact]
    public async Task Selic_ConsultaSerieOnzeComDatasEUnidadeDiaria()
    {
        var handler = new CallbackHandler(request =>
        {
            request.RequestUri!.AbsolutePath.Should().EndWith("/bcdata.sgs.11/dados");
            request.RequestUri.Query.Should().Contain("dataInicial=01%2F01%2F2025");
            request.RequestUri.Query.Should().Contain("dataFinal=03%2F01%2F2025");
            return Json("[{\"data\":\"02/01/2025\",\"valor\":\"0.05\"}]");
        });
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbIndicadoresProvider(client, cache, TimeProvider.System);

        var resultado = await provider.GetSelicAsync(new(2025, 1, 1), new(2025, 1, 3));

        resultado.CodigoSgs.Should().Be(11);
        resultado.Unidade.Should().Be("percentual ao dia");
        resultado.Periodicidade.Should().Be("diária");
        resultado.Origem.Should().Contain("Banco Central");
        resultado.Pontos.Should().ContainSingle().Which.Valor.Should().Be(0.05m);
    }

    [Fact]
    public async Task Ipca_AdmiteVariacaoNegativaEDevolveMesDeReferencia()
    {
        var handler = new CallbackHandler(request =>
        {
            request.RequestUri!.AbsolutePath.Should().EndWith("/bcdata.sgs.433/dados");
            return Json("[{\"data\":\"01/02/2025\",\"valor\":\"-0.12\"}]");
        });
        using var client = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbIndicadoresProvider(client, cache, TimeProvider.System);

        var resultado = await provider.GetIpcaAsync(new(2025, 1, 1), new(2025, 3, 1));

        resultado.CodigoSgs.Should().Be(433);
        resultado.Unidade.Should().Be("variação percentual mensal");
        resultado.Periodicidade.Should().Be("mensal");
        resultado.Pontos.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Data = new DateOnly(2025, 2, 1), Valor = -0.12m });
    }

    [Fact]
    public async Task Ipca_IncluiOMesInicialQuandoFiltroComecaDepoisDoPrimeiroDia()
    {
        using var client = new HttpClient(new CallbackHandler(_ => Json("[{\"data\":\"01/01/2025\",\"valor\":\"0.16\"}]")));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbIndicadoresProvider(client, cache, TimeProvider.System);

        var resultado = await provider.GetIpcaAsync(new(2025, 1, 15), new(2025, 2, 15));

        resultado.Pontos.Should().ContainSingle().Which.Data.Should().Be(new DateOnly(2025, 1, 1));
    }

    [Theory]
    [InlineData("invalid-json")]
    [InlineData("[{\"data\":\"01/01/2025\",\"valor\":\"inválido\"}]")]
    [InlineData("[{\"data\":\"01/01/2025\",\"valor\":\"0.1\"},{\"data\":\"01/01/2025\",\"valor\":\"0.2\"}]")]
    [InlineData("[null]")]
    [InlineData("[{\"data\":\"01/01/2024\",\"valor\":\"0.1\"}]")]
    public async Task RespostaInvalida_IndisponibilizaAFonteSemInventarDados(string conteudo)
    {
        using var client = new HttpClient(new CallbackHandler(_ => Json(conteudo)));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbIndicadoresProvider(client, cache, TimeProvider.System);
        var action = () => provider.GetIpcaAsync(new(2025, 1, 1), new(2025, 3, 1));
        await action.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Fact]
    public async Task ErroDeTransporte_IndisponibilizaAFonte()
    {
        using var client = new HttpClient(new CallbackHandler(_ => throw new HttpRequestException()));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbIndicadoresProvider(client, cache, TimeProvider.System);
        var action = () => provider.GetSelicAsync(new(2025, 1, 1), new(2025, 1, 2));
        await action.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Fact]
    public async Task PeriodoSemObservacoes_RetornaListaVaziaQuandoBcbIndicaAusencia()
    {
        using var client = new HttpClient(new CallbackHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"erro\":{\"statusCode\":404,\"detail\":\"br.gov.bcb.pec.sgs.comum.excecoes.SGSNegocioException: Value(s) not found\"}}", Encoding.UTF8, "application/json")
        }));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbIndicadoresProvider(client, cache, TimeProvider.System);

        var resultado = await provider.GetSelicAsync(new(2025, 1, 4), new(2025, 1, 5));

        resultado.Pontos.Should().BeEmpty();
    }

    [Fact]
    public async Task Http404NaoReconhecido_NaoSePassaPorSerieVazia()
    {
        using var client = new HttpClient(new CallbackHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"erro\":{\"statusCode\":404,\"detail\":\"route not found\"}}", Encoding.UTF8, "application/json")
        }));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbIndicadoresProvider(client, cache, TimeProvider.System);
        var action = () => provider.GetSelicAsync(new(2025, 1, 4), new(2025, 1, 5));

        await action.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task RespostaHtml_IndisponibilizaAFonte(HttpStatusCode status)
    {
        using var client = new HttpClient(new CallbackHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent("<html>Indisponível</html>", Encoding.UTF8, "text/html")
        }));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbIndicadoresProvider(client, cache, TimeProvider.System);
        var action = () => provider.GetSelicAsync(new(2025, 1, 4), new(2025, 1, 5));

        await action.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Fact]
    public async Task PeriodoInvalido_NaoChamaBcb()
    {
        using var client = new HttpClient(new CallbackHandler(_ => throw new Exception("Não deveria consultar a rede")));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbIndicadoresProvider(client, cache, TimeProvider.System);
        var action = () => provider.GetSelicAsync(new(2025, 2, 1), new(2025, 1, 1));
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DataFutura_UsaDiaDeBrasiliaMesmoDepoisDaMeiaNoiteUtc()
    {
        using var client = new HttpClient(new CallbackHandler(_ => Json("[]")));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var horario = new RelogioFixo(new DateTimeOffset(2025, 1, 2, 1, 0, 0, TimeSpan.Zero));
        var provider = new BcbIndicadoresProvider(client, cache, horario);

        var futuroNoBrasil = () => provider.GetSelicAsync(new(2025, 1, 2), new(2025, 1, 2));
        await futuroNoBrasil.Should().ThrowAsync<ArgumentException>();
        var hojeNoBrasil = await provider.GetSelicAsync(new(2025, 1, 1), new(2025, 1, 1));
        hojeNoBrasil.Pontos.Should().BeEmpty();
    }

    [Fact]
    public async Task Cache_SeparaSeriesEMantemDadosSemNovaConsulta()
    {
        var chamadas = 0;
        using var client = new HttpClient(new CallbackHandler(_ => { chamadas++; return Json("[]"); }));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new BcbIndicadoresProvider(client, cache, TimeProvider.System);
        await provider.GetSelicAsync(new(2025, 1, 1), new(2025, 1, 2));
        await provider.GetSelicAsync(new(2025, 1, 1), new(2025, 1, 2));
        await provider.GetIpcaAsync(new(2025, 1, 1), new(2025, 1, 2));
        chamadas.Should().Be(2);
    }

    private static HttpResponseMessage Json(string text) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(text, Encoding.UTF8, "application/json")
    };

    private sealed class CallbackHandler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(callback(request));
    }

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}
