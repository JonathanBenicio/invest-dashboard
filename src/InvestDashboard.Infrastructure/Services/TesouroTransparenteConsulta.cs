using InvestDashboard.Application.DTOs.MarketData;
using InvestDashboard.Application.Interfaces;

namespace InvestDashboard.Infrastructure.Services;

/// <summary>
/// Consulta um retrato íntegro do CSV público. O retrato é compartilhado apenas
/// entre leituras públicas, sem associá-lo a carteiras ou posições individuais.
/// </summary>
public sealed class TesouroTransparenteConsulta : ITesouroTransparenteConsulta, IDisposable
{
    private const int MaximoDias = 12_000;
    private const int MaximoPontos = 10_000;
    private static readonly TimeSpan Validade = TimeSpan.FromHours(6);
    private readonly Func<CancellationToken, Task<ResultadoTesouroTransparente>> coletar;
    private readonly TimeProvider tempo;
    private readonly SemaphoreSlim exclusao = new(1, 1);
    private Retrato? atual;

    public TesouroTransparenteConsulta(TesouroTransparenteCkanProvider provider, TimeProvider tempo)
        : this(provider.ColetarAsync, tempo) { }

    internal TesouroTransparenteConsulta(Func<CancellationToken, Task<ResultadoTesouroTransparente>> coletar, TimeProvider tempo)
    {
        this.coletar = coletar ?? throw new ArgumentNullException(nameof(coletar));
        this.tempo = tempo ?? throw new ArgumentNullException(nameof(tempo));
    }

    public async Task<CatalogoTesouroDto> ListarTitulosAsync(CancellationToken cancellationToken = default)
    {
        var retrato = await ObterAsync(cancellationToken);
        return new CatalogoTesouroDto(TesouroTransparenteCsvReader.Fonte, TesouroTransparenteCsvReader.UrlFonte,
            retrato.ColetadoEmUtc, retrato.UltimaData, retrato.Titulos);
    }

    public async Task<SerieCotacoesTesouroDto> ConsultarAsync(string tipoTitulo, DateOnly dataVencimento,
        DateOnly dataDe, DateOnly dataAte, CancellationToken cancellationToken = default)
    {
        tipoTitulo = tipoTitulo?.Trim() ?? string.Empty;
        if (tipoTitulo.Length is 0 or > 100 || dataVencimento == default || dataDe == default ||
            dataAte == default || dataDe > dataAte || dataAte.DayNumber - dataDe.DayNumber > MaximoDias)
            throw new ArgumentException("Informe título, vencimento e período válido de até 12000 dias.");

        var retrato = await ObterAsync(cancellationToken);
        var pontos = new List<PontoCotacaoTesouroDto>();
        var nome = tipoTitulo;
        if (retrato.Series.TryGetValue((tipoTitulo.ToUpperInvariant(), dataVencimento), out var serie))
        {
            nome = serie.Nome;
            foreach (var item in serie.Cotacoes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item.DataBase < dataDe) continue;
                if (item.DataBase > dataAte) break;
                if (pontos.Count == MaximoPontos)
                    throw new ArgumentException("O período contém mais cotações que o limite permitido.");
                pontos.Add(new PontoCotacaoTesouroDto(item.DataBase, item.TaxaCompraManha, item.TaxaVendaManha,
                    item.PuCompraManha, item.PuVendaManha, item.PuBaseManha));
            }
        }
        return new SerieCotacoesTesouroDto(TesouroTransparenteCsvReader.Fonte, TesouroTransparenteCsvReader.UrlFonte,
            retrato.ColetadoEmUtc, retrato.UltimaData, nome, dataVencimento, dataDe, dataAte, pontos);
    }

    private async Task<Retrato> ObterAsync(CancellationToken cancellationToken)
    {
        var cache = Volatile.Read(ref atual);
        if (EstaValido(cache)) return cache!;
        await exclusao.WaitAsync(cancellationToken);
        try
        {
            cache = Volatile.Read(ref atual);
            if (EstaValido(cache)) return cache!;

            // O novo retrato só substitui o anterior após download e validação completos.
            // Em falha, o retrato vencido não é devolvido como se fosse atual.
            var carregado = await coletar(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var series = carregado.Cotacoes
                .GroupBy(c => (c.TipoTitulo.ToUpperInvariant(), c.DataVencimento))
                .ToDictionary(g => g.Key, g => new Serie(g.First().TipoTitulo,
                    g.OrderBy(c => c.DataBase).ToArray()));
            var titulos = series.Select(g => new TituloTesouroDisponivelDto(g.Value.Nome, g.Key.DataVencimento,
                    g.Value.Cotacoes[^1].DataBase))
                .OrderBy(t => t.TipoTitulo, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.DataVencimento).ToArray();
            if (titulos.Length == 0)
                throw new InvalidDataException("O catálogo público não contém cotações.");
            var atualizado = new Retrato(carregado.ColetadoEmUtc, tempo.GetUtcNow(),
                titulos.Max(t => t.UltimaDataDisponivel), titulos, series);
            Volatile.Write(ref atual, atualizado);
            return atualizado;
        }
        finally { exclusao.Release(); }
    }

    private bool EstaValido(Retrato? retrato)
    {
        if (retrato is null) return false;
        var agora = tempo.GetUtcNow();
        return agora >= retrato.ArmazenadoEmUtc && agora - retrato.ArmazenadoEmUtc < Validade;
    }

    public void Dispose() => exclusao.Dispose();

    private sealed record Serie(string Nome, CotacaoTesouroTransparente[] Cotacoes);
    private sealed record Retrato(DateTimeOffset ColetadoEmUtc, DateTimeOffset ArmazenadoEmUtc, DateOnly UltimaData,
        IReadOnlyList<TituloTesouroDisponivelDto> Titulos,
        Dictionary<(string TipoTitulo, DateOnly DataVencimento), Serie> Series);
}
