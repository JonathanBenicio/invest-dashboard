using System.ComponentModel.DataAnnotations;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.MarketData;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/tesouro-direto")]
public sealed class TesouroDiretoCotacoesController(ITesouroTransparenteConsulta consulta) : ControllerBase
{
    [HttpGet("titulos")]
    public async Task<ActionResult<RespostaApi<CatalogoTesouroDto>>> ListarTitulos(CancellationToken cancellationToken)
        => Ok(new RespostaApi<CatalogoTesouroDto>(await consulta.ListarTitulosAsync(cancellationToken)));

    [HttpGet("cotacoes")]
    public async Task<ActionResult<RespostaApi<SerieCotacoesTesouroDto>>> Cotacoes(
        [FromQuery, Required, StringLength(100)] string tipoTitulo,
        [FromQuery, Required] DateOnly? dataVencimento,
        [FromQuery, Required] DateOnly? dataDe,
        [FromQuery, Required] DateOnly? dataAte,
        CancellationToken cancellationToken)
        => Ok(new RespostaApi<SerieCotacoesTesouroDto>(await consulta.ConsultarAsync(
            tipoTitulo, dataVencimento!.Value, dataDe!.Value, dataAte!.Value, cancellationToken)));
}
