using System.Text.Json;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Application.Services;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/portfolios/{carteiraId:guid}/tesouro-direto/posicoes/{ativoId:guid}/extrato")]
public sealed class ExtratoTesouroController(
    ICarteiraRepository carteiras,
    IUsuarioAtualService usuario,
    IBasePosicaoTesouroRepository bases,
    IRepositorioLoteImportacaoTesouro lotes,
    IEventoOficialTesouroRepository eventos,
    InvestDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespostaApi<ExtratoTesouroDto>>> Obter(
        Guid carteiraId, Guid ativoId, CancellationToken cancellationToken,
        [FromQuery] Guid? loteId = null,
        [FromQuery] int depoisDoIndice = -1,
        [FromQuery] int limite = 50)
    {
        Response.Headers.CacheControl = "no-store";
        if (loteId == Guid.Empty || ativoId == Guid.Empty || depoisDoIndice < -1 || limite is < 1 or > 100)
            return BadRequest();

        if (usuario.UserId is not Guid autor ||
            await carteiras.GetByIdForUserAsync(carteiraId, autor.ToString(), cancellationToken) is null)
            return NotFound();

        var historicoBases = await bases.ListarHistoricoRecenteAsync(carteiraId, ativoId, cancellationToken);
        var baseAtiva = historicoBases.SingleOrDefault(basePosicao => basePosicao.Ativa);
        if (baseAtiva is null)
        {
            if (loteId.HasValue) return BaseAlterada();
            return Ok(new RespostaApi<ExtratoTesouroDto>(new ExtratoTesouroDto(null, [], null, null)));
        }

        if (loteId.HasValue && loteId.Value != baseAtiva.LoteId)
            return BaseAlterada();

        var loteAtivo = await lotes.BuscarConfirmadoPorCarteiraEIdAsync(carteiraId, baseAtiva.LoteId, cancellationToken);
        if (loteAtivo?.Estado != "Confirmado" || loteAtivo.ConfirmadoEmUtc is not DateTime confirmadoEmUtc)
            return NotFound();

        var eventosAtivos = await eventos.ListarPorAtivoAsync(carteiraId, baseAtiva.LoteId, ativoId,
            depoisDoIndice, limite, cancellationToken);
        int? proximoIndice = null;
        if (eventosAtivos.Count == limite)
        {
            var ultimoIndice = eventosAtivos[^1].IndiceEvento;
            var existeProximaPagina = await eventos.ListarPorAtivoAsync(carteiraId, baseAtiva.LoteId, ativoId,
                ultimoIndice, 1, cancellationToken);
            if (existeProximaPagina.Count > 0) proximoIndice = ultimoIndice;
        }

        var baseAnterior = historicoBases.FirstOrDefault(item => item.Id != baseAtiva.Id);
        var loteAnterior = baseAnterior is null
            ? null
            : await lotes.BuscarConfirmadoPorCarteiraEIdAsync(carteiraId, baseAnterior.LoteId, cancellationToken);
        var revisaoJson = await db.Set<RevisaoSnapshotTesouro>().AsNoTracking()
            .Where(item => item.CarteiraId == carteiraId && item.AtivoId == ativoId &&
                item.LoteId == loteAtivo.Id && item.Decisao == "aceitar_nova_base")
            .OrderByDescending(item => item.CriadoEmUtc)
            .Select(item => item.DivergenciasJson)
            .FirstOrDefaultAsync(cancellationToken);
        var comparacaoPersistida = LerComparacao(revisaoJson);
        var resumo = ResumoUltimaImportacaoTesouro.Preparar(baseAtiva, loteAtivo, baseAnterior,
            loteAnterior, comparacaoPersistida);
        var dados = new ExtratoTesouroDto(
            new BaseAtivaExtratoTesouroDto(baseAtiva.LoteId, baseAtiva.DataReferencia, confirmadoEmUtc,
                baseAtiva.Quantidade, baseAtiva.CustoTotal, baseAtiva.PrecoUnitario),
            eventosAtivos.Select(evento => new EventoOficialExtratoTesouroDto(
                evento.LoteId, evento.IndiceEvento, evento.TransacaoId, "extrato_oficial_tesouro",
                LerEvento(evento.EventoJson))).ToArray(),
            proximoIndice,
            resumo);

        return Ok(new RespostaApi<ExtratoTesouroDto>(dados));
    }

    private ActionResult<RespostaApi<ExtratoTesouroDto>> BaseAlterada() =>
        Conflict(new RespostaApi<ExtratoTesouroDto>(null!, false,
            "A base ativa do extrato mudou. Atualize os detalhes do investimento."));

    private static MovimentoPreviaTesouroDto? LerEvento(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<MovimentoPreviaTesouroDto>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ComparacaoSnapshotTesouroDto? LerComparacao(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var comparacao = JsonSerializer.Deserialize<ComparacaoSnapshotTesouroDto>(json);
            return comparacao?.Divergencias is null ? null : comparacao;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
