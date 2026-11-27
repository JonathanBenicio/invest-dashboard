using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.MarketData;

public sealed record PontoIndicadorBcbDto(
    [property: JsonPropertyName("data")] DateOnly Data,
    [property: JsonPropertyName("valor")] decimal Valor);

public sealed record SerieIndicadorBcbDto(
    [property: JsonPropertyName("indicador")] string Indicador,
    [property: JsonPropertyName("codigoSgs")] int CodigoSgs,
    [property: JsonPropertyName("unidade")] string Unidade,
    [property: JsonPropertyName("periodicidade")] string Periodicidade,
    [property: JsonPropertyName("dataDe")] DateOnly DataDe,
    [property: JsonPropertyName("dataAte")] DateOnly DataAte,
    [property: JsonPropertyName("origem")] string Origem,
    [property: JsonPropertyName("atualizadoEmUtc")] DateTime AtualizadoEmUtc,
    [property: JsonPropertyName("pontos")] IReadOnlyList<PontoIndicadorBcbDto> Pontos);
