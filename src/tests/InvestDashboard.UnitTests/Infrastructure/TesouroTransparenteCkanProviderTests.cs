using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Infrastructure.Services;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class TesouroTransparenteCkanProviderTests
{
    private const string Pacote = "df56aa42-484a-4a59-8184-7676580c81e3";
    private const string Recurso = "796d2059-14e9-44e3-80c9-2d9e30b405c1";
    private const string UrlCsv = "https://www.tesourotransparente.gov.br/ckan/dataset/df56aa42-484a-4a59-8184-7676580c81e3/resource/796d2059-14e9-44e3-80c9-2d9e30b405c1/download/precotaxatesourodireto.csv";
    private const string Cabecalho = "Tipo Titulo;Data Vencimento;Data Base;Taxa Compra Manha;Taxa Venda Manha;PU Compra Manha;PU Venda Manha;PU Base Manha";
    private const string Linha = "Tesouro Selic;01/03/2031;12/11/2026;0,11;0,12;20201,00;20181,99;20181,99";

    [Fact]
    public async Task Busca_catalogo_recurso_fixo_e_retorna_cotacoes_com_proveniencia()
    {
        var chamadas = new List<Uri>();
        using var provider = new TesouroTransparenteCkanProvider(new CallbackHandler((request, _) =>
        {
            chamadas.Add(request.RequestUri!);
            if (chamadas.Count == 1) return Task.FromResult(Resposta(Catalogo(), "application/json"));
            var resposta = Resposta(Cabecalho + "\n" + Linha, "text/csv");
            resposta.Headers.ETag = new EntityTagHeaderValue("\"rev-1\"");
            return Task.FromResult(resposta);
        }), new RelogioFixo());

        var resultado = await provider.ColetarAsync();

        chamadas.Should().HaveCount(2);
        chamadas[0].Host.Should().Be("www.tesourotransparente.gov.br");
        chamadas[0].AbsolutePath.Should().Be("/ckan/api/3/action/package_show");
        chamadas[1].AbsoluteUri.Should().Be(UrlCsv);
        resultado.ColetadoEmUtc.Should().Be(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero));
        resultado.Metadados.RecursoId.Should().Be(Recurso);
        resultado.Metadados.Revisao.Should().Be("revision-test");
        resultado.Metadados.Licenca.Should().Be("odc-odbl");
        resultado.ETag.Should().Be("\"rev-1\"");
        resultado.Cotacoes.Should().ContainSingle();
        resultado.Cotacoes[0].DataBase.Should().Be(new DateOnly(2026, 11, 12));
    }

    [Theory]
    [InlineData("http://www.tesourotransparente.gov.br/ckan/arquivo.csv")]
    [InlineData("https://localhost/arquivo.csv")]
    [InlineData("https://www.tesourotransparente.gov.br.evil.example/arquivo.csv")]
    [InlineData(UrlCsv + "?alterado=1")]
    public async Task Endereco_do_catalogo_desconhecido_nao_e_acessado(string url)
    {
        var chamadas = 0;
        using var provider = new TesouroTransparenteCkanProvider(new CallbackHandler((_, _) =>
        {
            chamadas++;
            return Task.FromResult(Resposta(Catalogo(url: url), "application/json"));
        }), TimeProvider.System);

        var acao = () => provider.ColetarAsync();

        await acao.Should().ThrowAsync<MarketDataUnavailableException>();
        chamadas.Should().Be(1);
    }

    [Theory]
    [InlineData("application/json", "[]")]
    [InlineData("application/json", "{\"success\":false}")]
    [InlineData("text/html", "<html>erro</html>")]
    public async Task Rejeita_catalogo_invalido_ou_html(string tipo, string corpo)
    {
        using var provider = new TesouroTransparenteCkanProvider(new CallbackHandler((_, _) =>
            Task.FromResult(Resposta(corpo, tipo))), TimeProvider.System);
        var acao = () => provider.ColetarAsync();
        await acao.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Theory]
    [InlineData("text/html", "<html>erro</html>")]
    [InlineData("text/csv", "<html>erro</html>")]
    [InlineData("text/csv", Cabecalho + "\n" + Linha + "\nlinha inválida")]
    public async Task Rejeita_html_e_csv_invalido_sem_retornar_carga_parcial(string tipo, string corpo)
    {
        var chamadas = 0;
        using var provider = new TesouroTransparenteCkanProvider(new CallbackHandler((_, _) =>
            Task.FromResult(++chamadas == 1 ? Resposta(Catalogo(), "application/json") : Resposta(corpo, tipo))), TimeProvider.System);
        var acao = () => provider.ColetarAsync();
        await acao.Should().ThrowAsync<MarketDataUnavailableException>();
        chamadas.Should().Be(2);
    }

    [Fact]
    public async Task Redirect_nao_e_aceito()
    {
        var chamadas = 0;
        using var provider = new TesouroTransparenteCkanProvider(new CallbackHandler((_, _) =>
        {
            chamadas++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers = { Location = new Uri("http://127.0.0.1/") }
            });
        }), TimeProvider.System);
        var acao = () => provider.ColetarAsync();
        await acao.Should().ThrowAsync<MarketDataUnavailableException>();
        chamadas.Should().Be(1);
    }

    [Fact]
    public async Task Rejeita_resposta_acima_do_limite_declarado()
    {
        using var provider = new TesouroTransparenteCkanProvider(new CallbackHandler((_, _) =>
        {
            var resposta = Resposta("{}", "application/json");
            resposta.Content.Headers.ContentLength = 1_048_577;
            return Task.FromResult(resposta);
        }), TimeProvider.System);
        var acao = () => provider.ColetarAsync();
        await acao.Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Fact]
    public async Task Preserva_cancelamento_do_chamador()
    {
        using var cancelamento = new CancellationTokenSource();
        await cancelamento.CancelAsync();
        using var provider = new TesouroTransparenteCkanProvider(new CallbackHandler((_, token) =>
            Task.FromCanceled<HttpResponseMessage>(token)), TimeProvider.System);
        var acao = () => provider.ColetarAsync(cancelamento.Token);
        await acao.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Prazo_inclui_corpo_que_nao_termina(bool corpoCsv)
    {
        var relogio = new RelogioManual();
        using var fluxo = new FluxoQueAguardaCancelamento();
        var chamadas = 0;
        using var provider = new TesouroTransparenteCkanProvider(new CallbackHandler((_, _) =>
        {
            chamadas++;
            if (corpoCsv && chamadas == 1) return Task.FromResult(Resposta(Catalogo(), "application/json"));
            return Task.FromResult(RespostaComFluxo(fluxo, corpoCsv ? "text/csv" : "application/json"));
        }), relogio, TimeSpan.FromSeconds(10));

        var tarefa = provider.ColetarAsync();
        await fluxo.Iniciada.Task.WaitAsync(TimeSpan.FromSeconds(10));
        relogio.Expirar();

        await FluentActions.Awaiting(() => tarefa).Should().ThrowAsync<MarketDataUnavailableException>();
    }

    [Fact]
    public async Task Cancelamento_explicito_durante_leitura_do_corpo_nao_e_convertido_em_timeout()
    {
        using var cancelamento = new CancellationTokenSource();
        using var fluxo = new FluxoQueAguardaCancelamento();
        using var provider = new TesouroTransparenteCkanProvider(new CallbackHandler((_, _) =>
            Task.FromResult(RespostaComFluxo(fluxo, "application/json"))), new RelogioManual(), TimeSpan.FromSeconds(10));

        var tarefa = provider.ColetarAsync(cancelamento.Token);
        await fluxo.Iniciada.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cancelamento.CancelAsync();

        await FluentActions.Awaiting(() => tarefa).Should().ThrowAsync<OperationCanceledException>();
    }

    private static string Catalogo(string url = UrlCsv) => JsonSerializer.Serialize(new
    {
        success = true,
        result = new
        {
            id = Pacote, license_id = "odc-odbl",
            resources = new[] { new { id = Recurso, format = "CSV", state = "active", url,
                revision_id = "revision-test", last_modified = "2026-11-13T10:00:00" } }
        }
    });

    private static HttpResponseMessage Resposta(string texto, string tipo) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(texto, Encoding.UTF8, tipo)
    };

    private static HttpResponseMessage RespostaComFluxo(Stream fluxo, string tipo)
    {
        var resposta = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(fluxo) };
        resposta.Content.Headers.ContentType = new MediaTypeHeaderValue(tipo);
        return resposta;
    }

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class RelogioManual : TimeProvider
    {
        private TemporizadorManual? temporizador;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
            temporizador = new TemporizadorManual(callback, state);
        public void Expirar() => (temporizador ?? throw new InvalidOperationException("Temporizador não criado.")).Disparar();
    }

    private sealed class TemporizadorManual(TimerCallback callback, object? estado) : ITimer
    {
        public void Disparar() => callback(estado);
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FluxoQueAguardaCancelamento : Stream
    {
        public TaskCompletionSource<bool> Iniciada { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Iniciada.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class CallbackHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => callback(request, cancellationToken);
    }
}
