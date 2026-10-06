using System.Security.Claims;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/grupos-carteiras")]
public sealed class GruposCarteirasController(IGrupoCarteirasAppService grupos) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespostaApi<IReadOnlyList<GrupoCarteirasDto>>>> Listar(CancellationToken cancellationToken)
    {
        var userId = UserId();
        return Ok(new RespostaApi<IReadOnlyList<GrupoCarteirasDto>>(await grupos.ListarAsync(userId, cancellationToken)));
    }

    [HttpPost]
    public async Task<ActionResult<RespostaApi<GrupoCarteirasDto>>> Criar(
        [FromBody] CriarGrupoCarteirasDto request, CancellationToken cancellationToken)
    {
        var result = await grupos.CriarAsync(request.Nome, UserId(),
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty,
            User.FindFirstValue("name") ?? string.Empty, cancellationToken);
        return Ok(new RespostaApi<GrupoCarteirasDto>(result));
    }

    [HttpGet("{grupoId:guid}/membros")]
    public async Task<ActionResult<RespostaApi<IReadOnlyList<MembroGrupoDto>>>> Membros(Guid grupoId, CancellationToken cancellationToken)
    {
        var result = await grupos.ListarMembrosAsync(grupoId, UserId(), cancellationToken);
        return result is null ? Forbid() : Ok(new RespostaApi<IReadOnlyList<MembroGrupoDto>>(result));
    }

    [HttpPut("{grupoId:guid}/membros/{membroId:guid}/papel")]
    public async Task<ActionResult<RespostaApi<bool>>> AlterarPapel(Guid grupoId, Guid membroId, [FromBody] AlterarPapelGrupoDto request, CancellationToken cancellationToken)
    {
        var result = await grupos.AlterarPapelAsync(grupoId, membroId, request.Papel, UserId(), cancellationToken);
        return result is null ? Forbid() : result.Value ? Ok(new RespostaApi<bool>(true)) : BadRequest(new RespostaApi<bool>(false, false, "Papel inválido ou membro não encontrado."));
    }

    [HttpDelete("{grupoId:guid}/membros/{membroId:guid}")]
    public async Task<ActionResult<RespostaApi<bool>>> Desativar(Guid grupoId, Guid membroId, CancellationToken cancellationToken)
    {
        var result = await grupos.DesativarAsync(grupoId, membroId, UserId(), cancellationToken);
        return result is null ? Forbid() : result.Value ? Ok(new RespostaApi<bool>(true)) : BadRequest(new RespostaApi<bool>(false, false, "Membro não encontrado ou não pode ser desativado."));
    }

    [HttpPost("{grupoId:guid}/convites")]
    public async Task<ActionResult<RespostaApi<ResultadoConviteGrupoDto>>> Convidar(Guid grupoId, [FromBody] ConvidarMembroGrupoDto request, CancellationToken cancellationToken)
    {
        var result = await grupos.ConvidarAsync(grupoId, request.Email, request.Papel, UserId(), cancellationToken);
        return result is null
            ? Forbid()
            : Ok(new RespostaApi<ResultadoConviteGrupoDto>(result));
    }

    [HttpGet("convites-pendentes")]
    public async Task<ActionResult<RespostaApi<IReadOnlyList<ConvitePendenteGrupoDto>>>> ConvitesPendentes(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;
        var result = await grupos.ListarConvitesPendentesAsync(email, cancellationToken);
        return Ok(new RespostaApi<IReadOnlyList<ConvitePendenteGrupoDto>>(result));
    }

    [HttpPost("{grupoId:guid}/convites/{conviteId:guid}/aceitar")]
    public async Task<ActionResult<RespostaApi<bool>>> AceitarConvitePendente(Guid grupoId, Guid conviteId, CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;
        var name = User.FindFirstValue("name") ?? string.Empty;
        var result = await grupos.AceitarConviteAsync(grupoId, conviteId, UserId(), email, name, cancellationToken);
        return result is null
            ? Forbid()
            : result.Value ? Ok(new RespostaApi<bool>(true)) : BadRequest(new RespostaApi<bool>(false, false, "Convite expirado ou inválido."));
    }


    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
}
