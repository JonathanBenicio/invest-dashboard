using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.MarketData;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/benchmarks")]
public sealed class BenchmarksController(IBenchmarkCdiProvider cdiProvider) : ControllerBase
{
    [HttpGet("cdi")]
    public async Task<ActionResult<RespostaApi<SerieBenchmarkCdiDto>>> GetCdi(
        [FromQuery(Name = "dataDe")] DateOnly dataDe,
        [FromQuery(Name = "dataAte")] DateOnly dataAte,
        CancellationToken cancellationToken)
    {
        var serie = await cdiProvider.GetCdiAsync(dataDe, dataAte, cancellationToken);
        return Ok(new RespostaApi<SerieBenchmarkCdiDto>(serie));
    }
}
