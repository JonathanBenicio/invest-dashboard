using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Importacao;

public sealed record BaseAtivaExtratoTesouroDto(
    [property: JsonPropertyName("loteId")] Guid LoteId,
    [property: JsonPropertyName("dataReferencia")] DateOnly DataReferencia,
    [property: JsonPropertyName("confirmadoEmUtc")] DateTime ConfirmadoEmUtc,
    [property: JsonPropertyName("quantidade")] decimal Quantidade,
    [property: JsonPropertyName("custoTotal")] decimal CustoTotal,
    [property: JsonPropertyName("precoUnitario")] decimal PrecoUnitario);

public sealed record EventoOficialExtratoTesouroDto(
    [property: JsonPropertyName("loteId")] Guid LoteId,
    [property: JsonPropertyName("indiceEvento")] int IndiceEvento,
    [property: JsonPropertyName("transacaoId")] Guid? TransacaoId,
    [property: JsonPropertyName("origem")] string Origem,
    [property: JsonPropertyName("dados")] MovimentoPreviaTesouroDto? Dados);

public sealed record ResumoUltimaImportacaoTesouroDto(
    [property: JsonPropertyName("loteAnteriorId")] Guid? LoteAnteriorId,
    [property: JsonPropertyName("dataReferenciaAnterior")] DateOnly? DataReferenciaAnterior,
    [property: JsonPropertyName("dataReferenciaAtual")] DateOnly DataReferenciaAtual,
    [property: JsonPropertyName("quantidadeAnterior")] decimal? QuantidadeAnterior,
    [property: JsonPropertyName("quantidadeAtual")] decimal QuantidadeAtual,
    [property: JsonPropertyName("variacaoQuantidade")] decimal? VariacaoQuantidade,
    [property: JsonPropertyName("divergencias")] IReadOnlyList<DivergenciaSnapshotTesouroDto> Divergencias);

public sealed record ExtratoTesouroDto(
    [property: JsonPropertyName("baseAtiva")] BaseAtivaExtratoTesouroDto? BaseAtiva,
    [property: JsonPropertyName("eventos")] IReadOnlyList<EventoOficialExtratoTesouroDto> Eventos,
    [property: JsonPropertyName("proximoIndice")] int? ProximoIndice,
    [property: JsonPropertyName("resumoUltimaImportacao")] ResumoUltimaImportacaoTesouroDto? ResumoUltimaImportacao);
