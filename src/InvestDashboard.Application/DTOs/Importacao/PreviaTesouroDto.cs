using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Importacao;

public sealed record CampoPreviaTesouroDto(
    [property: JsonPropertyName("rotulo")] string Rotulo,
    [property: JsonPropertyName("valor")] string Valor);

public sealed record MovimentoPreviaTesouroDto(
    [property: JsonPropertyName("campos")] IReadOnlyList<CampoPreviaTesouroDto> Campos);

public sealed record PosicaoPreviaTesouroDto(
    [property: JsonPropertyName("numeroLinha")] int NumeroLinha,
    [property: JsonPropertyName("camposBrutos")] IReadOnlyDictionary<string, string> CamposBrutos);

/// <summary>Leitura preliminar sem conciliação ou efeito sobre carteira.</summary>
public sealed record PreviaTesouroDto(
    [property: JsonPropertyName("carteiraId")] Guid CarteiraId,
    [property: JsonPropertyName("formato")] string Formato,
    [property: JsonPropertyName("layout")] string Layout,
    [property: JsonPropertyName("titulo")] string? Titulo,
    [property: JsonPropertyName("vencimento")] string? Vencimento,
    [property: JsonPropertyName("vencimentoOriginal")] string? VencimentoOriginal,
    [property: JsonPropertyName("posicoes")] IReadOnlyList<PosicaoPreviaTesouroDto> Posicoes,
    [property: JsonPropertyName("movimentacoes")] IReadOnlyList<MovimentoPreviaTesouroDto> Movimentacoes,
    [property: JsonPropertyName("linhasNaoInterpretadas")] IReadOnlyList<int> LinhasNaoInterpretadas,
    [property: JsonPropertyName("avisos")] IReadOnlyList<string> Avisos,
    [property: JsonPropertyName("exigeRevisao")] bool ExigeRevisao,
    [property: JsonPropertyName("coberturaCompleta")] bool CoberturaCompleta,
    [property: JsonPropertyName("podeConfirmar")] bool PodeConfirmar);
