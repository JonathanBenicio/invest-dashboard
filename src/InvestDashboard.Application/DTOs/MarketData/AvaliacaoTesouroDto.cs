using System.Text.Json.Serialization;

namespace InvestDashboard.Application.DTOs.MarketData;

public sealed record AvaliacaoPosicaoTesouroDto(
    [property: JsonPropertyName("posicaoId")] Guid PosicaoId,
    [property: JsonPropertyName("ativoId")] Guid AtivoId,
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("titulo")] string Titulo,
    [property: JsonPropertyName("dataVencimento")] DateOnly DataVencimento,
    [property: JsonPropertyName("quantidade")] decimal Quantidade,
    [property: JsonPropertyName("origemCotacao")] string? OrigemCotacao,
    [property: JsonPropertyName("urlFonteCotacao")] string? UrlFonteCotacao,
    [property: JsonPropertyName("dataCotacao")] DateOnly? DataCotacao,
    [property: JsonPropertyName("coletadoEmUtc")] DateTimeOffset? ColetadoEmUtc,
    [property: JsonPropertyName("puBase")] decimal? PuBase,
    [property: JsonPropertyName("valorBrutoMercado")] decimal? ValorBrutoMercado,
    [property: JsonPropertyName("dataExtrato")] DateOnly? DataExtrato,
    [property: JsonPropertyName("valorBrutoExtrato")] decimal? ValorBrutoExtrato,
    [property: JsonPropertyName("impostoRendaExtrato")] decimal? ImpostoRendaExtrato,
    [property: JsonPropertyName("iofExtrato")] decimal? IofExtrato,
    [property: JsonPropertyName("valorLiquidoExtrato")] decimal? ValorLiquidoExtrato,
    [property: JsonPropertyName("dataTaxasExtrato")] DateOnly? DataTaxasExtrato,
    [property: JsonPropertyName("taxaCustodiaExtrato")] decimal? TaxaCustodiaExtrato,
    [property: JsonPropertyName("taxaInstituicaoExtrato")] decimal? TaxaInstituicaoExtrato,
    [property: JsonPropertyName("impostoRendaEstimado")] decimal? ImpostoRendaEstimado,
    [property: JsonPropertyName("iofEstimado")] decimal? IofEstimado,
    [property: JsonPropertyName("dataCalculoImpostos")] DateOnly? DataCalculoImpostos,
    [property: JsonPropertyName("valorLiquidoEstimado")] decimal? ValorLiquidoEstimado,
    [property: JsonPropertyName("motivoLiquidoIndisponivel")] string? MotivoLiquidoIndisponivel,
    [property: JsonPropertyName("avisos")] IReadOnlyList<string> Avisos);
