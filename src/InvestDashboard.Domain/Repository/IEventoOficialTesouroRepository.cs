using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Domain.Repository;

/// <summary>O chamador deve verificar autorização sobre a carteira e o lote.</summary>
public interface IEventoOficialTesouroRepository
{
    Task<IReadOnlyList<EventoOficialTesouro>> ListarAsync(Guid carteiraId, Guid loteId, int depoisDoIndice, int limite, CancellationToken cancellationToken = default);
}
