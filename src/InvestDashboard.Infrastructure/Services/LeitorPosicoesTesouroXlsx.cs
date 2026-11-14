using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace InvestDashboard.Infrastructure.Services;

/// <summary>
/// Reconhece dois layouts observados de posição. Não calcula saldos, datas, impostos
/// ou movimentações; os valores são apenas textos de origem para uma futura prévia.
/// </summary>
public sealed class LeitorPosicoesTesouroXlsx
{
    private static readonly (string Coluna, string Rotulo)[] CamposPortal =
    [
        ("A", "data_aplicacao"), ("B", "quantidade_titulos"), ("C", "preco_aplicacao"),
        ("D", "valor_investido"), ("E", "rentabilidade_contratada"),
        ("F", "rentabilidade_bruta_anualizada"), ("G", "rentabilidade_bruta_acumulada"),
        ("H", "valor_bruto"), ("I", "dias_corridos"), ("J", "aliquota_ir"),
        ("K", "imposto_previsto_ir"), ("L", "imposto_previsto_iof"),
        ("M", "taxa_devida_b3"), ("N", "taxa_devida_instituicao"), ("O", "valor_liquido")
    ];

    private static readonly (string Coluna, string Rotulo)[] CamposCorretora =
    [
        ("A", "titulo"), ("B", "saldo"), ("C", "alocacao"), ("D", "valor_aplicado"),
        ("E", "quantidade"), ("F", "disponivel"), ("G", "vencimento")
    ];

    private readonly LeitorPlanilhaXlsx leitor = new();

    public PosicoesTesouroXlsxLidas Ler(Stream arquivo)
    {
        var planilhas = leitor.Ler(arquivo);
        if (planilhas.Count != 1 || planilhas[0].Oculta)
            throw new InvalidDataException("O XLSX deve conter uma única folha visível.");
        var grade = new Dictionary<int, Dictionary<string, CelulaXlsxLida>>();
        foreach (var celula in planilhas[0].Celulas)
        {
            var match = Regex.Match(celula.Referencia, "^([A-Z]{1,3})([1-9][0-9]{0,6})$", RegexOptions.CultureInvariant);
            if (!match.Success || !int.TryParse(match.Groups[2].Value, out var numero))
                throw new InvalidDataException("Referência de célula inválida.");
            if (!grade.TryGetValue(numero, out var linha))
                grade[numero] = linha = new Dictionary<string, CelulaXlsxLida>(StringComparer.Ordinal);
            if (!linha.TryAdd(match.Groups[1].Value, celula))
                throw new InvalidDataException("Referência de célula duplicada.");
        }

        if (EhPortal(grade))
            return Extrair(grade, "portal_tesouro", 7, "A", CamposPortal,
                Valor(grade, 1, "A"), Valor(grade, 4, "A"));
        if (EhCorretora(grade))
            return Extrair(grade, "corretora_posicao", 8, "G", CamposCorretora, null, null);
        throw new InvalidDataException("Layout de posições XLSX não reconhecido.");
    }

    private static bool EhPortal(Dictionary<int, Dictionary<string, CelulaXlsxLida>> grade) =>
        Contem(grade, 6, "A", "data da aplicacao") && Contem(grade, 6, "B", "quantidade de titulos") &&
        Contem(grade, 6, "C", "preco do titulo na aplicacao") && Contem(grade, 6, "D", "valor investido") &&
        Contem(grade, 6, "E", "rentabilidade contratada") && Contem(grade, 6, "F", "rentabilidade bruta") &&
        Contem(grade, 6, "G", "rentabilidade bruta") && Contem(grade, 6, "H", "valor bruto") &&
        Contem(grade, 6, "I", "dias corridos") && Contem(grade, 6, "J", "aliquota") &&
        Contem(grade, 6, "K", "imposto previsto") && Contem(grade, 6, "L", "imposto previsto") &&
        Contem(grade, 6, "M", "taxa devida") && Contem(grade, 6, "N", "taxa devida") &&
        Contem(grade, 6, "O", "valor liquido") && Contem(grade, 7, "F", "anualizada") &&
        Contem(grade, 7, "G", "acumulada") && Contem(grade, 7, "K", "ir") &&
        Contem(grade, 7, "L", "iof") && Contem(grade, 7, "M", "b3") &&
        Contem(grade, 7, "N", "instituicao financeira");

    private static bool EhCorretora(Dictionary<int, Dictionary<string, CelulaXlsxLida>> grade) =>
        Contem(grade, 6, "A", "tesouro direto") && Contem(grade, 8, "B", "saldo") &&
        Contem(grade, 8, "C", "alocacao") && Contem(grade, 8, "D", "valor aplicado") &&
        Contem(grade, 8, "E", "quantidade") && Contem(grade, 8, "F", "disponivel") &&
        Contem(grade, 8, "G", "vencimento");

    private static PosicoesTesouroXlsxLidas Extrair(
        Dictionary<int, Dictionary<string, CelulaXlsxLida>> grade, string layout, int ultimaLinhaCabecalho,
        string colunaData, (string Coluna, string Rotulo)[] campos, string? cabecalho, string? vencimento)
    {
        var linhas = new List<LinhaPosicaoTesouroXlsx>();
        var ignoradas = new List<int>();
        foreach (var (numero, celulas) in grade.OrderBy(item => item.Key).Where(item => item.Key > ultimaLinhaCabecalho))
        {
            if (!celulas.TryGetValue(colunaData, out var data) || data.LinhaOculta || data.ColunaOculta ||
                !DateOnly.TryParseExact(data.ValorBruto.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _) || campos.Any(c => !celulas.ContainsKey(c.Coluna) || celulas[c.Coluna].LinhaOculta || celulas[c.Coluna].ColunaOculta))
            {
                ignoradas.Add(numero);
                continue;
            }
            linhas.Add(new LinhaPosicaoTesouroXlsx(numero, campos.ToDictionary(c => c.Rotulo, c => celulas[c.Coluna].ValorBruto)));
        }
        return new PosicoesTesouroXlsxLidas(layout, cabecalho, vencimento, linhas, ignoradas);
    }

    private static string? Valor(Dictionary<int, Dictionary<string, CelulaXlsxLida>> grade, int linha, string coluna) =>
        grade.TryGetValue(linha, out var celulas) && celulas.TryGetValue(coluna, out var celula) && !celula.LinhaOculta && !celula.ColunaOculta
            ? celula.ValorBruto : null;

    private static bool Contem(Dictionary<int, Dictionary<string, CelulaXlsxLida>> grade, int linha, string coluna, string esperado) =>
        Normalizar(Valor(grade, linha, coluna) ?? string.Empty).Contains(esperado, StringComparison.Ordinal);

    private static string Normalizar(string texto)
    {
        var letras = texto.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return Regex.Replace(new string(letras.ToArray()).ToLowerInvariant(), "\\s+", " ").Trim();
    }
}

public sealed record PosicoesTesouroXlsxLidas(string Layout, string? CabecalhoOriginal, string? VencimentoOriginal,
    IReadOnlyList<LinhaPosicaoTesouroXlsx> Linhas, IReadOnlyList<int> LinhasNaoInterpretadas);
public sealed record LinhaPosicaoTesouroXlsx(int NumeroLinha, IReadOnlyDictionary<string, string> CamposBrutos);
