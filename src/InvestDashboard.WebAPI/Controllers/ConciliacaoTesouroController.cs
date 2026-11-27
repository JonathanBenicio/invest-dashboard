using System.Text.Json;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Application.Services;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/portfolios/{carteiraId:guid}/tesouro-direto/lotes/{loteId:guid}/conciliacao")]
public sealed class ConciliacaoTesouroController(ICarteiraRepository carteiras, IUsuarioAtualService usuario,
    IRepositorioLoteImportacaoTesouro lotes, ITransacaoRepository transacoes, IBasePosicaoTesouroRepository bases,
    InvestDbContext db) : ControllerBase
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
            var temOperacaoManualAnterior = inicial is not null &&
                (await transacoes.GetByPortfolioIdAsync(carteiraId, cancellationToken)).Any(transacao =>
                    string.Equals(transacao.Ticker, inicial.Ticker, StringComparison.OrdinalIgnoreCase) &&
                    (transacao.Type is TipoTransacao.Buy or TipoTransacao.Sell) &&
                    LotesTesouroController.DataCivil(transacao.TransactionDate) <= lote.DataReferencia);
            var decisoesAtuais = await db.Set<DecisaoConciliacaoTesouro>().AsNoTracking()
                .Where(decisao => decisao.CarteiraId == carteiraId && decisao.LoteId == loteId)
                .GroupBy(decisao => decisao.IndiceEvento)
                .Select(grupo => grupo.OrderByDescending(decisao => decisao.Revisao).First())
                .ToListAsync(cancellationToken);
            var possuiVinculoManualAnterior = decisoesAtuais.Any(decisao => decisao.TransacaoId.HasValue);
            var podeCriar = lote.Estado == "Pendente" && inicial is not null && !jaExiste && posicaoInicialDisponivel &&
                !temOperacaoManualAnterior && !possuiVinculoManualAnterior &&
                previa.Movimentacoes.All(m => ConfirmacaoTesouro.EventoCompleto(m.DadosInterpretados, lote.DataReferencia));
            var motivosCriacao = podeCriar ? Array.Empty<string>() :
                ConfirmacaoTesouro.MotivosCriacaoPosicaoInicial(previa, lote.DataReferencia, lote.Estado, jaExiste);
            if (temOperacaoManualAnterior)
                motivosCriacao = [.. motivosCriacao, "Há operações manuais deste título anteriores à referência do extrato; reconcilie-as antes de criar uma posição a partir do HTML."];
            if (possuiVinculoManualAnterior)
                motivosCriacao = [.. motivosCriacao, "Uma revisão anterior vinculou um movimento a uma operação manual de outra posição. Confira a operação manual na carteira. Se o vínculo estiver incorreto, exclua esta prévia e envie o HTML novamente; a exclusão remove as revisões da prévia. Se o vínculo estiver correto, a operação precisa ser conciliada antes de criar o título."];
            return Ok(new RespostaApi<ConciliacaoTesouroDto>(resultado with
            {
                PodeConfirmar = podeCriar,
                PodeCriarPosicao = podeCriar,
                CriarPosicao = true,
                PosicaoPropostaDisponivel = posicaoInicialDisponivel,
                MotivosCriacaoPosicao = motivosCriacao,
                ImportacaoInicialSemRevisao = podeCriar && lote.DataReferencia <= LotesTesouroController.DataCivil(DateTime.UtcNow) &&
                    await ImportacaoInicialTesouro.CarteiraDisponivelAsync(db, carteiraId, loteId, cancellationToken),
                CriacaoPosicaoSemRevisao = podeCriar
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
        var revisoesVigentes = await db.Set<DecisaoConciliacaoTesouro>().AsNoTracking()
            .Where(item => item.CarteiraId == carteiraId && item.LoteId == loteId && item.AtivoId == ativoId)
            .GroupBy(item => item.IndiceEvento)
            .Select(grupo => grupo.OrderByDescending(item => item.Revisao).First())
            .ToDictionaryAsync(item => item.IndiceEvento, cancellationToken);
        var encerramento = previa.Posicoes.Count == 0 && ativa is not null
            ? CalculadorEncerramentoTesouro.Avaliar(previa, lote.DataReferencia, ativa, previaAnterior,
                historico.Where(item => item.AtivoId == ativoId).ToArray(), revisoesVigentes)
            : null;
        var posicaoPropostaDisponivel = ConfirmacaoTesouro.TentarCalcularSaldo(previa, lote.DataReferencia, out var saldoCalculado);
        var encerramentoComprovado = encerramento?.PodeEncerrar == true;
        decimal? saldoAtual = encerramentoComprovado ? 0m : posicaoPropostaDisponivel ? saldoCalculado.Quantidade : null;
        var comparacaoSnapshot = ComparadorSnapshotTesouro.Preparar(loteId, lote.HashConteudo, carteira.Version,
            lote.DataReferencia, previa, ativa, previaAnterior,
            historico.Where(item => item.AtivoId == ativoId).ToArray(), saldoAtual, encerramento);
        var podeConfirmar = lote.Estado == "Pendente" && ConfirmacaoTesouro.TituloCompativel(previa, ativo) &&
            (ativa is null || ativa.DataReferencia < lote.DataReferencia) &&
            (posicaoPropostaDisponivel || encerramentoComprovado) &&
            previa.Movimentacoes.All(m => ConfirmacaoTesouro.EventoCompleto(m.DadosInterpretados, lote.DataReferencia));
        return Ok(new RespostaApi<ConciliacaoTesouroDto>(resultado with
        {
            PodeConfirmar = podeConfirmar,
            DataBaseAtiva = ativa?.DataReferencia,
            ComparacaoSnapshot = comparacaoSnapshot,
            PosicaoPropostaDisponivel = posicaoPropostaDisponivel || encerramentoComprovado,
            EncerramentoPosicao = encerramento
        }));
    }
}

internal static class ImportacaoInicialTesouro
{
    internal static Task<bool> CarteiraDisponivelAsync(InvestDbContext db, Guid carteiraId, Guid loteId, CancellationToken ct) =>
        db.Portfolios.AnyAsync(c => c.Id == carteiraId &&
            !db.AssetPositions.Any(p => p.CarteiraId == c.Id) &&
            !db.Transactions.Any(t => t.CarteiraId == c.Id) &&
            !db.Set<BasePosicaoTesouro>().Any(b => b.CarteiraId == c.Id) &&
            !db.Set<EventoOficialTesouro>().Any(e => e.CarteiraId == c.Id) &&
            !db.Set<LoteImportacaoTesouro>().Any(l => l.CarteiraId == c.Id && l.Estado == "Confirmado") &&
            !db.Set<DecisaoConciliacaoTesouro>().Any(d => d.CarteiraId == c.Id && d.LoteId == loteId), ct);
}
