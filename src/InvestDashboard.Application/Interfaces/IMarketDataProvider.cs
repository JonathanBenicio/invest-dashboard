using InvestDashboard.Application.DTOs.MarketData;

namespace InvestDashboard.Application.Interfaces;

public interface IMarketDataProvider
{
    Task<IReadOnlyList<CotacaoMercadoDto>> GetQuotesAsync(IReadOnlyCollection<string> symbols, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PontoHistoricoMercadoDto>> GetDailyHistoryAsync(
        IReadOnlyCollection<string> symbols,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResultadoBuscaMercadoDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
