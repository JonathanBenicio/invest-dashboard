using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Taxes;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/taxes")]
public class TaxasController : ControllerBase
{
    private readonly ITaxasAppService _taxasAppService;
    private readonly IEstimativaImpostoAppService _estimativaImposto;

    public TaxasController(ITaxasAppService taxasAppService, IEstimativaImpostoAppService estimativaImposto)
    {
        _taxasAppService = taxasAppService;
        _estimativaImposto = estimativaImposto;
    }

    [HttpGet("estimativa-mensal")]
    public async Task<ActionResult<RespostaApi<EstimativaImpostoMensalDto>>> GetMonthlyEstimate(
        [FromQuery, Range(2000, 2100)] int ano,
        [FromQuery, Range(1, 12)] int mes,
        [FromQuery(Name = "grupoId"), Required] Guid grupoId,
        [FromQuery, StringLength(160)] string? titular,
        [FromQuery(Name = "titularId")] Guid? titularId)
    {
        var estimate = await _estimativaImposto.CalcularMensalAsync(ano, mes, grupoId, titular, titularId);
        return Ok(new RespostaApi<EstimativaImpostoMensalDto>(estimate));
    }

    [HttpGet]
    public async Task<ActionResult<RespostaApi<List<TaxaEconomicaDto>>>> GetAll([FromQuery(Name = "grupoId"), Required] Guid grupoId)
    {
        var taxas = await _taxasAppService.GetAllAsync(grupoId, UserId());
        return Ok(new RespostaApi<List<TaxaEconomicaDto>>(taxas, true));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RespostaApi<TaxaEconomicaDto>>> GetById(Guid id, [FromQuery(Name = "grupoId"), Required] Guid grupoId)
    {
        var taxa = await _taxasAppService.GetByIdAsync(id, grupoId, UserId());
        if (taxa == null)
            return NotFound(new RespostaApi<TaxaEconomicaDto>(null!, false, "Taxa não encontrada"));

        return Ok(new RespostaApi<TaxaEconomicaDto>(taxa));
    }

    [HttpGet("{id:guid}/historico")]
    public async Task<ActionResult<RespostaApi<IReadOnlyList<TaxaEconomicaHistoricoDto>>>> GetHistory(
        Guid id, [FromQuery(Name = "grupoId"), Required] Guid grupoId)
    {
        var history = await _taxasAppService.GetHistoryAsync(id, grupoId, UserId());
        return history is null
            ? NotFound(new RespostaApi<IReadOnlyList<TaxaEconomicaHistoricoDto>>(null!, false, "Taxa não encontrada"))
            : Ok(new RespostaApi<IReadOnlyList<TaxaEconomicaHistoricoDto>>(history));
    }

    [HttpPost]
    public async Task<ActionResult<RespostaApi<TaxaEconomicaDto>>> Create(
        [FromBody] CriarTaxaEconomicaDto dto, [FromQuery(Name = "grupoId"), Required] Guid grupoId)
    {
        var taxa = await _taxasAppService.CreateAsync(dto, grupoId, UserId());
        return CreatedAtAction(nameof(GetById), new { id = taxa.Id, grupoId }, new RespostaApi<TaxaEconomicaDto>(taxa));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RespostaApi<TaxaEconomicaDto>>> Update(
        Guid id, [FromBody] AtualizarTaxaEconomicaDto dto, [FromQuery(Name = "grupoId"), Required] Guid grupoId)
    {
        var taxa = await _taxasAppService.UpdateAsync(id, dto, grupoId, UserId());
        if (taxa == null)
            return NotFound(new RespostaApi<TaxaEconomicaDto>(null!, false, "Taxa não encontrada"));

        return Ok(new RespostaApi<TaxaEconomicaDto>(taxa));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<RespostaApi<object>>> Delete(Guid id, [FromQuery(Name = "grupoId"), Required] Guid grupoId)
    {
        var deleted = await _taxasAppService.DeleteAsync(id, grupoId, UserId());
        if (!deleted)
            return NotFound(new RespostaApi<object>(null!, false, "Taxa não encontrada"));

        return Ok(new RespostaApi<object>(null!, true, "Taxa removida com sucesso"));
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
}
