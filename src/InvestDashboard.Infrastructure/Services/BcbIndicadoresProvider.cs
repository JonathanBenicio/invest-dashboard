using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvestDashboard.Application.DTOs.MarketData;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace InvestDashboard.Infrastructure.Services;

public sealed class BcbIndicadoresProvider(HttpClient httpClient, IMemoryCache cache, TimeProvider timeProvider)
    : IIndicadoresBcbProvider
{
    private static readonly TimeZoneInfo FusoBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    public Task<SerieIndicadorBcbDto> GetSelicAsync(DateOnly dataDe, DateOnly dataAte, CancellationToken cancellationToken = default) =>
        ObterAsync("SELIC", 11, "percentual ao dia", "diária", dataDe, dataAte, cancellationToken);

    public Task<SerieIndicadorBcbDto> GetIpcaAsync(DateOnly dataDe, DateOnly dataAte, CancellationToken cancellationToken = default) =>
        ObterAsync("IPCA", 433, "variação percentual mensal", "mensal", dataDe, dataAte, cancellationToken);

    private async Task<SerieIndicadorBcbDto> ObterAsync(string indicador, int codigo, string unidade, string periodicidade,
        DateOnly dataDe, DateOnly dataAte, CancellationToken cancellationToken)
    {
        if (dataDe == default || dataAte == default || dataDe > dataAte ||
            dataAte > DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), FusoBrasil).DateTime) ||
            dataAte.DayNumber - dataDe.DayNumber > 3650)
            throw new ArgumentException("Informe um período válido, sem datas futuras e de até 3650 dias.");

        var key = $"bcb-indicador:{codigo}:{dataDe:yyyyMMdd}:{dataAte:yyyyMMdd}";
        if (cache.TryGetValue(key, out SerieIndicadorBcbDto? cached) && cached is not null) return cached;

        var query = $"?formato=json&dataInicial={Uri.EscapeDataString(dataDe.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))}" +
                    $"&dataFinal={Uri.EscapeDataString(dataAte.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))}";
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.bcb.gov.br/dados/serie/bcdata.sgs.{codigo}/dados{query}");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("InvestDashboard", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        List<BcbPoint?> rows;
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // O SGS usa 404 para períodos sem observações. Um 404 de outra origem
                // (por exemplo rota removida) não deve ser confundido com série vazia.
                var erro = await response.Content.ReadFromJsonAsync<BcbErrorResponse>(cancellationToken);
                if (erro?.Erro?.StatusCode != 404 ||
                    erro.Erro.Detail?.EndsWith("Value(s) not found", StringComparison.Ordinal) != true)
                    throw new MarketDataUnavailableException();
                rows = [];
            }
            else
            {
                if (!response.IsSuccessStatusCode) throw new MarketDataUnavailableException();
                rows = await response.Content.ReadFromJsonAsync<List<BcbPoint?>>(cancellationToken)
                       ?? throw new MarketDataUnavailableException();
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MarketDataUnavailableException();
        }
        catch (HttpRequestException) { throw new MarketDataUnavailableException(); }
        catch (JsonException) { throw new MarketDataUnavailableException(); }
        catch (NotSupportedException) { throw new MarketDataUnavailableException(); }

        var pontos = new List<PontoIndicadorBcbDto>(rows.Count);
        var datas = new HashSet<DateOnly>();
        // O SGS retorna séries mensais com a data de referência no primeiro dia do mês,
        // mesmo quando o início do filtro informado ocorre depois desse dia.
        var dataMinima = codigo == 433 ? new DateOnly(dataDe.Year, dataDe.Month, 1) : dataDe;
        foreach (var row in rows)
        {
            if (row is null ||
                !DateOnly.TryParseExact(row.Data, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) ||
                data < dataMinima || data > dataAte || !datas.Add(data) ||
                !decimal.TryParse(row.Valor, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var valor))
                throw new MarketDataUnavailableException();

            pontos.Add(new PontoIndicadorBcbDto(data, valor));
        }

        pontos.Sort((a, b) => a.Data.CompareTo(b.Data));
        var result = new SerieIndicadorBcbDto(indicador, codigo, unidade, periodicidade, dataDe, dataAte,
            $"Banco Central do Brasil — SGS série {codigo}", timeProvider.GetUtcNow().UtcDateTime, pontos);
        cache.Set(key, result, TimeSpan.FromHours(6));
        return result;
    }

    private sealed class BcbPoint
    {
        [JsonPropertyName("data")] public string? Data { get; init; }
        [JsonPropertyName("valor")] public string? Valor { get; init; }
    }

    private sealed class BcbErrorResponse
    {
        [JsonPropertyName("erro")] public BcbError? Erro { get; init; }
    }

    private sealed class BcbError
    {
        [JsonPropertyName("statusCode")] public int StatusCode { get; init; }
        [JsonPropertyName("detail")] public string? Detail { get; init; }
    }
}
