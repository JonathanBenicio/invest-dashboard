using System.Text.Json;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Application.Services;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/portfolios/{carteiraId:guid}/tesouro-direto/lotes/{loteId:guid}/conciliacao")]
public sealed class ConciliacaoTesouroController(ICarteiraRepository carteiras, IUsuarioAtualService usuario,
    IRepositorioLoteImportacaoTesouro lotes, ITransacaoRepository transacoes) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RespostaApi<ConciliacaoTesouroDto>>> Preparar(
        Guid carteiraId, Guid loteId, [FromBody] SolicitarConciliacaoTesouroDto solicitacao, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var usuarioId = usuario.UserId;
        if (usuarioId is null) return NotFound();
        var id = usuarioId.Value.ToString();
        var carteira = await carteiras.GetByIdForUserAsync(carteiraId, id, cancellationToken);
        if (carteira is null || !await carteiras.CanManageAsync(carteiraId, id, cancellationToken)) return NotFound();
        var lote = await lotes.BuscarPorIdAsync(carteiraId, usuarioId.Value, loteId, cancellationToken);
        if (lote is null) return NotFound();
        if (solicitacao.AtivoId.HasValue && !carteira.Positions.Any(p => p.AtivoId == solicitacao.AtivoId.Value && p.TipoAtivo == TipoAtivo.RendaFixa)) return NotFound();
        var previa = JsonSerializer.Deserialize<PreviaTesouroDto>(lote.PreviaJson) ?? throw new InvalidOperationException("Prévia indisponível.");
        var historico = solicitacao.AtivoId.HasValue ? await transacoes.GetByPortfolioIdAsync(carteiraId, cancellationToken) : [];
        return Ok(new RespostaApi<ConciliacaoTesouroDto>(ConciliaTesouro.Preparar(carteira, lote.DataReferencia, previa, solicitacao.AtivoId, historico)));
    }
}
