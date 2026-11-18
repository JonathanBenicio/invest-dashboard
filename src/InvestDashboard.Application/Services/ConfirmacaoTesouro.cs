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
        var normalizado = titulo.Normalize(NormalizationForm.FormD);
        var indexador = normalizado.Contains("SELIC", StringComparison.OrdinalIgnoreCase) ? "SELIC"
            : normalizado.Contains("IPCA", StringComparison.OrdinalIgnoreCase) ||
              normalizado.Contains("RENDA+", StringComparison.OrdinalIgnoreCase) ||
              normalizado.Contains("EDUCA+", StringComparison.OrdinalIgnoreCase) ? "IPCA"
            : normalizado.Contains("IGPM", StringComparison.OrdinalIgnoreCase) ||
              normalizado.Contains("IGP-M", StringComparison.OrdinalIgnoreCase) ? "IGPM"
            : normalizado.Contains("PREFIXADO", StringComparison.OrdinalIgnoreCase) ? "PREFIXADO"
            : null;
        if (indexador is null) return null;
        foreach (var linha in previa.Posicoes)
        {
            var contrato = linha.DadosInterpretados?.RentabilidadeContratada?.Trim();
            if (string.IsNullOrWhiteSpace(contrato)) return null;
            var taxas = Regex.Matches(contrato, @"([0-9]+(?:[.,][0-9]+)?)\s*%", RegexOptions.CultureInvariant);
            if (taxas.Count != 1 || !decimal.TryParse(taxas[0].Groups[1].Value.Replace(',', '.'),
                    NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _)) return null;
        }

        var referencia = Encoding.UTF8.GetBytes($"{titulo}|{vencimento:yyyy-MM-dd}".ToUpperInvariant());
        var ticker = "TD" + Convert.ToHexString(SHA256.HashData(referencia))[..12];
        return new RendaFixa(Guid.NewGuid(), ticker, titulo, precoUnitario, DateTime.UtcNow,
            indexador, null, vencimento.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), "Tesouro Nacional", "TESOURO_DIRETO");
    }

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

    public static bool EventoCompleto(DadosMovimentoTesouroDto? m, DateOnly dataReferencia) =>
        m?.Data is DateOnly data && data <= dataReferencia && m.Valor is not null &&
        m.CamposNaoInterpretados is { Count: 0 } &&
        ((m.Natureza == "Taxas" && m.Tipo is "Crédito" or "Débito") ||
            (TipoNegociacao(m) is not null && m.Quantidade is > 0 && m.Preco is > 0));
}
