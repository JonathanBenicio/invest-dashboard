using System;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.DTOs.Common;

namespace InvestDashboard.Application.Interfaces
{
    public interface ICarteiraAppService
    {
        Task<CarteiraDto> CreatePortfolioAsync(CriarCarteiraDto dto);
        Task<CarteiraDto?> GetPortfolioByIdAsync(Guid portfolioId);
        Task<CarteiraDto?> GetUserPortfolioAsync();
        Task<RespostaPaginada<CarteiraDto>> GetUserPortfoliosAsync(int page = 1, int pageSize = 10, Guid? grupoId = null);
        Task<ResumoCarteirasDto> GetUserPortfoliosSummaryAsync(Guid? grupoId = null);
        Task<ProjecaoRendaFixaConsolidadaDto> GetFixedIncomeProjectionAsync(Guid? grupoId = null);
        Task<ProjecaoRendaFixaDto?> GetFixedIncomeProjectionAsync(Guid positionId);
        Task<IReadOnlyList<PosicaoInvestimentoDto>> GetUserPositionsAsync(Guid? portfolioId = null, Guid? grupoId = null);
        Task<PosicaoInvestimentoDto?> GetPositionByIdAsync(Guid positionId);
        Task<PosicaoInvestimentoDto?> UpdatePositionValuationAsync(Guid positionId, decimal totalValue, DateTime observedAtUtc);
        Task<IReadOnlyList<PrecoHistoricoDto>> GetPriceHistoryAsync(Guid positionId, DateTime? fromDate = null);
        Task<IReadOnlyList<PontoHistoricoCarteiraDto>?> GetPortfolioHistoryAsync(Guid portfolioId, DateOnly? fromDate, DateOnly? toDate);
        Task<bool> DeleteInvestmentAsync(Guid positionId);
        Task<CarteiraDto?> UpdatePortfolioAsync(Guid portfolioId, AtualizarCarteiraDto dto);
        Task<bool> DeletePortfolioAsync(Guid portfolioId);
    }
}
