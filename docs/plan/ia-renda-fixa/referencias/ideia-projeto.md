> Exportação histórica de [Ideia projeto](https://chatgpt.com/space/page_69af4373948c8191999593587cb34fde). Para o escopo e as decisões vigentes, consulte o [épico e os subplanos](../epico.md).

Abaixo está o [consolidado ](https://chatgpt.com/space/page_85f156fd618481918d068ae937910f24 "https://chatgpt.com/space/page_85f156fd618481918d068ae937910f24")completo e integral de todas as soluções, arquiteturas, configurações e códigos discutidos ao longo deste histórico para o **Invest Dashboard** (.NET 10 + Clean Architecture / DDD), sem omissões.

[](page://page_85f156fd618481918d068ae937910f24)&#x20;

\


## 1. Visão Geral da Arquitetura & Escolha de Ferramentas

- **Extração de Imagens (Prints/Extratos) e Análise Qualitativa:** `Microsoft.Extensions.AI` (`IChatClient`) alternando dinamicamente entre **Ollama** (local/privado) e **OpenRouter** (nuvem) com **Structured Outputs** (`GetResponseAsync<T>`).
  - **Local (Ollama):** `qwen2.5vl:7b` (ou `qwen2.5vl:3b` para menos RAM/VRAM).
  - **Nuvem Free Tier (OpenRouter):** `qwen/qwen2.5-vl-72b-instruct:free` ou `meta-llama/llama-3.2-11b-vision-instruct:free`.
  - **Nuvem Produção (OpenRouter):** `google/gemini-2.5-flash` ou `openai/gpt-5.4-mini` (\~R$ 0,01 por imagem).
- **Integração com ChatGPT Plus:** **Custom GPT Actions (OpenAPI/Swagger)** autenticado via **OAuth Server nativo do Supabase**, garantindo isolamento de dados pelo `UserId` (`sub` do JWT).
- **Séries Temporais, Detecção de Outliers e Classificação Tabular:** `ML.NET` (`Microsoft.ML` e `Microsoft.ML.TimeSeries`) para validar anomalias em saldos/cotações (`DetectIidSpike`), classificar nomenclaturas sujas de títulos de renda fixa (`SdcaMaximumEntropy`) e projetar dividendos/curvas (`ForecastBySsa`).
- **Cálculos Financeiros Exatos (Juros Compostos, Dias Úteis ANBIMA 252, IR/IOF):** Matemática pura em C# no `InvestDashboard.Domain`.

## 2. Infraestrutura Docker, Rede, Healthchecks & Build

### 2.1 `docker-compose.yml` (Ollama + API .NET + Rede Interna + Healthchecks)

YAML

```
services:
  ollama:
    image: ollama/ollama:latest
    container_name: invest-ollama
    restart: unless-stopped
    ports:
      - "127.0.0.1:11434:11434"
    volumes:
      - ollama_data:/root/.ollama
    environment:
      - OLLAMA_KEEP_ALIVE=24h
      - OLLAMA_NUM_PARALLEL=2
    networks:
      - invest-network
    healthcheck:
      test: ["CMD-SHELL", "ollama list >/dev/null 2>&1 || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 15s
    # Descomente o bloco abaixo se estiver usando GPU NVIDIA no Linux/WSL2
    # deploy:
    #   resources:
    #     reservations:
    #       devices:
    #         - driver: nvidia
    #           count: all
    #           capabilities: [gpu]

  investimentos-api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: investimentos-api
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_HTTP_PORTS=8080
      - AI__Provider=${AI__Provider:-Ollama}
      - AI__Ollama__Endpoint=${AI__Ollama__Endpoint:-http://ollama:11434}
      - AI__Ollama__Model=${AI__Ollama__Model:-qwen2.5vl:7b}
      - AI__OpenRouter__Endpoint=${AI__OpenRouter__Endpoint:-https://openrouter.ai/api/v1}
      - AI__OpenRouter__ApiKey=${AI__OpenRouter__ApiKey:-}
      - AI__OpenRouter__Model=${AI__OpenRouter__Model:-google/gemini-2.5-flash}
    depends_on:
      ollama:
        condition: service_healthy
    networks:
      - invest-network
    healthcheck:
      test: ["CMD-SHELL", "curl -fsS http://127.0.0.1:8080/health >/dev/null || exit 1"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 20s

volumes:
  ollama_data:
    driver: local

networks:
  invest-network:
    driver: bridge

```

### 2.2 Variáveis de Ambiente (`.env.example` e `appsettings.json`)

Snippet de código

```
# Configuração de IA (Ollama ou OpenRouter)
AI__Provider=Ollama
AI__Ollama__Endpoint=http://ollama:11434
AI__Ollama__Model=qwen2.5vl:7b
AI__OpenRouter__Endpoint=https://openrouter.ai/api/v1
AI__OpenRouter__ApiKey=
AI__OpenRouter__Model=google/gemini-2.5-flash

```

JSON

```
{
  "AI": {
    "Provider": "Ollama",
    "Ollama": {
      "Endpoint": "http://ollama:11434",
      "Model": "qwen2.5vl:7b"
    },
    "OpenRouter": {
      "Endpoint": "https://openrouter.ai/api/v1",
      "ApiKey": "",
      "Model": "google/gemini-2.5-flash"
    }
  }
}

```

### 2.3 `Dockerfile` Multi-Stage para .NET 10 (Usuário Não-Root + Cache NuGet)

Dockerfile

```
# syntax=docker/dockerfile:1.7

# ==============================================================================
# 1. Estágio de Restore (isolado para aproveitar o cache de camadas do NuGet)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY Directory.Build.props* Directory.Packages.props* nuget.config* ./
COPY src/InvestDashboard.WebAPI/InvestDashboard.WebAPI.csproj src/InvestDashboard.WebAPI/

RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages \
    dotnet restore src/InvestDashboard.WebAPI/InvestDashboard.WebAPI.csproj

# ==============================================================================
# 2. Estágio de Build & Publish
# ==============================================================================
FROM restore AS build
WORKDIR /src

COPY . .

RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages \
    dotnet publish src/InvestDashboard.WebAPI/InvestDashboard.WebAPI.csproj \
      -c Release \
      -o /app/publish \
      --no-restore

# ==============================================================================
# 3. Estágio Final (Runtime leve + Usuário não-root + curl para healthcheck)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_HTTP_PORTS=8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --system --gid 10001 appgroup \
    && useradd --system --uid 10001 --gid 10001 --home-dir /app --shell /usr/sbin/nologin appuser

COPY --from=build --chown=10001:10001 /app/publish ./

USER 10001:10001
EXPOSE 8080

ENTRYPOINT ["dotnet", "InvestDashboard.WebAPI.dll"]

```

### 2.4 `.dockerignore`

Snippet de código

```
**/.git
**/.vs
**/.vscode
**/bin
**/obj
**/out
**/publish
**/.env*
*.user

```

## 3. Camada `InvestDashboard.Application` (DTOs, Interfaces e Casos de Uso)

### 3.1 DTOs de Extração (Genérico + Específico do Tesouro Direto)

C#

```
using System.ComponentModel;

namespace InvestDashboard.Application.DTOs.Tesouro;

public sealed record TesouroPosicaoExtraidaDto(
    [property: Description("Nome completo do título público (ex: Tesouro Selic 2029, Tesouro IPCA+ 2035)")]
    string NomeTitulo,

    [property: Description("Categoria ou tipo do título (ex: Tesouro Selic, Tesouro IPCA+, Tesouro Prefixado)")]
    string TipoTitulo,

    [property: Description("Saldo bruto ou líquido atual exibido no print, em BRL")]
    decimal SaldoAtual,

    [property: Description("Valor total investido/aplicado originalmente, em BRL")]
    decimal ValorAplicado,

    [property: Description("Data de vencimento do título no formato YYYY-MM-DD")]
    DateOnly DataVencimento,

    [property: Description("Data de referência/atualização da posição no formato YYYY-MM-DD, se visível")]
    DateOnly? DataAtualizacao,

    [property: Description("Lucro ou prejuízo financeiro em reais (SaldoAtual - ValorAplicado), se visível")]
    decimal? RentabilidadeValor,

    [property: Description("Rentabilidade percentual acumulada (ex: 12.45 para 12,45%), se visível")]
    decimal? RentabilidadePercentual
);

// DTO alternativo para extração genérica de múltiplos ativos na mesma tela
public sealed record PosicaoInvestimentoExtraida(
    [property: Description("Nome exato do ativo ou título (ex: Tesouro Selic 2029, PETR4)")]
    string NomeAtivo,

    [property: Description("Categoria: TesouroDireto, Acao, FII, RendaFixa ou Cripto")]
    string Categoria,

    [property: Description("Quantidade de cotas ou fração do título")]
    decimal Quantidade,

    [property: Description("Valor total aplicado/investido originalmente em BRL")]
    decimal ValorAplicado,

    [property: Description("Saldo bruto ou líquido atual em BRL")]
    decimal SaldoAtual,

    [property: Description("Data de vencimento no formato YYYY-MM-DD, se houver")]
    DateOnly? DataVencimento
);

public sealed record CarteiraExtraida(List<PosicaoInvestimentoExtraida> Ativos);

```

### 3.2 DTO de Resumo Enxuto para LLMs Externas (ChatGPT)

C#

```
namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed record PortfolioResumoIaDto(
    DateTime DataReferenciaUtc,
    decimal PatrimonioTotalBrl,
    decimal TotalInvestidoBrl,
    decimal RentabilidadeTotalBrl,
    decimal RentabilidadeTotalPercentual,
    IReadOnlyList<ClasseAtivoResumoDto> DistribuicaoPorClasse,
    IReadOnlyList<AtivoResumoIaDto> Posicoes
);

public sealed record ClasseAtivoResumoDto(
    string Classe,
    decimal ValorAtualBrl,
    decimal PercentualCarteira
);

public sealed record AtivoResumoIaDto(
    string TickerOuNome,
    string Classe,
    decimal ValorAtualBrl,
    decimal ValorAplicadoBrl,
    decimal RentabilidadePercentual,
    DateOnly? Vencimento
);

```

### 3.3 Interface do Extrator (`Interfaces/AI/ITesouroImageExtractorService.cs`)

C#

```
using InvestDashboard.Application.DTOs.Tesouro;

namespace InvestDashboard.Application.Interfaces.AI;

public interface ITesouroImageExtractorService
{
    Task<TesouroPosicaoExtraidaDto> ExtrairPosicaoAsync(
        ReadOnlyMemory<byte> imagemBytes,
        string mimeType,
        CancellationToken cancellationToken = default);
}

```

### 3.4 Caso de Uso de Importação (`UseCases/Tesouro/ImportarPosicaoTesouroPorImagemCommand.cs`)

C#

```
using InvestDashboard.Application.DTOs.Tesouro;
using InvestDashboard.Application.Interfaces.AI;

namespace InvestDashboard.Application.UseCases.Tesouro;

public sealed record ImportarPosicaoTesouroPorImagemCommand(
    Guid UserId,
    ReadOnlyMemory<byte> ImagemBytes,
    string MimeType
);

public sealed record ImportarPosicaoTesouroPorImagemResult(
    TesouroPosicaoExtraidaDto DadosExtraidos,
    bool AtualizadoComSucesso,
    string Mensagem
);

public sealed class ImportarPosicaoTesouroPorImagemHandler(
    ITesouroImageExtractorService extractorService
    // TODO: Injetar repositório/serviço de domínio existente (ex: ITesouroPosicaoRepository / IUnitOfWork)
)
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp"
    };

    public async Task<ImportarPosicaoTesouroPorImagemResult> HandleAsync(
        ImportarPosicaoTesouroPorImagemCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ImagemBytes.IsEmpty)
            throw new ArgumentException("A imagem enviada está vazia.", nameof(command.ImagemBytes));

        if (!AllowedMimeTypes.Contains(command.MimeType))
            throw new ArgumentException($"Formato de imagem não suportado: {command.MimeType}. Use PNG, JPEG ou WEBP.");

        var extraido = await extractorService.ExtrairPosicaoAsync(
            command.ImagemBytes,
            command.MimeType,
            cancellationToken);

        ValidarDadosExtraidos(extraido);

        var rentabilidadeValor = extraido.RentabilidadeValor
            ?? Math.Round(extraido.SaldoAtual - extraido.ValorAplicado, 2);

        var rentabilidadePercentual = extraido.RentabilidadePercentual
            ?? (extraido.ValorAplicado > 0
                ? Math.Round((rentabilidadeValor / extraido.ValorAplicado) * 100m, 2)
                : 0m);

        var dtoNormalizado = extraido with
        {
            RentabilidadeValor = rentabilidadeValor,
            RentabilidadePercentual = rentabilidadePercentual,
            DataAtualizacao = extraido.DataAtualizacao ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // TODO: Integrar com as entidades e regras de domínio existentes em InvestDashboard.Domain:
        // 1. Verificar anomalia estatística com ValidadorAnomaliaPosicao.EhValorSuspeito(historico, dtoNormalizado.SaldoAtual)
        // 2. Atualizar/criar a posição do usuário no repositório e persistir via UnitOfWork.

        return new ImportarPosicaoTesouroPorImagemResult(
            DadosExtraidos: dtoNormalizado,
            AtualizadoComSucesso: true,
            Mensagem: $"Posição de '{dtoNormalizado.NomeTitulo}' extraída e processada com sucesso."
        );
    }

    private static void ValidarDadosExtraidos(TesouroPosicaoExtraidaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NomeTitulo))
            throw new InvalidOperationException("Não foi possível identificar o nome do título na imagem.");

        if (dto.SaldoAtual <= 0)
            throw new InvalidOperationException("O saldo atual extraído é inválido ou zerado.");

        if (dto.ValorAplicado < 0)
            throw new InvalidOperationException("O valor aplicado extraído não pode ser negativo.");
    }
}

```

## 4. Camada `InvestDashboard.Infrastructure` (`Microsoft.Extensions.AI`)

### 4.1 Pacotes NuGet

Bash

```
dotnet add src/InvestDashboard.Infrastructure package Microsoft.Extensions.AI
dotnet add src/InvestDashboard.Infrastructure package Microsoft.Extensions.AI.Ollama
dotnet add src/InvestDashboard.Infrastructure package Microsoft.Extensions.AI.OpenAI

```

### 4.2 Implementação do Extrator (`AI/TesouroImageExtractorService.cs`)

C#

```
using InvestDashboard.Application.DTOs.Tesouro;
using InvestDashboard.Application.Interfaces.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace InvestDashboard.Infrastructure.AI;

public sealed class TesouroImageExtractorService(
    IChatClient chatClient,
    ILogger<TesouroImageExtractorService> logger) : ITesouroImageExtractorService
{
    private const string SystemPrompt = """
        Você é um especialista em extração de dados financeiros do Tesouro Direto brasileiro.
        Analise a imagem fornecida (print de aplicativo de corretora ou portal do Tesouro Direto) e extraia os dados do título.
        Regras estritas:
        1. Converta valores monetários em formato brasileiro (ex: R$ 12.345,67) para decimal com ponto (12345.67).
        2. Converta datas brasileiras (DD/MM/AAAA) para o formato ISO (YYYY-MM-DD).
        3. Não invente ou estime números que não estejam legíveis na imagem; use null nos campos opcionais ausentes.
        4. Retorne estritamente no schema JSON solicitado.
        """;

    public async Task<TesouroPosicaoExtraidaDto> ExtrairPosicaoAsync(
        ReadOnlyMemory<byte> imagemBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Iniciando extração de posição do Tesouro Direto via IA ({MimeType}, {Bytes} bytes).",
            mimeType,
            imagemBytes.Length);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User,
            [
                new TextContent("Extraia os dados da posição do título do Tesouro Direto desta imagem."),
                new DataContent(imagemBytes, mimeType)
            ])
        };

        var options = new ChatOptions
        {
            Temperature = 0.0f
        };

        var response = await chatClient.GetResponseAsync<TesouroPosicaoExtraidaDto>(
            messages,
            options,
            cancellationToken: cancellationToken);

        if (response.Result is null)
        {
            throw new InvalidOperationException("O modelo de IA não retornou uma estrutura válida para a imagem enviada.");
        }

        return response.Result;
    }
}

```

### 4.3 Injeção de Dependência Dinâmica (`DependencyInjection.cs`)

C#

```
using System.ClientModel;
using InvestDashboard.Application.Interfaces.AI;
using InvestDashboard.Application.UseCases.Tesouro;
using InvestDashboard.Infrastructure.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace InvestDashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["AI:Provider"] ?? "Ollama";

        if (string.Equals(provider, "OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = configuration["AI:OpenRouter:Endpoint"] ?? "https://openrouter.ai/api/v1";
            var apiKey = configuration["AI:OpenRouter:ApiKey"]
                ?? throw new InvalidOperationException("AI:OpenRouter:ApiKey não configurada.");
            var model = configuration["AI:OpenRouter:Model"] ?? "google/gemini-2.5-flash";

            var openAiClient = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

            services.AddChatClient(
                openAiClient.GetChatClient(model).AsIChatClient());
        }
        else
        {
            var endpoint = configuration["AI:Ollama:Endpoint"] ?? "http://ollama:11434";
            var model = configuration["AI:Ollama:Model"] ?? "qwen2.5vl:7b";

            services.AddChatClient(
                new OllamaChatClient(new Uri(endpoint), model));
        }

        services.AddScoped<ITesouroImageExtractorService, TesouroImageExtractorService>();
        services.AddScoped<ImportarPosicaoTesouroPorImagemHandler>();

        return services;
    }
}

```

## 5. Camada `InvestDashboard.WebAPI` (Endpoints & Health Checks JSON)

### 5.1 Endpoints para Frontend e Custom GPT Actions (`Endpoints/AiIntegrationEndpoints.cs`)

C#

```
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.UseCases.Tesouro;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Endpoints;

public static class AiIntegrationEndpoints
{
    public static IEndpointRouteBuilder MapAiIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .RequireAuthorization();

        // 1. Endpoint para Custom GPT Actions (JSON com Base64)
        group.MapPost("/tesouro/importar-imagem/base64", async (
            [FromBody] ImportarImagemBase64Request request,
            ClaimsPrincipal user,
            ImportarPosicaoTesouroPorImagemHandler handler,
            CancellationToken ct) =>
        {
            var userId = ObterUserId(user);
            var bytes = Convert.FromBase64String(LimparPrefixoDataUri(request.ImagemBase64));

            var result = await handler.HandleAsync(
                new ImportarPosicaoTesouroPorImagemCommand(userId, bytes, request.MimeType),
                ct);

            return Results.Ok(result);
        })
        .WithName("ImportarTesouroPorImagemBase64")
        .WithSummary("Importa posição do Tesouro Direto a partir de imagem em Base64")
        .WithDescription("Extrai via IA os dados de um print do Tesouro Direto enviado em Base64 e atualiza a posição do usuário autenticado.")
        .Produces<ImportarPosicaoTesouroPorImagemResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        // 2. Endpoint para Frontend React (Upload Multipart)
        group.MapPost("/tesouro/importar-imagem", async (
            IFormFile arquivo,
            ClaimsPrincipal user,
            ImportarPosicaoTesouroPorImagemHandler handler,
            CancellationToken ct) =>
        {
            if (arquivo.Length == 0)
                return Results.BadRequest("Nenhum arquivo enviado.");

            var userId = ObterUserId(user);
            using var ms = new MemoryStream();
            await arquivo.CopyToAsync(ms, ct);

            var result = await handler.HandleAsync(
                new ImportarPosicaoTesouroPorImagemCommand(userId, ms.ToArray(), arquivo.ContentType),
                ct);

            return Results.Ok(result);
        })
        .DisableAntiforgery()
        .WithName("ImportarTesouroPorImagemUpload")
        .WithSummary("Importa posição do Tesouro Direto via upload de arquivo de imagem")
        .Produces<ImportarPosicaoTesouroPorImagemResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        // 3. Endpoint enxuto para o ChatGPT analisar a carteira consolidada
        group.MapGet("/portfolio/resumo-ia", async (
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = ObterUserId(user);

            // TODO: Consultar seu QueryHandler / Repositório de Portfólio filtrado por userId
            var resumo = new PortfolioResumoIaDto(
                DataReferenciaUtc: DateTime.UtcNow,
                PatrimonioTotalBrl: 0m,
                TotalInvestidoBrl: 0m,
                RentabilidadeTotalBrl: 0m,
                RentabilidadeTotalPercentual: 0m,
                DistribuicaoPorClasse: [],
                Posicoes: []
            );

            return Results.Ok(resumo);
        })
        .WithName("ObterResumoPortfolioParaIa")
        .WithSummary("Retorna resumo consolidado da carteira otimizado para análise por IA")
        .WithDescription("Retorna patrimônio total, distribuição por classe de ativo e lista resumida de posições do usuário autenticado.")
        .Produces<PortfolioResumoIaDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static Guid ObterUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Token JWT não contém identificação do usuário (sub).");

        return Guid.Parse(sub);
    }

    private static string LimparPrefixoDataUri(string base64)
    {
        var commaIndex = base64.IndexOf(',');
        return commaIndex >= 0 ? base64[(commaIndex + 1)..] : base64;
    }
}

public sealed record ImportarImagemBase64Request(
    [property: Required]
    [property: Description("Conteúdo da imagem codificado em Base64 (com ou sem prefixo data:image/...)")]
    string ImagemBase64,

    [property: Required]
    [property: Description("MIME type da imagem: image/png, image/jpeg ou image/webp")]
    string MimeType
);

```

### 5.2 Health Checks Nativos + `ResponseWriter` JSON (`HealthCheckResponseWriter.cs` e `Program.cs`)

C#

```
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InvestDashboard.WebAPI.HealthChecks;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new HealthReportResponse(
            Status: report.Status.ToString(),
            TotalDurationMs: Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            Checks: report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new HealthEntryResponse(
                    Status: entry.Value.Status.ToString(),
                    DurationMs: Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                    Description: entry.Value.Description,
                    Error: entry.Value.Exception?.Message,
                    Tags: entry.Value.Tags.Count > 0 ? entry.Value.Tags.ToArray() : null
                )
            )
        );

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, JsonOptions),
            context.RequestAborted);
    }

    public sealed record HealthReportResponse(
        string Status,
        double TotalDurationMs,
        IReadOnlyDictionary<string, HealthEntryResponse> Checks
    );

    public sealed record HealthEntryResponse(
        string Status,
        double DurationMs,
        string? Description,
        string? Error,
        string[]? Tags
    );
}

```

Registro e mapeamento no `Program.cs`:

\


C#

```
using InvestDashboard.WebAPI.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var ollamaEndpoint = builder.Configuration["AI:Ollama:Endpoint"];

var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

if (string.Equals(builder.Configuration["AI:Provider"], "Ollama", StringComparison.OrdinalIgnoreCase)
    && !string.IsNullOrWhiteSpace(ollamaEndpoint))
{
    builder.Services.AddHttpClient("ollama-health", client =>
    {
        client.BaseAddress = new Uri(ollamaEndpoint);
        client.Timeout = TimeSpan.FromSeconds(3);
    });

    healthChecks.AddCheck("ollama", async (sp, ct) =>
    {
        try
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ollama-health");
            using var response = await client.GetAsync("/", ct);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Ollama acessível.")
                : HealthCheckResult.Degraded($"Ollama retornou status {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao conectar ao Ollama.", ex);
        }
    }, tags: ["ready"]);
}

// Após var app = builder.Build();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
});

```

## 6. Módulos Úteis de `ML.NET` (Anomalias & Classificação de Renda Fixa)

Pacotes: `Microsoft.ML` e `Microsoft.ML.TimeSeries`.

### 6.1 Detector de Anomalia Estatística em Saldos e Cotações (`DetectIidSpike`)

C#

```
using Microsoft.ML;
using Microsoft.ML.Data;

public sealed class HistoricoSaldo
{
    public float Saldo { get; set; }
}

public sealed class ResultadoAnomalia
{
    [VectorType(3)]
    public double[] Prediction { get; set; } = [];
}

public static class ValidadorAnomaliaPosicao
{
    public static bool EhValorSuspeito(IReadOnlyList<decimal> historicoSaldos, decimal novoSaldoExtraido)
    {
        if (historicoSaldos.Count < 5)
            return false;

        var mlContext = new MLContext(seed: 1);

        var dados = historicoSaldos
            .Append(novoSaldoExtraido)
            .Select(s => new HistoricoSaldo { Saldo = (float)s })
            .ToList();

        var dataView = mlContext.Data.LoadFromEnumerable(dados);

        var pipeline = mlContext.Transforms.DetectIidSpike(
            outputColumnName: nameof(ResultadoAnomalia.Prediction),
            inputColumnName: nameof(HistoricoSaldo.Saldo),
            confidence: 98.0,
            pvalueHistoryLength: Math.Max(4, historicoSaldos.Count / 2));

        var model = pipeline.Fit(dataView);
        var transformed = model.Transform(dataView);

        var ultimaPrevisao = mlContext.Data
            .CreateEnumerable<ResultadoAnomalia>(transformed, reuseRowObject: false)
            .Last();

        return ultimaPrevisao.Prediction[0] == 1.0;
    }
}

```

### 6.2 Classificador de Indexador e Produto de Renda Fixa (`SdcaMaximumEntropy`)

C#

```
using Microsoft.ML;
using Microsoft.ML.Data;

public sealed class TituloRendaFixaInput
{
    public string Descricao { get; set; } = string.Empty;
    public string Indexador { get; set; } = string.Empty;
}

public sealed class TituloRendaFixaPrediction
{
    [ColumnName("PredictedLabel")]
    public string IndexadorPrevisto { get; set; } = string.Empty;

    public float[] Score { get; set; } = [];
}

public sealed class ClassificadorIndexadorRendaFixa
{
    private readonly MLContext _mlContext = new(seed: 42);
    private PredictionEngine<TituloRendaFixaInput, TituloRendaFixaPrediction>? _engine;

    public void Treinar(IEnumerable<TituloRendaFixaInput> exemplos)
    {
        var data = _mlContext.Data.LoadFromEnumerable(exemplos);

        var pipeline = _mlContext.Transforms.Conversion
            .MapValueToKey(outputColumnName: "Label", inputColumnName: nameof(TituloRendaFixaInput.Indexador))
            .Append(_mlContext.Transforms.Text.FeaturizeText(
                outputColumnName: "Features",
                inputColumnName: nameof(TituloRendaFixaInput.Descricao)))
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        var model = pipeline.Fit(data);
        _engine = _mlContext.Model.CreatePredictionEngine<TituloRendaFixaInput, TituloRendaFixaPrediction>(model);
    }

    public (string Indexador, float Confianca) Classificar(string descricaoTitulo)
    {
        if (_engine is null)
            throw new InvalidOperationException("O modelo ainda não foi treinado.");

        var pred = _engine.Predict(new TituloRendaFixaInput { Descricao = descricaoTitulo });
        var confianca = pred.Score.Length > 0 ? pred.Score.Max() : 0f;

        return (pred.IndexadorPrevisto, confianca);
    }
}

```

## 7. Testes Unitários (`tests/`)

C#

```
using System.Text.Json;
using InvestDashboard.Application.DTOs.Tesouro;
using InvestDashboard.Application.UseCases.Tesouro;
using InvestDashboard.Infrastructure.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InvestDashboard.UnitTests.AI;

public sealed class ImportarPosicaoTesouroPorImagemHandlerTests
{
    [Fact]
    public async Task HandleAsync_QuandoImagemValida_DeveExtrairECalcularRentabilidadeAusente()
    {
        var expectedDto = new TesouroPosicaoExtraidaDto(
            NomeTitulo: "Tesouro Selic 2029",
            TipoTitulo: "Tesouro Selic",
            SaldoAtual: 11200.00m,
            ValorAplicado: 10000.00m,
            DataVencimento: new DateOnly(2029, 3, 1),
            DataAtualizacao: null,
            RentabilidadeValor: null,
            RentabilidadePercentual: null
        );

        var fakeChatClient = new FakeJsonChatClient(JsonSerializer.Serialize(expectedDto));
        var extractor = new TesouroImageExtractorService(
            fakeChatClient,
            NullLogger<TesouroImageExtractorService>.Instance);

        var handler = new ImportarPosicaoTesouroPorImagemHandler(extractor);
        var command = new ImportarPosicaoTesouroPorImagemCommand(
            UserId: Guid.NewGuid(),
            ImagemBytes: new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            MimeType: "image/png"
        );

        var result = await handler.HandleAsync(command);

        Assert.True(result.AtualizadoComSucesso);
        Assert.Equal("Tesouro Selic 2029", result.DadosExtraidos.NomeTitulo);
        Assert.Equal(1200.00m, result.DadosExtraidos.RentabilidadeValor);
        Assert.Equal(12.00m, result.DadosExtraidos.RentabilidadePercentual);
        Assert.NotNull(result.DadosExtraidos.DataAtualizacao);
    }

    [Fact]
    public async Task HandleAsync_QuandoMimeTypeInvalido_DeveLancarArgumentException()
    {
        var fakeChatClient = new FakeJsonChatClient("{}");
        var extractor = new TesouroImageExtractorService(
            fakeChatClient,
            NullLogger<TesouroImageExtractorService>.Instance);

        var handler = new ImportarPosicaoTesouroPorImagemHandler(extractor);
        var command = new ImportarPosicaoTesouroPorImagemCommand(
            UserId: Guid.NewGuid(),
            ImagemBytes: new byte[] { 1, 2, 3 },
            MimeType: "application/pdf"
        );

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(command));
    }

    private sealed class FakeJsonChatClient(string jsonResponse) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var message = new ChatMessage(ChatRole.Assistant, jsonResponse);
            return Task.FromResult(new ChatResponse(message));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}

```

## 8. Guia de Operação e Integração (`docs/integracao-ia-chatgpt.md`)

### 8.1 Comandos do Ollama no Docker

Bash

```
# Subir os containers
docker compose up -d --build

# Baixar o modelo de visão (persistido no volume ollama_data)
docker exec -it invest-ollama ollama pull qwen2.5vl:7b

# Verificar modelos instalados
docker exec -it invest-ollama ollama list

# Testar o healthcheck detalhado da API
curl -i http://localhost:8080/health

```

### 8.2 Configurar Custom GPT Actions + OAuth do Supabase

1. **Pré-requisito de Rede:** Expor a `InvestDashboard.WebAPI` em **HTTPS público** (ex.: Render, Railway, Azure ou túnel Cloudflare/ngrok).
2. **Supabase Auth (**`Authentication -> OAuth Server`**):**
   - Ative **Enable OAuth Server** e crie um OAuth App (ex.: `ChatGPT Invest Dashboard`).
   - Copie o **Client ID** e **Client Secret**.
   - Endpoints do Supabase:
     - **Authorization URL:** `https://<SEU-PROJETO>.supabase.co/auth/v1/oauth/authorize`
     - **Token URL:** `https://<SEU-PROJETO>.supabase.co/auth/v1/oauth/token`
3. **ChatGPT Plus (**`Explorar GPTs -> Criar -> Configurar -> Criar nova ação`**):**
   - Importe o OpenAPI JSON (`https://<seu-dominio>/swagger/v1/swagger.json`), garantindo que `servers[0].url` aponte para o domínio HTTPS público sem duplicar `/api`.
   - Em **Autenticação**, escolha **OAuth**, informe o **Client ID**, **Client Secret**, **Authorization URL**, **Token URL**, **Scope** (`openid email profile`) e **Token Exchange Method** (`Default (POST request)`).
   - Salve a Action, copie a **Callback URL** exibida pelo ChatGPT (`[https://chatgpt.com/aip/](https://chatgpt.com/aip/)<id>/oauth/callback`) e cadastre-a em **Redirect URIs** no OAuth App do Supabase.
4. **Isolamento Multi-Tenant:** Como todos os endpoints usam `.RequireAuthorization()` e leem o `UserId` exclusivamente da claim `sub` do JWT do Supabase, cada usuário consulta e atualiza apenas a própria carteira.

### 8.3 Boas Práticas Operacionais

1. **Redimensionamento antes da IA:** Redimensione prints maiores que `1280px` no lado maior antes de enviar ao `IChatClient` (reduz até 60% dos tokens na nuvem e acelera a inferência no Ollama sem perda de legibilidade).
2. **Validação Matemática:** Sempre valide no domínio se $\text{Quantidade} \times \text{Preço Médio} \approx \text{Valor Aplicado}$ e se $\text{SaldoAtual} - \text{ValorAplicado}$ condiz com a rentabilidade extraída.
