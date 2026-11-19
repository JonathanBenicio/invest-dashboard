using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;

namespace InvestDashboard.Application.Services;

/// <summary>Compara snapshots sem presumir que um evento ausente foi cancelado ou encerrado.</summary>
public static class ComparadorSnapshotTesouro
{
    private static readonly TimeZoneInfo FusoBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static ComparacaoSnapshotTesouroDto? Preparar(Guid loteAtualId, string hashConteudoAtual, int carteiraVersao,
        DateOnly dataReferenciaAtual, PreviaTesouroDto previaAtual, BasePosicaoTesouro? baseAnterior,
        PreviaTesouroDto? previaAnterior, IReadOnlyCollection<Transacao> transacoes,
        decimal? quantidadeAtual)
    {
        if (baseAnterior is null && previaAnterior is null && transacoes.Count == 0)
            return null;

        var divergencias = new List<DivergenciaSnapshotTesouroDto>();
        var dataBaseAnterior = baseAnterior?.DataReferencia;
        var quantidadeAnterior = baseAnterior?.Quantidade ?? 0m;
        var transacoesNaJanela = transacoes
            .Where(item => item.Type is TipoTransacao.Buy or TipoTransacao.Sell)
            .Where(item => DataCivil(item.TransactionDate) is DateOnly data && data <= dataReferenciaAtual &&
                (!dataBaseAnterior.HasValue || data > dataBaseAnterior.Value))
            .OrderBy(item => item.TransactionDate).ThenBy(item => item.Id).ToArray();

        if (baseAnterior is not null && previaAnterior is null)
            Adicionar(divergencias, "snapshot_anterior_indisponivel", baseAnterior.LoteId.ToString("N"),
                "Não foi possível carregar o snapshot que originou a base ativa.", baseAnterior.DataReferencia.ToString("yyyy-MM-dd"), null);

        if (previaAnterior is not null)
        {
            CompararLinhasPosicao(previaAnterior.Posicoes, previaAtual.Posicoes, divergencias);
            CompararMovimentacoes(previaAnterior.Movimentacoes, previaAtual.Movimentacoes, divergencias);
        }

        if (previaAtual.Posicoes.Count == 0 && (baseAnterior is not null || previaAnterior is not null))
            Adicionar(divergencias, "posicao_nao_observada", "posicao",
                "Este arquivo não apresenta uma posição interpretável do título. A ausência não comprova encerramento nem saldo zero; a base anterior não será substituída.",
                baseAnterior is null ? null : $"{baseAnterior.DataReferencia:yyyy-MM-dd} · quantidade {Numero(baseAnterior.Quantidade)}", null);

        foreach (var manual in transacoesNaJanela)
            Adicionar(divergencias, "movimento_manual_na_janela", manual.Id.ToString("N"),
                "Movimento manual na janela da nova base. Revise seu vínculo; o registro será preservado.",
                $"{manual.Type} · {DataCivil(manual.TransactionDate):yyyy-MM-dd} · quantidade {Numero(manual.Quantity)} · preço {Numero(manual.UnitPrice)}", null);

        var quantidadeEsperada = transacoesNaJanela.Length == 0 ? CalcularQuantidadeEsperada(quantidadeAnterior, dataBaseAnterior,
            dataReferenciaAtual, previaAtual.Movimentacoes, divergencias) : null;
        if (quantidadeEsperada.HasValue && quantidadeAtual.HasValue &&
            Math.Abs(quantidadeEsperada.Value - quantidadeAtual.Value) >= 0.00000001m)
            Adicionar(divergencias, "saldo_nao_explicado", "quantidade",
                "A quantidade proposta não coincide com a base anterior mais as movimentações reconhecidas.",
                quantidadeEsperada.Value.ToString("0.########", CultureInfo.InvariantCulture),
                quantidadeAtual.Value.ToString("0.########", CultureInfo.InvariantCulture));

        if (!previaAtual.CoberturaCompleta)
            Adicionar(divergencias, "cobertura_nao_comprovada", "html",
                "O leitor não consegue comprovar que todos os eventos do período aparecem neste HTML.",
                null, null);

        var listaOrdenada = divergencias.OrderBy(item => item.Tipo, StringComparer.Ordinal)
            .ThenBy(item => item.Chave, StringComparer.Ordinal).ToArray();
        var materialHash = JsonSerializer.Serialize(new
        {
            loteAtualId,
            hashConteudoAtual,
            carteiraVersao,
            dataReferenciaAtual,
            loteAnteriorId = baseAnterior?.LoteId,
            previaAnterior,
            dataBaseAnterior,
            quantidadeAnterior,
            quantidadeAtual,
            movimentosManuais = transacoesNaJanela.Select(item => new
            {
                item.Id,
                tipo = item.Type.ToString(),
                data = DataCivil(item.TransactionDate),
                item.Quantity,
                item.UnitPrice
            }).ToArray(),
            listaOrdenada
        });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(materialHash))).ToLowerInvariant();

        return new ComparacaoSnapshotTesouroDto(hash, dataBaseAnterior, quantidadeAnterior,
            quantidadeAtual, listaOrdenada, listaOrdenada.Length > 0);
    }

    private static decimal? CalcularQuantidadeEsperada(decimal quantidadeAnterior, DateOnly? dataBaseAnterior,
        DateOnly dataReferencia, IReadOnlyList<MovimentoPreviaTesouroDto> movimentos,
        List<DivergenciaSnapshotTesouroDto> divergencias)
    {
        if (!dataBaseAnterior.HasValue) return null;
        var esperada = quantidadeAnterior;

        foreach (var movimento in movimentos)
        {
            var dados = movimento.DadosInterpretados;
            var tipo = ConfirmacaoTesouro.TipoNegociacao(dados);
            if (tipo is null || dados?.Data is not DateOnly data || data > dataReferencia ||
                (dataBaseAnterior.HasValue && data <= dataBaseAnterior.Value)) continue;
            if (dados.Quantidade is not decimal quantidade || quantidade <= 0)
            {
                Adicionar(divergencias, "movimento_sem_quantidade", ChaveMovimento(movimento) ?? HashTexto(JsonSerializer.Serialize(movimento)),
                    "Uma compra/venda do período não possui quantidade suficiente para conferir o saldo.",
                    null, null);
                continue;
            }

            esperada += tipo == TipoTransacao.Buy ? quantidade : -quantidade;
        }
        return decimal.Round(esperada, 8, MidpointRounding.AwayFromZero);
    }

    private static void CompararLinhasPosicao(IReadOnlyList<PosicaoPreviaTesouroDto> anterior,
        IReadOnlyList<PosicaoPreviaTesouroDto> atual, List<DivergenciaSnapshotTesouroDto> divergencias)
    {
        var contagens = Contar(atual.Select(ChavePosicao));
        foreach (var linha in anterior)
        {
            var chave = ChavePosicao(linha);
            if (chave is null)
            {
                Adicionar(divergencias, "lote_anterior_sem_identidade", linha.NumeroLinha.ToString(CultureInfo.InvariantCulture),
                    "Uma linha de posição anterior não tem campos estáveis suficientes para comparação.",
                    ResumoPosicao(linha), null);
                continue;
            }
            if (Consumir(contagens, chave)) continue;
            Adicionar(divergencias, "lote_nao_observado", HashTexto(chave),
                "Uma linha de posição anterior não foi encontrada neste snapshot; isso não prova encerramento.",
                ResumoPosicao(linha), null);
        }
    }

    private static void CompararMovimentacoes(IReadOnlyList<MovimentoPreviaTesouroDto> anterior,
        IReadOnlyList<MovimentoPreviaTesouroDto> atual, List<DivergenciaSnapshotTesouroDto> divergencias)
    {
        var contagens = Contar(atual.Select(ChaveMovimento));
        foreach (var grupo in anterior.Select(ChaveMovimento).Where(x => x is not null).GroupBy(x => x!, StringComparer.Ordinal))
            if (grupo.Count() > 1 || contagens.GetValueOrDefault(grupo.Key) > 1)
                Adicionar(divergencias, "evento_ambiguo", HashTexto(grupo.Key),
                    "Há eventos com campos iguais; não é possível confirmar sua identidade entre arquivos.",
                    $"Ocorrências anteriores: {grupo.Count()}", $"Ocorrências atuais: {contagens.GetValueOrDefault(grupo.Key)}");
        foreach (var movimento in anterior)
        {
            var chave = ChaveMovimento(movimento);
            if (chave is null)
            {
                Adicionar(divergencias, "evento_anterior_sem_identidade", HashTexto(JsonSerializer.Serialize(movimento.Campos)),
                    "Um evento anterior não tem dados estáveis suficientes para comparação.",
                    ResumoMovimento(movimento), null);
                continue;
            }
            if (Consumir(contagens, chave)) continue;
            Adicionar(divergencias, "evento_nao_observado", HashTexto(chave),
                "Um evento anterior não foi encontrado neste snapshot; verifique a cobertura do arquivo.",
                ResumoMovimento(movimento), null);
        }
    }

    private static Dictionary<string, int> Contar(IEnumerable<string?> chaves)
    {
        var contagens = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var chave in chaves.Where(value => value is not null).Cast<string>())
            contagens[chave] = contagens.GetValueOrDefault(chave) + 1;
        return contagens;
    }

    private static bool Consumir(Dictionary<string, int> contagens, string chave)
    {
        if (!contagens.TryGetValue(chave, out var quantidade) || quantidade == 0) return false;
        if (quantidade == 1) contagens.Remove(chave);
        else contagens[chave] = quantidade - 1;
        return true;
    }

    private static string? ChavePosicao(PosicaoPreviaTesouroDto linha)
    {
        var dados = linha.DadosInterpretados;
        if (dados?.DataAplicacao is not DateOnly data || dados.PrecoAplicacao is not decimal preco ||
            dados.ValorInvestido is not decimal investido || string.IsNullOrWhiteSpace(dados.RentabilidadeContratada)) return null;
        return string.Join('|', data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Numero(preco),
            Numero(investido), dados.RentabilidadeContratada.Trim().ToUpperInvariant());
    }

    private static string? ChaveMovimento(MovimentoPreviaTesouroDto movimento)
    {
        var dados = movimento.DadosInterpretados;
        if (dados?.Data is not DateOnly data || string.IsNullOrWhiteSpace(dados.Natureza) || string.IsNullOrWhiteSpace(dados.Tipo))
            return null;
        return string.Join('|', data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), dados.Natureza, dados.Tipo,
            Numero(dados.Quantidade), Numero(dados.Preco), Numero(dados.TaxaAnual), Numero(dados.Valor));
    }

    private static string ResumoPosicao(PosicaoPreviaTesouroDto linha)
    {
        var dados = linha.DadosInterpretados;
        return $"linha {linha.NumeroLinha}, aplicação {dados?.DataAplicacao?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "?"}, quantidade {Numero(dados?.QuantidadeTitulos)}";
    }

    private static string ResumoMovimento(MovimentoPreviaTesouroDto movimento)
    {
        var dados = movimento.DadosInterpretados;
        return $"{dados?.Natureza ?? "movimento"} {dados?.Tipo ?? "?"}, data {dados?.Data?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "?"}, quantidade {Numero(dados?.Quantidade)}, preço {Numero(dados?.Preco)}, taxa {Numero(dados?.TaxaAnual)}, valor {Numero(dados?.Valor)}";
    }

    private static string Numero(decimal? valor) => valor?.ToString("0.########", CultureInfo.InvariantCulture) ?? "?";

    private static string HashTexto(string valor) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor))).ToLowerInvariant();

    private static void Adicionar(List<DivergenciaSnapshotTesouroDto> divergencias, string tipo, string chave,
        string descricao, string? anterior, string? atual) =>
        divergencias.Add(new DivergenciaSnapshotTesouroDto(tipo, chave, descricao, anterior, atual));

    private static DateOnly? DataCivil(DateTime data) => data.Kind switch
    {
        DateTimeKind.Utc => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(data, FusoBrasil)),
        DateTimeKind.Local => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(data, FusoBrasil)),
        _ => null
    };
}
