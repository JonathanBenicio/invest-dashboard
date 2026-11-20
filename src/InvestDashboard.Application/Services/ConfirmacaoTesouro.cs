using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Trading;

namespace InvestDashboard.Application.Services;

public static class ConfirmacaoTesouro
{
    private const decimal LimiteBase = 9_999_999_999.99999999m;

    public static RendaFixa? CriarAtivoInicial(PreviaTesouroDto previa, decimal precoUnitario)
    {
        if (previa.Formato != "html" || string.IsNullOrWhiteSpace(previa.Titulo) ||
            !DateOnly.TryParseExact(previa.Vencimento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var vencimento) ||
            previa.Posicoes.Count == 0)
            return null;

        var titulo = previa.Titulo.Trim();
        var indexador = IdentificarIndexador(titulo);
        if (indexador is null) return null;
        foreach (var linha in previa.Posicoes)
        {
            if (!TaxaContratadaValida(linha.DadosInterpretados?.RentabilidadeContratada)) return null;
        }

        var referencia = Encoding.UTF8.GetBytes($"{titulo}|{vencimento:yyyy-MM-dd}".ToUpperInvariant());
        var ticker = "TD" + Convert.ToHexString(SHA256.HashData(referencia))[..12];
        return new RendaFixa(Guid.NewGuid(), ticker, titulo, precoUnitario, DateTime.UtcNow,
            indexador, null, vencimento.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Tesouro Nacional", "TESOURO_DIRETO");
    }

    public static IReadOnlyList<string> MotivosCriacaoPosicaoInicial(PreviaTesouroDto previa,
        DateOnly dataReferencia, string estadoLote, bool tituloJaExiste)
    {
        var motivos = new List<string>();
        if (estadoLote != "Pendente") motivos.Add("Esta prévia já foi processada. Abra a posição existente para continuar.");
        if (previa.Formato != "html") motivos.Add("A criação inicial está disponível somente para extrato HTML.");
        if (string.IsNullOrWhiteSpace(previa.Titulo)) motivos.Add("O título não foi reconhecido no HTML.");
        else if (IdentificarIndexador(previa.Titulo) is null)
            motivos.Add("Não foi possível reconhecer o tipo do título pelo nome. Confira se o título é Prefixado, Selic ou indexado a IPCA/IGP-M.");
        if (!DateOnly.TryParseExact(previa.Vencimento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            motivos.Add("O vencimento não foi identificado em um formato válido no HTML.");
        if (previa.Posicoes.Count == 0)
            motivos.Add("Nenhuma linha de posição foi encontrada. O HTML precisa conter a posição do título, não apenas movimentações.");
        else
        {
            var linhasSemTaxa = previa.Posicoes.Select((linha, indice) => (linha, indice))
                .Where(item => !TaxaContratadaValida(item.linha.DadosInterpretados?.RentabilidadeContratada))
                .Select(item => (item.indice + 1).ToString(CultureInfo.InvariantCulture)).ToArray();
            if (linhasSemTaxa.Length > 0)
                motivos.Add($"A rentabilidade contratada não foi reconhecida em {FormatarLinhas(linhasSemTaxa)}. Confira esses campos na tabela de posições.");
        }
        if (tituloJaExiste)
            motivos.Add("Este título já tem uma posição na carteira. Selecione a posição existente para atualizar o saldo.");
        if (previa.LinhasNaoInterpretadas.Count > 0)
            motivos.Add($"Há {previa.LinhasNaoInterpretadas.Count} linhas sem interpretação; confira a tabela da prévia e o arquivo original.");
        if (!TentarCalcularSaldo(previa, dataReferencia, out _))
        {
            if (previa.Movimentacoes.Count > 10_000)
                motivos.Add("O extrato excede o limite de movimentações aceito para uma prévia.");
            else if (previa.Posicoes.Count > 0 && previa.LinhasNaoInterpretadas.Count == 0)
                motivos.Add("Não foi possível calcular o saldo: cada linha precisa de data de aplicação, quantidade, valor investido e valor bruto válidos.");
        }

        var movimentacoesIncompletas = previa.Movimentacoes.Select((movimento, indice) => (movimento, indice))
            .Where(item => !EventoCompleto(item.movimento.DadosInterpretados, dataReferencia)).ToArray();
        if (movimentacoesIncompletas.Length > 0)
        {
            var campos = movimentacoesIncompletas.SelectMany(item => item.movimento.DadosInterpretados?.CamposNaoInterpretados ?? [])
                .Distinct(StringComparer.Ordinal).ToArray();
            var detalhes = campos.Length == 0 ? "Confira data, natureza, tipo, quantidade, preço e valor." :
                $"Revise estes campos na tabela de movimentações: {string.Join(", ", campos.Select(RotuloCampoMovimento))}.";
            motivos.Add($"Há {movimentacoesIncompletas.Length} movimentações que precisam de revisão. {detalhes}");
        }

        return motivos.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string? IdentificarIndexador(string titulo)
    {
        var normalizado = titulo.Normalize(NormalizationForm.FormD);
        return normalizado.Contains("SELIC", StringComparison.OrdinalIgnoreCase) ? "SELIC"
            : normalizado.Contains("IPCA", StringComparison.OrdinalIgnoreCase) ||
              normalizado.Contains("RENDA+", StringComparison.OrdinalIgnoreCase) ||
              normalizado.Contains("EDUCA+", StringComparison.OrdinalIgnoreCase) ? "IPCA"
            : normalizado.Contains("IGPM", StringComparison.OrdinalIgnoreCase) ||
              normalizado.Contains("IGP-M", StringComparison.OrdinalIgnoreCase) ? "IGPM"
            : normalizado.Contains("PREFIXADO", StringComparison.OrdinalIgnoreCase) ? "PREFIXADO"
            : null;
    }

    private static bool TaxaContratadaValida(string? contrato)
    {
        if (string.IsNullOrWhiteSpace(contrato)) return false;
        var taxas = Regex.Matches(contrato.Trim(), @"([0-9]+(?:[.,][0-9]+)?)\s*%", RegexOptions.CultureInvariant);
        return taxas.Count == 1 && decimal.TryParse(taxas[0].Groups[1].Value.Replace(',', '.'),
            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _);
    }

    private static string RotuloCampoMovimento(string campo) => campo switch
    {
        "data" => "data",
        "natureza" => "natureza",
        "tipo" => "tipo",
        "quantidade" => "quantidade",
        "preco" => "preço",
        "taxaAnual" => "taxa do título (% a.a.)",
        "valor" => "valor",
        _ => campo
    };

    private static string FormatarLinhas(IReadOnlyList<string> linhas) =>
        linhas.Count == 1 ? $"na linha {linhas[0]}" : $"nas linhas {string.Join(", ", linhas)}";

    public static bool TituloCompativel(PreviaTesouroDto previa, RendaFixa? ativo) =>
        ativo is not null && ativo.Subtype == "TESOURO_DIRETO" && previa.Formato == "html" &&
        string.Equals(ativo.Name, previa.Titulo, StringComparison.OrdinalIgnoreCase) &&
        DateOnly.TryParseExact(previa.Vencimento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var vencimento) &&
        DateOnly.FromDateTime(ativo.MaturityDate) == vencimento;

    public static bool TentarCalcularSaldo(PreviaTesouroDto previa, DateOnly dataReferencia,
        out (decimal Quantidade, decimal CustoTotal, decimal PrecoUnitario) saldo)
    {
        saldo = default;
        if (previa.LinhasNaoInterpretadas.Count > 0 || previa.Posicoes.Count == 0 ||
            previa.Movimentacoes.Count > 10_000)
            return false;
        decimal quantidade = 0, custo = 0, bruto = 0;
        foreach (var linha in previa.Posicoes)
        {
            var p = linha.DadosInterpretados;
            if (p?.DataAplicacao is not DateOnly data || data > dataReferencia ||
                p.QuantidadeTitulos is not (> 0 and <= LimiteBase) || p.ValorInvestido is not (> 0 and <= LimiteBase) ||
                p.ValorBruto is not (> 0 and <= LimiteBase) || p.CamposNaoInterpretados is null ||
                p.CamposNaoInterpretados.Any(c => c is "dataAplicacao" or "quantidadeTitulos" or "valorInvestido" or "valorBruto"))
                return false;
            quantidade += p.QuantidadeTitulos.Value;
            custo += p.ValorInvestido.Value;
            bruto += p.ValorBruto.Value;
        }
        // Não aceitar ajustes silenciosos da precisão dos saldos persistidos.
        if (quantidade > LimiteBase || custo > LimiteBase || decimal.Round(quantidade, 8) != quantidade ||
            decimal.Round(custo, 8) != custo || quantidade < 0.00000001m)
            return false;
        var preco = decimal.Round(bruto / quantidade, 8);
        if (preco <= 0 || preco > LimiteBase) return false;
        saldo = (quantidade, custo, preco);
        return true;
    }

    public static TipoTransacao? TipoNegociacao(DadosMovimentoTesouroDto? m) => (m?.Natureza, m?.Tipo) switch
    {
        ("Compra", "Crédito") => TipoTransacao.Buy,
        ("Venda", "Débito") => TipoTransacao.Sell,
        _ => null
    };

    public static bool EventoCompleto(DadosMovimentoTesouroDto? m, DateOnly dataReferencia)
    {
        if (m?.Data is not DateOnly data || data > dataReferencia || m.Valor is null || m.CamposNaoInterpretados is null)
            return false;

        if (m.CamposNaoInterpretados.Count > 0) return false;

        return (m.Natureza == "Taxas" && m.Tipo is "Crédito" or "Débito") ||
            (TipoNegociacao(m) is not null && m.Quantidade is > 0 && m.Preco is > 0);
    }
}
