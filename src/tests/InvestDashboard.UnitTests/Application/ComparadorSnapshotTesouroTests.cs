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

    [Fact]
    public void ApresentaEventosELinhasDePosicaoNovosSemAgregarOcorrencias()
    {
        var carteira = Guid.NewGuid();
        var ativo = Guid.NewGuid();
        var dataBase = new DateOnly(2024, 3, 3);
        var anterior = new BasePosicaoTesouro(Guid.NewGuid(), Guid.NewGuid(), carteira, ativo, dataBase, 1m, 20m, 22m);
        var movimento = new DadosMovimentoTesouroDto(new DateOnly(2024, 3, 4), "Venda", "Débito", 1m, 22m, null, 22m, []);
        var linhaNova = new PosicaoPreviaTesouroDto(2, new Dictionary<string, string>(),
            new DadosPosicaoTesouroDto(new DateOnly(2024, 3, 4), 1m, 21m, 21m, "11%", null, null, 22m,
                1, null, null, null, null, null, 22m, []));
        var resultado = ComparadorSnapshotTesouro.Preparar(Guid.NewGuid(), new string('a', 64), 1,
            new DateOnly(2024, 3, 4), Previa(carteira, [new([], movimento), new([], movimento)], [linhaNova]),
            anterior, Previa(carteira, []), [], 1m);

        resultado.Should().NotBeNull();
        resultado!.Divergencias.Count(x => x.Tipo == "evento_novo").Should().Be(2);
        resultado.Divergencias.Should().ContainSingle(x => x.Tipo == "evento_ambiguo");
        resultado.Divergencias.Should().ContainSingle(x => x.Tipo == "linha_posicao_nova");
    }

    [Fact]
    public void DiferenciaFechamentoComprovadoDePosicaoNaoObservada()
    {
        var carteira = Guid.NewGuid();
        var baseAtiva = new BasePosicaoTesouro(Guid.NewGuid(), Guid.NewGuid(), carteira, Guid.NewGuid(),
            new DateOnly(2024, 3, 3), 1m, 20m, 22m);
        var previaSemPosicao = Previa(carteira, [], []);
        var encerramento = new EncerramentoPosicaoTesouroDto(true, 1m, 0m, "Venda integral comprovada.");

        var resultado = ComparadorSnapshotTesouro.Preparar(Guid.NewGuid(), new string('b', 64), 1,
            new DateOnly(2024, 3, 4), previaSemPosicao, baseAtiva, Previa(carteira, []), [], 0m, encerramento);

        resultado!.Divergencias.Should().ContainSingle(x => x.Tipo == "posicao_encerrada_por_venda");
        resultado.Divergencias.Should().NotContain(x => x.Tipo == "posicao_nao_observada");
    }

    private static PreviaTesouroDto Previa(Guid carteira, IReadOnlyList<MovimentoPreviaTesouroDto> movimentos,
        IReadOnlyList<PosicaoPreviaTesouroDto>? posicoes = null)
    {
        var posicao = new DadosPosicaoTesouroDto(new DateOnly(2024, 3, 2), 1m, 20m, 20m, "10%", null, null, 22m, 1, null, null, null, null, null, 22m, []);
        return new(carteira, "html", "portal", "Tesouro Prefixado 2035", "2035-01-01", null,
            posicoes ?? [new(1, new Dictionary<string, string>(), posicao)], movimentos, [], [], true, false, false);
    }
}
