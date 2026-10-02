using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.MarketData;

public sealed record PontoBenchmarkCdiDto(
    [property: JsonPropertyName("data")] DateOnly Data,
    [property: JsonPropertyName("taxaDiariaPercentual")] decimal TaxaDiariaPercentual,
    [property: JsonPropertyName("indiceBase100")] decimal IndiceBase100);

public sealed record SerieBenchmarkCdiDto(
    [property: JsonPropertyName("dataDe")] DateOnly DataDe,
    [property: JsonPropertyName("dataAte")] DateOnly DataAte,
    [property: JsonPropertyName("origem")] string Origem,
    [property: JsonPropertyName("atualizadoEmUtc")] DateTime AtualizadoEmUtc,
    [property: JsonPropertyName("pontos")] IReadOnlyList<PontoBenchmarkCdiDto> Pontos);
