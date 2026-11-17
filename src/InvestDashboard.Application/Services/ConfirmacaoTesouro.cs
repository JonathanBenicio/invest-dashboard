using System.Globalization;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Trading;

namespace InvestDashboard.Application.Services;

public static class ConfirmacaoTesouro
{
    private const decimal LimiteBase = 9_999_999_999.99999999m;

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
            previa.Movimentacoes.Count is 0 or > 10_000)
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
