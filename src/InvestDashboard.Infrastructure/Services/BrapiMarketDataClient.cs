using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvestDashboard.Application.DTOs.MarketData;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvestDashboard.Infrastructure.Services;

public sealed class BrapiMarketDataClient(
    HttpClient client,
    IConfiguration configuration,
    ILogger<BrapiMarketDataClient> logger) : IMarketDataProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CotacaoMercadoDto>> GetQuotesAsync(
        IReadOnlyCollection<string> symbols,
        CancellationToken cancellationToken = default)
    {
        var normalized = symbols.Select(symbol => symbol.Trim().ToUpperInvariant())
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToArray();
        if (normalized.Length == 0) return [];

        var cryptoSymbols = normalized.Where(symbol => !symbol.Any(char.IsDigit)).ToArray();
        var stockSymbols = normalized.Except(cryptoSymbols, StringComparer.OrdinalIgnoreCase).ToArray();
        var quotes = new List<CotacaoMercadoDto>();

        if (stockSymbols.Length > 0)
        {
            var uri = BuildUri("v2/stocks/quote", ("symbols", string.Join(',', stockSymbols)));
            using var request = CreateRequest(uri);
            var response = await SendAsync<BrapiQuoteResponse>(request, cancellationToken);
            quotes.AddRange(response.Results
                .Select(result => (Result: result, ObservedAt: ParseDate(result.Data?.RegularMarketTime)))
                .Where(item => item.Result.Data?.RegularMarketPrice is not null && item.ObservedAt.HasValue)
                .Select(item => new CotacaoMercadoDto(
                    item.Result.Symbol,
                    item.Result.Data!.LongName ?? item.Result.Data.ShortName ?? item.Result.Symbol,
                    item.Result.Data.RegularMarketPrice!.Value,
                    item.ObservedAt.GetValueOrDefault(),
                    item.Result.Data.Currency ?? "BRL",
                    item.Result.Data.Sector,
                    null)));
        }

        if (cryptoSymbols.Length > 0)
        {
            var uri = BuildUri("v2/crypto", ("coin", string.Join(',', cryptoSymbols)), ("currency", "BRL"));
            using var request = CreateRequest(uri);
            var response = await SendAsync<BrapiCryptoResponse>(request, cancellationToken);
            quotes.AddRange(response.Coins
                .Select(coin => (Coin: coin, ObservedAt: ParseDate(coin.RegularMarketTime)))
                .Where(item => item.Coin.RegularMarketPrice is not null && item.ObservedAt.HasValue)
                .Select(item => new CotacaoMercadoDto(
                    item.Coin.Coin,
                    item.Coin.CoinName ?? item.Coin.Coin,
                    item.Coin.RegularMarketPrice!.Value,
                    item.ObservedAt.GetValueOrDefault(),
                    item.Coin.Currency ?? "BRL",
                    null,
                    "CRYPTO")));
        }

        return quotes;
    }

    public async Task<IReadOnlyList<PontoHistoricoMercadoDto>> GetDailyHistoryAsync(
        IReadOnlyCollection<string> symbols,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (startDate > endDate || endDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("History date range is invalid.");

        var normalized = symbols.Select(symbol => symbol.Trim().ToUpperInvariant())
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToArray();
        if (normalized.Length == 0) return [];

        var uri = BuildUri(
            "v2/stocks/historical",
            ("symbols", string.Join(',', normalized)),
            ("startDate", startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("endDate", endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("interval", "1d"),
            ("sortOrder", "asc"));
        using var request = CreateRequest(uri);
        var response = await SendAsync<BrapiHistoryResponse>(request, cancellationToken);

        return response.Results.SelectMany(result => (result.Data?.History ?? [])
            .Where(point => point.AdjustedClose is not null || point.Close is not null)
            .Select(point => new PontoHistoricoMercadoDto(
                result.Symbol,
                DateTimeOffset.FromUnixTimeSeconds(point.Date).UtcDateTime,
                point.AdjustedClose ?? point.Close!.Value,
                "brapi",
                point.AdjustedClose.HasValue)))
            .ToList();
    }

    public async Task<IReadOnlyList<ResultadoBuscaMercadoDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri("quote/list", ("search", query.Trim()), ("limit", "20"), ("page", "1"));
        using var request = CreateRequest(uri);
        var response = await SendAsync<BrapiSearchResponse>(request, cancellationToken);

        return response.Stocks.Select(stock => new ResultadoBuscaMercadoDto(
            stock.Stock,
            stock.Name,
            "BRL",
            stock.Sector,
            MapSubtype(stock.SubType, stock.Type))).ToList();
    }

    private HttpRequestMessage CreateRequest(Uri uri)
    {
        var token = configuration["MarketData:BrapiToken"];
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Brapi returned status {StatusCode}.", response.StatusCode);
                throw new MarketDataUnavailableException();
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                ?? throw new MarketDataUnavailableException();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Brapi request timed out.");
            throw new MarketDataUnavailableException();
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Brapi request failed.");
            throw new MarketDataUnavailableException();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Brapi returned an invalid response.");
            throw new MarketDataUnavailableException();
        }
    }

    private Uri BuildUri(string path, params (string Name, string Value)[] query)
    {
        var baseUrl = configuration["MarketData:BaseUrl"] ?? "https://brapi.dev/api/";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("MarketData:BaseUrl must be a valid HTTPS URL.");

        var builder = new UriBuilder(new Uri(uri.ToString().TrimEnd('/') + "/" + path.TrimStart('/')));
        builder.Query = string.Join("&", query.Select(item =>
            $"{Uri.EscapeDataString(item.Name)}={Uri.EscapeDataString(item.Value)}"));
        return builder.Uri;
    }

    private static string MapSubtype(string? subtype, string? type) => subtype?.ToLowerInvariant() switch
    {
        "fii" or "fi-infra" or "fi-agro" or "fip" or "fidc" => "FII",
        "etf" => "ETF",
        "bdr" => "BDR",
        "unit" or "stock" => string.Equals(type, "bdr", StringComparison.OrdinalIgnoreCase) ? "BDR" : "ACAO",
        _ => "ACAO"
    };

    private static DateTime? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date.UtcDateTime
            : null;

    private sealed class BrapiQuoteResponse
    {
        [JsonPropertyName("results")]
        public List<BrapiStockResult> Results { get; init; } = [];
    }

    private sealed class BrapiStockResult
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; init; } = string.Empty;
        [JsonPropertyName("data")]
        public BrapiStockQuote? Data { get; init; }
    }

    private sealed class BrapiStockQuote
    {
        [JsonPropertyName("shortName")]
        public string? ShortName { get; init; }
        [JsonPropertyName("longName")]
        public string? LongName { get; init; }
        [JsonPropertyName("currency")]
        public string? Currency { get; init; }
        [JsonPropertyName("regularMarketPrice")]
        public decimal? RegularMarketPrice { get; init; }
        [JsonPropertyName("regularMarketTime")]
        public string? RegularMarketTime { get; init; }
        [JsonPropertyName("sector")]
        public string? Sector { get; init; }
    }

    private sealed class BrapiCryptoResponse
    {
        [JsonPropertyName("coins")]
        public List<BrapiCoinQuote> Coins { get; init; } = [];
    }

    private sealed class BrapiCoinQuote
    {
        [JsonPropertyName("coin")]
        public string Coin { get; init; } = string.Empty;
        [JsonPropertyName("coinName")]
        public string? CoinName { get; init; }
        [JsonPropertyName("currency")]
        public string? Currency { get; init; }
        [JsonPropertyName("regularMarketPrice")]
        public decimal? RegularMarketPrice { get; init; }
        [JsonPropertyName("regularMarketTime")]
        public string? RegularMarketTime { get; init; }
    }

    private sealed class BrapiHistoryResponse
    {
        [JsonPropertyName("results")]
        public List<BrapiHistoryResult> Results { get; init; } = [];
    }

    private sealed class BrapiHistoryResult
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; init; } = string.Empty;
        [JsonPropertyName("data")]
        public BrapiHistoryData? Data { get; init; }
    }

    private sealed class BrapiHistoryData
    {
        [JsonPropertyName("historicalDataPrice")]
        public List<BrapiHistoryPoint> History { get; init; } = [];
    }

    private sealed class BrapiHistoryPoint
    {
        [JsonPropertyName("date")]
        public long Date { get; init; }
        [JsonPropertyName("close")]
        public decimal? Close { get; init; }
        [JsonPropertyName("adjustedClose")]
        public decimal? AdjustedClose { get; init; }
    }

    private sealed class BrapiSearchResponse
    {
        [JsonPropertyName("stocks")]
        public List<BrapiSearchItem> Stocks { get; init; } = [];
    }

    private sealed class BrapiSearchItem
    {
        [JsonPropertyName("stock")]
        public string Stock { get; init; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;
        [JsonPropertyName("close")]
        public decimal? Close { get; init; }
        [JsonPropertyName("sector")]
        public string? Sector { get; init; }
        [JsonPropertyName("type")]
        public string? Type { get; init; }
        [JsonPropertyName("subType")]
        public string? SubType { get; init; }
    }
}
