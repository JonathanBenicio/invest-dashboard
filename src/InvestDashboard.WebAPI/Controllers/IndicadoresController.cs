using System.ComponentModel.DataAnnotations;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.MarketData;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/indicadores")]
public sealed class IndicadoresController(IIndicadoresBcbProvider provider) : ControllerBase
{
    [HttpGet("selic")]
    public async Task<ActionResult<RespostaApi<SerieIndicadorBcbDto>>> GetSelic(
        [FromQuery, Required] DateOnly? dataDe, [FromQuery, Required] DateOnly? dataAte, CancellationToken cancellationToken)
    {
        var serie = await provider.GetSelicAsync(dataDe!.Value, dataAte!.Value, cancellationToken);
        return Ok(new RespostaApi<SerieIndicadorBcbDto>(serie));
    }

    [HttpGet("ipca")]
    public async Task<ActionResult<RespostaApi<SerieIndicadorBcbDto>>> GetIpca(
        [FromQuery, Required] DateOnly? dataDe, [FromQuery, Required] DateOnly? dataAte, CancellationToken cancellationToken)
    {
        var serie = await provider.GetIpcaAsync(dataDe!.Value, dataAte!.Value, cancellationToken);
        return Ok(new RespostaApi<SerieIndicadorBcbDto>(serie));
    }
}
