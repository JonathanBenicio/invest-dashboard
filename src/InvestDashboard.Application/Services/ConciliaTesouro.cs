using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Trading;

namespace InvestDashboard.Application.Services;

/// <summary>Calcula apenas sugestões; não associa, altera ou confirma registros.</summary>
public static class ConciliaTesouro
{
    private static readonly TimeZoneInfo FusoBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static ConciliacaoTesouroDto Preparar(Carteira carteira, DateOnly dataReferencia,
        PreviaTesouroDto previa, Guid? ativoId, IReadOnlyList<Transacao> transacoes)
    {
        var posicoesRendaFixa = carteira.Positions.Where(p => p.TipoAtivo == TipoAtivo.RendaFixa).ToArray();
        if (ativoId.HasValue && !posicoesRendaFixa.Any(p => p.AtivoId == ativoId.Value))
            throw new ArgumentException("Ativo não disponível na carteira.", nameof(ativoId));

        var posicoesListadas = ativoId.HasValue
            ? posicoesRendaFixa.Where(p => p.AtivoId == ativoId.Value)
            : posicoesRendaFixa.Where(p => ConfirmacaoTesouro.TituloCompativel(previa, p.Ativo as RendaFixa));
        var ativos = posicoesListadas
            .Select(p => new AtivoConciliacaoTesouroDto(
                p.AtivoId,
                p.Ticker,
                p.Ativo?.Name ?? p.Ticker,
                p.Ativo is RendaFixa rendaFixa ? DateOnly.FromDateTime(rendaFixa.MaturityDate) : null))
            .OrderBy(p => p.Nome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Vencimento)
            .ThenBy(p => p.Ticker, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var relevantes = ativoId is null ? [] : transacoes
            .Where(t => t.CarteiraId == carteira.Id && t.AtivoId == ativoId && t.Type is TipoTransacao.Buy or TipoTransacao.Sell)
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.Id).ToArray();
        var lista = relevantes.Select(t => new TransacaoConciliacaoTesouroDto(t.Id, t.Type.ToString(), t.TransactionDate,
            t.Quantity, t.UnitPrice, DiaCivil(t.TransactionDate) is DateOnly d && d > dataReferencia)).ToArray();

        // Não deduplicar por data/valor: múltiplas operações idênticas são possíveis.
        var indice = relevantes.Where(t => DiaCivil(t.TransactionDate) is DateOnly d && d <= dataReferencia)
            .GroupBy(t => (DiaCivil(t.TransactionDate)!.Value, t.Type, t.Quantity, t.UnitPrice))
            .ToDictionary(g => g.Key, g => g.Select(t => t.Id).ToArray());
        var eventos = previa.Movimentacoes.Select((m, i) =>
        {
            var dados = m.DadosInterpretados;
            var tipo = (dados?.Natureza, dados?.Tipo) switch
            {
                ("Compra", "Crédito") => TipoTransacao.Buy,
                ("Venda", "Débito") => TipoTransacao.Sell,
                _ => (TipoTransacao?)null
            };
            Guid[] candidatos = [];
            if (dados?.Data is DateOnly dia && dia <= dataReferencia && tipo.HasValue && dados.Quantidade is decimal quantidade && dados.Preco is decimal preco)
                indice.TryGetValue((dia, tipo.Value, quantidade, preco), out candidatos!);
            return new EventoConciliacaoTesouroDto(i, dados, candidatos ?? []);
        }).ToArray();
        return new ConciliacaoTesouroDto(carteira.Version, dataReferencia, ativoId, ativos, eventos, lista, false);
    }

    private static DateOnly? DiaCivil(DateTime instante) => instante.Kind == DateTimeKind.Utc
        ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(instante, FusoBrasil)) : null;
}
