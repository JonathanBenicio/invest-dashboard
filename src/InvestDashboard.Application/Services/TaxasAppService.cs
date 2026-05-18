using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Taxes;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services;

public class TaxasAppService : ITaxasAppService
{
    private readonly ITaxaEconomicaRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TaxasAppService(ITaxaEconomicaRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<TaxaEconomicaDto>> GetAllAsync()
    {
        var rates = await _repository.GetAllAsync();
        return rates.Select(MapToDto).ToList();
    }

    public async Task<TaxaEconomicaDto?> GetByIdAsync(Guid id)
    {
        var taxa = await _repository.GetByIdAsync(id);
        return taxa is null ? null : MapToDto(taxa);
    }

    public async Task<TaxaEconomicaDto> CreateAsync(CriarTaxaEconomicaDto dto)
    {
        var taxa = new TaxaEconomica(
            Guid.NewGuid(),
            dto.Name,
            dto.Symbol,
            dto.CurrentValue,
            dto.PreviousValue,
            dto.Description,
            dto.Source,
            DateTime.UtcNow
        );

        await _repository.AddAsync(taxa);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(taxa);
    }

    public async Task<TaxaEconomicaDto?> UpdateAsync(Guid id, AtualizarTaxaEconomicaDto dto)
    {
        var taxa = await _repository.GetByIdAsync(id);
        if (taxa is null) return null;

        taxa.UpdateValue(dto.CurrentValue, DateTime.UtcNow);
        _repository.Update(taxa);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(taxa);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var taxa = await _repository.GetByIdAsync(id);
        if (taxa is null) return false;

        _repository.Delete(taxa);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static TaxaEconomicaDto MapToDto(TaxaEconomica taxa)
    {
        return new TaxaEconomicaDto
        {
            Id = taxa.Id,
            Name = taxa.Name,
            Symbol = taxa.Symbol,
            CurrentValue = taxa.CurrentValue,
            PreviousValue = taxa.PreviousValue,
            Variation = taxa.Variation,
            Description = taxa.Description,
            Source = taxa.Source,
            LastUpdate = taxa.LastUpdate.ToString("yyyy-MM-dd")
        };
    }
}
