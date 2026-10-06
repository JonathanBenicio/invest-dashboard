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
    public async Task<ActionResult<RespostaApi<IReadOnlyList<CotacaoMercadoDto>>>> GetQuotes(
        [FromQuery(Name = "simbolos"), Required] string symbols,
        CancellationToken cancellationToken)
    {
        var tickers = symbols.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tickers.Length is 0 or > 100)
            return BadRequest(new RespostaApi<object>(null!, false, "Request between 1 and 100 symbols."));

        var quotes = await marketData.GetQuotesAsync(tickers, cancellationToken);
        return Ok(new RespostaApi<IReadOnlyList<CotacaoMercadoDto>>(quotes));
    }

    [HttpGet("search")]
    public async Task<ActionResult<RespostaApi<IReadOnlyList<ResultadoBuscaMercadoDto>>>> Search(
        [FromQuery(Name = "consulta"), Required, StringLength(80, MinimumLength = 2)] string query,
        CancellationToken cancellationToken)
    {
        var result = await marketData.SearchAsync(query, cancellationToken);
        return Ok(new RespostaApi<IReadOnlyList<ResultadoBuscaMercadoDto>>(result));
    }

    [HttpGet("history")]
    public async Task<ActionResult<RespostaApi<IReadOnlyList<PontoHistoricoMercadoDto>>>> GetHistory(
        [FromQuery(Name = "simbolos"), Required] string symbols,
        [FromQuery(Name = "dataInicio"), Required] DateOnly startDate,
        [FromQuery(Name = "dataFim"), Required] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var tickers = symbols.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tickers.Length is 0 or > 100)
            return BadRequest(new RespostaApi<object>(null!, false, "Request between 1 and 100 symbols."));

        var history = await marketData.GetDailyHistoryAsync(tickers, startDate, endDate, cancellationToken);
        return Ok(new RespostaApi<IReadOnlyList<PontoHistoricoMercadoDto>>(history));
    }
}
