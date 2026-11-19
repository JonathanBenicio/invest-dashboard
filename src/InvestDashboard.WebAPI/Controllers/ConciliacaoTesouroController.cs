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
    IRepositorioLoteImportacaoTesouro lotes, ITransacaoRepository transacoes, IBasePosicaoTesouroRepository bases) : ControllerBase
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
        var resultado = ConciliaTesouro.Preparar(carteira, lote.DataReferencia, previa, solicitacao.AtivoId, historico);
        if (solicitacao.AtivoId is not Guid ativoId)
        {
            var inicial = ConfirmacaoTesouro.CriarAtivoInicial(previa, 1m);
            var jaExiste = inicial is not null && carteira.Positions.Any(p =>
                string.Equals(p.Ticker, inicial.Ticker, StringComparison.OrdinalIgnoreCase) ||
                ConfirmacaoTesouro.TituloCompativel(previa, p.Ativo as RendaFixa));
            var posicaoInicialDisponivel = ConfirmacaoTesouro.TentarCalcularSaldo(previa, lote.DataReferencia, out _);
            var podeCriar = lote.Estado == "Pendente" && inicial is not null && !jaExiste && posicaoInicialDisponivel &&
                previa.Movimentacoes.All(m => ConfirmacaoTesouro.EventoCompleto(m.DadosInterpretados, lote.DataReferencia));
            var motivosCriacao = podeCriar ? Array.Empty<string>() :
                ConfirmacaoTesouro.MotivosCriacaoPosicaoInicial(previa, lote.DataReferencia, lote.Estado, jaExiste);
            return Ok(new RespostaApi<ConciliacaoTesouroDto>(resultado with
            {
                PodeConfirmar = podeCriar,
                PodeCriarPosicao = podeCriar,
                CriarPosicao = true,
                PosicaoPropostaDisponivel = posicaoInicialDisponivel,
                MotivosCriacaoPosicao = motivosCriacao
            }));
        }
        var ativo = carteira.Positions.SingleOrDefault(p => p.AtivoId == ativoId)?.Ativo as RendaFixa;
        var ativa = (await bases.GetAtivasAsync(carteiraId, cancellationToken)).SingleOrDefault(b => b.AtivoId == ativoId);
        var loteAnterior = ativa is null ? null : await lotes.BuscarPorIdAsync(carteiraId, usuarioId.Value, ativa.LoteId, cancellationToken);
        PreviaTesouroDto? previaAnterior = null;
        if (loteAnterior is not null)
        {
            try { previaAnterior = JsonSerializer.Deserialize<PreviaTesouroDto>(loteAnterior.PreviaJson); }
            catch (JsonException) { previaAnterior = null; }
        }
        var posicaoPropostaDisponivel = ConfirmacaoTesouro.TentarCalcularSaldo(previa, lote.DataReferencia, out var saldoCalculado);
        decimal? saldoAtual = posicaoPropostaDisponivel ? saldoCalculado.Quantidade : null;
        var comparacaoSnapshot = ComparadorSnapshotTesouro.Preparar(loteId, lote.HashConteudo, carteira.Version,
            lote.DataReferencia, previa, ativa, previaAnterior,
            historico.Where(item => item.AtivoId == ativoId).ToArray(), saldoAtual);
        var podeConfirmar = lote.Estado == "Pendente" && ConfirmacaoTesouro.TituloCompativel(previa, ativo) &&
            (ativa is null || ativa.DataReferencia < lote.DataReferencia) &&
            posicaoPropostaDisponivel &&
            previa.Movimentacoes.All(m => ConfirmacaoTesouro.EventoCompleto(m.DadosInterpretados, lote.DataReferencia));
        return Ok(new RespostaApi<ConciliacaoTesouroDto>(resultado with
        {
            PodeConfirmar = podeConfirmar,
            DataBaseAtiva = ativa?.DataReferencia,
            ComparacaoSnapshot = comparacaoSnapshot,
            PosicaoPropostaDisponivel = posicaoPropostaDisponivel
        }));
    }
}
