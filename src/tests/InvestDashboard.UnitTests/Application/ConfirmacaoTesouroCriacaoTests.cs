using FluentAssertions;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Services;

namespace InvestDashboard.UnitTests.Application;

public sealed class ConfirmacaoTesouroCriacaoTests
{
    [Fact]
    public void CriaAtivoInicialComDadosContratuaisReconhecidos()
    {
        var ativo = ConfirmacaoTesouro.CriarAtivoInicial(Previa("Tesouro Prefixado 2035", "10,50%"), 22m);

        ativo.Should().NotBeNull();
        ativo!.Indexer.Should().Be("PREFIXADO");
        ativo.InterestRate.Should().BeNull();
        ativo.MaturityDate.Should().Be(new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CriaAtivoComTaxasDistintasSemDefinirTaxaCompartilhada()
    {
        var primeira = Previa("Tesouro Prefixado 2035", "10,50%");
        var segunda = Previa("Tesouro Prefixado 2035", "12,00%");
        var previa = primeira with { Posicoes = [primeira.Posicoes[0], segunda.Posicoes[0]] };

        var ativo = ConfirmacaoTesouro.CriarAtivoInicial(previa, 22m);

        ativo.Should().NotBeNull();
        ativo!.InterestRate.Should().BeNull();
        previa.Posicoes.Select(p => p.DadosInterpretados!.RentabilidadeContratada).Should().Equal("10,50%", "12,00%");
    }

    [Fact]
    public void CalculaSaldoInicialDeSnapshotValidoSemMovimentacoes()
    {
        var previa = Previa("Tesouro Prefixado 2035", "10,50%");

        ConfirmacaoTesouro.TentarCalcularSaldo(previa, new DateOnly(2024, 3, 3), out var saldo).Should().BeTrue();
        saldo.Quantidade.Should().Be(1m);
        saldo.CustoTotal.Should().Be(21m);
    }

    [Theory]
    [InlineData("Tesouro Prefixado 2035", "10% a 12%")]
    [InlineData("Título desconhecido", "10%")]
    [InlineData("Tesouro Prefixado 2035", "não informada")]
    public void RecusaAtivoInicialQuandoContratoForAmbiguo(string titulo, string taxa)
    {
        ConfirmacaoTesouro.CriarAtivoInicial(Previa(titulo, taxa), 22m).Should().BeNull();
    }

    private static PreviaTesouroDto Previa(string titulo, string taxa)
    {
        var posicao = new DadosPosicaoTesouroDto(new DateOnly(2024, 3, 2), 1m, 20m, 21m, taxa,
            null, null, 22m, 1, null, null, null, null, null, 22m, []);
        return new PreviaTesouroDto(Guid.NewGuid(), "html", "portal", titulo, "2035-01-01", null,
            [new PosicaoPreviaTesouroDto(1, new Dictionary<string, string>(), posicao)], [], [], [], true, false, false);
    }
}
