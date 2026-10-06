using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Taxes;

namespace InvestDashboard.Application.Interfaces;

public interface ITaxasAppService
{
    Task<List<TaxaEconomicaDto>> GetAllAsync(Guid groupId, string userId);
    Task<TaxaEconomicaDto?> GetByIdAsync(Guid id, Guid groupId, string userId);
    Task<TaxaEconomicaDto> CreateAsync(CriarTaxaEconomicaDto dto, Guid groupId, string userId);
    Task<TaxaEconomicaDto?> UpdateAsync(Guid id, AtualizarTaxaEconomicaDto dto, Guid groupId, string userId);
    Task<bool> DeleteAsync(Guid id, Guid groupId, string userId);
    Task<IReadOnlyList<TaxaEconomicaHistoricoDto>?> GetHistoryAsync(Guid id, Guid groupId, string userId);
}
