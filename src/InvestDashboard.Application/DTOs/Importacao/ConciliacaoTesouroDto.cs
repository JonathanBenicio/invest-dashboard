using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Importacao;

public sealed record SolicitarConciliacaoTesouroDto([property: JsonPropertyName("ativoId")] Guid? AtivoId);
public sealed record AtivoConciliacaoTesouroDto([property: JsonPropertyName("ativoId")] Guid AtivoId, [property: JsonPropertyName("ticker")] string Ticker);
public sealed record TransacaoConciliacaoTesouroDto(
    [property: JsonPropertyName("id")] Guid Id, [property: JsonPropertyName("tipo")] string Tipo,
    [property: JsonPropertyName("data")] DateTime Data, [property: JsonPropertyName("quantidade")] decimal Quantidade,
    [property: JsonPropertyName("preco")] decimal Preco, [property: JsonPropertyName("posteriorBase")] bool PosteriorBase);
public sealed record EventoConciliacaoTesouroDto(
    [property: JsonPropertyName("indice")] int Indice,
    [property: JsonPropertyName("dadosInterpretados")] DadosMovimentoTesouroDto? DadosInterpretados,
    [property: JsonPropertyName("candidatos")] IReadOnlyList<Guid> Candidatos);
public sealed record ConciliacaoTesouroDto(
    [property: JsonPropertyName("carteiraVersao")] int CarteiraVersao,
    [property: JsonPropertyName("dataReferencia")] DateOnly DataReferencia,
    [property: JsonPropertyName("ativoId")] Guid? AtivoId,
    [property: JsonPropertyName("ativos")] IReadOnlyList<AtivoConciliacaoTesouroDto> Ativos,
    [property: JsonPropertyName("eventos")] IReadOnlyList<EventoConciliacaoTesouroDto> Eventos,
    [property: JsonPropertyName("transacoes")] IReadOnlyList<TransacaoConciliacaoTesouroDto> Transacoes,
    [property: JsonPropertyName("podeConfirmar")] bool PodeConfirmar,
    [property: JsonPropertyName("dataBaseAtiva")] DateOnly? DataBaseAtiva = null);
