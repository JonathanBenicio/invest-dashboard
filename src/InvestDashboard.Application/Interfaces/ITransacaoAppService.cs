using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Trading;

namespace InvestDashboard.Application.Interfaces
{
    public interface ITransacaoAppService
    {
        Task<TransacaoDto> RegisterTransactionAsync(RegistrarTransacaoDto dto);
        Task<List<TransacaoDto>> GetTransactionsByPortfolioIdAsync(Guid portfolioId);
    }
}
