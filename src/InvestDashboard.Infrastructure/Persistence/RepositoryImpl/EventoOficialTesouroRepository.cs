using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public sealed class EventoOficialTesouroRepository(InvestDbContext context) : IEventoOficialTesouroRepository
{
    public async Task<IReadOnlyList<EventoOficialTesouro>> ListarAsync(Guid carteiraId, Guid loteId, int depoisDoIndice, int limite, CancellationToken cancellationToken = default)
    {
        if (carteiraId == Guid.Empty || loteId == Guid.Empty || depoisDoIndice < -1 || limite is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(limite), "Parâmetros de consulta inválidos.");
        return await context.Set<EventoOficialTesouro>().AsNoTracking()
            .Where(x => x.CarteiraId == carteiraId && x.LoteId == loteId && x.IndiceEvento > depoisDoIndice)
            .OrderBy(x => x.IndiceEvento).Take(limite).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EventoOficialTesouro>> ListarPorAtivoAsync(Guid carteiraId, Guid loteId, Guid ativoId,
        int depoisDoIndice, int limite, CancellationToken cancellationToken = default)
    {
        if (carteiraId == Guid.Empty || loteId == Guid.Empty || ativoId == Guid.Empty || depoisDoIndice < -1 || limite is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(limite), "Parâmetros de consulta inválidos.");
        return await context.Set<EventoOficialTesouro>().AsNoTracking()
            .Where(x => x.CarteiraId == carteiraId && x.LoteId == loteId && x.AtivoId == ativoId && x.IndiceEvento > depoisDoIndice)
            .OrderBy(x => x.IndiceEvento).Take(limite).ToListAsync(cancellationToken);
    }
}
