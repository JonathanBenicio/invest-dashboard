using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public sealed class TitularCarteiraRepository(InvestDbContext context) : ITitularCarteiraRepository
{
    public async Task<IReadOnlyList<TitularCarteira>> ListarAsync(Guid grupoId, CancellationToken cancellationToken = default) =>
        await context.PortfolioHolders.AsNoTracking().Where(holder => holder.GrupoId == grupoId)
            .OrderBy(holder => holder.Nome).ThenBy(holder => holder.Id).ToListAsync(cancellationToken);
    public Task<TitularCarteira?> GetByIdInGroupAsync(Guid id, Guid grupoId, CancellationToken cancellationToken = default) =>
        context.PortfolioHolders.FirstOrDefaultAsync(holder => holder.Id == id && holder.GrupoId == grupoId, cancellationToken);

    public async Task<TitularCarteira?> GetByNormalizedNameAsync(Guid grupoId, string nomeNormalizado, CancellationToken cancellationToken = default)
    {
        var matches = await context.PortfolioHolders.Where(holder =>
            holder.GrupoId == grupoId && holder.NomeNormalizado == nomeNormalizado && holder.UsuarioId == null)
            .Take(2).ToListAsync(cancellationToken);
        if (matches.Count > 1) throw new ArgumentException("Há titulares com o mesmo nome. Selecione o perfil pelo ID.");
        return matches.SingleOrDefault();
    }

    public Task<TitularCarteira?> GetByUserIdAsync(Guid grupoId, string usuarioId, CancellationToken cancellationToken = default) =>
        context.PortfolioHolders.FirstOrDefaultAsync(holder => holder.GrupoId == grupoId && holder.UsuarioId == usuarioId, cancellationToken);

    public async Task AddAsync(TitularCarteira titular, CancellationToken cancellationToken = default) =>
        await context.PortfolioHolders.AddAsync(titular, cancellationToken);

    public void Update(TitularCarteira titular) => context.PortfolioHolders.Update(titular);
}
