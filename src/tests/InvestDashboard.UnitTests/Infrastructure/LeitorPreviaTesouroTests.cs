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

    private static string TabelaPosicao(string quantidade = "0,01", string valorLiquido = "1,23", string dias = "30", bool incluirLinha = true) => $$"""
        <table><tbody>
        <tr class="saldo-table-data-names"><td></td><td>DATA DA APLICAÇÃO</td><td>QUANTIDADE DE TÍTULOS</td><td>PREÇO DO TÍTULO NA APLICAÇÃO</td><td>VALOR INVESTIDO</td><td>RENTABILIDADE CONTRATADA</td></tr>
        <tr class="saldo-table-data-names"><td colspan="2">RENTABILIDADE CONTRATADA</td><td>VALOR BRUTO</td><td>TEMPO DA APLICAÇÃO</td><td>ALÍQUOTA</td><td colspan="2">IMPOSTO PREVISTO</td><td colspan="2">TAXA DEVIDA</td><td>VALOR LÍQUIDO</td></tr>
        <tr class="saldo-table-data-names"><td>ANUALIZADA</td><td>ACUMULADA</td><td>I.R.</td><td>IOF</td><td>B3</td><td>INSTITUIÇÃO FINANCEIRA</td></tr>
        {{(incluirLinha ? $"<tr class=\"saldo-table-data-values\"><td></td><td>02/03/2024</td><td>{quantidade}</td><td>R$ 123,45</td><td>1,23</td><td>IPCA + 6%</td><td>-1,20%</td><td>2%</td><td>1,25</td><td>{dias}</td><td>22,5%</td><td>0,01</td><td>0,00</td><td>0,01</td><td>0,00</td><td>{valorLiquido}</td></tr>" : string.Empty)}}
        </tbody></table>
        """;

    [Fact]
    public void InterpretaPosicaoSemAlterarTextoOriginalOuConfirmar()
    {
        var html = Html("02/03/2024", "Compra", "Crédito", "1", "1", "1", "1").Replace("</div>\n</div>", "</div>\n" + TabelaPosicao() + "\n</div>");
        var previa = Ler(html);
        var posicao = previa.Posicoes.Should().ContainSingle().Subject;
        var dados = posicao.DadosInterpretados!;
        dados.DataAplicacao.Should().Be(new DateOnly(2024, 3, 2));
        dados.QuantidadeTitulos.Should().Be(0.01m);
        dados.PrecoAplicacao.Should().Be(123.45m);
        dados.RentabilidadeContratada.Should().Be("IPCA + 6%");
        dados.RentabilidadeBrutaAnualizada.Should().Be(-1.20m);
        dados.DiasCorridos.Should().Be(30);
        dados.ValorLiquido.Should().Be(1.23m);
        dados.CamposNaoInterpretados.Should().BeEmpty();
        posicao.CamposBrutos["preco_aplicacao"].Should().Be("R$ 123,45");
        previa.SecaoPosicoesReconhecida.Should().BeTrue();
        previa.PodeConfirmar.Should().BeFalse();
    }

    [Fact]
    public void ReconheceTabelaCanonicaVaziaSemSugerirSaldoOuPosicao()
    {
        var html = Html("02/03/2024", "Compra", "Crédito", "1", "1", "1", "1")
            .Replace("</div>\n</div>", "</div>\n" + TabelaPosicao(incluirLinha: false) + "\n</div>");

        var previa = Ler(html);

        previa.Posicoes.Should().BeEmpty();
        previa.SecaoPosicoesReconhecida.Should().BeTrue();
    }

    [Fact]
    public void IndicaQueSecaoDePosicoesNaoFoiReconhecidaQuandoElaEstaAusente()
    {
        var previa = Ler(Html("02/03/2024", "Compra", "Crédito", "1", "1", "1", "1"));

        previa.SecaoPosicoesReconhecida.Should().BeFalse();
    }

    [Fact]
    public void SinalizaDadosAmbiguosDePosicaoSemInventarValores()
    {
        var html = Html("02/03/2024", "Compra", "Crédito", "1", "1", "1", "1").Replace("</div>\n</div>", "</div>\n" + TabelaPosicao("1.23", "-1,00", "1,5") + "\n</div>");
        var dados = Ler(html).Posicoes.Should().ContainSingle().Subject.DadosInterpretados!;
        dados.QuantidadeTitulos.Should().BeNull();
        dados.DiasCorridos.Should().BeNull();
        dados.ValorLiquido.Should().BeNull();
        dados.CamposNaoInterpretados.Should().Equal("quantidadeTitulos", "diasCorridos", "valorLiquido");
    }

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

    [Fact]
    public void DistingueCamposNaoAplicaveisEmTaxasSemAtribuirZero()
    {
        var previa = Ler(Html("02/03/2024", "Taxas   (?)", "Débito", "-", "—", "–", "1,25"));
        var movimento = previa.Movimentacoes.Should().ContainSingle().Subject;
        var dados = movimento.DadosInterpretados!;
        dados.Natureza.Should().Be("Taxas");
        dados.Tipo.Should().Be("Débito");
        dados.Quantidade.Should().BeNull();
        dados.Preco.Should().BeNull();
        dados.TaxaAnual.Should().BeNull();
        dados.Valor.Should().Be(1.25m);
        dados.CamposNaoAplicaveis.Should().Equal("quantidade", "preco", "taxaAnual");
        dados.CamposNaoInterpretados.Should().BeEmpty();
        movimento.Campos[1].Valor.Should().Be("Taxas   (?)");
    }

    [Fact]
    public void TracoEmCompraSinalizaTaxaComoNaoInformadaSemTratarComoNaoAplicavel()
    {
        var dados = Ler(Html("02/03/2024", "Compra", "Crédito", "-", "-", "-", "1"))
            .Movimentacoes.Should().ContainSingle().Subject.DadosInterpretados!;
        dados.CamposNaoInterpretados.Should().Equal("quantidade", "preco");
        dados.CamposNaoAplicaveis.Should().BeEmpty();
        dados.CamposNaoInformados.Should().Equal("taxaAnual");
    }

    [Fact]
    public void TaxaAnualMalformadaEmCompraContinuaExigindoRevisao()
    {
        var dados = Ler(Html("02/03/2024", "Compra", "Crédito", "1", "1", "desconhecida", "1"))
            .Movimentacoes.Should().ContainSingle().Subject.DadosInterpretados!;
        dados.CamposNaoInterpretados.Should().Equal("taxaAnual");
        dados.CamposNaoInformados.Should().BeEmpty();
    }

    [Fact]
    public void TracoEmVendaContinuaNaoAplicavel()
    {
        var dados = Ler(Html("02/03/2024", "Venda", "Débito", "1", "1", "-", "1"))
            .Movimentacoes.Should().ContainSingle().Subject.DadosInterpretados!;
        dados.CamposNaoInterpretados.Should().BeEmpty();
        dados.CamposNaoAplicaveis.Should().Equal("taxaAnual");
        dados.CamposNaoInformados.Should().BeEmpty();
    }
}
