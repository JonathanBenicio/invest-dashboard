using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentAssertions;
using InvestDashboard.Infrastructure.Services;

namespace InvestDashboard.UnitTests.Infrastructure;

public class LeitorPosicoesTesouroXlsxTests
{
    [Fact]
    public void Reconhece_lotes_do_portal_e_sinaliza_linhas_nao_interpretadas()
    {
        var linhas = CabecalhoPortal();
        linhas.Add(Linha(8, ["01/02/2020", "1,2345", "850,00", "1.049,33", "10%", "8%", "10,00", "1.100,00", "30", "22,5", "10,00", "0,00", "1,00", "0,00", "1.089,00"]));
        linhas.Add(Linha(9, ["Total"]));
        using var arquivo = Criar(linhas);

        var resultado = new LeitorPosicoesTesouroXlsx().Ler(arquivo);

        resultado.Layout.Should().Be("portal_tesouro");
        resultado.Linhas.Should().ContainSingle();
        resultado.Linhas[0].CamposBrutos["quantidade_titulos"].Should().Be("1,2345");
        resultado.Linhas[0].CamposBrutos["imposto_previsto_ir"].Should().Be("10,00");
        resultado.Linhas[0].CamposBrutos["taxa_devida_b3"].Should().Be("1,00");
        resultado.LinhasNaoInterpretadas.Should().Equal(9);
    }

    [Fact]
    public void Reconhece_layout_de_corretora_sem_gerar_movimentacoes()
    {
        var linhas = new List<Row>
        {
            Linha(6, ["Tesouro Direto"]),
            Linha(8, ["", "Saldo", "% Alocação", "Valor aplicado", "Quantidade", "Disponível", "Vencimento"]),
            Linha(9, ["Tesouro Prefixado 2035", "R$ 1.100,00", "50%", "R$ 1.000,00", "1,2", "1,2", "01/01/2035"])
        };
        using var arquivo = Criar(linhas);

        var resultado = new LeitorPosicoesTesouroXlsx().Ler(arquivo);

        resultado.Layout.Should().Be("corretora_posicao");
        resultado.Linhas.Should().ContainSingle();
        resultado.Linhas[0].CamposBrutos["titulo"].Should().Be("Tesouro Prefixado 2035");
        resultado.Linhas[0].CamposBrutos["vencimento"].Should().Be("01/01/2035");
        resultado.LinhasNaoInterpretadas.Should().BeEmpty();
    }

    [Fact]
    public void Cabecalho_alterado_nao_e_aceito_silenciosamente()
    {
        var linhas = CabecalhoPortal();
        var cabecalho = linhas.Single(r => r.RowIndex!.Value == 6);
        cabecalho.Elements<Cell>().Single(c => c.CellReference!.Value == "K6").InlineString = new InlineString(new Text("Outro campo"));
        using var arquivo = Criar(linhas);

        var acao = () => new LeitorPosicoesTesouroXlsx().Ler(arquivo);
        acao.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Linha_oculta_ou_incompleta_nao_e_interpretada()
    {
        var linhas = CabecalhoPortal();
        var linhaOculta = Linha(8, ["01/02/2020", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14"]);
        linhaOculta.Hidden = true;
        linhas.Add(linhaOculta);
        linhas.Add(Linha(9, ["01/02/2020", "1"]));
        using var arquivo = Criar(linhas);

        var resultado = new LeitorPosicoesTesouroXlsx().Ler(arquivo);

        resultado.Linhas.Should().BeEmpty();
        resultado.LinhasNaoInterpretadas.Should().Equal(8, 9);
    }

    [Fact]
    public void Coluna_oculta_de_cabecalho_ou_posicao_nao_e_utilizada()
    {
        var linhas = CabecalhoPortal();
        linhas.Add(Linha(8, ["01/02/2020", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14"]));
        using var arquivo = Criar(linhas, ocultarColuna: true);

        var acao = () => new LeitorPosicoesTesouroXlsx().Ler(arquivo);

        acao.Should().Throw<InvalidDataException>();
    }

    private static List<Row> CabecalhoPortal() =>
    [
        Linha(1, ["Extrato - Tesouro Prefixado 2035"]),
        Linha(4, ["Vencimento: 01/01/2035"]),
        Linha(6, ["Data da aplicação", "Quantidade de títulos A", "Preço do título na aplicação", "Valor investido", "Rentabilidade contratada", "Rentabilidade bruta", "Rentabilidade bruta", "Valor bruto", "Tempo em dias corridos", "Alíquota IR", "Imposto previsto", "Imposto previsto", "Taxa devida", "Taxa devida", "Valor líquido"]),
        Linha(7, ["", "", "", "", "", "Acumulada anualizada", "Acumulada", "", "", "", "IR", "IOF", "B3", "Instituição Financeira", ""])
    ];

    private static Row Linha(uint numero, string[] valores) => new(valores.Select((valor, indice) =>
        new Cell { CellReference = $"{(char)('A' + indice)}{numero}", DataType = CellValues.InlineString,
            InlineString = new InlineString(new Text(valor)) })) { RowIndex = numero };

    private static MemoryStream Criar(IEnumerable<Row> linhas, bool ocultarColuna = false)
    {
        var arquivo = new MemoryStream();
        using (var documento = SpreadsheetDocument.Create(arquivo, SpreadsheetDocumentType.Workbook, true))
        {
            var workbook = documento.AddWorkbookPart();
            workbook.Workbook = new Workbook();
            var folha = workbook.AddNewPart<WorksheetPart>();
            folha.Worksheet = new Worksheet();
            if (ocultarColuna)
                folha.Worksheet.Append(new Columns(new Column { Min = 2U, Max = 2U, Hidden = true }));
            folha.Worksheet.Append(new SheetData(linhas));
            workbook.Workbook.AppendChild(new Sheets()).Append(new Sheet
            {
                Id = workbook.GetIdOfPart(folha), SheetId = 1U, Name = "Exemplo"
            });
        }
        arquivo.Position = 0;
        return arquivo;
    }
}
