using InvestDashboard.Application.DTOs.MarketData;

namespace InvestDashboard.Application.Interfaces;

public interface IMarketDataProvider
{
    Task<IReadOnlyList<MarketQuoteDto>> GetQuotesAsync(IReadOnlyCollection<string> symbols, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MarketHistoryPointDto>> GetDailyHistoryAsync(
        IReadOnlyCollection<string> symbols,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MarketSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
