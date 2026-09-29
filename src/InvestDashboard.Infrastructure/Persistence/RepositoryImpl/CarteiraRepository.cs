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
            .Include(p => p.Positions)
            .ThenInclude(position => position.Ativo)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Carteira?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _context.Portfolios
            .Include(portfolio => portfolio.Positions)
            .ThenInclude(position => position.Ativo)
            .FirstOrDefaultAsync(portfolio => portfolio.Id == id && portfolio.UserId == userId, cancellationToken);
    }

    public async Task<Carteira?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _context.Portfolios
            .Include(p => p.Positions)
            .ThenInclude(position => position.Ativo)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Carteira>> GetByUserIdPageAsync(
        string userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Array.Empty<Carteira>();

        return await _context.Portfolios
            .AsNoTracking()
            .Include(portfolio => portfolio.Positions)
            .ThenInclude(position => position.Ativo)
            .Where(portfolio => portfolio.UserId == userId)
            .OrderBy(portfolio => portfolio.Name)
            .ThenBy(portfolio => portfolio.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByUserIdAsync(string userId, CancellationToken cancellationToken = default) =>
        _context.Portfolios.CountAsync(portfolio => portfolio.UserId == userId, cancellationToken);

    public async Task<(Carteira Portfolio, PosicaoInvestimento Position)?> GetPositionByIdForUserAsync(
        Guid positionId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await _context.Portfolios
            .Include(item => item.Positions)
            .ThenInclude(position => position.Ativo)
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.Positions.Any(position => position.Id == positionId),
                cancellationToken);

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
