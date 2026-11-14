using System.IO.Compression;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentAssertions;
using InvestDashboard.Infrastructure.Services;

namespace InvestDashboard.UnitTests.Infrastructure;

public class LeitorPlanilhaXlsxTests
{
    [Fact]
    public void Le_preservando_valores_brutos_tipos_e_linhas_ocultas()
    {
        using var arquivo = CriarPlanilha(
            new Row(
                new Cell { CellReference = "A1", DataType = CellValues.SharedString, CellValue = new CellValue("0") },
                new Cell { CellReference = "B1", DataType = CellValues.InlineString, InlineString = new InlineString(new Text("15/08/2035")) },
                new Cell { CellReference = "C1", DataType = CellValues.Number, CellValue = new CellValue("2.5") }),
            new Row(new Cell { CellReference = "A2", DataType = CellValues.SharedString, CellValue = new CellValue("1") }) { Hidden = true });

        var resultado = new LeitorPlanilhaXlsx().Ler(arquivo);

        resultado.Should().ContainSingle();
        resultado[0].Oculta.Should().BeFalse();
        resultado[0].Celulas.Select(c => (c.Referencia, c.ValorBruto, c.LinhaOculta)).Should().Equal(
            ("A1", "Quantidade", false), ("B1", "15/08/2035", false), ("C1", "2.5", false),
            ("A2", "1,234567", true));
    }

    [Fact]
    public void Rejeita_formula_em_vez_de_confiar_em_valor_calculado()
    {
        using var arquivo = CriarPlanilha(new Row(new Cell
        {
            CellReference = "A1", CellFormula = new CellFormula("1+1"), CellValue = new CellValue("2")
        }));
        var acao = () => new LeitorPlanilhaXlsx().Ler(arquivo);
        acao.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Rejeita_referencia_a_texto_inexistente()
    {
        using var arquivo = CriarPlanilha(new Row(new Cell
        {
            CellReference = "A1", DataType = CellValues.SharedString, CellValue = new CellValue("99")
        }));
        var acao = () => new LeitorPlanilhaXlsx().Ler(arquivo);
        acao.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Sinaliza_celulas_de_intervalo_de_colunas_ocultas()
    {
        using var arquivo = CriarPlanilhaComColunaOculta();

        var resultado = new LeitorPlanilhaXlsx().Ler(arquivo);

        resultado[0].Celulas.Select(c => (c.Referencia, c.ColunaOculta)).Should().Equal(
            ("A1", false), ("B1", true), ("C1", true), ("D1", false));
    }

    [Fact]
    public void Rejeita_entrada_duplicada_no_pacote()
    {
        using var arquivo = new MemoryStream();
        using (var zip = new ZipArchive(arquivo, ZipArchiveMode.Create, true))
        {
            foreach (var nome in new[] { "xl/workbook.xml", "XL/workbook.xml" })
            {
                using var escrita = new StreamWriter(zip.CreateEntry(nome).Open(), Encoding.UTF8);
                escrita.Write("conteúdo sintético");
            }
        }
        arquivo.Position = 0;
        var acao = () => new LeitorPlanilhaXlsx().Ler(arquivo);
        acao.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Rejeita_arquivo_acima_do_limite()
    {
        using var arquivo = new MemoryStream(new byte[10 * 1024 * 1024 + 1]);
        var acao = () => new LeitorPlanilhaXlsx().Ler(arquivo);
        acao.Should().Throw<InvalidDataException>();
    }

    private static MemoryStream CriarPlanilha(params Row[] linhas)
    {
        var arquivo = new MemoryStream();
        using (var documento = SpreadsheetDocument.Create(arquivo, SpreadsheetDocumentType.Workbook, true))
        {
            var workbook = documento.AddWorkbookPart();
            workbook.Workbook = new Workbook();
            var strings = workbook.AddNewPart<SharedStringTablePart>();
            strings.SharedStringTable = new SharedStringTable(
                new SharedStringItem(new Text("Quantidade")), new SharedStringItem(new Text("1,234567")));
            var folha = workbook.AddNewPart<WorksheetPart>();
            folha.Worksheet = new Worksheet(new SheetData(linhas));
            workbook.Workbook.AppendChild(new Sheets()).Append(new Sheet
            {
                Id = workbook.GetIdOfPart(folha), SheetId = 1U, Name = "Exemplo"
            });
        }
        arquivo.Position = 0;
        return arquivo;
    }

    private static MemoryStream CriarPlanilhaComColunaOculta()
    {
        var arquivo = new MemoryStream();
        using (var documento = SpreadsheetDocument.Create(arquivo, SpreadsheetDocumentType.Workbook, true))
        {
            var workbook = documento.AddWorkbookPart();
            workbook.Workbook = new Workbook();
            var folha = workbook.AddNewPart<WorksheetPart>();
            folha.Worksheet = new Worksheet(
                new Columns(new Column { Min = 2U, Max = 3U, Hidden = true }),
                new SheetData(new Row(Enumerable.Range(0, 4).Select(indice => new Cell
                {
                    CellReference = $"{(char)('A' + indice)}1", DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text("sintético"))
                }))));
            workbook.Workbook.AppendChild(new Sheets()).Append(new Sheet
            {
                Id = workbook.GetIdOfPart(folha), SheetId = 1U, Name = "Exemplo"
            });
        }
        arquivo.Position = 0;
        return arquivo;
    }
}
