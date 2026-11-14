using System.IO.Compression;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace InvestDashboard.Infrastructure.Services;

/// <summary>
/// Leitura estrutural de XLSX em memória. Os valores são retornados como texto,
/// sem interpretação financeira, acesso externo, execução de fórmulas ou persistência.
/// </summary>
public sealed class LeitorPlanilhaXlsx
{
    private const int MaxArquivo = 10 * 1024 * 1024;
    private const long MaxDescompactado = 32 * 1024 * 1024;
    private const int MaxEntradas = 256;
    private const int MaxPlanilhas = 10;
    private const int MaxCelulas = 100_000;
    private const int MaxTexto = 10_000;

    public IReadOnlyList<PlanilhaXlsxLida> Ler(Stream arquivo)
    {
        ArgumentNullException.ThrowIfNull(arquivo);
        using var copia = new MemoryStream();
        var buffer = new byte[81920];
        int quantidade;
        while ((quantidade = arquivo.Read(buffer, 0, buffer.Length)) != 0)
        {
            if (copia.Length + quantidade > MaxArquivo)
                throw new InvalidDataException("O arquivo XLSX excede o tamanho permitido.");
            copia.Write(buffer, 0, quantidade);
        }

        copia.Position = 0;
        using (var zip = new ZipArchive(copia, ZipArchiveMode.Read, leaveOpen: true))
        {
            if (zip.Entries.Count > MaxEntradas ||
                zip.Entries.Sum(entry => entry.Length) > MaxDescompactado ||
                zip.Entries.Select(entry => entry.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != zip.Entries.Count ||
                zip.Entries.Any(entry => entry.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("A estrutura do arquivo XLSX não é suportada.");
        }

        copia.Position = 0;
        using var documento = SpreadsheetDocument.Open(copia, false);
        var workbook = documento.WorkbookPart ?? throw new InvalidDataException("Planilha XLSX inválida.");
        var folhas = workbook.Workbook.Sheets?.Elements<Sheet>().ToList()
            ?? throw new InvalidDataException("Planilha XLSX sem folhas.");
        if (folhas.Count is 0 or > MaxPlanilhas)
            throw new InvalidDataException("Quantidade de folhas XLSX não suportada.");

        var textos = workbook.SharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>()
            .Select(item => LimitarTexto(item.InnerText)).ToArray() ?? [];
        var resultado = new List<PlanilhaXlsxLida>();
        var totalCelulas = 0;
        foreach (var folha in folhas)
        {
            if (folha.Id?.Value is not string id || workbook.GetPartById(id) is not WorksheetPart parte)
                throw new InvalidDataException("Folha XLSX não suportada.");
            var colunasOcultas = new List<(uint Inicio, uint Fim)>();
            foreach (var coluna in parte.Worksheet.Elements<Columns>().SelectMany(c => c.Elements<Column>()))
            {
                var inicio = coluna.Min?.Value ?? 0;
                var fim = coluna.Max?.Value ?? 0;
                if (inicio == 0 || fim < inicio || fim > 16_384)
                    throw new InvalidDataException("Intervalo de colunas XLSX inválido.");
                if (coluna.Hidden?.Value == true)
                    colunasOcultas.Add((inicio, fim));
            }
            var celulas = new List<CelulaXlsxLida>();
            foreach (var linha in parte.Worksheet.Descendants<Row>())
            {
                foreach (var celula in linha.Elements<Cell>())
                {
                    if (++totalCelulas > MaxCelulas || celula.CellFormula is not null)
                        throw new InvalidDataException("A planilha contém fórmulas ou excede o limite de células.");
                    var referencia = celula.CellReference?.Value;
                    if (string.IsNullOrWhiteSpace(referencia))
                        throw new InvalidDataException("Célula XLSX sem referência.");
                    var indiceColuna = IndiceColuna(referencia);
                    var tipo = celula.DataType?.Value;
                    string? valor = celula.CellValue?.InnerText;
                    if (tipo == CellValues.SharedString)
                    {
                        if (!int.TryParse(valor, out var indice) || indice < 0 || indice >= textos.Length)
                            throw new InvalidDataException("Referência de texto XLSX inválida.");
                        valor = textos[indice];
                    }
                    else if (tipo == CellValues.InlineString)
                        valor = celula.InlineString?.InnerText;
                    else if (tipo == CellValues.Error)
                        throw new InvalidDataException("A planilha contém uma célula com erro.");
                    else if (tipo is not null && tipo != CellValues.Number && tipo != CellValues.String &&
                             tipo != CellValues.Boolean && tipo != CellValues.Date)
                        throw new InvalidDataException("Tipo de célula XLSX não suportado.");

                    celulas.Add(new CelulaXlsxLida(referencia, LimitarTexto(valor ?? string.Empty),
                        tipo?.ToString() ?? "Number", linha.Hidden?.Value == true,
                        colunasOcultas.Any(intervalo => indiceColuna >= intervalo.Inicio && indiceColuna <= intervalo.Fim)));
                }
            }
            resultado.Add(new PlanilhaXlsxLida(LimitarTexto(folha.Name?.Value ?? string.Empty),
                folha.State?.Value != null && folha.State.Value != SheetStateValues.Visible, celulas));
        }
        return resultado;
    }

    private static string LimitarTexto(string texto) => texto.Length <= MaxTexto
        ? texto : throw new InvalidDataException("O conteúdo de uma célula XLSX excede o limite permitido.");

    private static uint IndiceColuna(string referencia)
    {
        uint indice = 0;
        var i = 0;
        while (i < referencia.Length && referencia[i] is >= 'A' and <= 'Z')
        {
            indice = indice * 26 + (uint)(referencia[i] - 'A' + 1);
            if (indice > 16_384)
                throw new InvalidDataException("Referência de coluna XLSX inválida.");
            i++;
        }
        if (indice == 0 || i == referencia.Length || referencia[i] is < '1' or > '9' ||
            referencia[i..].Any(c => c is < '0' or > '9'))
            throw new InvalidDataException("Referência de célula XLSX inválida.");
        return indice;
    }
}

public sealed record PlanilhaXlsxLida(string Nome, bool Oculta, IReadOnlyList<CelulaXlsxLida> Celulas);
public sealed record CelulaXlsxLida(string Referencia, string ValorBruto, string Tipo, bool LinhaOculta, bool ColunaOculta);
