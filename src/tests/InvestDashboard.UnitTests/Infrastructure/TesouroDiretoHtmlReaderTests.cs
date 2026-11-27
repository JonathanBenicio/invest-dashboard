using FluentAssertions;
using InvestDashboard.Infrastructure.Services;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class TesouroDiretoHtmlReaderTests
{
    // Dados exclusivamente sintéticos, sem informações de arquivos particulares.
    private const string Cartao = """
        <div class="td-posicao-detalhada__info">
          <p class="td-posicao-detalhada__info__valor">Data: <span class="td-posicao-detalhada__info__data">02/03/2024</span></p>
          <p class="td-posicao-detalhada__info__valor">Campo adicional: <span class="td-posicao-detalhada__info__data">Informação sintética</span></p>
          <p class="td-posicao-detalhada__info__valor">Tipo: <span class="td-posicao-detalhada__info__data">Compra</span></p>
          <p class="td-posicao-detalhada__info__valor">Quantidade: <span class="td-posicao-detalhada__info__data">0,01</span></p>
          <p class="td-posicao-detalhada__info__valor">Preço (R$): <span class="td-posicao-detalhada__info__data">123,45</span></p>
          <p class="td-posicao-detalhada__info__valor">Taxa (% a.a.) <span class="td-posicao-detalhada__info__data">10,00</span></p>
          <p class="td-posicao-detalhada__info__valor">Valor (R$): <span class="td-posicao-detalhada__info__data">1,23</span></p>
        </div>
        """;

    private const string CabecalhosPosicao = """
        <tr class="saldo-table-vencimento"><th colspan="16">VENCIMENTO</th></tr>
        <tr class="saldo-table-headers"><th colspan="6">CABEÇALHO</th><th colspan="10">CABEÇALHO</th></tr>
        <tr class="saldo-table-data-names"><td></td><td>DATA DA APLICAÇÃO</td><td>QUANTIDADE DE TÍTULOS</td><td>PREÇO DO TÍTULO NA APLICAÇÃO</td><td>VALOR INVESTIDO</td><td>RENTABILIDADE CONTRATADA</td></tr>
        <tr class="saldo-table-data-names"><td colspan="2">RENTABILIDADE CONTRATADA</td><td>VALOR BRUTO</td><td>TEMPO DA APLICAÇÃO</td><td>ALÍQUOTA</td><td colspan="2">IMPOSTO PREVISTO</td><td colspan="2">TAXA DEVIDA</td><td>VALOR LÍQUIDO</td></tr>
        <tr class="saldo-table-data-names"><td>ANUALIZADA</td><td>ACUMULADA</td><td>I.R.</td><td>IOF</td><td>B3</td><td>INSTITUIÇÃO FINANCEIRA</td></tr>
        """;

    private static string Documento(string cartoes, params string[] referencias) => $$"""
        <div class="td-custom-meus-investimentos-titulo">
          <h1 class="td-meus-investimentos-detalhe__titulo_card">Tesouro Prefixado 2035</h1>
          {{string.Concat(referencias.Select(referencia => $"<h2>{referencia}</h2>"))}}
          <table><tbody><tr class="saldo-table-vencimento"><th><strong>VENCIMENTO: 01/01/2035</strong></th></tr></tbody></table>
          {{cartoes}}
        </div>
        """;

    [Fact]
    public void Ler_ExtraiCartoesSemInterpretarOCampoAdicional()
    {
        var html = Documento(Cartao + Cartao.Replace("Compra", "Venda"));

        var resultado = new TesouroDiretoHtmlReader().Ler(html);

        resultado.Titulo.Should().Be("Tesouro Prefixado 2035");
        resultado.Vencimento.Should().Be(new DateOnly(2035, 1, 1));
        resultado.Movimentacoes.Should().HaveCount(2);
        resultado.Movimentacoes[0].Campos.Select(c => c.Valor).Should().Equal(
            "02/03/2024", "Informação sintética", "Compra", "0,01", "123,45", "10,00", "1,23");
        resultado.Movimentacoes[0].Campos[1].Rotulo.Should().Be("Campo adicional:");
        resultado.Movimentacoes[1].Campos[2].Valor.Should().Be("Venda");
    }

    [Fact]
    public void Ler_ExtraiMesEAnoDaReferenciaVisivelDoExtrato()
    {
        var resultado = new TesouroDiretoHtmlReader().Ler(Documento(Cartao, "Posição em Novembro/2026"));

        resultado.MesReferencia.Should().Be(11);
        resultado.AnoReferencia.Should().Be(2026);
    }

    [Fact]
    public void Ler_ExtraiReferenciaDoTextoDoBlocoSemMisturarValorFilho()
    {
        var cabecalho = "<div class=\"td-meu-investimento-titulo\">Posição em Novembro/2026<span class=\"td-meu-investimento-titulo--valor\">Saldo sintético</span></div>";
        var html = Documento(Cartao).Replace("</h1>", "</h1>" + cabecalho);

        var resultado = new TesouroDiretoHtmlReader().Ler(html);

        resultado.MesReferencia.Should().Be(11);
        resultado.AnoReferencia.Should().Be(2026);
    }

    [Fact]
    public void Ler_ExtraiInstituicaoDoCabecalhoSemUsarTextoDeOutrosCampos()
    {
        const string instituicao = """
            <div class="td-meus-investimentos-detalhe-box">
              <div class="td-meus-investimentos-deatlhe-instituicao">
                <p class="td-meus-investimentos-deatlhe-instituicao__info"><span class="td-meus-investimentos-deatlhe-instituicao__nome">Instituição sintética CTVM S/A</span></p>
              </div>
            </div>
            """;
        var html = Documento(Cartao, "Posição em Novembro/2026").Replace("</h1>", "</h1>" + instituicao);

        var resultado = new TesouroDiretoHtmlReader().Ler(html);

        resultado.InstituicaoHtml.Should().Be("Instituição sintética CTVM S/A");
    }

    [Fact]
    public void Ler_DeixaInstituicaoVaziaQuandoOIdentificadorDoCabecalhoEstaDuplicado()
    {
        const string instituicao = """
            <div class="td-meus-investimentos-detalhe-box">
              <div class="td-meus-investimentos-deatlhe-instituicao">
                <p class="td-meus-investimentos-deatlhe-instituicao__info"><span class="td-meus-investimentos-deatlhe-instituicao__nome">Instituição sintética CTVM S/A</span></p>
              </div>
            </div>
            """;
        var html = Documento(Cartao).Replace("</h1>", "</h1>" + instituicao + instituicao);

        var resultado = new TesouroDiretoHtmlReader().Ler(html);

        resultado.InstituicaoHtml.Should().BeNull();
    }

    [Fact]
    public void Ler_DeixaReferenciaManualQuandoCabecalhosConflitam()
    {
        var resultado = new TesouroDiretoHtmlReader().Ler(Documento(Cartao,
            "Posição em Novembro/2026", "Posição em Dezembro/2026"));

        resultado.MesReferencia.Should().BeNull();
        resultado.AnoReferencia.Should().BeNull();
    }

    [Fact]
    public void Ler_DeixaReferenciaManualQuandoFormatoNaoEhReconhecido()
    {
        var resultado = new TesouroDiretoHtmlReader().Ler(Documento(Cartao, "Posição em Novemb/2026"));

        resultado.MesReferencia.Should().BeNull();
        resultado.AnoReferencia.Should().BeNull();
    }

    [Fact]
    public void Ler_IgnoraCartoesNoTextoDeScriptESomenteLeMarkup()
    {
        var html = $"<script>{Cartao}</script><img src='https://invalid.example/external'>{Documento(Cartao)}";

        var resultado = new TesouroDiretoHtmlReader().Ler(html);

        resultado.Movimentacoes.Should().ContainSingle();
    }

    [Fact]
    public void Ler_ExtraiValoresDaPosicaoSemInterpretarTributos()
    {
        const string tabela = """
            <table><tbody>
            <tr class="saldo-table-data-names"><td></td><td>DATA DA APLICAÇÃO</td><td>QUANTIDADE DE TÍTULOS</td><td>PREÇO DO TÍTULO NA APLICAÇÃO</td><td>VALOR INVESTIDO</td><td>RENTABILIDADE CONTRATADA</td></tr>
            <tr class="saldo-table-data-names"><td colspan="2">RENTABILIDADE CONTRATADA</td><td>VALOR BRUTO</td><td>TEMPO DA APLICAÇÃO</td><td>ALÍQUOTA</td><td colspan="2">IMPOSTO PREVISTO</td><td colspan="2">TAXA DEVIDA</td><td>VALOR LÍQUIDO</td></tr>
            <tr class="saldo-table-data-names"><td>ANUALIZADA</td><td>ACUMULADA</td><td>I.R.</td><td>IOF</td><td>B3</td><td>INSTITUIÇÃO FINANCEIRA</td></tr>
            <tr class="saldo-table-data-values"><td></td><td>02/03/2024</td><td>0,01</td><td>123,45</td><td>1,23</td><td>10%</td><td>11%</td><td>2%</td><td>1,25</td><td>30</td><td>22,5%</td><td>0,01</td><td>0,00</td><td>0,01</td><td>0,00</td><td>1,23</td></tr>
            </tbody></table>
            """;
        var resultado = new TesouroDiretoHtmlReader().Ler(Documento(Cartao + tabela));
        var campos = resultado.Posicoes.Should().ContainSingle().Which.CamposBrutos;
        campos["quantidade_titulos"].Should().Be("0,01");
        campos["imposto_previsto_ir"].Should().Be("0,01");
        campos["valor_liquido"].Should().Be("1,23");

        var semCartoes = new TesouroDiretoHtmlReader().Ler(Documento(tabela));
        semCartoes.Posicoes.Should().ContainSingle();
        semCartoes.Movimentacoes.Should().BeEmpty();
    }

    [Fact]
    public void Ler_MarcaSecaoDePosicoesReconhecidaMesmoQuandoATabelaEstaVazia()
    {
        var tabela = $"<table><tbody>{CabecalhosPosicao}</tbody></table>";

        var resultado = new TesouroDiretoHtmlReader().Ler(Documento(Cartao + tabela));

        resultado.Posicoes.Should().BeEmpty();
        resultado.SecaoPosicoesReconhecida.Should().BeTrue();
    }

    [Fact]
    public void Ler_NaoMarcaSecaoReconhecidaQuandoTabelaVaziaTemLinhaDeClasseDesconhecida()
    {
        var tabela = $"<table><tbody>{CabecalhosPosicao}<tr class=\"saldo-table-dados-alternativos\"><td>Saldo sintético</td></tr></tbody></table>";

        var resultado = new TesouroDiretoHtmlReader().Ler(Documento(Cartao + tabela));

        resultado.Posicoes.Should().BeEmpty();
        resultado.SecaoPosicoesReconhecida.Should().BeFalse();
        resultado.Movimentacoes.Should().ContainSingle();
    }

    [Fact]
    public void Ler_ExigeCelulasDeCabecalhoEmThParaReconhecerSecaoVazia()
    {
        var cabecalhosInvalidos = CabecalhosPosicao
            .Replace("<th colspan=\"6\">", "<td colspan=\"6\">")
            .Replace("</th><th colspan=\"10\">", "</td><td colspan=\"10\">")
            .Replace("</th>", "</td>");
        var tabela = $"<table><tbody>{cabecalhosInvalidos}</tbody></table>";

        var resultado = new TesouroDiretoHtmlReader().Ler(Documento(Cartao + tabela));

        resultado.Posicoes.Should().BeEmpty();
        resultado.SecaoPosicoesReconhecida.Should().BeFalse();
    }

    [Fact]
    public void Ler_NaoCombinaCabecalhosEDadosDePosicaoEmTabelasDiferentes()
    {
        const string tabelaDados = """
            <table><tbody><tr class="saldo-table-data-values"><td></td><td>02/03/2024</td><td>0,01</td><td>123,45</td><td>1,23</td><td>10%</td><td>11%</td><td>2%</td><td>1,25</td><td>30</td><td>22,5%</td><td>0,01</td><td>0,00</td><td>0,01</td><td>0,00</td><td>1,23</td></tr></tbody></table>
            """;
        var documento = Documento(Cartao + $"<table><tbody>{CabecalhosPosicao}</tbody></table>" + tabelaDados);

        var resultado = new TesouroDiretoHtmlReader().Ler(documento);

        resultado.Posicoes.Should().BeEmpty();
        resultado.SecaoPosicoesReconhecida.Should().BeFalse();
        resultado.Movimentacoes.Should().ContainSingle();
    }

    [Fact]
    public void Ler_NaoMarcaSecaoDePosicoesQuandoATabelaEstaAusente()
    {
        var resultado = new TesouroDiretoHtmlReader().Ler(Documento(Cartao));

        resultado.SecaoPosicoesReconhecida.Should().BeFalse();
    }

    [Fact]
    public void Ler_RejeitaTabelaDePosicoesComCabecalhoDesconhecido()
    {
        var tabela = "<table><tr class='saldo-table-data-values'><td>valor</td></tr></table>";
        var acao = () => new TesouroDiretoHtmlReader().Ler(Documento(Cartao + tabela));
        acao.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Ler_RejeitaEstruturaIncompletaSemRetornarResultadoParcial()
    {
        var invalido = Cartao.Replace("Quantidade:", "Outro:");
        var acao = () => new TesouroDiretoHtmlReader().Ler(Documento(Cartao + invalido));

        acao.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData("hidden")]
    [InlineData("aria-hidden='true'")]
    [InlineData("style='display: none !important'")]
    [InlineData("style='visibility: hidden'")]
    public void Ler_RejeitaCartaoOculto(string atributo)
    {
        var acao = () => new TesouroDiretoHtmlReader().Ler($"<section {atributo}>{Documento(Cartao)}</section>");

        acao.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Ler_RejeitaConteudoOcultoDentroDeValor()
    {
        var modificado = Cartao.Replace("Compra</span>", "Compra<span hidden>ignorar</span></span>");
        var acao = () => new TesouroDiretoHtmlReader().Ler(Documento(modificado));

        acao.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Ler_IndicaRevisaoObrigatoriaQuandoVisibilidadePorCssNaoEhInterpretada()
    {
        var documento = "<style>.td-posicao-detalhada__info { display: none; }</style>" + Documento(Cartao);

        var resultado = new TesouroDiretoHtmlReader().Ler(documento);

        resultado.Movimentacoes.Should().ContainSingle();
        resultado.VisibilidadeCssVerificada.Should().BeFalse();
        resultado.ExigeRevisao.Should().BeTrue();
    }

    [Fact]
    public void Ler_RejeitaPaginaSemCartoesEArquivoGrande()
    {
        var reader = new TesouroDiretoHtmlReader();

        var estruturaDesconhecida = () => reader.Ler("<html><body>sem cartões</body></html>");
        var muitoGrande = () => reader.Ler(new string('x', TesouroDiretoHtmlReader.MaximoCaracteres + 1));

        estruturaDesconhecida.Should().Throw<InvalidDataException>();
        muitoGrande.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Ler_RejeitaTituloOuVencimentoAmbiguo()
    {
        var documento = Documento(Cartao);
        var semVencimento = documento.Replace("VENCIMENTO: 01/01/2035", "VENCIMENTO: desconhecido");
        var tituloDuplicado = documento.Replace("</h1>", "</h1><h1 class='td-meus-investimentos-detalhe__titulo_card'>Outro título</h1>");
        var cartaoForaDoContexto = documento + Cartao;
        var reader = new TesouroDiretoHtmlReader();

        var primeiro = () => reader.Ler(semVencimento);
        var segundo = () => reader.Ler(tituloDuplicado);
        var terceiro = () => reader.Ler(cartaoForaDoContexto);

        primeiro.Should().Throw<InvalidDataException>();
        segundo.Should().Throw<InvalidDataException>();
        terceiro.Should().Throw<InvalidDataException>();
    }
}
