using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Domain.Repository;

public interface IBasePosicaoTesouroRepository
{
    Task<IReadOnlyList<BasePosicaoTesouro>> GetAtivasAsync(Guid carteiraId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BasePosicaoTesouro>> ListarHistoricoRecenteAsync(Guid carteiraId, Guid ativoId, CancellationToken cancellationToken = default);
    Task AddAsync(BasePosicaoTesouro basePosicao, CancellationToken cancellationToken = default);
}
