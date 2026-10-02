using InvestDashboard.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvestDashboard.Infrastructure.BackgroundWorkers;

public sealed class AtualizadorVencimentosWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<AtualizadorVencimentosWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var updater = scope.ServiceProvider.GetRequiredService<AtualizadorVencimentosService>();
                var changed = await updater.MarkDueAsync(timeProvider.GetUtcNow().UtcDateTime, stoppingToken);
                if (changed > 0)
                    logger.LogInformation("Marked {Count} fixed-income positions as matured.", changed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Fixed-income maturity update failed; positions will be retried.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
