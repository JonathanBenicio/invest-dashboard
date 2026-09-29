using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/portfolios")]
public sealed class CarteirasController(ICarteiraAppService portfolioService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CarteiraDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await portfolioService.GetUserPortfoliosAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CarteiraDto>>> GetById(Guid id)
    {
        var portfolio = await portfolioService.GetPortfolioByIdAsync(id);
        return portfolio is null
            ? NotFound(new ApiResponse<CarteiraDto>(null!, false, "Portfolio not found."))
            : Ok(new ApiResponse<CarteiraDto>(portfolio));
    }

    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<ApiResponse<ResumoCarteiraDto>>> GetSummary(Guid id)
    {
        var portfolio = await portfolioService.GetPortfolioByIdAsync(id);
        if (portfolio is null)
            return NotFound(new ApiResponse<ResumoCarteiraDto>(null!, false, "Portfolio not found."));

        var allocation = portfolio.Positions
            .GroupBy(position => (position.Type, position.Subtype))
            .Select(group =>
            {
                var value = group.Sum(position => position.CurrentValue);
                var percentage = portfolio.TotalValue > 0 ? value / portfolio.TotalValue * 100 : 0;
                return new AlocacaoAtivoDto
                {
                    Category = group.Key.Subtype,
                    Value = value,
                    Percentage = percentage,
                    Color = group.Key.Type == "fixed_income" ? "#f59e0b" : "#3b82f6"
                };
            })
            .ToList();

        var summary = new ResumoCarteiraDto
        {
            Id = portfolio.Id,
            Name = portfolio.Name,
            Description = portfolio.Description,
            TotalValue = portfolio.TotalValue,
            TotalInvested = portfolio.TotalInvested,
            TotalGain = portfolio.TotalGain,
            UnrealizedGain = portfolio.UnrealizedGain,
            RealizedGain = portfolio.RealizedGain,
            GainPercentage = portfolio.GainPercentage,
            Currency = portfolio.Currency,
            Positions = portfolio.Positions,
            AssetsCount = portfolio.AssetsCount,
            AssetAllocation = allocation,
            PerformanceHistory = []
        };

        return Ok(new ApiResponse<ResumoCarteiraDto>(summary));
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PontoHistoricoCarteiraDto>>>> GetHistory(
        Guid id,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var points = await portfolioService.GetPortfolioHistoryAsync(id, fromDate, toDate);
        return points is null
            ? NotFound(new ApiResponse<IReadOnlyList<PontoHistoricoCarteiraDto>>(null!, false, "Portfolio not found."))
            : Ok(new ApiResponse<IReadOnlyList<PontoHistoricoCarteiraDto>>(points));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CarteiraDto>>> Create([FromBody] CriarCarteiraDto request)
    {
        var portfolio = await portfolioService.CreatePortfolioAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = portfolio.Id }, new ApiResponse<CarteiraDto>(portfolio));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CarteiraDto>>> Update(Guid id, [FromBody] AtualizarCarteiraDto request)
    {
        var portfolio = await portfolioService.UpdatePortfolioAsync(id, request);
        return portfolio is null
            ? NotFound(new ApiResponse<CarteiraDto>(null!, false, "Portfolio not found."))
            : Ok(new ApiResponse<CarteiraDto>(portfolio));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var deleted = await portfolioService.DeletePortfolioAsync(id);
        return deleted
            ? Ok(new ApiResponse<bool>(true, true, "Portfolio deleted."))
            : NotFound(new ApiResponse<bool>(false, false, "Portfolio not found."));
    }
}
