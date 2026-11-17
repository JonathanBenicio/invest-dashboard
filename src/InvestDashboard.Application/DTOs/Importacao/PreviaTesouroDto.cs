using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.Importacao;

public sealed record CampoPreviaTesouroDto(
    [property: JsonPropertyName("rotulo")] string Rotulo,
    [property: JsonPropertyName("valor")] string Valor);

public sealed record MovimentoPreviaTesouroDto(
    [property: JsonPropertyName("campos")] IReadOnlyList<CampoPreviaTesouroDto> Campos,
    [property: JsonPropertyName("dadosInterpretados")] DadosMovimentoTesouroDto? DadosInterpretados = null);

/// <summary>Interpretação preliminar, sem identidade, conciliação ou confirmação.</summary>
public sealed record DadosMovimentoTesouroDto(
    [property: JsonPropertyName("data")] DateOnly? Data,
    [property: JsonPropertyName("natureza")] string? Natureza,
    [property: JsonPropertyName("tipo")] string? Tipo,
    [property: JsonPropertyName("quantidade")] decimal? Quantidade,
    [property: JsonPropertyName("preco")] decimal? Preco,
    [property: JsonPropertyName("taxaAnual")] decimal? TaxaAnual,
    [property: JsonPropertyName("valor")] decimal? Valor,
    [property: JsonPropertyName("camposNaoInterpretados")] IReadOnlyList<string> CamposNaoInterpretados);

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
