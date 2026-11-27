using InvestDashboard.Application.Interfaces;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public sealed class RepositorioLoteImportacaoTesouro(InvestDbContext context) : IRepositorioLoteImportacaoTesouro
{
    public async Task AdicionarAsync(LoteImportacaoTesouro lote, CancellationToken cancellationToken = default) =>
        await context.Set<LoteImportacaoTesouro>().AddAsync(lote, cancellationToken);

    public Task<LoteImportacaoTesouro?> BuscarPorIdAsync(Guid carteiraId, Guid usuarioId, Guid id, CancellationToken cancellationToken = default) =>
        context.Set<LoteImportacaoTesouro>().FirstOrDefaultAsync(x => x.CarteiraId == carteiraId && x.UsuarioId == usuarioId && x.Id == id, cancellationToken);

    public Task<LoteImportacaoTesouro?> BuscarConfirmadoPorCarteiraEIdAsync(Guid carteiraId, Guid id, CancellationToken cancellationToken = default) =>
        context.Set<LoteImportacaoTesouro>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.CarteiraId == carteiraId && x.Id == id && x.Estado == "Confirmado", cancellationToken);

    public Task<LoteImportacaoTesouro?> BuscarPorChaveAsync(Guid carteiraId, Guid usuarioId, string hashConteudo, DateOnly dataReferencia, CancellationToken cancellationToken = default)
    {
        var hashNormalizado = hashConteudo.ToLowerInvariant();
        return context.Set<LoteImportacaoTesouro>().FirstOrDefaultAsync(x => x.CarteiraId == carteiraId && x.UsuarioId == usuarioId && x.HashConteudo == hashNormalizado && x.DataReferencia == dataReferencia, cancellationToken);
    }

    public async Task<IReadOnlyList<LoteTesouroResumoDto>> ListarPorCarteiraAsync(Guid carteiraId, Guid usuarioId, int pagina = 1, CancellationToken cancellationToken = default)
    {
        if (pagina is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(pagina));
        const int tamanhoPagina = 50;
        return await context.Set<LoteImportacaoTesouro>().AsNoTracking()
            .Where(x => x.CarteiraId == carteiraId && x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.CriadoEmUtc).ThenByDescending(x => x.Id)
            .Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina)
            .Select(x => new LoteTesouroResumoDto(x.Id, x.CarteiraId, x.DataReferencia, x.Formato, x.Estado, x.CriadoEmUtc, x.ConfirmadoEmUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task SalvarAsync(CancellationToken cancellationToken = default) => await context.SaveChangesAsync(cancellationToken);
}
