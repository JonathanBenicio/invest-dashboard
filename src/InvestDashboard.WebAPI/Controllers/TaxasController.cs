using System;
using System.Threading.Tasks;
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

    public TaxasController(ITaxasAppService taxasAppService)
    {
        _taxasAppService = taxasAppService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetAll()
    {
        var taxas = await _taxasAppService.GetAllAsync();
        return Ok(new ApiResponse<object>(taxas, true));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TaxaEconomicaDto>>> GetById(Guid id)
    {
        var taxa = await _taxasAppService.GetByIdAsync(id);
        if (taxa == null)
            return NotFound(new ApiResponse<TaxaEconomicaDto>(null!, false, "Taxa não encontrada"));

        return Ok(new ApiResponse<TaxaEconomicaDto>(taxa));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TaxaEconomicaDto>>> Create([FromBody] CriarTaxaEconomicaDto dto)
    {
        var taxa = await _taxasAppService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = taxa.Id }, new ApiResponse<TaxaEconomicaDto>(taxa));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TaxaEconomicaDto>>> Update(Guid id, [FromBody] AtualizarTaxaEconomicaDto dto)
    {
        var taxa = await _taxasAppService.UpdateAsync(id, dto);
        if (taxa == null)
            return NotFound(new ApiResponse<TaxaEconomicaDto>(null!, false, "Taxa não encontrada"));

        return Ok(new ApiResponse<TaxaEconomicaDto>(taxa));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var deleted = await _taxasAppService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new ApiResponse<object>(null!, false, "Taxa não encontrada"));

        return Ok(new ApiResponse<object>(null!, true, "Taxa removida com sucesso"));
    }
}
