using System.Text.Json;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Application.Services;

public static class ResumoUltimaImportacaoTesouro
{
    public static ResumoUltimaImportacaoTesouroDto? Preparar(BasePosicaoTesouro baseAtual,
        LoteImportacaoTesouro loteAtual, BasePosicaoTesouro? baseAnterior, LoteImportacaoTesouro? loteAnterior,
        ComparacaoSnapshotTesouroDto? comparacaoPersistida = null)
    {
        if (baseAnterior is null)
            return null;

        if (comparacaoPersistida?.Divergencias is { } divergenciasPersistidas)
            return CriarResumo(baseAtual, baseAnterior, loteAnterior, divergenciasPersistidas);

        if (baseAtual.Quantidade == 0)
        {
            return CriarResumo(baseAtual, baseAnterior, loteAnterior,
            [
                new DivergenciaSnapshotTesouroDto("snapshot_revisao_indisponivel", baseAtual.LoteId.ToString("N"),
                    "A posição encerrada não tem a revisão de snapshot confirmada disponível.",
                    baseAnterior.DataReferencia.ToString("yyyy-MM-dd"), baseAtual.DataReferencia.ToString("yyyy-MM-dd"))
            ]);
        }

        var previaAnterior = loteAnterior is null || loteAnterior.Estado != "Confirmado"
            ? null
            : TentarLerPrevia(loteAnterior.PreviaJson);
        var previaAtual = TentarLerPrevia(loteAtual.PreviaJson);
        if (previaAtual is null)
        {
            return CriarResumo(baseAtual, baseAnterior, loteAnterior,
            [
                new DivergenciaSnapshotTesouroDto("snapshot_atual_indisponivel", loteAtual.Id.ToString("N"),
                    "Não foi possível carregar os dados do extrato atual para comparação.",
                    baseAnterior.DataReferencia.ToString("yyyy-MM-dd"), loteAtual.DataReferencia.ToString("yyyy-MM-dd"))
            ]);
        }

        var comparacao = ComparadorSnapshotTesouro.Preparar(loteAtual.Id, loteAtual.HashConteudo, 0,
            loteAtual.DataReferencia, previaAtual, baseAnterior, previaAnterior, [], baseAtual.Quantidade);
        var divergencias = comparacao?.Divergencias ?? [];
        return CriarResumo(baseAtual, baseAnterior, loteAnterior, divergencias);
    }

    private static ResumoUltimaImportacaoTesouroDto CriarResumo(BasePosicaoTesouro baseAtual,
        BasePosicaoTesouro baseAnterior, LoteImportacaoTesouro? loteAnterior,
        IReadOnlyList<DivergenciaSnapshotTesouroDto> divergencias) =>
        new(loteAnterior?.Id, baseAnterior.DataReferencia,
            baseAtual.DataReferencia, baseAnterior.Quantidade, baseAtual.Quantidade,
            baseAtual.Quantidade - baseAnterior.Quantidade, divergencias);

    private static PreviaTesouroDto? TentarLerPrevia(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<PreviaTesouroDto>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
