using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using InvestDashboard.Application.Exceptions;

[assembly: InternalsVisibleTo("InvestDashboard.UnitTests")]

namespace InvestDashboard.Infrastructure.Services;

/// <summary>Consulta o catálogo oficial e processa integralmente o CSV conhecido, sem alterar posições.</summary>
public sealed class TesouroTransparenteCkanProvider : IDisposable
{
    private const string Pacote = "df56aa42-484a-4a59-8184-7676580c81e3";
    private const string Recurso = "796d2059-14e9-44e3-80c9-2d9e30b405c1";
    private const string UrlCatalogo = "https://www.tesourotransparente.gov.br/ckan/api/3/action/package_show?id=taxas-dos-titulos-ofertados-pelo-tesouro-direto";
    private const string UrlCsv = "https://www.tesourotransparente.gov.br/ckan/dataset/df56aa42-484a-4a59-8184-7676580c81e3/resource/796d2059-14e9-44e3-80c9-2d9e30b405c1/download/precotaxatesourodireto.csv";
    private const int MaximoCatalogo = 1024 * 1024;
    private const int MaximoCsv = 32 * 1024 * 1024;
    private readonly HttpClient cliente;
    private readonly TimeProvider tempo;
    private readonly TimeSpan tempoMaximo;

    // O cliente de produção não segue redirects: nem o catálogo nem o CSV podem
    // direcionar requisições para outro endereço ou rede interna.
    public TesouroTransparenteCkanProvider(TimeProvider tempo)
        : this(new SocketsHttpHandler { AllowAutoRedirect = false }, tempo) { }

    internal TesouroTransparenteCkanProvider(HttpMessageHandler handler, TimeProvider tempo, TimeSpan? tempoMaximo = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        this.tempo = tempo ?? throw new ArgumentNullException(nameof(tempo));
        this.tempoMaximo = tempoMaximo ?? TimeSpan.FromMinutes(2);
        if (this.tempoMaximo <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(tempoMaximo));
        cliente = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
    }

    public async Task<ResultadoTesouroTransparente> ColetarAsync(CancellationToken cancellationToken = default)
    {
        // ResponseHeadersRead entrega o controle antes do corpo. O prazo precisa
        // abranger também o download e a interpretação integral dos dois corpos.
        using var prazo = new CancellationTokenSource(tempoMaximo, tempo);
        using var cancelamento = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, prazo.Token);
        var token = cancelamento.Token;
        try
        {
            using var catalogo = await ObterAsync(UrlCatalogo, "application/json", MaximoCatalogo, token);
            using var catalogoStream = new FluxoLimitado(await catalogo.Content.ReadAsStreamAsync(token), MaximoCatalogo);
            using var json = await JsonDocument.ParseAsync(catalogoStream, cancellationToken: token);
            var metadados = LerMetadados(json.RootElement);

            using var csv = await ObterAsync(UrlCsv, "text/csv", MaximoCsv, token);
            using var csvStream = new FluxoLimitado(await csv.Content.ReadAsStreamAsync(token), MaximoCsv);
            using var texto = new StreamReader(csvStream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
            var cotacoes = new List<CotacaoTesouroTransparente>();
            await foreach (var cotacao in TesouroTransparenteCsvReader.LerAsync(texto, token))
                cotacoes.Add(cotacao);

            token.ThrowIfCancellationRequested();
            return new ResultadoTesouroTransparente(tempo.GetUtcNow(), metadados,
                csv.Headers.ETag?.ToString(), csv.Content.Headers.LastModified, cotacoes);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MarketDataUnavailableException();
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidDataException or IOException or DecoderFallbackException)
        {
            throw new MarketDataUnavailableException();
        }
    }

    private async Task<HttpResponseMessage> ObterAsync(string url, string aceite, int limite, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("InvestDashboard", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(aceite));
        var resposta = await cliente.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        var tipo = resposta.Content.Headers.ContentType?.MediaType;
        var tipoValido = aceite == "application/json"
            ? string.Equals(tipo, "application/json", StringComparison.OrdinalIgnoreCase)
            : tipo is not null && (tipo.Equals("text/csv", StringComparison.OrdinalIgnoreCase) ||
                                   tipo.Equals("application/csv", StringComparison.OrdinalIgnoreCase) ||
                                   tipo.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase) ||
                                   tipo.Equals("text/plain", StringComparison.OrdinalIgnoreCase));
        if (!resposta.IsSuccessStatusCode || !tipoValido || resposta.Content.Headers.ContentLength > limite)
        {
            resposta.Dispose();
            throw new InvalidDataException("Resposta do Tesouro Transparente inválida.");
        }
        return resposta;
    }

    private static MetadadosRecursoTesouro LerMetadados(JsonElement raiz)
    {
        if (raiz.ValueKind != JsonValueKind.Object || !raiz.TryGetProperty("success", out var sucesso) || sucesso.ValueKind != JsonValueKind.True ||
            !raiz.TryGetProperty("result", out var pacote) || pacote.ValueKind != JsonValueKind.Object ||
            Texto(pacote, "id") != Pacote || !pacote.TryGetProperty("resources", out var recursos) ||
            recursos.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Catálogo CKAN inválido.");

        var encontrados = recursos.EnumerateArray().Where(r => r.ValueKind == JsonValueKind.Object && Texto(r, "id") == Recurso).ToList();
        if (encontrados.Count != 1)
            throw new InvalidDataException("Recurso CSV oficial ausente ou ambíguo.");
        var recurso = encontrados[0];
        if (!string.Equals(Texto(recurso, "format"), "CSV", StringComparison.OrdinalIgnoreCase) ||
            Texto(recurso, "state") != "active" || Texto(recurso, "url") != UrlCsv)
            throw new InvalidDataException("Identidade ou endereço do recurso CSV não reconhecido.");

        return new MetadadosRecursoTesouro(Pacote, Recurso, UrlCatalogo, UrlCsv,
            Texto(recurso, "revision_id"), Texto(recurso, "last_modified"), Texto(pacote, "license_id"));
    }

    private static string? Texto(JsonElement elemento, string propriedade) =>
        elemento.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;

    public void Dispose() => cliente.Dispose();

    private sealed class FluxoLimitado(Stream fonte, long limite) : Stream
    {
        private long lidos;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => lidos; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var quantidade = fonte.Read(buffer, offset, count);
            Verificar(quantidade);
            return quantidade;
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var quantidade = await fonte.ReadAsync(buffer, cancellationToken);
            Verificar(quantidade);
            return quantidade;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        private void Verificar(int quantidade)
        {
            lidos += quantidade;
            if (lidos > limite) throw new InvalidDataException("Resposta excede o limite permitido.");
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

public sealed record MetadadosRecursoTesouro(string PacoteId, string RecursoId, string Catalogo, string UrlCsv,
    string? Revisao, string? UltimaModificacao, string? Licenca);
public sealed record ResultadoTesouroTransparente(DateTimeOffset ColetadoEmUtc, MetadadosRecursoTesouro Metadados,
    string? ETag, DateTimeOffset? LastModified, IReadOnlyList<CotacaoTesouroTransparente> Cotacoes);
