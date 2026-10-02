using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/grupos-carteiras/{grupoId:guid}/titulares")]
public sealed class TitularesController(TitularesAppService service, IUsuarioAtualService usuarioAtual) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespostaApi<IReadOnlyList<TitularCarteiraDto>>>> Listar(Guid grupoId) =>
        Ok(new RespostaApi<IReadOnlyList<TitularCarteiraDto>>(await service.ListarAsync(grupoId, UserId())));

    [HttpPost]
    public async Task<ActionResult<RespostaApi<TitularCarteiraDto>>> Criar(Guid grupoId, SalvarTitularCarteiraDto request) =>
        Ok(new RespostaApi<TitularCarteiraDto>(await service.SalvarAsync(grupoId, null, request, UserId())));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RespostaApi<TitularCarteiraDto>>> Atualizar(Guid grupoId, Guid id, SalvarTitularCarteiraDto request) =>
        Ok(new RespostaApi<TitularCarteiraDto>(await service.SalvarAsync(grupoId, id, request, UserId())));

    private string UserId() => usuarioAtual.UserId?.ToString() ?? string.Empty;
}
