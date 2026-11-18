using InvestDashboard.Application.DTOs.MarketData;

namespace InvestDashboard.Application.Interfaces;

public interface IAvaliacaoTesouroAppService
{
    Task<AvaliacaoPosicaoTesouroDto?> ConsultarAsync(Guid posicaoId, CancellationToken cancellationToken = default);
}
