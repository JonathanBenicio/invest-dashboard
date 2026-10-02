using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvestDashboard.Application.DTOs.MarketData;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace InvestDashboard.Infrastructure.Services;

public sealed class BcbCdiBenchmarkProvider(
    HttpClient httpClient,
    IMemoryCache cache,
    TimeProvider timeProvider) : IBenchmarkCdiProvider
{
    private const string Fonte = "Banco Central do Brasil — SGS série 12 (CDI, percentual ao dia)";
    private static readonly Uri BaseUri = new("https://api.bcb.gov.br/dados/serie/bcdata.sgs.12/dados");

    public async Task<SerieBenchmarkCdiDto> GetCdiAsync(
        DateOnly dataDe,
        DateOnly dataAte,
        CancellationToken cancellationToken = default)
    {
        if (dataDe > dataAte || dataAte > DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime) ||
            dataAte.DayNumber - dataDe.DayNumber > 3650)
            throw new ArgumentException("Benchmark date range must be valid, not future, and no longer than ten years.");

        var key = $"bcb-cdi:{dataDe:yyyyMMdd}:{dataAte:yyyyMMdd}";
        if (cache.TryGetValue(key, out SerieBenchmarkCdiDto? cached) && cached is not null)
            return cached;

        var query = $"?formato=json&dataInicial={Uri.EscapeDataString(dataDe.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))}" +
                    $"&dataFinal={Uri.EscapeDataString(dataAte.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))}";
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(BaseUri + query));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("InvestDashboard", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        List<BcbSgsPoint> rows;
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new MarketDataUnavailableException();

            rows = await response.Content.ReadFromJsonAsync<List<BcbSgsPoint>>(cancellationToken)
                ?? throw new MarketDataUnavailableException();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MarketDataUnavailableException();
        }
        catch (HttpRequestException)
        {
            throw new MarketDataUnavailableException();
        }
        catch (JsonException)
        {
            throw new MarketDataUnavailableException();
        }

        decimal index = 100m;
        var points = new List<PontoBenchmarkCdiDto>(rows.Count);
        foreach (var row in rows)
        {
            if (!DateOnly.TryParseExact(row.Data, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                !decimal.TryParse(row.Valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var dailyRate) ||
                dailyRate < 0)
                throw new MarketDataUnavailableException();

            index *= 1 + dailyRate / 100m;
            points.Add(new PontoBenchmarkCdiDto(date, dailyRate, index));
        }

        var result = new SerieBenchmarkCdiDto(
            dataDe,
            dataAte,
            Fonte,
            timeProvider.GetUtcNow().UtcDateTime,
            points);
        cache.Set(key, result, TimeSpan.FromHours(6));
        return result;
    }

    private sealed class BcbSgsPoint
    {
        [JsonPropertyName("data")]
        public string Data { get; init; } = string.Empty;

        [JsonPropertyName("valor")]
        public string Valor { get; init; } = string.Empty;
    }
}
