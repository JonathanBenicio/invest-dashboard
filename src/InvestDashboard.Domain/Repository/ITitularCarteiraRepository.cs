using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Domain.Repository;

public interface ITitularCarteiraRepository
{
    Task<IReadOnlyList<TitularCarteira>> ListarAsync(Guid grupoId, CancellationToken cancellationToken = default);
    Task<TitularCarteira?> GetByIdInGroupAsync(Guid id, Guid grupoId, CancellationToken cancellationToken = default);
    Task<TitularCarteira?> GetByNormalizedNameAsync(Guid grupoId, string nomeNormalizado, CancellationToken cancellationToken = default);
    Task<TitularCarteira?> GetByUserIdAsync(Guid grupoId, string usuarioId, CancellationToken cancellationToken = default);
    Task AddAsync(TitularCarteira titular, CancellationToken cancellationToken = default);
    void Update(TitularCarteira titular);
}
