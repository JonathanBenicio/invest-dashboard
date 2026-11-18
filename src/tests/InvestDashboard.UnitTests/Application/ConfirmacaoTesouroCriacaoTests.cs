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
        ativo.InterestRate.Should().Be(10.50m);
        ativo.MaturityDate.Should().Be(new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc));
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
