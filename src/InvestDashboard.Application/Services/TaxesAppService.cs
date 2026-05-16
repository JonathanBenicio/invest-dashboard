using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Taxes;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services;

public class TaxesAppService : ITaxesAppService
{
    private readonly IEconomicRateRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TaxesAppService(IEconomicRateRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<EconomicRateDto>> GetAllAsync()
    {
        var rates = await _repository.GetAllAsync();
        return rates.Select(MapToDto).ToList();
    }

    public async Task<EconomicRateDto?> GetByIdAsync(Guid id)
    {
        var rate = await _repository.GetByIdAsync(id);
        return rate is null ? null : MapToDto(rate);
    }

    public async Task<EconomicRateDto> CreateAsync(CreateEconomicRateDto dto)
    {
        var rate = new EconomicRate(
            Guid.NewGuid(),
            dto.Name,
            dto.Symbol,
            dto.CurrentValue,
            dto.PreviousValue,
            dto.Description,
            dto.Source,
            DateTime.UtcNow
        );

        await _repository.AddAsync(rate);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(rate);
    }

    public async Task<EconomicRateDto?> UpdateAsync(Guid id, UpdateEconomicRateDto dto)
    {
        var rate = await _repository.GetByIdAsync(id);
        if (rate is null) return null;

        rate.UpdateValue(dto.CurrentValue, DateTime.UtcNow);
        _repository.Update(rate);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(rate);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var rate = await _repository.GetByIdAsync(id);
        if (rate is null) return false;

        _repository.Delete(rate);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static EconomicRateDto MapToDto(EconomicRate rate)
    {
        return new EconomicRateDto
        {
            Id = rate.Id,
            Name = rate.Name,
            Symbol = rate.Symbol,
            CurrentValue = rate.CurrentValue,
            PreviousValue = rate.PreviousValue,
            Variation = rate.Variation,
            Description = rate.Description,
            Source = rate.Source,
            LastUpdate = rate.LastUpdate.ToString("yyyy-MM-dd")
        };
    }
}
