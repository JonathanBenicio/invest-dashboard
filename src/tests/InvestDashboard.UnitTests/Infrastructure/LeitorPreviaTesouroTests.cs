using System.Text;
using FluentAssertions;
using InvestDashboard.Infrastructure.Services;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class LeitorPreviaTesouroTests
{
    private static string Html(string data, string natureza, string tipo, string quantidade, string preco, string taxa, string valor, string rotuloNatureza = "Natureza") => $$"""
        <div class="td-custom-meus-investimentos-titulo">
          <h1 class="td-meus-investimentos-detalhe__titulo_card">Título sintético</h1>
          <table><tr class="saldo-table-vencimento"><th><strong>VENCIMENTO: 01/01/2035</strong></th></tr></table>
          <div class="td-posicao-detalhada__info">
            <p class="td-posicao-detalhada__info__valor">Data: <span class="td-posicao-detalhada__info__data">{{data}}</span></p>
            <p class="td-posicao-detalhada__info__valor">{{rotuloNatureza}}: <span class="td-posicao-detalhada__info__data">{{natureza}}</span></p>
            <p class="td-posicao-detalhada__info__valor">Tipo: <span class="td-posicao-detalhada__info__data">{{tipo}}</span></p>
            <p class="td-posicao-detalhada__info__valor">Quantidade: <span class="td-posicao-detalhada__info__data">{{quantidade}}</span></p>
            <p class="td-posicao-detalhada__info__valor">Preço (R$): <span class="td-posicao-detalhada__info__data">{{preco}}</span></p>
            <p class="td-posicao-detalhada__info__valor">Taxa (% a.a.): <span class="td-posicao-detalhada__info__data">{{taxa}}</span></p>
            <p class="td-posicao-detalhada__info__valor">Valor (R$): <span class="td-posicao-detalhada__info__data">{{valor}}</span></p>
          </div>
        </div>
        """;

    private static InvestDashboard.Application.DTOs.Importacao.PreviaTesouroDto Ler(string html) =>
        new LeitorPreviaTesouro().Ler(Guid.NewGuid(), "html", new MemoryStream(Encoding.UTF8.GetBytes(html)));

    [Fact]
    public void InterpretaCamposConhecidosSemEliminarOsDadosOriginais()
    {
        var previa = Ler(Html("02/03/2024", "Compra", "Crédito", "0,01", "R$ 1.234,56", "10,00%", "12,35"));
        var movimento = previa.Movimentacoes.Should().ContainSingle().Subject;
        var dados = movimento.DadosInterpretados!;
        dados.Data.Should().Be(new DateOnly(2024, 3, 2));
        dados.Natureza.Should().Be("Compra");
        dados.Tipo.Should().Be("Crédito");
        dados.Quantidade.Should().Be(0.01m);
        dados.Preco.Should().Be(1234.56m);
        dados.TaxaAnual.Should().Be(10m);
        dados.Valor.Should().Be(12.35m);
        dados.CamposNaoInterpretados.Should().BeEmpty();
        movimento.Campos[1].Valor.Should().Be("Compra");
        previa.PodeConfirmar.Should().BeFalse();
    }

    [Fact]
    public void MantemValoresAmbiguosParaRevisaoSemInferirOperacao()
    {
        var previa = Ler(Html("31/02/2024", "Resgate", "desconhecido", "0", "1.23", "desconhecida", "-12,35"));
        var movimento = previa.Movimentacoes.Should().ContainSingle().Subject;
        var dados = movimento.DadosInterpretados!;
        dados.Data.Should().BeNull();
        dados.Natureza.Should().BeNull();
        dados.Tipo.Should().BeNull();
        dados.Quantidade.Should().BeNull();
        dados.Preco.Should().BeNull();
        dados.TaxaAnual.Should().BeNull();
        dados.Valor.Should().BeNull();
        dados.CamposNaoInterpretados.Should().Equal("data", "natureza", "tipo", "quantidade", "preco", "taxaAnual", "valor");
        movimento.Campos[4].Valor.Should().Be("1.23");
    }

    [Theory]
    [InlineData("Compra", "Crédito")]
    [InlineData("Venda", "Débito")]
    [InlineData("Taxas", "Débito")]
    public void PreservaNaturezaETipoSeparadamente(string natureza, string tipo)
    {
        var dados = Ler(Html("02/03/2024", natureza, tipo, "1", "1", "1", "1"))
            .Movimentacoes.Should().ContainSingle().Subject.DadosInterpretados!;
        dados.Natureza.Should().Be(natureza);
        dados.Tipo.Should().Be(tipo);
        dados.CamposNaoInterpretados.Should().BeEmpty();
    }

    [Fact]
    public void NaoInterpretaCampoComRotuloDiferenteComoNatureza()
    {
        var dados = Ler(Html("02/03/2024", "Compra", "Crédito", "1", "1", "1", "1", "Outro"))
            .Movimentacoes.Should().ContainSingle().Subject.DadosInterpretados!;
        dados.Natureza.Should().BeNull();
        dados.CamposNaoInterpretados.Should().Contain("natureza");
    }
}
