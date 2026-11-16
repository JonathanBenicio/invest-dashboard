using System.Text;
using FluentAssertions;
using InvestDashboard.Infrastructure.Services;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class LeitorPreviaTesouroTests
{
    private static string Html(string data, string tipo, string quantidade, string preco, string taxa, string valor) => $$"""
        <div class="td-custom-meus-investimentos-titulo">
          <h1 class="td-meus-investimentos-detalhe__titulo_card">Título sintético</h1>
          <table><tr class="saldo-table-vencimento"><th><strong>VENCIMENTO: 01/01/2035</strong></th></tr></table>
          <div class="td-posicao-detalhada__info">
            <p class="td-posicao-detalhada__info__valor">Data: <span class="td-posicao-detalhada__info__data">{{data}}</span></p>
            <p class="td-posicao-detalhada__info__valor">Adicional: <span class="td-posicao-detalhada__info__data">sem significado estabelecido</span></p>
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
        var previa = Ler(Html("02/03/2024", "Compra", "0,01", "R$ 1.234,56", "10,00%", "12,35"));
        var movimento = previa.Movimentacoes.Should().ContainSingle().Subject;
        var dados = movimento.DadosInterpretados!;
        dados.Data.Should().Be(new DateOnly(2024, 3, 2));
        dados.Tipo.Should().Be("Compra");
        dados.Quantidade.Should().Be(0.01m);
        dados.Preco.Should().Be(1234.56m);
        dados.TaxaAnual.Should().Be(10m);
        dados.Valor.Should().Be(12.35m);
        dados.CamposNaoInterpretados.Should().BeEmpty();
        movimento.Campos[1].Valor.Should().Be("sem significado estabelecido");
        previa.PodeConfirmar.Should().BeFalse();
    }

    [Fact]
    public void MantemValoresAmbiguosParaRevisaoSemInferirOperacao()
    {
        var previa = Ler(Html("31/02/2024", "Resgate", "0", "1.23", "desconhecida", "-12,35"));
        var movimento = previa.Movimentacoes.Should().ContainSingle().Subject;
        var dados = movimento.DadosInterpretados!;
        dados.Data.Should().BeNull();
        dados.Tipo.Should().BeNull();
        dados.Quantidade.Should().BeNull();
        dados.Preco.Should().BeNull();
        dados.TaxaAnual.Should().BeNull();
        dados.Valor.Should().BeNull();
        dados.CamposNaoInterpretados.Should().Equal("data", "tipo", "quantidade", "preco", "taxaAnual", "valor");
        movimento.Campos[4].Valor.Should().Be("1.23");
    }
}
