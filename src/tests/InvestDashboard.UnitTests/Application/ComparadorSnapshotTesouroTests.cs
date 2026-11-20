using FluentAssertions;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Services;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.UnitTests.Application;

public sealed class ComparadorSnapshotTesouroTests
{
    [Fact]
    public void ApresentaCadaEventoAnteriorNaoObservadoSemApagarDuplicatas()
    {
        var carteira = Guid.NewGuid();
        var ativo = Guid.NewGuid();
        var anterior = new BasePosicaoTesouro(Guid.NewGuid(), Guid.NewGuid(), carteira, ativo, new DateOnly(2024, 3, 3), 1m, 20m, 22m);
        var compra = new DadosMovimentoTesouroDto(new DateOnly(2024, 3, 2), "Compra", "Crédito", 1m, 20m, null, 20m, []);
        var antiga = Previa(carteira, [new([], compra), new([], compra)]);
        var atual = Previa(carteira, [new([], compra)]);

        var resultado = ComparadorSnapshotTesouro.Preparar(Guid.NewGuid(), new string('a', 64), 1,
            new DateOnly(2024, 3, 5), atual, anterior, antiga, [], 1m);

        resultado.Should().NotBeNull();
        resultado!.Divergencias.Should().ContainSingle(x => x.Tipo == "evento_nao_observado");
        resultado.RequerAceite.Should().BeTrue();
    }

    [Fact]
    public void HashMudaQuandoCarteiraOuSnapshotAnteriorMudam()
    {
        var carteira = Guid.NewGuid();
        var anterior = new BasePosicaoTesouro(Guid.NewGuid(), Guid.NewGuid(), carteira, Guid.NewGuid(), new DateOnly(2024, 3, 3), 1m, 20m, 22m);
        var lote = Guid.NewGuid();
        var previa = Previa(carteira, []);
        var original = ComparadorSnapshotTesouro.Preparar(lote, new string('a', 64), 1, new DateOnly(2024, 3, 5), previa, anterior, previa, [], 1m);
        var alterada = ComparadorSnapshotTesouro.Preparar(lote, new string('a', 64), 2, new DateOnly(2024, 3, 5), previa, anterior, previa, [], 1m);
        original!.Hash.Should().NotBe(alterada!.Hash);
    }

    private static PreviaTesouroDto Previa(Guid carteira, IReadOnlyList<MovimentoPreviaTesouroDto> movimentos)
    {
        var posicao = new DadosPosicaoTesouroDto(new DateOnly(2024, 3, 2), 1m, 20m, 20m, "10%", null, null, 22m, 1, null, null, null, null, null, 22m, []);
        return new(carteira, "html", "portal", "Tesouro Prefixado 2035", "2035-01-01", null,
            [new(1, new Dictionary<string, string>(), posicao)], movimentos, [], [], true, false, false);
    }
}
