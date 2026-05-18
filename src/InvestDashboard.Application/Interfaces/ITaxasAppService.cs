using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Taxes;

namespace InvestDashboard.Application.Interfaces;

public interface ITaxasAppService
{
    Task<List<TaxaEconomicaDto>> GetAllAsync();
    Task<TaxaEconomicaDto?> GetByIdAsync(Guid id);
    Task<TaxaEconomicaDto> CreateAsync(CriarTaxaEconomicaDto dto);
    Task<TaxaEconomicaDto?> UpdateAsync(Guid id, AtualizarTaxaEconomicaDto dto);
    Task<bool> DeleteAsync(Guid id);
}
