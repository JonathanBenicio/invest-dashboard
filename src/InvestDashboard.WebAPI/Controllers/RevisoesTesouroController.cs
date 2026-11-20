using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InvestDashboard.WebAPI.Controllers;

/// <summary>Revisões são propostas auditáveis, sem efeito sobre saldo ou confirmação.</summary>
[Authorize]
[ApiController]
[Route("api/v1/portfolios/{carteiraId:guid}/tesouro-direto/lotes/{loteId:guid}/revisoes")]
public sealed class RevisoesTesouroController(InvestDbContext db, ICarteiraRepository carteiras,
    IUsuarioAtualService usuario, IRepositorioLoteImportacaoTesouro lotes, ITransacaoRepository transacoes) : ControllerBase
{
    private static readonly TimeZoneInfo FusoBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    [HttpGet("{indiceEvento:int}")]
    public async Task<ActionResult<RespostaApi<IReadOnlyList<RevisaoTesouroDto>>>> Historico(
        Guid carteiraId, Guid loteId, int indiceEvento, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var usuarioId = usuario.UserId;
        if (indiceEvento < 0 || usuarioId is null) return NotFound();
        var user = usuarioId.Value.ToString();
        var carteira = await carteiras.GetByIdForUserAsync(carteiraId, user, cancellationToken);
        if (carteira is null || !await carteiras.CanManageAsync(carteiraId, user, cancellationToken)) return NotFound();
        var lote = await lotes.BuscarPorIdAsync(carteiraId, usuarioId.Value, loteId, cancellationToken);
        if (lote is null) return NotFound();
        var previa = JsonSerializer.Deserialize<PreviaTesouroDto>(lote.PreviaJson) ?? throw new InvalidOperationException("Prévia indisponível.");
        if (indiceEvento >= previa.Movimentacoes.Count) return NotFound();
        var historico = await db.Set<DecisaoConciliacaoTesouro>().AsNoTracking()
            .Where(x => x.LoteId == loteId && x.CarteiraId == carteiraId && x.IndiceEvento == indiceEvento)
            .OrderBy(x => x.Revisao).Take(100).ToListAsync(cancellationToken);
        var movimentos = historico.Any(x => x.TransacaoId.HasValue)
            ? (await transacoes.GetByPortfolioIdAsync(carteiraId, cancellationToken)).ToDictionary(x => x.Id)
            : new Dictionary<Guid, Transacao>();
        IReadOnlyList<RevisaoTesouroDto> resultado = historico.Select(x => Mapear(x, carteira.Version,
            previa.Movimentacoes[indiceEvento].DadosInterpretados,
            x.TransacaoId.HasValue && movimentos.TryGetValue(x.TransacaoId.Value, out var manual) ? manual : null)).ToArray();
        return Ok(new RespostaApi<IReadOnlyList<RevisaoTesouroDto>>(resultado));
    }

    [HttpPost]
    public async Task<ActionResult<RespostaApi<RevisaoTesouroDto>>> Registrar(Guid carteiraId, Guid loteId,
        [FromBody] RegistrarRevisaoTesouroDto pedido, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var usuarioId = usuario.UserId;
        if (usuarioId is null) return NotFound();
        if (pedido.IndiceEvento < 0 || pedido.RevisaoAnterior is < 0 or >= 100 || pedido.CarteiraVersao < 0 ||
            pedido.HashPrevia is not { Length: 64 } || pedido.HashPrevia.Any(c => !Uri.IsHexDigit(c)) ||
            pedido.Decisao is not ("vincular" or "sem_vinculo") ||
            (pedido.Decisao == "vincular" && (!pedido.AtivoId.HasValue || !pedido.TransacaoId.HasValue)) ||
            (pedido.Decisao == "sem_vinculo" && pedido.TransacaoId.HasValue)) return BadRequest();
        // Isolamento serializável impede concorrência entre revisões do mesmo
        // evento ou vinculações concorrentes de uma transação a eventos distintos.
        if (!db.Database.IsNpgsql()) return StatusCode(503);
        try
        {
            await using var transacaoDb = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var user = usuarioId.Value.ToString();
            var carteira = await carteiras.GetByIdForUserAsync(carteiraId, user, cancellationToken);
            if (carteira is null || !await carteiras.CanManageAsync(carteiraId, user, cancellationToken)) return NotFound();
            var lote = await lotes.BuscarPorIdAsync(carteiraId, usuarioId.Value, loteId, cancellationToken);
            if (lote is null) return NotFound();
            if (lote.Estado != "Pendente" || carteira.Version != pedido.CarteiraVersao) return Conflict();
            var hashPrevia = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lote.PreviaJson))).ToLowerInvariant();
            if (!string.Equals(hashPrevia, pedido.HashPrevia, StringComparison.Ordinal)) return Conflict();
            var previa = JsonSerializer.Deserialize<PreviaTesouroDto>(lote.PreviaJson);
            if (previa is null || pedido.IndiceEvento >= previa.Movimentacoes.Count) return BadRequest();
            if (pedido.AtivoId.HasValue && !carteira.Positions.Any(p => p.AtivoId == pedido.AtivoId && p.TipoAtivo == TipoAtivo.RendaFixa)) return NotFound();
            var decisoes = db.Set<DecisaoConciliacaoTesouro>();
            var atual = await decisoes.Where(x => x.LoteId == loteId && x.IndiceEvento == pedido.IndiceEvento)
                .OrderByDescending(x => x.Revisao).FirstOrDefaultAsync(cancellationToken);
            if ((atual?.Revisao ?? 0) != pedido.RevisaoAnterior) return Conflict();
            // Cada arquivo HTML representa um título: propostas ativas em outros
            // eventos não podem apontar para ativos diferentes do escolhido.
            if (pedido.AtivoId.HasValue && await decisoes.AnyAsync(x => x.LoteId == loteId && x.IndiceEvento != pedido.IndiceEvento &&
                    x.AtivoId != null && x.AtivoId != pedido.AtivoId &&
                    !decisoes.Any(y => y.LoteId == x.LoteId && y.IndiceEvento == x.IndiceEvento && y.Revisao > x.Revisao), cancellationToken))
                return Conflict();
            Transacao? manual = null;
            var movimento = previa.Movimentacoes[pedido.IndiceEvento].DadosInterpretados;
            if (pedido.Decisao == "vincular")
            {
                var tipo = (movimento?.Natureza, movimento?.Tipo) switch
                {
                    ("Compra", "Crédito") => TipoTransacao.Buy,
                    ("Venda", "Débito") => TipoTransacao.Sell,
                    _ => (TipoTransacao?)null
                };
                if (movimento?.Data is not DateOnly data || data > lote.DataReferencia ||
                    movimento.Quantidade is not decimal quantidade || quantidade <= 0 ||
                    movimento.Preco is not decimal preco || preco <= 0 || movimento.CamposNaoInterpretados is null ||
                    movimento.CamposNaoInterpretados.Any(c => c is "data" or "natureza" or "tipo" or "quantidade" or "preco"))
                    return BadRequest();
                manual = await transacoes.GetByIdAsync(pedido.TransacaoId!.Value, cancellationToken);
                if (manual is null || manual.CarteiraId != carteiraId || manual.AtivoId != pedido.AtivoId ||
                    !tipo.HasValue || manual.Type != tipo || manual.TransactionDate.Kind != DateTimeKind.Utc ||
                    DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(manual.TransactionDate, FusoBrasil)) > lote.DataReferencia)
                    return BadRequest();
                // Apenas a revisão mais recente por evento representa uma proposta ativa.
                var usada = await decisoes.AnyAsync(x => x.LoteId == loteId && x.IndiceEvento != pedido.IndiceEvento &&
                    x.TransacaoId == manual.Id && !decisoes.Any(y => y.LoteId == x.LoteId && y.IndiceEvento == x.IndiceEvento && y.Revisao > x.Revisao), cancellationToken);
                if (usada) return Conflict();
            }
            var registro = new DecisaoConciliacaoTesouro(Guid.NewGuid(), carteiraId, loteId, usuarioId.Value, pedido.AtivoId,
                pedido.IndiceEvento, pedido.RevisaoAnterior + 1, pedido.Decisao, pedido.TransacaoId, carteira.Version, DateTime.UtcNow);
            db.Add(registro);
            await db.SaveChangesAsync(cancellationToken);
            await transacaoDb.CommitAsync(cancellationToken);
            return Ok(new RespostaApi<RevisaoTesouroDto>(Mapear(registro, carteira.Version, movimento, manual)));
        }
        catch (Exception erro) when (ConflitoConcorrencia(erro)) { return Conflict(); }
    }

    // Divergências são calculadas com os dados atuais; o futuro fluxo de
    // confirmação deve revalidar tudo e registrar um snapshot das evidências.
    private static RevisaoTesouroDto Mapear(DecisaoConciliacaoTesouro x, int versaoAtual,
        DadosMovimentoTesouroDto? evento, Transacao? manual)
    {
        var divergencias = new List<string>();
        if (x.CarteiraVersao != versaoAtual) divergencias.Add("carteiraAlterada");
        if (x.TransacaoId.HasValue)
        {
            if (manual is null || manual.CarteiraId != x.CarteiraId || manual.AtivoId != x.AtivoId || manual.TransactionDate.Kind != DateTimeKind.Utc)
                divergencias.Add("transacaoIndisponivel");
            else
            {
                if (evento?.Data != DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(manual.TransactionDate, FusoBrasil))) divergencias.Add("data");
                if (evento?.Quantidade != manual.Quantity) divergencias.Add("quantidade");
                if (evento?.Preco != manual.UnitPrice) divergencias.Add("preco");
            }
        }
        return new(x.Id, x.IndiceEvento, x.Revisao, x.Decisao, x.AtivoId, x.TransacaoId, x.CarteiraVersao, x.CriadoEmUtc, divergencias);
    }

    private static bool ConflitoConcorrencia(Exception erro)
    {
        for (Exception? causa = erro; causa is not null; causa = causa.InnerException)
            if (causa is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected }) return true;
        return false;
    }
}
