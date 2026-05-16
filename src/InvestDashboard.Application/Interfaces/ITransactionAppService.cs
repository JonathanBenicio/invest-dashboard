using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Trading;

namespace InvestDashboard.Application.Interfaces
{
    public interface ITransactionAppService
    {
        Task<TransactionDto> RegisterTransactionAsync(RegisterTransactionDto dto);
        Task<List<TransactionDto>> GetTransactionsByPortfolioIdAsync(Guid portfolioId);
    }
}
