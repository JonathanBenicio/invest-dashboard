using System.Globalization;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;

namespace InvestDashboard.Application.Services;

/// <summary>Reconcilia vendas pós-base sem inferir encerramento pela ausência de uma posição.</summary>
public static class CalculadorEncerramentoTesouro
{
    private const decimal LimiteQuantidade = 9_999_999_999.99999999m;
    private static readonly TimeZoneInfo FusoBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static EncerramentoPosicaoTesouroDto Avaliar(PreviaTesouroDto previa,
        DateOnly dataReferencia, BasePosicaoTesouro? baseAtiva, PreviaTesouroDto? previaAnterior,
        IReadOnlyCollection<Transacao> transacoes,
        IReadOnlyDictionary<int, DecisaoConciliacaoTesouro> revisoes)
    {
        var quantidadeAnterior = baseAtiva?.Quantidade;
        if (previa.Posicoes.Count > 0)
            return Rejeitar(quantidadeAnterior, null, "O HTML ainda apresenta uma posição para este título.");
        if (baseAtiva is null)
            return Rejeitar(null, null, "Não há uma base ativa anterior para reconciliar as vendas.");
        if (baseAtiva.DataReferencia >= dataReferencia)
            return Rejeitar(quantidadeAnterior, null, "A data do extrato precisa ser posterior à base ativa.");
        if (baseAtiva.Quantidade <= 0)
            return Rejeitar(quantidadeAnterior, baseAtiva.Quantidade, "A base ativa já registra a posição encerrada.");
        if (!previa.SecaoPosicoesReconhecida)
            return Rejeitar(quantidadeAnterior, null, "A seção de posições do HTML não foi reconhecida por inteiro. A base ativa será preservada.");
        if (previa.LinhasNaoInterpretadas.Count > 0 || previaAnterior is null)
            return Rejeitar(quantidadeAnterior, null, "Há dados incompletos para comparar os extratos. A base ativa será preservada para revisão.");

        var movimentos = new List<(int Indice, DateOnly Data, DadosMovimentoTesouroDto Dados, TipoTransacao Tipo, decimal Quantidade)>();
        foreach (var (movimento, indice) in previa.Movimentacoes.Select((movimento, indice) => (movimento, indice)))
        {
            var dados = movimento.DadosInterpretados;
            if (!ConfirmacaoTesouro.EventoCompleto(dados, dataReferencia))
                return Rejeitar(quantidadeAnterior, null, "Uma movimentação está incompleta. A base ativa será preservada para revisão.");
            if (dados!.Data is not DateOnly data || data <= baseAtiva.DataReferencia || data > dataReferencia || dados.Natureza == "Taxas")
                continue;

            var tipo = ConfirmacaoTesouro.TipoNegociacao(dados);
            if (tipo is null || dados.Quantidade is not decimal quantidade || quantidade <= 0 ||
                decimal.Round(quantidade, 8) != quantidade)
                return Rejeitar(quantidadeAnterior, null, "Uma movimentação da janela não permite reconciliar a quantidade. A base ativa será preservada.");

            movimentos.Add((indice, data, dados, tipo.Value, quantidade));
        }

        if (movimentos.Count == 0 || movimentos.All(movimento => movimento.Tipo != TipoTransacao.Sell))
            return Rejeitar(quantidadeAnterior, quantidadeAnterior, "Não há venda posterior à base ativa que comprove o encerramento da posição.");

        if (TemMovimentosAmbiguos(movimentos, previaAnterior, baseAtiva.DataReferencia))
            return Rejeitar(quantidadeAnterior, null, "Há movimentações ambíguas na janela. A base ativa será preservada para revisão.");

        if (!ManuaisDaJanelaEstaoVinculados(movimentos, revisoes, transacoes, baseAtiva.DataReferencia,
                dataReferencia))
            return Rejeitar(quantidadeAnterior, null, "Há movimentações manuais sem vínculo na janela. Revise-as antes de encerrar a posição.");

        var quantidadeReconciliada = baseAtiva.Quantidade;
        foreach (var dia in movimentos.GroupBy(movimento => movimento.Data).OrderBy(grupo => grupo.Key))
        {
            var vendas = dia.Where(movimento => movimento.Tipo == TipoTransacao.Sell)
                .Sum(movimento => movimento.Quantidade);
            var compras = dia.Where(movimento => movimento.Tipo == TipoTransacao.Buy)
                .Sum(movimento => movimento.Quantidade);

            // O HTML traz somente a data civil, sem a ordem intradiária. Se as vendas
            // precisarem depender de compras do mesmo dia, a quantidade não está provada.
            if (vendas > quantidadeReconciliada)
            {
                if (compras == 0)
                    return Rejeitar(quantidadeAnterior, quantidadeReconciliada - vendas,
                        "As vendas excedem a quantidade da base ativa. A base ativa será preservada.");
                return Rejeitar(quantidadeAnterior, null,
                    "A ordem das compras e vendas no mesmo dia é ambígua. A base ativa será preservada para revisão.");
            }

            quantidadeReconciliada = quantidadeReconciliada - vendas + compras;
            if (quantidadeReconciliada < 0 || quantidadeReconciliada > LimiteQuantidade ||
                decimal.Round(quantidadeReconciliada, 8) != quantidadeReconciliada)
                return Rejeitar(quantidadeAnterior, quantidadeReconciliada, "A quantidade reconciliada é inválida. A base ativa será preservada.");
        }

        if (quantidadeReconciliada != 0)
            return Rejeitar(quantidadeAnterior, quantidadeReconciliada, "As vendas não zeram a quantidade reconciliada. A base ativa será preservada para revisão.");

        return new EncerramentoPosicaoTesouroDto(true, quantidadeAnterior, 0m,
            "As vendas registradas desde a base ativa zeram a posição. Ao confirmar, ela será encerrada e o histórico ficará preservado.");
    }

    private static bool TemMovimentosAmbiguos(
        IReadOnlyList<(int Indice, DateOnly Data, DadosMovimentoTesouroDto Dados, TipoTransacao Tipo, decimal Quantidade)> atuais,
        PreviaTesouroDto anterior, DateOnly dataBase)
    {
        var chavesAtuais = atuais.Select(movimento => ChaveMovimento(movimento.Dados)).ToArray();
        if (chavesAtuais.Any(chave => chave is null) ||
            chavesAtuais.Where(chave => chave is not null).GroupBy(chave => chave!, StringComparer.Ordinal).Any(grupo => grupo.Count() > 1))
            return true;

        var chavesAnteriores = anterior.Movimentacoes
            .Select(movimento => movimento.DadosInterpretados)
            .Where(dados => dados?.Data is DateOnly dia && dia > dataBase)
            .Select(dados => ChaveMovimento(dados!))
            .Where(chave => chave is not null)
            .ToHashSet(StringComparer.Ordinal);

        return chavesAtuais.Any(chave => chave is not null && chavesAnteriores.Contains(chave));
    }

    private static bool ManuaisDaJanelaEstaoVinculados(
        IReadOnlyList<(int Indice, DateOnly Data, DadosMovimentoTesouroDto Dados, TipoTransacao Tipo, decimal Quantidade)> movimentos,
        IReadOnlyDictionary<int, DecisaoConciliacaoTesouro> revisoes,
        IReadOnlyCollection<Transacao> transacoes, DateOnly dataBase, DateOnly dataReferencia)
    {
        var transacoesNoPeriodo = transacoes.Where(transacao =>
        {
            var data = DataCivil(transacao.TransactionDate);
            return (transacao.Type is TipoTransacao.Buy or TipoTransacao.Sell) && data is DateOnly dia &&
                dia > dataBase && dia <= dataReferencia;
        }).ToDictionary(transacao => transacao.Id);
        var vinculadas = new HashSet<Guid>();

        foreach (var movimento in movimentos)
        {
            if (!revisoes.TryGetValue(movimento.Indice, out var revisao) || revisao.Decisao != "vincular")
                continue;
            if (revisao.TransacaoId is not Guid transacaoId || !vinculadas.Add(transacaoId) ||
                !transacoesNoPeriodo.TryGetValue(transacaoId, out var transacao) ||
                !Corresponde(movimento, transacao))
                return false;
        }

        return transacoesNoPeriodo.Keys.All(vinculadas.Contains);
    }

    private static bool Corresponde((int Indice, DateOnly Data, DadosMovimentoTesouroDto Dados, TipoTransacao Tipo, decimal Quantidade) movimento,
        Transacao transacao) =>
        movimento.Tipo == transacao.Type && DataCivil(transacao.TransactionDate) == movimento.Data &&
        movimento.Quantidade == transacao.Quantity && movimento.Dados.Preco == transacao.UnitPrice;

    private static string? ChaveMovimento(DadosMovimentoTesouroDto dados)
    {
        if (dados.Data is not DateOnly data || string.IsNullOrWhiteSpace(dados.Natureza) ||
            string.IsNullOrWhiteSpace(dados.Tipo))
            return null;

        return string.Join('|', data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), dados.Natureza,
            dados.Tipo, Numero(dados.Quantidade), Numero(dados.Preco), Numero(dados.TaxaAnual), Numero(dados.Valor));
    }

    private static string Numero(decimal? valor) =>
        valor?.ToString("0.########", CultureInfo.InvariantCulture) ?? "?";

    private static DateOnly? DataCivil(DateTime data) => data.Kind switch
    {
        DateTimeKind.Utc => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(data, FusoBrasil)),
        DateTimeKind.Local => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(data, FusoBrasil)),
        _ => null
    };

    private static EncerramentoPosicaoTesouroDto Rejeitar(decimal? anterior, decimal? reconciliada, string motivo) =>
        new(false, anterior, reconciliada, motivo);
}
