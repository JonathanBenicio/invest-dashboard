using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public sealed class InstituicaoFinanceiraRepository(InvestDbContext context) : IInstituicaoFinanceiraRepository
{
    public async Task<IReadOnlyList<InstituicaoFinanceira>> GetCatalogoAsync(Guid? grupoId, CancellationToken cancellationToken = default) =>
        await context.FinancialInstitutions.AsNoTracking()
            .Where(institution => institution.GrupoId == null || grupoId.HasValue && institution.GrupoId == grupoId)
            .OrderBy(institution => institution.Categoria)
            .ThenBy(institution => institution.Nome)
            .ToListAsync(cancellationToken);

    public Task<InstituicaoFinanceira?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.FinancialInstitutions.FirstOrDefaultAsync(institution => institution.Id == id, cancellationToken);

    public Task<InstituicaoFinanceira?> GetByNameAsync(Guid? grupoId, string nomeNormalizado, CancellationToken cancellationToken = default) =>
        context.FinancialInstitutions.Where(institution =>
            institution.NomeNormalizado == nomeNormalizado &&
            (institution.GrupoId == null || grupoId.HasValue && institution.GrupoId == grupoId))
            .OrderBy(institution => institution.GrupoId == grupoId ? 0 : 1)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(InstituicaoFinanceira instituicao, CancellationToken cancellationToken = default) =>
        await context.FinancialInstitutions.AddAsync(instituicao, cancellationToken);
}
