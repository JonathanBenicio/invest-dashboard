using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Realtime.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvestDashboard.Infrastructure.BackgroundWorkers;

public sealed class AtualizadorDadosMercadoWorker(
    IServiceScopeFactory scopeFactory,
    IHubContext<DadosMercadoHub> hubContext,
    IConfiguration configuration,
    IMarketDataProvider marketData,
    ILogger<AtualizadorDadosMercadoWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Clamp(configuration.GetValue("MarketData:IntervalSeconds", 60), 30, 3600);
        logger.LogInformation("Market quote worker started. Polling interval: {IntervalSeconds} seconds.", intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateMarketDataAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Market data update failed; the last stored prices were kept.");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task UpdateMarketDataAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var assetsRepository = scope.ServiceProvider.GetRequiredService<IAtivoRepository>();
        var pricesRepository = scope.ServiceProvider.GetRequiredService<IPrecoHistoricoRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var assets = await assetsRepository.GetAllAsync(cancellationToken);
        var marketAssets = assets.Where(asset => asset.TipoAtivo != TipoAtivo.RendaFixa).ToList();
        if (marketAssets.Count == 0) return;

        var quotes = await marketData.GetQuotesAsync(
            marketAssets.Select(asset => asset.Ticker).ToArray(),
            cancellationToken);
        var quotesBySymbol = quotes.ToDictionary(quote => quote.Symbol, StringComparer.OrdinalIgnoreCase);
        var nowUtc = DateTime.UtcNow;
        var pricesAlreadyStored = await pricesRepository.GetByAtivoIdsAsync(
            marketAssets.Select(asset => asset.Id).ToArray(),
            nowUtc.AddDays(-30).Date,
            cancellationToken);
        var historicalDays = pricesAlreadyStored
            .Select(price => (price.AtivoId, DateOnly.FromDateTime(price.Date)))
            .ToHashSet();
        var priceUpdates = new List<(string Ticker, decimal Price, DateTime UpdatedAt, string Source)>();

        foreach (var asset in marketAssets)
        {
            if (!quotesBySymbol.TryGetValue(asset.Ticker, out var quote) || quote.Price <= 0)
            {
                logger.LogInformation("No valid quote was returned for {Ticker}; retaining its last price.", asset.Ticker);
                continue;
            }
            if (quote.ObservedAtUtc > nowUtc.AddMinutes(5))
            {
                logger.LogWarning("The provider returned a future observation for {Ticker}; ignoring it.", asset.Ticker);
                continue;
            }
            if (quote.ObservedAtUtc <= asset.LastUpdatedUtc)
                continue;

            asset.UpdatePrice(quote.Price, quote.ObservedAtUtc);
            assetsRepository.Update(asset);
            var observationDate = DateOnly.FromDateTime(quote.ObservedAtUtc);
            if (historicalDays.Add((asset.Id, observationDate)))
            {
                await pricesRepository.AddAsync(new PrecoHistorico(
                    Guid.NewGuid(),
                    asset.Id,
                    quote.Price,
                    quote.ObservedAtUtc,
                    "brapi"));
            }
            priceUpdates.Add((asset.Ticker, quote.Price, quote.ObservedAtUtc, quote.Source));
        }

        if (priceUpdates.Count == 0) return;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var update in priceUpdates)
        {
            await hubContext.Clients.Group(update.Ticker).SendAsync(
                "OnPriceUpdate",
                new
                {
                    ticker = update.Ticker,
                    preco = decimal.Round(update.Price, 4),
                    observadoEmUtc = update.UpdatedAt,
                    origem = update.Source
                },
                cancellationToken);
        }

        logger.LogInformation("Stored and broadcast {QuoteCount} real market quotes.", priceUpdates.Count);
    }
}
