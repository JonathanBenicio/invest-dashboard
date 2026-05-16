using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Infrastructure.Persistence;
using InvestDashboard.Infrastructure.Realtime.SignalR;

namespace InvestDashboard.Infrastructure.BackgroundWorkers
{
    public class MarketDataUpdateWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<MarketDataHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MarketDataUpdateWorker> _logger;
        private readonly Random _random = new();

        public MarketDataUpdateWorker(
            IServiceScopeFactory scopeFactory,
            IHubContext<MarketDataHub> hubContext,
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<MarketDataUpdateWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalSeconds = _configuration.GetValue<int>("MarketData:IntervalSeconds", 10);
            if (intervalSeconds <= 0) intervalSeconds = 10;

            _logger.LogInformation("MarketDataUpdateWorker started with interval: {Interval} seconds", intervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateMarketDataAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in MarketDataUpdateWorker during cotação update.");
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }

        private async Task UpdateMarketDataAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var assetRepository = scope.ServiceProvider.GetRequiredService<IAssetRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var assets = await assetRepository.GetAllAsync(cancellationToken);
            if (!assets.Any())
            {
                _logger.LogInformation("No tracked assets found in local database to update.");
                return;
            }

            var useBrapi = _configuration.GetValue<bool>("MarketData:UseBrapi", false);
            var brapiToken = _configuration["MarketData:BrapiToken"];

            Dictionary<string, decimal> newPrices = new(StringComparer.OrdinalIgnoreCase);

            if (useBrapi && !string.IsNullOrEmpty(brapiToken))
            {
                _logger.LogInformation("Updating cotações using Brapi API for {Count} assets.", assets.Count);
                newPrices = await FetchPricesFromBrapiAsync(assets.Select(a => a.Ticker), brapiToken, cancellationToken);
            }

            // Fallback or default simulation if Brapi is off or failed to return prices
            foreach (var asset in assets)
            {
                decimal updatedPrice;
                if (newPrices.TryGetValue(asset.Ticker, out var price))
                {
                    updatedPrice = price;
                }
                else
                {
                    // Random Walk simulation (default flutuação)
                    var changePercent = (_random.NextDouble() - 0.49) * 0.02; // slight upward bias
                    updatedPrice = asset.CurrentPrice * (1.0m + (decimal)changePercent);
                    if (updatedPrice < 0.01m) updatedPrice = 0.01m; // Price cannot drop below 1 cent
                }

                asset.UpdatePrice(updatedPrice, DateTime.UtcNow);
                assetRepository.Update(asset);

                // Broadcast to SignalR client group subscribed to this ticker
                await _hubContext.Clients.Group(asset.Ticker).SendAsync("OnPriceUpdate", new
                {
                    ticker = asset.Ticker,
                    price = Math.Round(updatedPrice, 2),
                    updatedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cotações successfully updated and broadcasted.");
        }

        private async Task<Dictionary<string, decimal>> FetchPricesFromBrapiAsync(
            IEnumerable<string> tickers, 
            string token, 
            CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            try
            {
                // Join tickers comma separated
                var tickerList = string.Join(",", tickers.Select(Uri.EscapeDataString));
                var url = $"https://brapi.dev/api/quote/{tickerList}?token={token}";

                var response = await _httpClient.GetFromJsonAsync<BrapiResponse>(url, cancellationToken);
                if (response?.Results != null)
                {
                    foreach (var item in response.Results)
                    {
                        if (!string.IsNullOrEmpty(item.Symbol) && item.RegularMarketPrice.HasValue)
                        {
                            results[item.Symbol.ToUpperInvariant()] = item.RegularMarketPrice.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch prices from Brapi. Falling back to Random Walk simulation.");
            }

            return results;
        }

        private class BrapiResponse
        {
            [JsonPropertyName("results")]
            public List<BrapiResult> Results { get; set; } = new();
        }

        private class BrapiResult
        {
            [JsonPropertyName("symbol")]
            public string Symbol { get; set; } = string.Empty;

            [JsonPropertyName("regularMarketPrice")]
            public decimal? RegularMarketPrice { get; set; }
        }
    }
}
