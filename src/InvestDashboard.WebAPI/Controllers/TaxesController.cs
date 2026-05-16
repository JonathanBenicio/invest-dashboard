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
public class TaxesController : ControllerBase
{
    private readonly ITaxesAppService _taxesAppService;

    public TaxesController(ITaxesAppService taxesAppService)
    {
        _taxesAppService = taxesAppService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetAll()
    {
        var rates = await _taxesAppService.GetAllAsync();
        return Ok(new ApiResponse<object>(rates, true));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EconomicRateDto>>> GetById(Guid id)
    {
        var rate = await _taxesAppService.GetByIdAsync(id);
        if (rate == null)
            return NotFound(new ApiResponse<EconomicRateDto>(null!, false, "Taxa não encontrada"));

        return Ok(new ApiResponse<EconomicRateDto>(rate));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EconomicRateDto>>> Create([FromBody] CreateEconomicRateDto dto)
    {
        var rate = await _taxesAppService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = rate.Id }, new ApiResponse<EconomicRateDto>(rate));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EconomicRateDto>>> Update(Guid id, [FromBody] UpdateEconomicRateDto dto)
    {
        var rate = await _taxesAppService.UpdateAsync(id, dto);
        if (rate == null)
            return NotFound(new ApiResponse<EconomicRateDto>(null!, false, "Taxa não encontrada"));

        return Ok(new ApiResponse<EconomicRateDto>(rate));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var deleted = await _taxesAppService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new ApiResponse<object>(null!, false, "Taxa não encontrada"));

        return Ok(new ApiResponse<object>(null!, true, "Taxa removida com sucesso"));
    }
}
