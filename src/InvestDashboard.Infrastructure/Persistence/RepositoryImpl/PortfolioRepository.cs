using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public class PortfolioRepository : IPortfolioRepository
{
    private readonly InvestDbContext _context;

    public PortfolioRepository(InvestDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Portfolios
            .Include(p => p.Positions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Portfolio?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _context.Portfolios
            .Include(p => p.Positions)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Portfolio portfolio, CancellationToken cancellationToken = default)
    {
        if (portfolio is null)
            throw new ArgumentNullException(nameof(portfolio));

        await _context.Portfolios.AddAsync(portfolio, cancellationToken);
    }

    public void Update(Portfolio portfolio)
    {
        if (portfolio is null)
            throw new ArgumentNullException(nameof(portfolio));

        _context.Portfolios.Update(portfolio);
    }

    public void Delete(Portfolio portfolio)
    {
        if (portfolio is null)
            throw new ArgumentNullException(nameof(portfolio));

        _context.Portfolios.Remove(portfolio);
    }
}
