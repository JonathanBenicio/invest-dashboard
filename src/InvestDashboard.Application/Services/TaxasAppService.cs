using InvestDashboard.Application.DTOs.Taxes;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services;

public sealed class TaxasAppService(
    ITaxaEconomicaRepository repository,
    IGrupoCarteirasRepository groups,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ITaxasAppService
{
    public async Task<List<TaxaEconomicaDto>> GetAllAsync(Guid groupId, string userId)
    {
        await RequireMemberAsync(groupId, userId);
        return (await repository.GetAllInGroupAsync(groupId)).Select(MapToDto).ToList();
    }

    public async Task<TaxaEconomicaDto?> GetByIdAsync(Guid id, Guid groupId, string userId)
    {
        await RequireMemberAsync(groupId, userId);
        var rate = await repository.GetByIdInGroupAsync(id, groupId);
        return rate is null ? null : MapToDto(rate);
    }

    public async Task<TaxaEconomicaDto> CreateAsync(CriarTaxaEconomicaDto dto, Guid groupId, string userId)
    {
        await RequireAdminAsync(groupId, userId);
        ValidateMetadata(dto.Unit, dto.Periodicity, dto.ReferenceDate);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var rate = new TaxaEconomica(Guid.NewGuid(), dto.Name, dto.Symbol, dto.CurrentValue, dto.PreviousValue,
            dto.Description, dto.Source, now, groupId, dto.Unit, dto.Periodicity, dto.ReferenceDate, userId);
        await repository.AddAsync(rate);
        await repository.AddHistoryAsync(new TaxaEconomicaHistorico(Guid.NewGuid(), rate.Id, dto.PreviousValue,
            dto.CurrentValue, dto.Source, dto.ReferenceDate, userId, now, dto.Unit, dto.Unit, dto.Periodicity, dto.Periodicity));
        await unitOfWork.SaveChangesAsync();
        return MapToDto(rate);
    }

    public async Task<TaxaEconomicaDto?> UpdateAsync(Guid id, AtualizarTaxaEconomicaDto dto, Guid groupId, string userId)
    {
        await RequireAdminAsync(groupId, userId);
        ValidateMetadata(dto.Unit, dto.Periodicity, dto.ReferenceDate);
        var rate = await repository.GetByIdInGroupAsync(id, groupId);
        if (rate is null) return null;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var previousValue = rate.CurrentValue;
        var previousUnit = rate.Unit;
        var previousPeriodicity = rate.Periodicity;
        rate.Update(dto.Name, dto.Symbol, dto.CurrentValue, dto.PreviousValue, dto.Description, dto.Source, now,
            dto.Unit, dto.Periodicity, dto.ReferenceDate, userId);
        repository.Update(rate);
        await repository.AddHistoryAsync(new TaxaEconomicaHistorico(Guid.NewGuid(), rate.Id, previousValue,
            dto.CurrentValue, dto.Source, dto.ReferenceDate, userId, now, previousUnit, dto.Unit, previousPeriodicity, dto.Periodicity));
        await unitOfWork.SaveChangesAsync();
        return MapToDto(rate);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid groupId, string userId)
    {
        await RequireAdminAsync(groupId, userId);
        var rate = await repository.GetByIdInGroupAsync(id, groupId);
        if (rate is null) return false;
        repository.Delete(rate);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<TaxaEconomicaHistoricoDto>?> GetHistoryAsync(Guid id, Guid groupId, string userId)
    {
        await RequireMemberAsync(groupId, userId);
        if (await repository.GetByIdInGroupAsync(id, groupId) is null) return null;
        return (await repository.GetHistoryAsync(id)).Select(item => new TaxaEconomicaHistoricoDto
        {
            ValorAnterior = item.ValorAnterior,
            ValorNovo = item.ValorNovo,
            UnidadeAnterior = item.UnidadeAnterior,
            UnidadeNova = item.UnidadeNova,
            PeriodicidadeAnterior = item.PeriodicidadeAnterior,
            PeriodicidadeNova = item.PeriodicidadeNova,
            Origem = item.Origem,
            DataReferencia = item.DataReferencia,
            ResponsavelUserId = item.ResponsavelUserId,
            AtualizadoEmUtc = item.AtualizadoEmUtc
        }).ToList();
    }

    private async Task RequireMemberAsync(Guid groupId, string userId)
    {
        if (groupId == Guid.Empty) throw new ArgumentException("A group is required.");
        var member = await groups.GetMemberAsync(groupId, userId);
        if (member is null || !member.Ativo) throw new UnauthorizedAccessException("Active membership in this group is required.");
    }

    private async Task RequireAdminAsync(Guid groupId, string userId)
    {
        await RequireMemberAsync(groupId, userId);
        var member = await groups.GetMemberAsync(groupId, userId);
        if (member?.Papel != PapelGrupo.Admin) throw new UnauthorizedAccessException("Only group admins can edit economic rates.");
    }

    private static void ValidateMetadata(string unit, string periodicity, DateOnly referenceDate)
    {
        var validUnits = new[] { "Percentual", "R$/US$", "BRL", "Pontos" };
        var validPeriods = new[] { "Diaria", "Mensal", "Anual", "Pontual" };
        if (!validUnits.Contains(unit, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Unidade inválida. Use Percentual, R$/US$, BRL ou Pontos.");
        if (!validPeriods.Contains(periodicity, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Periodicidade inválida.");
        if (referenceDate == default || referenceDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Data de referência inválida.");
    }

    private static TaxaEconomicaDto MapToDto(TaxaEconomica rate) => new()
    {
        Id = rate.Id,
        Name = rate.Name,
        Symbol = rate.Symbol,
        CurrentValue = rate.CurrentValue,
        PreviousValue = rate.PreviousValue,
        Variation = rate.Variation,
        Description = rate.Description,
        Source = rate.Source,
        LastUpdate = rate.LastUpdate.ToString("yyyy-MM-dd"),
        GrupoId = rate.GroupId,
        Unit = rate.Unit,
        Periodicity = rate.Periodicity,
        ReferenceDate = rate.ReferenceDate,
        UpdatedByUserId = rate.UpdatedByUserId
    };
}
