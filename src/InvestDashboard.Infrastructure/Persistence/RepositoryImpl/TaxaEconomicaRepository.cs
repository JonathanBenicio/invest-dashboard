using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public class TaxaEconomicaRepository : ITaxaEconomicaRepository
{
    private readonly InvestDbContext _context;

    public TaxaEconomicaRepository(InvestDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<TaxaEconomica?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EconomicRates
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<TaxaEconomica>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EconomicRates
            .OrderBy(r => r.Symbol)
            .ToListAsync(cancellationToken);
    }

    public Task<TaxaEconomica?> GetByIdInGroupAsync(Guid id, Guid groupId, CancellationToken cancellationToken = default) =>
        _context.EconomicRates.FirstOrDefaultAsync(rate => rate.Id == id && rate.GroupId == groupId, cancellationToken);

    public Task<List<TaxaEconomica>> GetAllInGroupAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        _context.EconomicRates.Where(rate => rate.GroupId == groupId).OrderBy(rate => rate.Symbol).ToListAsync(cancellationToken);

    public async Task AddHistoryAsync(TaxaEconomicaHistorico history, CancellationToken cancellationToken = default) =>
        await _context.EconomicRateHistory.AddAsync(history, cancellationToken);

    public Task<List<TaxaEconomicaHistorico>> GetHistoryAsync(Guid rateId, CancellationToken cancellationToken = default) =>
        _context.EconomicRateHistory.AsNoTracking().Where(item => item.TaxaId == rateId)
            .OrderByDescending(item => item.AtualizadoEmUtc).ToListAsync(cancellationToken);

    public async Task AddAsync(TaxaEconomica rate, CancellationToken cancellationToken = default)
    {
        if (rate is null)
            throw new ArgumentNullException(nameof(rate));

        await _context.EconomicRates.AddAsync(rate, cancellationToken);
    }

    public void Update(TaxaEconomica rate)
    {
        if (rate is null)
            throw new ArgumentNullException(nameof(rate));

        _context.EconomicRates.Update(rate);
    }

    public void Delete(TaxaEconomica rate)
    {
        if (rate is null)
            throw new ArgumentNullException(nameof(rate));

        _context.EconomicRates.Remove(rate);
    }
}
