using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Taxes;

namespace InvestDashboard.Application.Interfaces;

public interface ITaxesAppService
{
    Task<List<EconomicRateDto>> GetAllAsync();
    Task<EconomicRateDto?> GetByIdAsync(Guid id);
    Task<EconomicRateDto> CreateAsync(CreateEconomicRateDto dto);
    Task<EconomicRateDto?> UpdateAsync(Guid id, UpdateEconomicRateDto dto);
    Task<bool> DeleteAsync(Guid id);
}
