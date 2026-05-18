using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace InvestDashboard.Infrastructure.Realtime.SignalR
{
    [Authorize]
    public class DadosMercadoHub : Hub
    {
        private readonly ILogger<DadosMercadoHub> _logger;
        private static readonly ConcurrentDictionary<string, HashSet<string>> _subscriptions = new();

        public DadosMercadoHub(ILogger<DadosMercadoHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier ?? Context.User?.FindFirst("sub")?.Value ?? "anonymous";
            _logger.LogInformation("Client connected: ConnectionId={ConnectionId}, User={UserId}", Context.ConnectionId, userId);
            _subscriptions[Context.ConnectionId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: ConnectionId={ConnectionId}", Context.ConnectionId);
            _subscriptions.TryRemove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task Subscribe(IEnumerable<string> tickers)
        {
            if (tickers == null) return;
            
            if (_subscriptions.TryGetValue(Context.ConnectionId, out var subs))
            {
                foreach (var ticker in tickers)
                {
                    var cleanTicker = ticker.Trim().ToUpperInvariant();
                    if (!string.IsNullOrEmpty(cleanTicker))
                    {
                        subs.Add(cleanTicker);
                        await Groups.AddToGroupAsync(Context.ConnectionId, cleanTicker);
                        _logger.LogInformation("Client {ConnectionId} subscribed to ticker: {Ticker}", Context.ConnectionId, cleanTicker);
                    }
                }
            }
        }

        public async Task Unsubscribe(IEnumerable<string> tickers)
        {
            if (tickers == null) return;

            if (_subscriptions.TryGetValue(Context.ConnectionId, out var subs))
            {
                foreach (var ticker in tickers)
                {
                    var cleanTicker = ticker.Trim().ToUpperInvariant();
                    if (!string.IsNullOrEmpty(cleanTicker))
                    {
                        subs.Remove(cleanTicker);
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, cleanTicker);
                        _logger.LogInformation("Client {ConnectionId} unsubscribed from ticker: {Ticker}", Context.ConnectionId, cleanTicker);
                    }
                }
            }
        }

        public static HashSet<string> GetActiveTickersForConnection(string connectionId)
        {
            return _subscriptions.TryGetValue(connectionId, out var subs) ? subs : new HashSet<string>();
        }
        
        public static IEnumerable<string> GetActiveConnections()
        {
            return _subscriptions.Keys;
        }
    }
}
