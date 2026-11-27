using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Application.DTOs.Importacao;

namespace InvestDashboard.Application.Interfaces;

public interface IRepositorioLoteImportacaoTesouro
{
    Task AdicionarAsync(LoteImportacaoTesouro lote, CancellationToken cancellationToken = default);
    Task<LoteImportacaoTesouro?> BuscarPorIdAsync(Guid carteiraId, Guid usuarioId, Guid id, CancellationToken cancellationToken = default);
    Task<LoteImportacaoTesouro?> BuscarConfirmadoPorCarteiraEIdAsync(Guid carteiraId, Guid id, CancellationToken cancellationToken = default);
    Task<LoteImportacaoTesouro?> BuscarPorChaveAsync(Guid carteiraId, Guid usuarioId, string hashConteudo, DateOnly dataReferencia, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoteTesouroResumoDto>> ListarPorCarteiraAsync(Guid carteiraId, Guid usuarioId, int pagina = 1, CancellationToken cancellationToken = default);
    Task SalvarAsync(CancellationToken cancellationToken = default);
}
