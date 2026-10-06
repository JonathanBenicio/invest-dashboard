using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public class CarteiraRepository : ICarteiraRepository
{
    private readonly InvestDbContext _context;

    public CarteiraRepository(InvestDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Carteira?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Portfolios
            .Include(p => p.TitularProfile)
            .Include(p => p.InstituicaoFinanceiraProfile)
            .Include(p => p.Positions)
            .ThenInclude(position => position.Ativo)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    private IQueryable<Carteira> Accessible(string userId) =>
        _context.Portfolios.Where(portfolio =>
            portfolio.GrupoId == null && portfolio.UserId == userId ||
            portfolio.GrupoId.HasValue && _context.GroupMembers.Any(member =>
                member.GrupoId == portfolio.GrupoId && member.UsuarioId == userId && member.Ativo &&
                (portfolio.Visibilidade == VisibilidadeCarteira.PublicaDoGrupo || member.Papel == PapelGrupo.Admin ||
                 _context.PortfolioHolders.Any(holder => holder.Id == portfolio.TitularId &&
                     holder.GrupoId == portfolio.GrupoId && holder.UsuarioId == userId))));

    private static IQueryable<Carteira> WithDetails(IQueryable<Carteira> query) => query
        .Include(portfolio => portfolio.TitularProfile)
        .Include(portfolio => portfolio.InstituicaoFinanceiraProfile)
        .Include(portfolio => portfolio.Positions).ThenInclude(position => position.Ativo);

    public Task<Carteira?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default) =>
        WithDetails(Accessible(userId)).FirstOrDefaultAsync(portfolio => portfolio.Id == id, cancellationToken);

    public Task<Carteira?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        WithDetails(Accessible(userId)).OrderBy(portfolio => portfolio.Name).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Carteira>> GetByUserIdPageAsync(
        string userId, int skip, int take, Guid? groupId = null, CancellationToken cancellationToken = default) =>
        await WithDetails(Accessible(userId)).AsNoTracking()
            .Where(portfolio => !groupId.HasValue || portfolio.GrupoId == groupId)
            .OrderBy(portfolio => portfolio.Name).ThenBy(portfolio => portfolio.Id)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);

    public Task<int> CountByUserIdAsync(string userId, Guid? groupId = null, CancellationToken cancellationToken = default) =>
        Accessible(userId).CountAsync(portfolio => !groupId.HasValue || portfolio.GrupoId == groupId, cancellationToken);

    public async Task<bool> CanManageAsync(Guid portfolioId, string userId, CancellationToken cancellationToken = default)
    {
        var portfolio = await Accessible(userId).AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == portfolioId, cancellationToken);
        if (portfolio is null) return false;
        if (portfolio.GrupoId is null) return portfolio.UserId == userId;
        return await _context.GroupMembers.AnyAsync(member =>
            member.GrupoId == portfolio.GrupoId && member.UsuarioId == userId && member.Ativo &&
            (member.Papel == PapelGrupo.Admin || member.Papel == PapelGrupo.Investidor &&
             (portfolio.Visibilidade == VisibilidadeCarteira.PublicaDoGrupo ||
              _context.PortfolioHolders.Any(holder => holder.Id == portfolio.TitularId &&
                  holder.GrupoId == portfolio.GrupoId && holder.UsuarioId == userId))), cancellationToken);
    }

    public async Task<(Carteira Portfolio, PosicaoInvestimento Position)?> GetPositionByIdForUserAsync(
        Guid positionId, string userId, CancellationToken cancellationToken = default)
    {
        var portfolio = await WithDetails(Accessible(userId))
            .FirstOrDefaultAsync(item => item.Positions.Any(position => position.Id == positionId), cancellationToken);
        var position = portfolio?.Positions.FirstOrDefault(item => item.Id == positionId);
        return position is null ? null : (portfolio!, position);
    }

    public async Task AddAsync(Carteira portfolio, CancellationToken cancellationToken = default)
    {
        if (portfolio is null)
            throw new ArgumentNullException(nameof(portfolio));

        await _context.Portfolios.AddAsync(portfolio, cancellationToken);
    }

    public void AddPosition(PosicaoInvestimento position)
    {
        if (position is null)
            throw new ArgumentNullException(nameof(position));

        _context.AssetPositions.Add(position);
    }

    public void Update(Carteira portfolio)
    {
        if (portfolio is null)
            throw new ArgumentNullException(nameof(portfolio));

        var entry = _context.Entry(portfolio);
        if (entry.State == EntityState.Detached)
            entry.State = EntityState.Modified;

        foreach (var position in portfolio.Positions)
        {
            var positionEntry = _context.Entry(position);
            if (positionEntry.State == EntityState.Detached)
                _context.AssetPositions.Add(position);
        }
    }

    public void Delete(Carteira portfolio)
    {
        if (portfolio is null)
            throw new ArgumentNullException(nameof(portfolio));

        _context.Portfolios.Remove(portfolio);
    }
}
