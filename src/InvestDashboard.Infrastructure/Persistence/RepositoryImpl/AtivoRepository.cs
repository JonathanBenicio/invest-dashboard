using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public class AtivoRepository : IAtivoRepository
{
    private readonly InvestDbContext _context;

    public AtivoRepository(InvestDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Ativo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Ativo?> GetByTickerAsync(string ticker, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            return null;

        string normalizedTicker = ticker.Trim().ToUpperInvariant();
        return await _context.Assets
            .FirstOrDefaultAsync(a => a.Ticker == normalizedTicker, cancellationToken);
    }

    public async Task<IReadOnlyList<Ativo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Ativo asset, CancellationToken cancellationToken = default)
    {
        if (asset is null)
            throw new ArgumentNullException(nameof(asset));

        await _context.Assets.AddAsync(asset, cancellationToken);
    }

    public void Update(Ativo asset)
    {
        if (asset is null)
            throw new ArgumentNullException(nameof(asset));

        _context.Assets.Update(asset);
    }

    public void Delete(Ativo asset)
    {
        if (asset is null)
            throw new ArgumentNullException(nameof(asset));

        _context.Assets.Remove(asset);
    }
}
