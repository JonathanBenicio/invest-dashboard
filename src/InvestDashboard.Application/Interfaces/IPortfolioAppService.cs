using System;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Portfolio;

namespace InvestDashboard.Application.Interfaces
{
    public interface IPortfolioAppService
    {
        Task<PortfolioDto> CreatePortfolioAsync(CreatePortfolioDto dto);
        Task<PortfolioDto?> GetPortfolioByIdAsync(Guid portfolioId);
        Task<PortfolioDto?> GetUserPortfolioAsync();
        Task<bool> DeleteInvestmentAsync(Guid positionId);
    }
}
