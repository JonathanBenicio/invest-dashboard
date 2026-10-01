using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.Infrastructure.Services;

public sealed class AtualizadorVencimentosService(InvestDbContext context)
{
    private static readonly TimeZoneInfo SaoPauloTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public async Task<int> MarkDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var positions = await context.AssetPositions
            .Include(position => position.Ativo)
            .Where(position => position.TipoAtivo == TipoAtivo.RendaFixa &&
                position.Quantity > 0 && position.MaturedAtUtc == null)
            .ToListAsync(cancellationToken);

        var changed = positions.Count(position => position.TryMarkMatured(nowUtc, SaoPauloTimeZone));
        if (changed > 0)
            await context.SaveChangesAsync(cancellationToken);

        return changed;
    }
}
