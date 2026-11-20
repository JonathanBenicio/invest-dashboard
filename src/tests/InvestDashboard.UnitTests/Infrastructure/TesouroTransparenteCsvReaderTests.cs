using FluentAssertions;
using InvestDashboard.Infrastructure.Services;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class TesouroTransparenteCsvReaderTests
{
    private const string Cabecalho = "Tipo Titulo;Data Vencimento;Data Base;Taxa Compra Manha;Taxa Venda Manha;PU Compra Manha;PU Venda Manha;PU Base Manha";

    [Fact]
    public async Task LerAsync_LeFormatoOficialSemConfundirDataBaseComVencimento()
    {
        var csv = "\uFEFF" + Cabecalho + "\n" +
                  "Tesouro Prefixado;01/01/2032;12/11/2026;14,65;14,77;487,51;484,65;484,65\n" +
                  "Tesouro Selic;01/03/2031;13/11/2026;0,11;0,12;20201,00;20181,99;20181,99\n";

        var dados = await LerTudo(csv);

        dados.Should().HaveCount(2);
        dados[0].TipoTitulo.Should().Be("Tesouro Prefixado");
        dados[0].DataVencimento.Should().Be(new DateOnly(2032, 1, 1));
        dados[0].DataBase.Should().Be(new DateOnly(2026, 11, 12));
        dados[0].TaxaCompraManha.Should().Be(14.65m);
        dados[0].TaxaVendaManha.Should().Be(14.77m);
        dados[0].PuCompraManha.Should().Be(487.51m);
        dados[0].PuVendaManha.Should().Be(484.65m);
        dados[0].PuBaseManha.Should().Be(484.65m);
        dados[1].PuBaseManha.Should().Be(20181.99m);
        TesouroTransparenteCsvReader.Fonte.Should().Contain("Tesouro Transparente");
    }

    [Fact]
    public async Task LerAsync_AceitaCamposEntreAspasEAspasEscapadas()
    {
        var csv = Cabecalho + "\n\"Tesouro \"\"Exemplo\"\"\";01/01/2032;12/11/2026;1,23;1,24;1.234,56;1.200,01;1.200,00";

        var dados = await LerTudo(csv);

        dados.Should().ContainSingle();
        dados[0].TipoTitulo.Should().Be("Tesouro \"Exemplo\"");
        dados[0].PuCompraManha.Should().Be(1234.56m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Tipo;Data;PU")]
    [InlineData(Cabecalho + ";Coluna Nova")]
    [InlineData(Cabecalho)]
    public async Task LerAsync_RejeitaArquivoVazioOuCabecalhoDesconhecido(string csv)
    {
        var action = async () => await LerTudo(csv);
        await action.Should().ThrowAsync<InvalidDataException>();
    }

    [Theory]
    [InlineData("Tesouro Selic;01/03/2031;12/11/2026;0,11;0,12;20201,00;20181,99;")]
    [InlineData("Tesouro Selic;01/03/2031;12/11/2026;0,11;0,12;20201,00;20181,99")]
    [InlineData("Tesouro Selic;31/02/2031;12/11/2026;0,11;0,12;20201,00;20181,99;20181,99")]
    [InlineData("Tesouro Selic;01/03/2031;12/11/2026;0.11;0,12;20201,00;20181,99;20181,99")]
    [InlineData("\"Tesouro Selic;01/03/2031;12/11/2026;0,11;0,12;20201,00;20181,99;20181,99")]
    public async Task LerAsync_RejeitaCamposAusentesOuInvalidos(string registro)
    {
        var action = async () => await LerTudo(Cabecalho + "\n" + registro);
        await action.Should().ThrowAsync<InvalidDataException>().WithMessage("*Linha 2*");
    }

    [Fact]
    public async Task LerAsync_RejeitaChaveDuplicadaMesmoQuandoNaoConsecutiva()
    {
        const string registro = "Tesouro Selic;01/03/2031;12/11/2026;0,11;0,12;20201,00;20181,99;20181,99";
        const string outraData = "Tesouro Selic;01/03/2031;13/11/2026;0,11;0,12;20201,00;20181,99;20181,99";
        var action = async () => await LerTudo(Cabecalho + "\n" + registro + "\n" + outraData + "\n" + registro);

        await action.Should().ThrowAsync<InvalidDataException>().WithMessage("*Linha 4*duplicada*");
    }

    [Fact]
    public async Task LerAsync_RespeitaCancelamento()
    {
        using var cancelamento = new CancellationTokenSource();
        await cancelamento.CancelAsync();
        var action = async () => await LerTudo(Cabecalho, cancelamento.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task LerAsync_RejeitaLinhaMuitoLongaAntesDeConsumirArquivoInteiro()
    {
        var csv = Cabecalho + "\r\n" + new string('x', 32769);
        var action = async () => await LerTudo(csv);
        await action.Should().ThrowAsync<InvalidDataException>().WithMessage("*tamanho permitido*");
    }

    [Fact]
    public async Task LerAsync_AceitaSeparadoresDeLinhaCrLfECr()
    {
        const string linha1 = "Tesouro Selic;01/03/2031;12/11/2026;0,11;0,12;20201,00;20181,99;20181,99";
        const string linha2 = "Tesouro Selic;01/03/2031;13/11/2026;0,11;0,12;20201,00;20181,99;20181,99";
        var dados = await LerTudo(Cabecalho + "\r\n" + linha1 + "\r" + linha2 + "\n");
        dados.Should().HaveCount(2);
    }

    private static async Task<List<CotacaoTesouroTransparente>> LerTudo(string csv, CancellationToken token = default)
    {
        using var reader = new StringReader(csv);
        var result = new List<CotacaoTesouroTransparente>();
        await foreach (var row in TesouroTransparenteCsvReader.LerAsync(reader, token)) result.Add(row);
        return result;
    }
}
