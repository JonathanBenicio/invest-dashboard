using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.MarketData;

public sealed record TituloTesouroDisponivelDto(
    [property: JsonPropertyName("tipoTitulo")] string TipoTitulo,
    [property: JsonPropertyName("dataVencimento")] DateOnly DataVencimento,
    [property: JsonPropertyName("ultimaDataDisponivel")] DateOnly UltimaDataDisponivel);

public sealed record CatalogoTesouroDto(
    [property: JsonPropertyName("origem")] string Origem,
    [property: JsonPropertyName("urlFonte")] string UrlFonte,
    [property: JsonPropertyName("coletadoEmUtc")] DateTimeOffset ColetadoEmUtc,
    [property: JsonPropertyName("ultimaDataDisponivel")] DateOnly UltimaDataDisponivel,
    [property: JsonPropertyName("titulos")] IReadOnlyList<TituloTesouroDisponivelDto> Titulos);

public sealed record PontoCotacaoTesouroDto(
    [property: JsonPropertyName("dataBase")] DateOnly DataBase,
    [property: JsonPropertyName("taxaCompraManha")] decimal TaxaCompraManha,
    [property: JsonPropertyName("taxaVendaManha")] decimal TaxaVendaManha,
    [property: JsonPropertyName("puCompraManha")] decimal PuCompraManha,
    [property: JsonPropertyName("puVendaManha")] decimal PuVendaManha,
    [property: JsonPropertyName("puBaseManha")] decimal PuBaseManha);

public sealed record SerieCotacoesTesouroDto(
    [property: JsonPropertyName("origem")] string Origem,
    [property: JsonPropertyName("urlFonte")] string UrlFonte,
    [property: JsonPropertyName("coletadoEmUtc")] DateTimeOffset ColetadoEmUtc,
    [property: JsonPropertyName("ultimaDataDisponivel")] DateOnly UltimaDataDisponivel,
    [property: JsonPropertyName("tipoTitulo")] string TipoTitulo,
    [property: JsonPropertyName("dataVencimento")] DateOnly DataVencimento,
    [property: JsonPropertyName("dataDe")] DateOnly DataDe,
    [property: JsonPropertyName("dataAte")] DateOnly DataAte,
    [property: JsonPropertyName("cotacoes")] IReadOnlyList<PontoCotacaoTesouroDto> Cotacoes);
