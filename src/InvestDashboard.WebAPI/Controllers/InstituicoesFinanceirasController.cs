using System.ComponentModel.DataAnnotations;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/instituicoes-financeiras")]
public sealed class InstituicoesFinanceirasController(
    IInstituicoesFinanceirasAppService instituicoes,
    IUsuarioAtualService usuarioAtual) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespostaApi<IReadOnlyList<InstituicaoFinanceiraDto>>>> Listar(
        [FromQuery(Name = "grupoId")] Guid? grupoId, CancellationToken cancellationToken)
    {
        var list = await instituicoes.ListarAsync(grupoId, UserId(), cancellationToken);
        return Ok(new RespostaApi<IReadOnlyList<InstituicaoFinanceiraDto>>(list));
    }

    [HttpPost]
    public async Task<ActionResult<RespostaApi<InstituicaoFinanceiraDto>>> CriarOutra(
        [FromQuery(Name = "grupoId"), Required] Guid grupoId,
        [FromBody] CriarInstituicaoPersonalizadaDto request,
        CancellationToken cancellationToken)
    {
        var created = await instituicoes.CriarOutraAsync(grupoId, request.Nome, UserId(), cancellationToken);
        return CreatedAtAction(nameof(Listar), new { grupoId }, new RespostaApi<InstituicaoFinanceiraDto>(created));
    }

    private string UserId() => usuarioAtual.UserId?.ToString() ?? string.Empty;
}
