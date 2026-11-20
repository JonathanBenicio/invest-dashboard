using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Importacao;

public sealed record RegistrarRevisaoTesouroDto(
    [property: JsonPropertyName("indiceEvento")] int IndiceEvento,
    [property: JsonPropertyName("decisao")] string Decisao,
    [property: JsonPropertyName("ativoId")] Guid? AtivoId,
    [property: JsonPropertyName("transacaoId")] Guid? TransacaoId,
    [property: JsonPropertyName("carteiraVersao")] int CarteiraVersao,
    [property: JsonPropertyName("revisaoAnterior")] int RevisaoAnterior,
    [property: JsonPropertyName("hashPrevia")] string? HashPrevia = null);

public sealed record RevisaoTesouroDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("indiceEvento")] int IndiceEvento,
    [property: JsonPropertyName("revisao")] int Revisao,
    [property: JsonPropertyName("decisao")] string Decisao,
    [property: JsonPropertyName("ativoId")] Guid? AtivoId,
    [property: JsonPropertyName("transacaoId")] Guid? TransacaoId,
    [property: JsonPropertyName("carteiraVersao")] int CarteiraVersao,
    [property: JsonPropertyName("criadoEmUtc")] DateTime CriadoEmUtc,
    [property: JsonPropertyName("divergencias")] IReadOnlyList<string> Divergencias);

public sealed record RevisaoParaConfirmacaoTesouroDto(
    [property: JsonPropertyName("indiceEvento")] int IndiceEvento,
    [property: JsonPropertyName("revisao")] int Revisao);

public sealed record ConfirmarLoteTesouroDto(
    [property: JsonPropertyName("ativoId")] Guid? AtivoId,
    [property: JsonPropertyName("criarPosicao")] bool CriarPosicao,
    [property: JsonPropertyName("carteiraVersao")] int CarteiraVersao,
    [property: JsonPropertyName("revisoes")] IReadOnlyList<RevisaoParaConfirmacaoTesouroDto> Revisoes,
    [property: JsonPropertyName("hashDivergencias")] string? HashDivergencias = null,
    [property: JsonPropertyName("aceitarDivergencias")] bool AceitarDivergencias = false);
