using InvestDashboard.Application.DTOs.MarketData;

namespace InvestDashboard.Application.Interfaces;

public interface IBenchmarkCdiProvider
{
    Task<SerieBenchmarkCdiDto> GetCdiAsync(DateOnly dataDe, DateOnly dataAte, CancellationToken cancellationToken = default);
}
