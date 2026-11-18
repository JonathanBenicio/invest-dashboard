using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.MarketData;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/investments")]
public sealed class AvaliacaoTesouroController(IAvaliacaoTesouroAppService avaliacao) : ControllerBase
{
    [HttpGet("{posicaoId:guid}/tesouro-direto/avaliacao")]
    public async Task<ActionResult<RespostaApi<AvaliacaoPosicaoTesouroDto>>> Consultar(
        Guid posicaoId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var resultado = await avaliacao.ConsultarAsync(posicaoId, cancellationToken);
        return resultado is null
            ? NotFound(new RespostaApi<AvaliacaoPosicaoTesouroDto>(null!, false, "Avaliação Tesouro indisponível para esta posição."))
            : Ok(new RespostaApi<AvaliacaoPosicaoTesouroDto>(resultado));
    }
}
