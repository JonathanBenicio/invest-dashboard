using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Application.Services;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.Domain.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Npgsql;

namespace InvestDashboard.WebAPI.Controllers;

public sealed record LoteTesouroResposta(Guid Id, Guid CarteiraId, DateOnly DataReferencia, string Formato,
    string Estado, DateTime CriadoEmUtc, DateTime? ConfirmadoEmUtc, PreviaTesouroDto Previa);

[Authorize]
[ApiController]
[Route("api/v1/portfolios/{carteiraId:guid}/tesouro-direto/lotes")]
[DesativarLeituraAutomaticaFormulario]
public sealed class LotesTesouroController(
    ICarteiraRepository carteiras,
    IUsuarioAtualService usuario,
    ILeitorPreviaTesouro leitor,
    IRepositorioLoteImportacaoTesouro lotes,
    IEventoOficialTesouroRepository eventos,
    InvestDbContext db, ITransacaoRepository transacoes, ITransacaoAppService transacaoApp,
    IAtivoRepository ativos) : ControllerBase
{
    [HttpGet("{loteId:guid}/eventos")]
    public async Task<IActionResult> ListarEventos(Guid carteiraId, Guid loteId, CancellationToken cancellationToken,
        [FromQuery] int depoisDoIndice = -1, [FromQuery] int limite = 50)
    {
        Response.Headers.CacheControl = "no-store";
        if (limite is < 1 or > 100 || depoisDoIndice < -1) return BadRequest();
        var autor = usuario.UserId;
        if (autor is null || !await PodeGerenciar(carteiraId, autor.Value, cancellationToken)) return NotFound();
        var lote = await lotes.BuscarPorIdAsync(carteiraId, autor.Value, loteId, cancellationToken);
        if (lote?.Estado != "Confirmado") return NotFound();

        var registros = await eventos.ListarAsync(carteiraId, loteId, depoisDoIndice, limite, cancellationToken);
        return Ok(new RespostaApi<object>(registros.Select(evento => new
        {
            evento.IndiceEvento,
            evento.AtivoId,
            evento.TransacaoId,
            Dados = JsonSerializer.Deserialize<MovimentoPreviaTesouroDto>(evento.EventoJson)
        }).ToArray()));
    }

    private static readonly TimeZoneInfo FusoBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    [HttpPost("{loteId:guid}/confirmacao")]
    public async Task<ActionResult<RespostaApi<LoteTesouroResposta>>> Confirmar(Guid carteiraId, Guid loteId,
        [FromBody] ConfirmarLoteTesouroDto pedido, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        if (usuario.UserId is not Guid autor) return NotFound();
        if ((pedido.AtivoId == Guid.Empty) || (pedido.CriarPosicao == pedido.AtivoId.HasValue) ||
            pedido.CarteiraVersao < 0 || pedido.Revisoes is null ||
            pedido.Revisoes.Count > 10_000 || pedido.Revisoes.Any(r => r is null || r.IndiceEvento < 0 || r.Revisao < 1) ||
            pedido.Revisoes.Select(r => r.IndiceEvento).Distinct().Count() != pedido.Revisoes.Count)
            return BadRequest();
        if (!db.Database.IsNpgsql()) return StatusCode(503);

        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var carteira = await carteiras.GetByIdForUserAsync(carteiraId, autor.ToString(), ct);
            if (carteira is null || !await carteiras.CanManageAsync(carteiraId, autor.ToString(), ct)) return NotFound();
            var lote = await lotes.BuscarPorIdAsync(carteiraId, autor, loteId, ct);
            if (lote is null) return NotFound();

            var bases = db.Set<BasePosicaoTesouro>();
            var vigentes = await db.Set<DecisaoConciliacaoTesouro>().Where(x => x.LoteId == loteId && x.CarteiraId == carteiraId)
                .GroupBy(x => x.IndiceEvento).Select(g => g.OrderByDescending(x => x.Revisao).First()).ToListAsync(ct);
            var esperadas = pedido.Revisoes.ToDictionary(x => x.IndiceEvento, x => x.Revisao);
            if (vigentes.Count != esperadas.Count || vigentes.Any(x => x.CarteiraVersao != pedido.CarteiraVersao ||
                    x.AtivoId != pedido.AtivoId || !esperadas.TryGetValue(x.IndiceEvento, out var revisao) || revisao != x.Revisao))
                return Conflito("Revise novamente as movimentações e a versão da carteira.");
            if (lote.Estado == "Confirmado")
                return await bases.AnyAsync(x => x.LoteId == loteId && (!pedido.AtivoId.HasValue || x.AtivoId == pedido.AtivoId), ct)
                    ? Ok(new RespostaApi<LoteTesouroResposta>(Mapear(lote))) : Conflito("O lote já foi confirmado para outro ativo.");
            if (lote.Estado != "Pendente" || carteira.Version != pedido.CarteiraVersao)
                return Conflito("A carteira ou o lote mudou após a revisão.");

            var previa = Mapear(lote).Previa;
            if (lote.DataReferencia > DataCivil(DateTime.UtcNow) ||
                previa.Movimentacoes.Count != vigentes.Count ||
                !ConfirmacaoTesouro.TentarCalcularSaldo(previa, lote.DataReferencia, out var saldoCalculado))
                return Conflito("O extrato contém lacunas, posições inválidas, movimentações sem revisão ou data futura.");

            var ativoId = pedido.AtivoId;
            if (pedido.CriarPosicao)
            {
                var candidato = ConfirmacaoTesouro.CriarAtivoInicial(previa, saldoCalculado.PrecoUnitario);
                if (candidato is null || carteira.Positions.Any(p =>
                        string.Equals(p.Ticker, candidato.Ticker, StringComparison.OrdinalIgnoreCase) ||
                        ConfirmacaoTesouro.TituloCompativel(previa, p.Ativo as RendaFixa)))
                    return Conflito("Os dados do HTML não permitem criar uma posição nova ou o título já está na carteira.");
                var existente = await ativos.GetByTickerAsync(candidato.Ticker, ct);
                var ativoExistente = existente as RendaFixa;
                if (existente is not null && !ConfirmacaoTesouro.TituloCompativel(previa, ativoExistente))
                    return Conflito("O identificador do título já está associado a outro ativo.");
                var ativoNovo = ativoExistente ?? candidato;
                if (ativoExistente is null) await ativos.AddAsync(ativoNovo, ct);
                ativoId = ativoNovo.Id;
                // Persistir o ativo dentro da mesma transação para que a reconstrução do ledger o encontre.
                if (ativoExistente is null) await db.SaveChangesAsync(ct);
            }
            else
            {
                var ativo = carteira.Positions.SingleOrDefault(x => x.AtivoId == ativoId)?.Ativo as RendaFixa;
                if (!ConfirmacaoTesouro.TituloCompativel(previa, ativo))
                    return Conflito("O título e o vencimento do extrato precisam corresponder à posição do Tesouro.");
            }

            var anterior = await bases.SingleOrDefaultAsync(x => x.CarteiraId == carteiraId && x.AtivoId == ativoId && x.Ativa, ct);
            if (anterior is not null && anterior.DataReferencia >= lote.DataReferencia)
                return Conflito("A data do extrato precisa ser posterior à da base ativa.");

            var manuais = (await transacoes.GetByPortfolioIdAsync(carteiraId, ct))
                .Where(x => x.AtivoId == ativoId && x.Type is TipoTransacao.Buy or TipoTransacao.Sell)
                .ToDictionary(x => x.Id);
            var vinculadas = new HashSet<Guid>();
            var oficiais = new List<EventoOficialTesouro>();
            var agora = DateTime.UtcNow;
            foreach (var decisao in vigentes)
            {
                if (decisao.IndiceEvento < 0 || decisao.IndiceEvento >= previa.Movimentacoes.Count)
                    return Conflito("Uma revisão não corresponde a uma movimentação do extrato.");
                var evento = previa.Movimentacoes[decisao.IndiceEvento];
                var m = evento.DadosInterpretados;
                var tipo = ConfirmacaoTesouro.TipoNegociacao(m);
                if (m?.Data is not DateOnly data || !ConfirmacaoTesouro.EventoCompleto(m, lote.DataReferencia))
                    return Conflito("Uma movimentação contém dados incompletos ou uma natureza não reconhecida.");
                if (decisao.Decisao == "vincular")
                {
                    if (decisao.TransacaoId is not Guid manualId || !vinculadas.Add(manualId) ||
                        !manuais.TryGetValue(manualId, out var manual) || manual.Type != tipo ||
                        manual.TransactionDate.Kind != DateTimeKind.Utc || DataCivil(manual.TransactionDate) != data || manual.Quantity != m.Quantidade || manual.UnitPrice != m.Preco)
                        return Conflito("Uma movimentação vinculada diverge do lançamento manual atual.");
                }
                else if (decisao.Decisao != "sem_vinculo" || decisao.TransacaoId.HasValue)
                    return Conflito("Uma revisão contém uma decisão inválida.");
                oficiais.Add(new EventoOficialTesouro(Guid.NewGuid(), loteId, carteiraId, ativoId!.Value,
                    decisao.IndiceEvento, JsonSerializer.Serialize(evento), decisao.TransacaoId, agora));
            }

            if (manuais.Values.Any(x => DataCivil(x.TransactionDate) <= lote.DataReferencia &&
                (anterior is null || DataCivil(x.TransactionDate) > anterior.DataReferencia) && !vinculadas.Contains(x.Id)))
                return Conflito("Há operações manuais anteriores à data-base sem correspondência no extrato.");

            // A restrição de base ativa é verificada pelo PostgreSQL a cada instrução.
            // Desative antes de inserir a nova base, mantendo as duas etapas na mesma transação.
            if (anterior is not null)
            {
                anterior.Desativar();
                await db.SaveChangesAsync(ct);
            }
            var saldo = new BasePosicaoTesouro(Guid.NewGuid(), loteId, carteiraId, ativoId!.Value,
                lote.DataReferencia, saldoCalculado.Quantidade, saldoCalculado.CustoTotal, saldoCalculado.PrecoUnitario);
            saldo.Ativar();
            db.Add(saldo);
            db.AddRange(oficiais);
            lote.MarcarConfirmado(agora);
            await db.SaveChangesAsync(ct);
            await transacaoApp.RebuildPortfolioAsync(carteiraId);
            await tx.CommitAsync(ct);
            return Ok(new RespostaApi<LoteTesouroResposta>(Mapear(lote)));
        }
        catch (TransactionLedgerConflictException)
        {
            return Conflito("As operações posteriores não são compatíveis com o saldo-base do extrato.");
        }
        catch (Exception erro) when (ConflitoConcorrencia(erro))
        {
            return Conflito("A carteira foi modificada durante a confirmação. Atualize a revisão.");
        }
    }

    private static bool ConflitoConcorrencia(Exception erro)
    {
        for (Exception? causa = erro; causa is not null; causa = causa.InnerException)
            if (causa is DbUpdateConcurrencyException ||
                causa is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or
                    PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.DeadlockDetected }) return true;
        return false;
    }

    private ConflictObjectResult Conflito(string mensagem) =>
        Conflict(new RespostaApi<LoteTesouroResposta>(null!, false, mensagem));

    private static DateOnly DataCivil(DateTime data) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(data, FusoBrasil));

    private const int MaximoArquivo = 10 * 1024 * 1024;
    private const int MaximoRequisicao = MaximoArquivo + 64 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaximoRequisicao)]
    public async Task<ActionResult<RespostaApi<LoteTesouroResposta>>> Criar(
        Guid carteiraId, [FromQuery] DateOnly? dataReferencia, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var usuarioId = usuario.UserId;
        if (usuarioId is null || !await PodeGerenciar(carteiraId, usuarioId.Value, cancellationToken))
            return NotFound();
        if (!dataReferencia.HasValue || dataReferencia == default(DateOnly))
            return BadRequest(new RespostaApi<LoteTesouroResposta>(null!, false, "Informe a data de referência do extrato."));

        try
        {
            if (!MediaTypeHeaderValue.TryParse(Request.ContentType, out var tipo) ||
                !string.Equals(tipo.MediaType.Value, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
                return ArquivoInvalido();
            var boundary = HeaderUtilities.RemoveQuotes(tipo.Boundary).Value;
            if (string.IsNullOrWhiteSpace(boundary) || boundary.Length > 128) return ArquivoInvalido();
            var multipart = new MultipartReader(boundary, Request.Body)
            {
                BodyLengthLimit = MaximoArquivo, HeadersCountLimit = 16, HeadersLengthLimit = 16 * 1024
            };
            var section = await multipart.ReadNextSectionAsync(cancellationToken);
            if (section is null || !ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition) ||
                !string.Equals(disposition.DispositionType.Value, "form-data", StringComparison.OrdinalIgnoreCase) ||
                HeaderUtilities.RemoveQuotes(disposition.Name).Value != "arquivo") return ArquivoInvalido();
            var nome = HeaderUtilities.RemoveQuotes(disposition.FileNameStar.HasValue ? disposition.FileNameStar : disposition.FileName).Value;
            var extensao = Path.GetExtension(nome ?? string.Empty).ToLowerInvariant();
            if (extensao is not (".html" or ".htm")) return ArquivoInvalido();
            await using var arquivo = new MemoryStream();
            var buffer = new byte[81920];
            int lidos;
            while ((lidos = await section.Body.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (arquivo.Length + lidos > MaximoArquivo) return ArquivoInvalido();
                await arquivo.WriteAsync(buffer.AsMemory(0, lidos), cancellationToken);
            }
            if (arquivo.Length == 0 || await multipart.ReadNextSectionAsync(cancellationToken) is not null) return ArquivoInvalido();
            var hash = Convert.ToHexString(SHA256.HashData(arquivo.GetBuffer().AsSpan(0, checked((int)arquivo.Length)))).ToLowerInvariant();
            var formato = extensao == ".htm" ? "html" : extensao[1..];
            var existente = await lotes.BuscarPorChaveAsync(carteiraId, usuarioId.Value, hash, dataReferencia.Value, cancellationToken);
            if (existente is not null)
                return existente.Formato == formato ? Ok(new RespostaApi<LoteTesouroResposta>(Mapear(existente))) : ArquivoInvalido();
            arquivo.Position = 0;
            var previa = leitor.Ler(carteiraId, formato, arquivo);
            var lote = new LoteImportacaoTesouro(Guid.NewGuid(), carteiraId, usuarioId.Value, dataReferencia.Value,
                hash, formato, JsonSerializer.Serialize(previa), DateTime.UtcNow);
            await lotes.AdicionarAsync(lote, cancellationToken);
            try { await lotes.SalvarAsync(cancellationToken); }
            catch (DbUpdateException erro) when (erro.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Uma requisição concorrente pode ter gravado a mesma chave.
                existente = await lotes.BuscarPorChaveAsync(carteiraId, usuarioId.Value, hash, dataReferencia.Value, cancellationToken);
                if (existente is null) return Conflict(new RespostaApi<LoteTesouroResposta>(null!, false, "Não foi possível criar o lote: já existe uma importação com esta data e conteúdo."));
                return Ok(new RespostaApi<LoteTesouroResposta>(Mapear(existente)));
            }
            return CreatedAtAction(nameof(Obter), new { carteiraId, loteId = lote.Id }, new RespostaApi<LoteTesouroResposta>(Mapear(lote)));
        }
        catch (Exception erro) when (erro is InvalidDataException or IOException or DecoderFallbackException or
                                      OpenXmlPackageException or System.Xml.XmlException or FormatException or ArgumentException)
        {
            return ArquivoInvalido();
        }
    }

    [HttpGet("{loteId:guid}")]
    public async Task<ActionResult<RespostaApi<LoteTesouroResposta>>> Obter(Guid carteiraId, Guid loteId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var usuarioId = usuario.UserId;
        if (usuarioId is null || !await PodeGerenciar(carteiraId, usuarioId.Value, cancellationToken)) return NotFound();
        var lote = await lotes.BuscarPorIdAsync(carteiraId, usuarioId.Value, loteId, cancellationToken);
        return lote is null ? NotFound() : Ok(new RespostaApi<LoteTesouroResposta>(Mapear(lote)));
    }

    [HttpGet]
    public async Task<ActionResult<RespostaApi<IReadOnlyList<LoteTesouroResumoDto>>>> Listar(Guid carteiraId, [FromQuery] int pagina = 1, CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "no-store";
        var usuarioId = usuario.UserId;
        if (usuarioId is null || !await PodeGerenciar(carteiraId, usuarioId.Value, cancellationToken)) return NotFound();
        if (pagina is < 1 or > 10_000) return BadRequest();
        var lista = await lotes.ListarPorCarteiraAsync(carteiraId, usuarioId.Value, pagina, cancellationToken);
        return Ok(new RespostaApi<IReadOnlyList<LoteTesouroResumoDto>>(lista));
    }

    private async Task<bool> PodeGerenciar(Guid carteiraId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var id = usuarioId.ToString();
        return await carteiras.GetByIdForUserAsync(carteiraId, id, cancellationToken) is not null &&
            await carteiras.CanManageAsync(carteiraId, id, cancellationToken);
    }

    private static LoteTesouroResposta Mapear(LoteImportacaoTesouro lote) =>
        new(lote.Id, lote.CarteiraId, lote.DataReferencia, lote.Formato, lote.Estado,
            lote.CriadoEmUtc, lote.ConfirmadoEmUtc,
            JsonSerializer.Deserialize<PreviaTesouroDto>(lote.PreviaJson) ?? throw new InvalidOperationException("Prévia indisponível."));

    private BadRequestObjectResult ArquivoInvalido() =>
        BadRequest(new RespostaApi<LoteTesouroResposta>(null!, false, "Arquivo inválido ou formato não reconhecido."));
}
