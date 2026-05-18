using System;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Portfolio;

namespace InvestDashboard.Application.Interfaces
{
    public interface ICarteiraAppService
    {
        Task<CarteiraDto> CreatePortfolioAsync(CriarCarteiraDto dto);
        Task<CarteiraDto?> GetPortfolioByIdAsync(Guid portfolioId);
        Task<CarteiraDto?> GetUserPortfolioAsync();
        Task<bool> DeleteInvestmentAsync(Guid positionId);
    }
}
