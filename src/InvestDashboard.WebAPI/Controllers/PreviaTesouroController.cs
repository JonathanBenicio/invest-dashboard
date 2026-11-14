using System.Text;
using DocumentFormat.OpenXml.Packaging;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/portfolios/{carteiraId:guid}/tesouro-direto")]
[DesativarLeituraAutomaticaFormulario]
public sealed class PreviaTesouroController(ICarteiraAppService carteiras, ILeitorPreviaTesouro leitor) : ControllerBase
{
    private const int MaximoArquivo = 10 * 1024 * 1024;
    private const int MaximoRequisicao = MaximoArquivo + 64 * 1024;

    /// <summary>Analisa o arquivo na requisição; não cria nem altera posições ou operações.</summary>
    [HttpPost("previa")]
    [RequestSizeLimit(MaximoRequisicao)]
    public async Task<ActionResult<RespostaApi<PreviaTesouroDto>>> CriarPrevia(Guid carteiraId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        // Consultar acesso antes de processar o arquivo; ocultar existência de outras carteiras.
        if (await carteiras.GetPortfolioByIdAsync(carteiraId) is null)
            return NotFound(new RespostaApi<PreviaTesouroDto>(null!, false, "Carteira não encontrada."));

        if (!MediaTypeHeaderValue.TryParse(Request.ContentType, out var tipo) ||
            !string.Equals(tipo.MediaType.Value, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new RespostaApi<PreviaTesouroDto>(null!, false, "Envie um arquivo HTML ou XLSX."));

        var boundary = HeaderUtilities.RemoveQuotes(tipo.Boundary).Value;
        if (string.IsNullOrWhiteSpace(boundary) || boundary.Length > 128)
            return BadRequest(new RespostaApi<PreviaTesouroDto>(null!, false, "Requisição de arquivo inválida."));

        try
        {
            // MultipartReader lê o corpo diretamente; não usa IFormFile/FormReader,
            // que pode gravar arquivos particulares em diretórios temporários.
            var multipart = new MultipartReader(boundary, Request.Body)
            {
                BodyLengthLimit = MaximoArquivo,
                HeadersCountLimit = 16,
                HeadersLengthLimit = 16 * 1024
            };
            var section = await multipart.ReadNextSectionAsync(cancellationToken);
            if (section is null || !ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition) ||
                !string.Equals(disposition.DispositionType.Value, "form-data", StringComparison.OrdinalIgnoreCase) ||
                HeaderUtilities.RemoveQuotes(disposition.Name).Value != "arquivo")
                return ArquivoInvalido();

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
            if (arquivo.Length == 0 || await multipart.ReadNextSectionAsync(cancellationToken) is not null)
                return ArquivoInvalido();

            cancellationToken.ThrowIfCancellationRequested();
            arquivo.Position = 0;
            var previa = leitor.Ler(carteiraId, extensao == ".htm" ? "html" : extensao[1..], arquivo);
            return Ok(new RespostaApi<PreviaTesouroDto>(previa));
        }
        catch (Exception erro) when (erro is InvalidDataException or IOException or DecoderFallbackException or
                                      OpenXmlPackageException or System.Xml.XmlException or FormatException or ArgumentException)
        {
            // Nunca incluir nome, conteúdo ou detalhes de parser em logs/resposta.
            return ArquivoInvalido();
        }
    }

    private BadRequestObjectResult ArquivoInvalido() =>
        BadRequest(new RespostaApi<PreviaTesouroDto>(null!, false, "Arquivo inválido ou formato não reconhecido."));
}

// Impede que o model binding leia e armazene o formulário antes da leitura limitada.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class DesativarLeituraAutomaticaFormularioAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var factories = context.ValueProviderFactories;
        for (var i = factories.Count - 1; i >= 0; i--)
            if (factories[i] is FormValueProviderFactory or FormFileValueProviderFactory or JQueryFormValueProviderFactory)
                factories.RemoveAt(i);
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}
