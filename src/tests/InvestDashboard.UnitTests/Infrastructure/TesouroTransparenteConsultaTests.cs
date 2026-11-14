using FluentAssertions;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Infrastructure.Services;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class TesouroTransparenteConsultaTests
{
    [Fact]
    public async Task CatalogoESerie_CompartilhamCargaEFiltramTituloVencimentoEPeriodo()
    {
        var relogio = new Relogio(new DateTimeOffset(2026, 1, 5, 15, 0, 0, TimeSpan.Zero));
        var chamadas = 0;
        using var consulta = new TesouroTransparenteConsulta(_ => { chamadas++; return Task.FromResult(Carga(relogio.GetUtcNow())); }, relogio);

        var catalogo = await consulta.ListarTitulosAsync();
        var serie = await consulta.ConsultarAsync("tesouro prefixado", new(2032, 1, 1), new(2025, 1, 2), new(2025, 1, 5));

        chamadas.Should().Be(1);
        catalogo.Titulos.Should().HaveCount(2);
        catalogo.UltimaDataDisponivel.Should().Be(new DateOnly(2025, 1, 5));
        serie.Origem.Should().Contain("Tesouro Transparente");
        serie.ColetadoEmUtc.Should().Be(relogio.GetUtcNow());
        serie.TipoTitulo.Should().Be("Tesouro Prefixado");
        serie.Cotacoes.Select(x => x.DataBase).Should().Equal(new DateOnly(2025, 1, 2), new DateOnly(2025, 1, 5));
        serie.Cotacoes[0].PuBaseManha.Should().Be(800m);
    }

    [Fact]
    public async Task Expiracao_ReconsultaESinalizaFalhaSemRetornarRetratoVencido()
    {
        var relogio = new Relogio(new DateTimeOffset(2026, 1, 5, 15, 0, 0, TimeSpan.Zero));
        var chamadas = 0;
        using var consulta = new TesouroTransparenteConsulta(_ =>
        {
            chamadas++;
            if (chamadas == 2) throw new MarketDataUnavailableException();
            return Task.FromResult(Carga(relogio.GetUtcNow()));
        }, relogio);

        await consulta.ListarTitulosAsync();
        relogio.Agora = relogio.Agora.AddHours(6);
        var falhou = () => consulta.ListarTitulosAsync();
        await falhou.Should().ThrowAsync<MarketDataUnavailableException>();
        var renovado = await consulta.ListarTitulosAsync();
        chamadas.Should().Be(3);
        renovado.ColetadoEmUtc.Should().Be(relogio.Agora);
    }

    [Fact]
    public async Task SolicitacoesConcorrentes_CompartilhamUmaCargaIntegra()
    {
        var relogio = new Relogio(DateTimeOffset.UtcNow);
        var comecou = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var liberar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var chamadas = 0;
        using var consulta = new TesouroTransparenteConsulta(async token =>
        {
            Interlocked.Increment(ref chamadas);
            comecou.TrySetResult();
            await liberar.Task.WaitAsync(token);
            return Carga(relogio.GetUtcNow());
        }, relogio);

        var consultas = Enumerable.Range(0, 8).Select(_ => consulta.ListarTitulosAsync()).ToArray();
        await comecou.Task;
        liberar.SetResult();
        var resultados = await Task.WhenAll(consultas);
        chamadas.Should().Be(1);
        resultados.Should().OnlyContain(r => r.Titulos.Count == 2);
    }

    [Fact]
    public async Task PeriodoInvalido_NaoConsultaFonte()
    {
        using var consulta = new TesouroTransparenteConsulta(_ => throw new InvalidOperationException("Não deve consultar"), TimeProvider.System);
        var action = () => consulta.ConsultarAsync("Tesouro Selic", new(2030, 1, 1), new(2025, 1, 2), new(2025, 1, 1));
        await action.Should().ThrowAsync<ArgumentException>();
    }

    private static ResultadoTesouroTransparente Carga(DateTimeOffset data) => new(data,
        new MetadadosRecursoTesouro("pacote", "recurso", "catalogo", "csv", null, null, "odbl"), null, null,
        [
            new("Tesouro Prefixado", new(2032, 1, 1), new(2025, 1, 5), 12, 13, 900, 890, 890),
            new("Tesouro Prefixado", new(2032, 1, 1), new(2025, 1, 2), 12, 13, 810, 800, 800),
            new("Tesouro Prefixado", new(2033, 1, 1), new(2025, 1, 3), 12, 13, 700, 690, 690)
        ]);

    private sealed class Relogio(DateTimeOffset agora) : TimeProvider
    {
        public DateTimeOffset Agora { get; set; } = agora;
        public override DateTimeOffset GetUtcNow() => Agora;
    }
}
