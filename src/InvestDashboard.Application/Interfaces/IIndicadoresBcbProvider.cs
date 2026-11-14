using InvestDashboard.Application.DTOs.MarketData;

namespace InvestDashboard.Application.Interfaces;

public interface IIndicadoresBcbProvider
{
    Task<SerieIndicadorBcbDto> GetSelicAsync(DateOnly dataDe, DateOnly dataAte, CancellationToken cancellationToken = default);
    Task<SerieIndicadorBcbDto> GetIpcaAsync(DateOnly dataDe, DateOnly dataAte, CancellationToken cancellationToken = default);
}
