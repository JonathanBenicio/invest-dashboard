using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public sealed class BasePosicaoTesouroRepository(InvestDbContext context) : IBasePosicaoTesouroRepository
{
    public async Task<IReadOnlyList<BasePosicaoTesouro>> GetAtivasAsync(Guid carteiraId, CancellationToken cancellationToken = default) =>
        await context.Set<BasePosicaoTesouro>().Where(x => x.CarteiraId == carteiraId && x.Ativa).ToListAsync(cancellationToken);

    public async Task AddAsync(BasePosicaoTesouro basePosicao, CancellationToken cancellationToken = default) =>
        await context.Set<BasePosicaoTesouro>().AddAsync(basePosicao, cancellationToken);
}
