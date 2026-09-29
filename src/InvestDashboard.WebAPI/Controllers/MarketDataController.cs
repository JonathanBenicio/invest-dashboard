using System.ComponentModel.DataAnnotations;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.MarketData;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/market-data")]
public sealed class MarketDataController(IMarketDataProvider marketData) : ControllerBase
{
    [HttpGet("quotes")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MarketQuoteDto>>>> GetQuotes(
        [FromQuery, Required] string symbols,
        CancellationToken cancellationToken)
    {
        var tickers = symbols.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tickers.Length is 0 or > 100)
            return BadRequest(new ApiResponse<object>(null!, false, "Request between 1 and 100 symbols."));

        var quotes = await marketData.GetQuotesAsync(tickers, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<MarketQuoteDto>>(quotes));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MarketSearchResultDto>>>> Search(
        [FromQuery, Required, StringLength(80, MinimumLength = 2)] string query,
        CancellationToken cancellationToken)
    {
        var result = await marketData.SearchAsync(query, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<MarketSearchResultDto>>(result));
    }

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MarketHistoryPointDto>>>> GetHistory(
        [FromQuery, Required] string symbols,
        [FromQuery, Required] DateOnly startDate,
        [FromQuery, Required] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var tickers = symbols.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tickers.Length is 0 or > 100)
            return BadRequest(new ApiResponse<object>(null!, false, "Request between 1 and 100 symbols."));

        var history = await marketData.GetDailyHistoryAsync(tickers, startDate, endDate, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<MarketHistoryPointDto>>(history));
    }
}
