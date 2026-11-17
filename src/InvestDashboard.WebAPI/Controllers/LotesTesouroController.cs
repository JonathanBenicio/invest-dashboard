using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.Portfolio;
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
    IEventoOficialTesouroRepository eventos) : ControllerBase
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
            if (extensao is not (".html" or ".htm" or ".xlsx")) return ArquivoInvalido();
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
