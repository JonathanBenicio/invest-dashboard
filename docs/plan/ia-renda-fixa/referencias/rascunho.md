> Exportação histórica de [Rascunho](https://chatgpt.com/space/page_85f156fd618481918d068ae937910f24). Para o escopo e as decisões vigentes, consulte o [épico e os subplanos](../epico.md).

aqui tem histórico de ontem saiu consolidado

\
hoje uso o .html do site do tesouro como base para importar posição de investimento, se usase uma imagem assim, que ia local eu teria que rodar,configurar e quantos custaria na api chatgpt ou usaar gpt code cli

Para extrair os dados dessa imagem (título, saldo, valor aplicado, vencimento e rentabilidade) e transformar em JSON ou CSV automaticamente, **eu iria de API da OpenAI (modelo `gpt-5.4-mini` ou `gpt-6-luna`) por custar menos de R$ 0,03 por imagem e exigir zero configuração de GPU**. Se a sua prioridade absoluta for privacidade offline, rodar um modelo local leve com **Ollama** resolve facilmente.

---

**1. Opção Local (Custo Zero, 100% Privado)**

Prints de aplicativos têm fundo limpo e fonte nítida, então você não precisa de modelos gigantes. Um modelo multimodal pequeno (3B a 7B parâmetros) já lê esses números com altíssima precisão.

- **Qual IA escolher:** **Qwen2.5-VL (3B ou 7B)** ou **Llama 3.2 Vision (11B)**. Para esse layout simples, até um OCR tradicional em Python como **PaddleOCR** ou **EasyOCR** + Regex resolve sem precisar de placa de vídeo dedicada.
- **Como configurar:**
  1. Instale o [**Ollama**](https://ollama.com/) na sua máquina.
  2. No terminal, baixe o modelo com o comando: `ollama run qwen2.5vl:7b` (exige cerca de 6 GB de VRAM ou 8 GB de RAM).
  3. Faça seu script Python enviar a imagem em Base64 para a API local do Ollama (`http://localhost:11434/api/generate`) pedindo saída em formato `json` (Structured Outputs).

---

**2. Opção API da OpenAI (Mais Rápida e Barata na Nuvem)**

Uma imagem vertical de celular como essa consome cerca de **1.000 a 1.300 tokens** em alta resolução. Somando o prompt e a resposta curta em JSON (\~150 tokens), o custo é praticamente irrisório.

| Modelo OpenAI | Custo por 1 imagem | Custo para 100 imagens | Indicado para |
| - | - | - | - |
| **gpt-6-luna / gpt-5.4-nano** | \~US$ 0,0003 (**\~R$ 0,002**) | \~US$ 0,03 (**\~R$ 0,18**) | Máxima economia em volume |
| **gpt-5.4-mini / gpt-6.1-terra** | \~US$ 0,0015 (**\~R$ 0,01**) | \~US$ 0,15 (**\~R$ 0,90**) | Melhor equilíbrio entre precisão e preço |
| **gpt-6.1-sol / gpt-6-astra** | \~US$ 0,006 a US$ 0,015 (**R$ 0,03 a R$ 0,09**) | \~US$ 1,00 (**\~R$ 5,80**) | Documentos complexos ou borrados |

*Nota: Valores aproximados considerando dólar a R$ 5,80 e preços da **[API da OpenAI](https://developers.openai.com/api/docs/pricing)**.* [[1](https://developers.openai.com/api/docs/pricing)]

---

**3. Usando o Codex CLI (GPT Code CLI)**

O **OpenAI Codex CLI** funciona como um agente de terminal para programar e manipular arquivos locais, e pode ler imagens anexadas no comando.

- **Se você já assina o ChatGPT Plus (US$ 20/mês):** O uso do Codex CLI está incluído na assinatura sem custo extra por token, sujeito apenas à franquia de mensagens a cada 5 horas. É ótimo se você quiser jogar o print no terminal e pedir para ele atualizar sua planilha ou banco localmente. [[1](https://vdf.ai/blog/codex-pricing/), [2](https://www.nocode.mba/articles/codex-pricing)]
- **Se usar com API Key:** Você paga exatamente os valores da tabela acima por token consumido, mas o agente gasta um pouco mais de tokens de contexto a cada interação por carregar instruções do sistema e ler arquivos do projeto. [[1](https://vdf.ai/blog/codex-pricing/)]
- **Limitação para automação:** O CLI é feito para uso interativo no terminal. Se a sua ideia é apertar um botão e importar vários prints automaticamente, chamar a **API direta via script Python** é muito mais rápido, barato e previsível.

---

**Comparativo Resumido**

| Critério | IA Local (Ollama) | API OpenAI (`mini`/`luna`) | Codex CLI (Plano Plus) |
| - | - | - | - |
| **Custo** | R$ 0 (usa sua energia/hardware) | \~R$ 0,01 por print | US$ 20/mês (incluso no plano) |
| **Configuração** | Média (instalar Ollama + baixar modelo) | Simples (10 linhas de Python + API Key) | Simples (instalar CLI + login) |
| **Privacidade** | Total (dados não saem do PC) | Dados enviados à nuvem (sem treino por padrão) | Dados enviados à nuvem |
| **Automação** | Excelente | Excelente | Manual / Interativo |

Para eu te passar o código pronto de extração, me diga:

- Qual **configuração do seu computador** (placa de vídeo ou memória RAM)
- Em qual **formato** você salva hoje (CSV, Excel, banco de dados ou JSON)

Mostrar código

**+4**

- VDF.AI

  **Codex Pricing 2026: Plus vs Pro, CLI Cost and Limits - VDF.AI**

  14 de nov. de 2026 — Monthly price | $20 | $100 | $200 | $500 | | Annual billing. CLI pricing with an API key. The token counts are ours, chosen for ro...
- OpenAI Help Center

  **Using Codex with your ChatGPT plan | OpenAI Help Center**

  October 22, 2026 — # Overview \* Availability Codex is included across ChatGPT plans, including Free and Go. Codex Cloud is available to eligible Plus...
- Microsoft Azure

  **Azure OpenAI Service - Pricing**

  GPT 5.4 Series. Built for Reliable AI Production: Stronger reasoning, dependable execution, and agentic workflows at scale. ; GPT-

**Mostrar tudo**

api key gpt tem cretido fre?

**Não, contas novas não ganham mais créditos gratuitos automáticos na API da OpenAI**. Hoje o sistema é pré-pago: você precisa cadastrar um cartão e comprar pelo menos **US$ 5** (cerca de R$ 29) para começar a usar. [[1](https://community.openai.com/t/api-access-using-free-tier/710656), [2](https://github.com/orgs/community/discussions/181068), [3](https://community.openai.com/t/can-i-use-openai-api-with-the-free-account/977476), [4](https://benchlm.ai/free-tier/openai)]

As únicas exceções oficiais para ter saldo gratuito são:

- **Assinantes ChatGPT Pro ou Business:** Quem paga os planos Pro (US$ 100/200/500) ou Business recebe o valor equivalente em créditos mensais de API vinculados ao mesmo e-mail (não vale para o plano Plus de US$ 20). [[1](https://developers.openai.com/api/docs/guides/chatgpt-api-credits), [2](https://help.openai.com/en/articles/20001626-api-credits-benefit-for-eligible-chatgpt-subscriptions)]
- **Programa de Compartilhamento de Dados:** Após ter saldo positivo na conta, você pode aceitar que a OpenAI use seus dados de entrada/saída para treinar modelos e ganhar até **10 milhões de tokens grátis por dia** em modelos `mini`/`nano` (não recomendado para prints financeiros com seus dados pessoais). [[1](https://pricepertoken.com/endpoints/openai/free)]

---

**Alternativas 100% gratuitas na nuvem (sem cartão)**

Se você não quer gastar os US$ 5 na OpenAI e também não quer rodar IA pesada no seu PC, pode usar APIs gratuitas que lêem imagens muito bem:

1. **Google AI Studio (Gemini API):** Tem um nível gratuito real (Free Tier) sem precisar cadastrar cartão de crédito, com limite de requisições por minuto mais que suficiente para uso pessoal.
2. **Groq ou OpenRouter:** Ambos oferecem chaves de API gratuitas com cota diária para modelos abertos multimodais (como o **Llama 3.2 Vision** ou **Qwen-VL**) rodando nos servidores deles.

Se você quiser testar sem gastar nada, posso montar o script em Python usando o **Google AI Studio (gratuito)** ou **Ollama (local)** — qual deles você prefere?

**+4**

- OpenAI Developer Community

  **API Access using free tier**

  April 8, 2024 — “Free tier” is if you were granted API credits through a promotion or trial. OpenAI is no longer giving any credits to pay for use...
- OpenAI Developers

  **API credits for ChatGPT subscribers**

  Eligible paid Pro and Business plans ・ free monthly credits up to $500. Pro ($100/$200/$500 tiers) Business Standard/Premium grant...
- OpenAI Help Center

  **API credits benefit for eligible ChatGPT subscriptions**

  21 de nov. de 2026 — Eligible paid ChatGPT Pro and ChatGPT Business subscribers can claim free API credits as a subscription benefit to build apps and ...

**Mostrar tudo**

meu back é em .net, usaria maf?

**Não, para ler esse print e extrair os dados, eu não usaria o Microsoft Agent Framework (MAF) — eu usaria direto o `Microsoft.Extensions.AI` (`IChatClient`)**.

Como você só precisa enviar uma imagem e receber um objeto C# tipado (Structured Outputs), colocar o [**Microsoft Agent Framework**](https://learn.microsoft.com/pt-br/agent-framework/overview/) no meio adiciona uma camada desnecessária de orquestração. [[1](https://learn.microsoft.com/pt-br/agent-framework/overview/), [2](https://learn.microsoft.com/en-us/agent-framework/overview/)]

**Quando usar cada um no .NET**

| Abordagem | Pacote NuGet Principal | Quando vale a pena |
| - | - | - |
| **`Microsoft.Extensions.AI`** (Recomendado) | `Microsoft.Extensions.AI` | **Chamada direta (1 passo):** você manda o print e recebe um `record PosicaoTesouro(...)` desserializado na hora. Funciona igual para OpenAI, Gemini ou Ollama local. |
| **Microsoft Agent Framework (MAF)** | `Microsoft.Agents.AI` | **Fluxos complexos (multi-passo):** quando o agente precisa decidir sozinho quais ferramentas (`Tools`) chamar, consultar seu banco de dados para comparar saldos, pedir confirmação humana ou encadear vários agentes. |

---

**Como fica a estrutura limpa no seu backend .NET**

O próprio MAF usa o `Microsoft.Extensions.AI` por baixo dos panos. Se você começar com ele agora, seu código fica desacoplado do provedor de IA: [[1](https://balta.io/articles/construindo-aplicacoes-de-ia-com-dotnet), [2](https://managed-code.com/blog-post/microsoft-extensions-ai-explained)]

1. **Injeção de Dependência (`Program.cs`):** Você registra uma implementação de `IChatClient` (ex: `OllamaChatClient` para rodar local ou `OpenAIChatClient` / `GeminiChatClient` para nuvem).
2. **Envio da Imagem:** Você monta um `ChatMessage` contendo o texto do prompt e um `DataContent` com os bytes da imagem (`image/jpeg` ou `image/png`).
3. **Retorno Tipado:** Você chama `chatClient.GetResponseAsync<PosicaoTesouro>(messages)` e o framework já força o modelo a responder no schema do seu DTO C# e faz o parse automático. [[1](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai), [2](https://balta.io/articles/construindo-aplicacoes-de-ia-com-dotnet)]

Se você quiser o trecho de código em C# pronto, me diga qual **provedor** pretende plugar no `IChatClient` (**Ollama local**, **Google Gemini gratuito** ou **OpenAI**).

**+4**

- Microsoft Learn

  **bibliotecas de Microsoft.Extensions.AI - .NET | Microsoft Learn**

  As bibliotecas Microsoft.Extensions.AI para .NET fornecem uma abordagem unificada para integrar serviços de IA generativa. Pacotes...
- Microsoft Learn

  **Microsoft.Extensions.AI libraries - .NET | Microsoft Learn**

  September 14, 2026 — The IImageGenerator interface represents a generator for creating images from text prompts or other input.
- Microsoft Learn

  **Visão geral do Microsoft Agent Framework | Microsoft Learn**

  O Microsoft Agent Framework reúne Agentes (LLMs, ferramentas, servidores MCP; suporta Microsoft Foundry, Anthropic, Azure OpenAI, ...

**Mostrar tudo**

Qwen2.5-VL (3B ou 7B) ou Llama 3.2 Vision (11B). Para esse layout simples, até um OCR tradicional em Python como PaddleOCR ou EasyOCR + Regex resolve sem precisar de placa de vídeo dedicada.

eu pago o gpt plus e projeto [https://github.com/JonathanBenicio/invest-dashboard/tree/codex/tesouro-previa-e-cotacoes](https://github.com/JonathanBenicio/invest-dashboard/tree/codex/tesouro-previa-e-cotacoes), crie um plano,intruções para eu manda no chat. usando Microsoft.Extensions.AI (OpenRouter e IA Local (Ollama)), configurar o docker do ollama e rede. Duvida, como integrar meu sistema ao chat gpt?(assim pessoas poderiam pedi analise ou atualizar posição)Qual IA local/Openrouter teria que ter para isso tbm?

Como você já usa o **ChatGPT Plus**, a forma mais prática de integrar o seu **Invest Dashboard (.NET 10 + DDD)** diretamente ao ChatGPT (para que usuários possam pedir análises ou atualizar posições conversando com ele) é criar um **Custom GPT com Actions (OpenAPI)** ou um **servidor MCP (Model Context Protocol)** apontando para a sua API.

Abaixo está a resposta completa dividida em: **1) Modelos recomendados para Vision + Tool Calling**, **2) Como integrar ao ChatGPT Plus**, e **3) O prompt/plano pronto** para você colar direto no seu Codex CLI ou ChatGPT.

---

**1. Qual IA escolher (Vision + Chamada de Ferramentas / JSON Estruturado)**

Para ler o print do Tesouro Direto e devolver um JSON estruturado (ou chamar funções do seu backend), o modelo precisa suportar **Vision (visão)** + **Structured Outputs / Tool Calling**:

- **Local (Ollama):** **`qwen2.5vl:7b`** (ou `qwen2.5vl:3b` se tiver pouca RAM/VRAM). Ele é excelente para ler números em português, aceita saída estruturada em JSON via `Microsoft.Extensions.AI` e suporta chamadas de ferramentas.
- **Nuvem (OpenRouter):**
  - **Gratuito / Custo Zero:** **`qwen/qwen2.5-vl-72b-instruct:free`** ou **`meta-llama/llama-3.2-11b-vision-instruct:free`** (sujeitos a limite de requisições por minuto do tier gratuito do OpenRouter).
  - **Produção / Muito Barato (\~R$ 0,01 por imagem):** **`google/gemini-2.5-flash`** ou **`openai/gpt-5.4-mini`** via OpenRouter (ambos têm suporte nativo a Vision, JSON Schema estrito e Tool Calling).

---

**2. Como integrar o seu sistema ao ChatGPT (para usuários consultarem e atualizarem posições)**

Você tem **duas formas oficiais** de fazer o ChatGPT conversar com o seu backend `.NET 10`:

1. **Custom GPT + Actions (OpenAPI / Swagger) — Mais rápido e visual:**
   - No seu `InvestDashboard.WebAPI`, você expõe a documentação OpenAPI/Swagger (ex: `/swagger/v1/swagger.json`) com endpoints específicos para o agente (ex: `GET /api/portfolio/resumo` e `POST /api/portfolio/tesouro/importar-imagem`).
   - No ChatGPT Plus, você vai em **Explorar GPTs -> Criar -> Configurar -> Criar nova ação**, cola o JSON do seu Swagger e configura a autenticação (OAuth 2.0 com o seu Supabase Auth ou uma API Key por usuário).
   - Assim, qualquer pessoa com o link do seu GPT pode mandar um print no chat do ChatGPT e dizer *"Atualize minha posição no Tesouro e analise minha carteira"*, e o ChatGPT chamará sua API.
2. **Servidor MCP (Model Context Protocol) — Padrão mais moderno:**
   - Você adiciona o pacote oficial `ModelContextProtocol` no seu backend `.NET` e expõe seus casos de uso (`InvestDashboard.Application`) como *MCP Tools*. O ChatGPT (via modo desenvolvedor/conectores) e o Codex CLI conseguem se conectar diretamente ao endpoint SSE/HTTP do seu servidor MCP.

---

**3. Plano e Instruções (Copie e cole no ChatGPT / Codex CLI)**

Como o seu repositório já segue a arquitetura DDD (`Domain`, `Application`, `Infrastructure`, `WebAPI`) e possui `docker-compose.yml`, cole o texto abaixo no seu chat/Codex para ele implementar tudo seguindo os padrões do projeto:

**markdown**

```
Atue como Arquiteto .NET Sênior especialista em Clean Architecture, DDD e Microsoft.Extensions.AI.
Analise a estrutura atual deste repositório (`invest-dashboard`, branch `codex/tesouro-previa-e-cotacoes`, com `.NET 10`, `InvestDashboard.Domain`, `InvestDashboard.Application`, `InvestDashboard.Infrastructure`, `InvestDashboard.WebAPI` e `docker-compose.yml`) e implemente a extração de posições do Tesouro Direto a partir de imagens (prints de aplicativos) e a preparação da API para integração com o ChatGPT.

### Requisitos Técnicos

1. **Infraestrutura Docker & Rede (`docker-compose.yml` e `.env.example`):**
   - Adicione um serviço `ollama` no `docker-compose.yml` na mesma rede interna dos containers (`api` e `db`), expondo a porta `11434` apenas internamente (ou localhost), com volume persistente `ollama_data:/root/.ollama` e suporte opcional comentado para GPU NVIDIA.
   - Adicione variáveis de ambiente no `.env.example` e `appsettings`:
     - `AI__Provider`: `"Ollama"` ou `"OpenRouter"`
     - `AI__Ollama__Endpoint`: `"http://ollama:11434"`
     - `AI__Ollama__Model`: `"qwen2.5vl:7b"`
     - `AI__OpenRouter__Endpoint`: `"https://openrouter.ai"`
     - `AI__OpenRouter__ApiKey`: `""`
     - `AI__OpenRouter__Model`: `"google/gemini-2.5-flash"`

2. **Abstração de IA com `Microsoft.Extensions.AI` (`InvestDashboard.Application` e `Infrastructure`):**
   - Instale os pacotes `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.Ollama` e `Microsoft.Extensions.AI.OpenAI` (para compatibilidade OpenAI-compatible com o OpenRouter).
   - Crie uma interface na camada `Application` (ex: `ITesouroImageExtractorService`) que receba os bytes/stream da imagem e retorne um DTO tipado (`TesouroPosicaoExtraidaDto` contendo: `NomeTitulo`, `TipoTitulo`, `SaldoAtual`, `ValorAplicado`, `DataVencimento`, `DataAtualizacao`, `RentabilidadeValor`, `RentabilidadePercentual`).
   - Implemente o serviço na `Infrastructure` usando `IChatClient` com **Structured Outputs** (`GetResponseAsync<TesouroPosicaoExtraidaDto>`) enviando a imagem via `DataContent`.
   - Registre o `IChatClient` no DI (`DependencyInjection.cs`) alternando dinamicamente entre Ollama e OpenRouter conforme `AI__Provider`.

3. **Caso de Uso e Endpoint (`Application` e `WebAPI`):**
   - Crie um Command/UseCase para importar a posição extraída da imagem, validando os valores e reaproveitando as regras de domínio existentes de atualização de posição do Tesouro Direto.
   - Crie o endpoint `POST /api/tesouro/importar-imagem` (aceitando `IFormFile` ou Base64 JSON) documentado com OpenAPI/Swagger (`ProducesResponseType`, descrições claras nos parâmetros e schemas) para que possa ser consumido tanto pelo frontend React quanto por uma **Custom GPT Action** no ChatGPT.
   - Crie também um endpoint `GET /api/portfolio/resumo-ia` otimizado para retornar o consolidado da carteira em formato enxuto para análise por LLMs externas (ChatGPT).

4. **Testes & Documentação:**
   - Crie testes unitários em `tests/` mockando o `IChatClient` para validar o fluxo do Use Case.
   - Gere um arquivo `docs/integracao-ia-chatgpt.md` explicando:
     a) Como baixar o modelo no container (`docker exec -it <container_ollama> ollama pull qwen2.5vl:7b`).
     b) Como importar o `swagger.json` no ChatGPT Plus (Custom GPT Actions) e configurar a autenticação JWT/Supabase para usuários consultarem e atualizarem suas carteiras pelo chat.

Antes de escrever o código, leia os arquivos existentes de `InvestDashboard.Domain`, `InvestDashboard.Application` (especialmente o fluxo atual de importação HTML do Tesouro Direto) e `docker-compose.yml` para manter total compatibilidade com as entidades, convenções e contratos atuais.

```

Use o código com cuidado.

Se você quiser, posso detalhar como configurar a **autenticação OAuth do Supabase dentro do Custom GPT** para que cada usuário acesse apenas a própria carteira ao conversar com o ChatGPT.

Extrair dados de investimentos com IA

Para extrair dados de investimentos (prints de corretoras, notas de corretagem, extratos do Tesouro Direto ou PDFs) usando IA em um backend **.NET 10** com **Microsoft.Extensions.AI**, o segredo está em combinar **modelos multimodais (Vision)** com **Structured Outputs (Saída Estruturada)**, garantindo que a IA devolva exatamente o objeto C# tipado que você precisa.

### 1. Escolha do Modelo (Local vs. Nuvem)

Para ler tabelas e números financeiros sem alucinação, você precisa de modelos com bom suporte a *Vision* e *JSON Schema*:

- **Local (Custo Zero / Privacidade Total via Ollama):**
  - `qwen2.5vl:7b` (ou `qwen2.5vl:3b` para máquinas com menos memória): Excelente leitura de caracteres e números em português, leve e compatível com saída estruturada.
- **Nuvem (Via OpenRouter — Pay-per-use ou Free Tier):**
  - **Gratuito:** `qwen/qwen2.5-vl-72b-instruct:free` ou `meta-llama/llama-3.2-11b-vision-instruct:free` (ótimos para desenvolvimento e baixo volume).
  - **Produção (\~R$ 0,01 por imagem):** `google/gemini-2.5-flash` ou `openai/gpt-5.4-mini` (baixa latência e altíssima precisão em tabelas financeiras).

### 2. Implementação Limpa em .NET (`Microsoft.Extensions.AI`)

Usar a abstração `IChatClient` evita acoplamento com um provedor específico — você troca entre Ollama local e OpenRouter apenas alterando o `appsettings.json`.

#### Passo A: Definir o Contrato de Saída (DTO)

Use `record` e anotações de descrição para guiar o parser da IA:

**C#**

```
using System.ComponentModel;

public record PosicaoInvestimentoExtraida(
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

```

#### Passo B: Serviço de Extração com `IChatClient`

Envie os bytes da imagem via `DataContent` e peça a desserialização tipada com `GetResponseAsync<T>`:

**C#**

```
using Microsoft.Extensions.AI;

public class InvestimentoExtractorService(IChatClient chatClient)
{
    public async Task<PosicaoInvestimentoExtraida?> ExtrairDeImagemAsync(
        byte[] imagemBytes,
        string mimeType,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, """
                Você é um especialista em extração de dados financeiros brasileiros.
                Extraia os valores exatamente como aparecem na imagem.
                Converta formatos monetários brasileiros (ex: R$ 1.250,50) para decimal padrão (1250.50).
                Não invente valores; se um campo não existir na imagem, retorne null ou 0.
                """),
            new(ChatRole.User, [
                new TextContent("Extraia a posição de investimento desta imagem."),
                new DataContent(imagemBytes, mimeType)
            ])
        };

        var response = await chatClient.GetResponseAsync<PosicaoInvestimentoExtraida>(
            messages,
            cancellationToken: ct);

        return response.Result;
    }
}

```

#### Passo C: Registro no Container de DI (`Program.cs`)

**C#**

```
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var provider = builder.Configuration["AI:Provider"]; // "Ollama" ou "OpenRouter"

if (provider == "Ollama")
{
    builder.Services.AddChatClient(new OllamaChatClient(
        new Uri(builder.Configuration["AI:Ollama:Endpoint"]!),
        builder.Configuration["AI:Ollama:Model"]!));
}
else
{
    var openAiClient = new OpenAIClient(
        new ApiKeyCredential(builder.Configuration["AI:OpenRouter:ApiKey"]!),
        new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") });

    builder.Services.AddChatClient(
        openAiClient.GetChatClient(builder.Configuration["AI:OpenRouter:Model"]!).AsIChatClient());
}

```

### 3. Boas Práticas para Evitar Erros de Extração

1. **Validação Matemática Pós-IA:** Nunca confie 100% na leitura de OCR/LLM. Crie uma regra de domínio que valide se Quantidade×Prec¸o Meˊdio≈Valor AplicadoQuantidade×Prec¸​o Meˊdio≈Valor Aplicado (com margem de tolerância para arredondamentos).
2. **Múltiplos Ativos na Mesma Tela:** Se o print contiver uma lista de ativos (como uma carteira inteira na B3 ou Tesouro), envolva o DTO em um objeto raiz: `public record CarteiraExtraida(List<PosicaoInvestimentoExtraida> Ativos);`.
3. **Resolução da Imagem:** Redimensione imagens muito grandes para no máximo `1280px` no lado maior antes de enviar — isso reduz o consumo de tokens na nuvem em até 60% e acelera a inferência local no Ollama sem perder legibilidade nos números.

Gere a configuração do docker-compose.yml para subir o Ollama na mesma rede da API .NET com volume persistente.

### `docker-compose.yml` para Ollama + API .NET

Abaixo está uma configuração com **rede interna compartilhada (**`app-network`**)**, **volume persistente (**`ollama_data`**)** para evitar o re-download de modelos pesados (como o `qwen2.5vl:7b`) a cada recriação de container, e **healthcheck** para que a API só inicie após o serviço do Ollama responder.

**YAML**

```
services:
  ollama:
    image: ollama/ollama:latest
    container_name: ollama-service
    restart: unless-stopped
    ports:
      - "11434:11434"
    volumes:
      - ollama_data:/root/.ollama
    environment:
      - OLLAMA_KEEP_ALIVE=24h
      - OLLAMA_NUM_PARALLEL=2
    networks:
      - app-network
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
      # Nome do serviço na rede interna do Docker Compose
      - AI__Provider=Ollama
      - AI__Ollama__Endpoint=http://ollama:11434
      - AI__Ollama__Model=qwen2.5vl:7b
    depends_on:
      ollama:
        condition: service_healthy
    networks:
      - app-network

volumes:
  ollama_data:
    driver: local

networks:
  app-network:
    driver: bridge

```

### Pontos importantes da arquitetura

1. **Comunicação via DNS interno:** Dentro da rede `app-network`, o container `investimentos-api` acessa o Ollama em `http://ollama:11434` (pelo nome do serviço), enquanto o `11434:11434` exposto no host permite rodar comandos diretos da sua máquina para baixar ou inspecionar modelos.
2. **Persistência de modelos:** O volume nomeado `ollama_data` guarda os pesos em `/root/.ollama`. Mesmo rodando `docker compose down`, os modelos permanecem salvos; só são removidos se você passar explicitamente a flag `-v`.
3. **Pré-aquecimento e&#x20;**`OLLAMA_KEEP_ALIVE`**:** Definir `OLLAMA_KEEP_ALIVE=24h` impede que o modelo seja descarregado da memória após poucos minutos de inatividade, evitando latência extra na primeira extração de um print ou PDF.
4. **Mapeamento de variáveis no .NET:** No `IConfiguration` do .NET, o duplo underscore `__` mapeia diretamente para a hierarquia JSON (`AI:Ollama:Endpoint` e `AI:Ollama:Model`).

### Comandos essenciais para subir e baixar o modelo

**Bash**

```
# 1. Subir os serviços
docker compose up -d

# 2. Baixar o modelo de visão dentro do container do Ollama
docker exec -it ollama-service ollama pull qwen2.5vl:7b

# 3. Acompanhar os logs da API e do Ollama
docker compose logs -f

```

> ***Nota sobre GPU no WSL2 / Linux:** Para habilitar aceleração de hardware com placa NVIDIA, instale o NVIDIA Container Toolkit no host/WSL2 e descomente a seção `deploy.resources.reservations.devices` no serviço `ollama`.*

Crie um Dockerfile multi-stage para publicar uma API em .NET 10 com usuário não-root e cache de camadas NuGet.

Abaixo está um **Dockerfile multi-stage** para uma API em **.NET 10** (`aspnet:10.0` e `sdk:10.0`, que no .NET 10 têm Ubuntu 24.04 como base padrão), usando usuário não-root e cache de pacotes NuGet tanto entre camadas quanto via BuildKit cache mount (`--mount=type=cache`).

Workik



### `Dockerfile`

**Dockerfile**

```
# syntax=docker/dockerfile:1.7

# ==============================================================================
# 1. Estágio de Restore (isolado para aproveitar o cache de camadas do NuGet)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copie arquivos da solução e projetos antes do código-fonte
# Ajuste os caminhos conforme a estrutura do seu repositório (ex.: .sln, .csproj, Directory.Packages.props)
COPY Directory.Build.props* Directory.Packages.props* nuget.config* ./
COPY src/Investimentos.Api/Investimentos.Api.csproj src/Investimentos.Api/

# Restaura os pacotes usando cache persistente do BuildKit
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages \
    dotnet restore src/Investimentos.Api/Investimentos.Api.csproj

# ==============================================================================
# 2. Estágio de Build & Publish
# ==============================================================================
FROM restore AS build
WORKDIR /src

COPY . .

RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages \
    dotnet publish src/Investimentos.Api/Investimentos.Api.csproj \
      -c Release \
      -o /app/publish \
      --no-restore

# ==============================================================================
# 3. Estágio Final (Runtime leve + Usuário não-root)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Evita que o .NET escreva caches ou arquivos temporários em pastas sem permissão
ENV DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_HTTP_PORTS=8080

# Cria usuário e grupo dedicados sem privilégios de root (UID/GID 10001)
RUN groupadd --system --gid 10001 appgroup \
    && useradd --system --uid 10001 --gid 10001 --home-dir /app --shell /usr/sbin/nologin appuser

# Copia os binários publicados já com propriedade do usuário não-root
COPY --from=build --chown=10001:10001 /app/publish ./

USER 10001:10001
EXPOSE 8080

# Substitua "Investimentos.Api.dll" pelo nome do assembly gerado pela sua API
ENTRYPOINT ["dotnet", "Investimentos.Api.dll"]

```

### Arquivo `.dockerignore` recomendado

Sem um `.dockerignore`, o contexto enviado ao daemon Docker incluirá `bin/`, `obj/`, `.git/` e arquivos locais, quebrando o cache de camadas e aumentando o tempo de build:

**Snippet de código**

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

### Por que essa estrutura funciona bem

1. **Cache em dois níveis para o NuGet:**
   - Copiar apenas os `.csproj` / `Directory.Packages.props` no estágio `restore` garante que alterações no código C# não invalidem a etapa de restauração.
   - O `--mount=type=cache,id=nuget-packages,target=/root/.nuget/packages` mantém os pacotes baixados no cache do BuildKit entre builds sucessivos (mesmo quando você altera um `.csproj` ou adiciona um pacote novo).
2. **Usuário não-root explícito (**`10001:10001`**):**
   - Usar `--chown=10001:10001` no `COPY` evita uma camada extra pesada de `RUN chown -R` sobre todos os arquivos publicados.
   - Com `ASPNETCORE_HTTP_PORTS=8080`, o Kestrel escuta na porta `8080` sem precisar de permissão privilegiada (portas `< 1024`).
3. **Variante opcional (**`chiseled`**):**
   - Se não precisar de `bash` ou utilitários do sistema operacional dentro do container em produção, você pode trocar a imagem final para `[mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled](https://mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled)` (mantendo o `USER` compatível — as imagens *chiseled* já vêm configuradas para rodar sem root por padrão, como o usuário `app`).

### Como testar o build

**Bash**

```
DOCKER_BUILDKIT=1 docker build -t investimentos-api:10.0 .

```

Mostre como configurar o endpoint de /health no .NET 10 para o Docker Compose monitorar a API.

Para configurar o endpoint `/health` no **.NET 10** e integrá-lo ao **Docker Compose**, você utiliza o middleware nativo `Microsoft.AspNetCore.Diagnostics.HealthChecks` (já incluso no framework ASP.NET Core) e declara um `healthcheck` no serviço `investimentos-api`.

Uma boa prática em ambientes com LLM/Ollama é separar **liveness** (`/health/live`, verifica apenas se o processo HTTP da API está de pé) de **readiness** (`/health/ready`, verifica se o serviço do Ollama está alcançável), evitando que a API entre em loop de reinício se o modelo ainda estiver carregando na memória.

### 1. Configurar os Health Checks no `Program.cs`

Adicione o registro de serviços, uma verificação HTTP simples para o endpoint do Ollama (sem depender de pacotes extras) e o mapeamento dos endpoints:

**C#**

```
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var ollamaEndpoint = builder.Configuration["AI:Ollama:Endpoint"];

var healthChecks = builder.Services.AddHealthChecks()
    // Liveness: apenas confirma que a pipeline ASP.NET está respondendo
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

// Readiness: verifica conectividade com o Ollama quando o provider for Ollama
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
            // O Ollama responde 200 OK na raiz ("/") quando o servidor está ativo
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

var app = builder.Build();

// Endpoint geral /health (executa todas as verificações registradas)
app.MapHealthChecks("/health");

// Opcional: endpoints dedicados para liveness e readiness
app.MapHealthChecks("/health/live", new()
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

// ... restante da configuração (endpoints, controllers, etc.)

app.Run();

```

> ***Dica de produção:** Mantenha `/health` leve e rápido. Evite chamar `/api/generate` ou carregar um modelo pesado dentro do health check, pois o Docker executa a sonda em intervalos curtos e isso pode saturar a CPU/GPU ou gerar timeouts falsos.*

### 2. Atualizar o `docker-compose.yml`

No serviço `investimentos-api`, adicione o bloco `healthcheck`. Como a imagem base do .NET 10 no container é leve e pode não ter `curl` instalado por padrão, use `wget` ou `dotnet` / `bash` com `/dev/tcp` (ou `curl` se você instalou na imagem).

**YAML**

```
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
      - AI__Provider=Ollama
      - AI__Ollama__Endpoint=http://ollama:11434
      - AI__Ollama__Model=qwen2.5vl:7b
    depends_on:
      ollama:
        condition: service_healthy
    networks:
      - app-network
    healthcheck:
      test: ["CMD-SHELL", "bash -c '</dev/tcp/127.0.0.1/8080' && (wget -qO- http://127.0.0.1:8080/health >/dev/null || exit 1)"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 20s

```

Se a sua imagem final não tiver `wget`, você pode usar um pequeno comando em `dotnet` ou `curl` (se instalado). Uma alternativa portável sem depender de `curl` ou `wget` na imagem final é usar o próprio `dotnet` ou um script/ferramenta presente — no entanto, em imagens `aspnet:10.0` padrão (Ubuntu 24.04), se precisar testar HTTP de forma limpa, instalar `curl` no `Dockerfile` ou checar via `wget` é bastante direto:

**Dockerfile**

```
# No estágio final (runtime) do Dockerfile, caso queira usar curl no healthcheck:
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

```

Com `curl` disponível no container, o `test` no `docker-compose.yml` fica ainda mais claro:

**YAML**

```
    healthcheck:
      test: ["CMD-SHELL", "curl -fsS http://127.0.0.1:8080/health >/dev/null || exit 1"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 20s

```

### 3. Como verificar se está funcionando

1. **Suba ou atualize os containers:**

   **Bash**
   ```
   docker compose up -d --build

   ```
2. **Inspecione o status de saúde (**`healthy`**):**

   **Bash**
   ```
   docker compose ps

   ```
   A coluna de status da `investimentos-api` deve mostrar `Up ... (healthy)` após o `start_period`.
3. **Teste o endpoint diretamente:**

   **Bash**
   ```
   curl -i http://localhost:8080/health

   ```
   A resposta esperada é HTTP `200 OK` com o corpo `Healthy`.

Mostre como customizar o ResponseWriter do MapHealthChecks no .NET 10 para retornar um JSON com o status de cada check e duração.

Para customizar a resposta do `/health` no **.NET 10** e retornar um JSON detalhado com o status geral, duração total e o status, duração, descrição e erro de cada *health check* individual, você configura a propriedade `ResponseWriter` dentro de `HealthCheckOptions` no `app.MapHealthChecks(...)`.

### 1. DTOs e `ResponseWriter` Customizado

Criar uma classe ou função dedicada mantém o `Program.cs` limpo e facilita o reuso nos endpoints `/health`, `/health/live` e `/health/ready`:

**C#**

```
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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

### 2. Aplicando no `Program.cs`

Passe `ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync` nas opções do `MapHealthChecks`:

**C#**

```
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

// ... restante do Program.cs ...

var app = builder.Build();

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

app.Run();

```

### 3. Exemplo de Resposta JSON

Ao consultar `GET /health`, a resposta será semelhante a esta:

**JSON**

```
{
  "status": "Healthy",
  "totalDurationMs": 14.82,
  "checks": {
    "self": {
      "status": "Healthy",
      "durationMs": 0.04,
      "tags": [
        "live"
      ]
    },
    "ollama": {
      "status": "Healthy",
      "durationMs": 14.61,
      "description": "Ollama acessível.",
      "tags": [
        "ready"
      ]
    }
  }
}

```

E caso o Ollama esteja fora do ar, o HTTP status code continua sendo `503 Service Unavailable` (padrão do ASP.NET Core para `Unhealthy`), mas agora você vê exatamente qual check falhou e o motivo:

**JSON**

```
{
  "status": "Unhealthy",
  "totalDurationMs": 3004.19,
  "checks": {
    "self": {
      "status": "Healthy",
      "durationMs": 0.03,
      "tags": [
        "live"
      ]
    },
    "ollama": {
      "status": "Unhealthy",
      "durationMs": 3004.05,
      "description": "Falha ao conectar ao Ollama.",
      "error": "The request was canceled due to the configured HttpClient.Timeout of 3 seconds elapsing.",
      "tags": [
        "ready"
      ]
    }
  }
}

```

### Observação sobre Segurança e Logs

- **Exposição de detalhes (**`Exception.Message`**):** Em APIs internas ou dentro da rede Docker, incluir `Error` ajuda bastante no diagnóstico. Se o endpoint `/health` for exposto externamente sem autenticação, evite vazar detalhes internos de exceções ou strings de conexão em ambientes de produção.
- **Códigos HTTP:** Por padrão, `Healthy` e `Degraded` retornam `200 OK`, e `Unhealthy` retorna `503 Service Unavailable`. Se quiser que `Degraded` também retorne `503` (para que um balanceador/orquestrador pare de enviar tráfego quando uma dependência degrada), basta configurar `ResultStatusCodes` no `HealthCheckOptions`.

Atue como Arquiteto .NET Sênior especialista em Clean Architecture, DDD e Microsoft.Extensions.AI.\
Analise a estrutura atual deste repositório (\`invest-dashboard\`, branch \`codex/tesouro-previa-e-cotacoes\`, com \`.NET 10\`, \`InvestDashboard.Domain\`, \`InvestDashboard.Application\`, \`InvestDashboard.Infrastructure\`, \`InvestDashboard.WebAPI\` e \`docker-compose.yml\`) e implemente a extração de posições do Tesouro Direto a partir de imagens (prints de aplicativos) e a preparação da API para integração com o ChatGPT.\
\
\### Requisitos Técnicos\
\
1\. \*\*Infraestrutura Docker & Rede (\`docker-compose.yml\` e \`.env.example\`):\*\*\
&#x20;  \- Adicione um serviço \`ollama\` no \`docker-compose.yml\` na mesma rede interna dos containers (\`api\` e \`db\`), expondo a porta \`11434\` apenas internamente (ou localhost), com volume persistente \`ollama_data:/root/.ollama\` e suporte opcional comentado para GPU NVIDIA.\
&#x20;  \- Adicione variáveis de ambiente no \`.env.example\` e \`appsettings\`:\
&#x20;    \- \`AI__Provider\`: \`"Ollama"\` ou \`"OpenRouter"\`\
&#x20;    \- \`AI__Ollama__Endpoint\`: \`"http\://ollama:11434"\`\
&#x20;    \- \`AI__Ollama__Model\`: \`"qwen2.5vl:7b"\`\
&#x20;    \- \`AI__OpenRouter__Endpoint\`: \`"[https://openrouter.ai](https://openrouter.ai)"\`\
&#x20;    \- \`AI__OpenRouter__ApiKey\`: \`""\`\
&#x20;    \- \`AI__OpenRouter__Model\`: \`"google/gemini-2.5-flash"\`\
\
2\. \*\*Abstração de IA com \`Microsoft.Extensions.AI\` (\`InvestDashboard.Application\` e \`Infrastructure\`):\*\*\
&#x20;  \- Instale os pacotes \`Microsoft.Extensions.AI\`, \`Microsoft.Extensions.AI.Ollama\` e \`Microsoft.Extensions.AI.OpenAI\` (para compatibilidade OpenAI-compatible com o OpenRouter).\
&#x20;  \- Crie uma interface na camada \`Application\` (ex: \`ITesouroImageExtractorService\`) que receba os bytes/stream da imagem e retorne um DTO tipado (\`TesouroPosicaoExtraidaDto\` contendo: \`NomeTitulo\`, \`TipoTitulo\`, \`SaldoAtual\`, \`ValorAplicado\`, \`DataVencimento\`, \`DataAtualizacao\`, \`RentabilidadeValor\`, \`RentabilidadePercentual\`).\
&#x20;  \- Implemente o serviço na \`Infrastructure\` usando \`IChatClient\` com \*\*Structured Outputs\*\* (\`GetResponseAsync\<TesouroPosicaoExtraidaDto>\`) enviando a imagem via \`DataContent\`.\
&#x20;  \- Registre o \`IChatClient\` no DI (\`DependencyInjection.cs\`) alternando dinamicamente entre Ollama e OpenRouter conforme \`AI__Provider\`.\
\
3\. \*\*Caso de Uso e Endpoint (\`Application\` e \`WebAPI\`):\*\*\
&#x20;  \- Crie um Command/UseCase para importar a posição extraída da imagem, validando os valores e reaproveitando as regras de domínio existentes de atualização de posição do Tesouro Direto.\
&#x20;  \- Crie o endpoint \`POST /api/tesouro/importar-imagem\` (aceitando \`IFormFile\` ou Base64 JSON) documentado com OpenAPI/Swagger (\`ProducesResponseType\`, descrições claras nos parâmetros e schemas) para que possa ser consumido tanto pelo frontend React quanto por uma \*\*Custom GPT Action\*\* no ChatGPT.\
&#x20;  \- Crie também um endpoint \`GET /api/portfolio/resumo-ia\` otimizado para retornar o consolidado da carteira em formato enxuto para análise por LLMs externas (ChatGPT).\
\
4\. \*\*Testes & Documentação:\*\*\
&#x20;  \- Crie testes unitários em \`tests/\` mockando o \`IChatClient\` para validar o fluxo do Use Case.\
&#x20;  \- Gere um arquivo \`docs/integracao-ia-chatgpt.md\` explicando:\
&#x20;    a) Como baixar o modelo no container (\`docker exec -it \<container_ollama> ollama pull qwen2.5vl:7b\`).\
&#x20;    b) Como importar o \`swagger.json\` no ChatGPT Plus (Custom GPT Actions) e configurar a autenticação JWT/Supabase para usuários consultarem e atualizarem suas carteiras pelo chat.\
\
Antes de escrever o código, leia os arquivos existentes de \`InvestDashboard.Domain\`, \`InvestDashboard.Application\` (especialmente o fluxo atual de importação HTML do Tesouro Direto) e \`docker-compose.yml\` para manter total compatibilidade com as entidades, convenções e contratos atuais.\


Como não tenho acesso direto de leitura e escrita ao seu repositório local neste chat, estruturei toda a implementação em **Clean Architecture + DDD** pronta para ser integrada ao seu projeto (`InvestDashboard.Domain`, `InvestDashboard.Application`, `InvestDashboard.Infrastructure`, `InvestDashboard.WebAPI`, `tests/` e `docker-compose.yml`).

> ***Nota de integração:** Como você mencionou a existência do fluxo atual de importação HTML do Tesouro Direto, marquei com `// TODO: Adaptar para o contrato existente` os pontos onde você deve plugar sua entidade/repositório real de posições e a resolução do `UserId` via Supabase Auth.*

### 1. Infraestrutura Docker & Variáveis de Ambiente

#### `docker-compose.yml` (Trecho a adicionar/ajustar)

Garanta que o serviço `ollama` esteja na mesma rede que `api` e `db`, com bind apenas em `127.0.0.1` no host e volume persistente:

**YAML**

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
      - invest-network # Substitua pelo nome da rede usada pelos serviços api e db
    healthcheck:
      test: ["CMD-SHELL", "ollama list >/dev/null 2>&1 || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 15s
    # Descomente para habilitar GPU NVIDIA no Linux / WSL2:
    # deploy:
    #   resources:
    #     reservations:
    #       devices:
    #         - driver: nvidia
    #           count: all
    #           capabilities: [gpu]

  api:
    # ... suas configurações existentes ...
    environment:
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

volumes:
  ollama_data:
    driver: local

```

#### `.env.example` e `appsettings.json`

**Snippet de código**

```
# Configuração de IA (Ollama ou OpenRouter)
AI__Provider=Ollama
AI__Ollama__Endpoint=http://ollama:11434
AI__Ollama__Model=qwen2.5vl:7b
AI__OpenRouter__Endpoint=https://openrouter.ai/api/v1
AI__OpenRouter__ApiKey=
AI__OpenRouter__Model=google/gemini-2.5-flash

```

**JSON**

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

### 2. Camada `InvestDashboard.Application`

#### `DTOs/TesouroPosicaoExtraidaDto.cs`

As anotações `[Description]` orientam o *Structured Output* do `Microsoft.Extensions.AI` durante a geração do JSON Schema:

**C#**

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

```

#### `Interfaces/AI/ITesouroImageExtractorService.cs`

**C#**

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

#### `UseCases/Tesouro/ImportarPosicaoTesouroPorImagemCommand.cs` e Handler

Reaproveita a validação de domínio e atualiza/insere a posição do usuário:

**C#**

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
    // TODO: Injetar o repositório/serviço de domínio existente do Tesouro Direto (ex: ITesouroPosicaoRepository / IUnitOfWork)
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

        // Normaliza rentabilidade caso o print não exiba o valor explícito
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

        // TODO: Reaproveitar a entidade e regras de domínio existentes no InvestDashboard.Domain:
        // var posicao = await _tesouroRepository.ObterPorUsuarioETituloAsync(command.UserId, dtoNormalizado.NomeTitulo, cancellationToken);
        // if (posicao is null) { ... criar nova entidade ... } else { posicao.AtualizarPosicao(...); }
        // await _unitOfWork.CommitAsync(cancellationToken);

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

#### `DTOs/Portfolio/PortfolioResumoIaDto.cs`

Formato enxuto e semântico para consumo direto pelo ChatGPT sem estourar contexto:

**C#**

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

### 3. Camada `InvestDashboard.Infrastructure`

#### Pacotes NuGet necessários

Adicione ao `InvestDashboard.Infrastructure.csproj`:

**Bash**

```
dotnet add src/InvestDashboard.Infrastructure package Microsoft.Extensions.AI
dotnet add src/InvestDashboard.Infrastructure package Microsoft.Extensions.AI.Ollama
dotnet add src/InvestDashboard.Infrastructure package Microsoft.Extensions.AI.OpenAI

```

#### `AI/OllamaTesouroImageExtractorService.cs` (Implementação agnóstica via `IChatClient`)

**C#**

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
        logger.LogInformation("Iniciando extração de posição do Tesouro Direto via IA ({MimeType}, {Bytes} bytes).",
            mimeType, imagemBytes.Length);

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
            Temperature = 0.0f // Determinístico para evitar alucinação em números
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

#### `DependencyInjection.cs` (Registro dinâmico entre Ollama e OpenRouter)

**C#**

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

### 4. Camada `InvestDashboard.WebAPI`

#### Contratos de Request e Endpoints (`Endpoints/AiIntegrationEndpoints.cs`)

Aceita tanto upload via `multipart/form-data` quanto payload JSON em Base64 (ideal para **Custom GPT Actions**, que lidam melhor com JSON do que `multipart/form-data`):

**C#**

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

### 5. Testes Unitários (`tests/`)

#### `InvestDashboard.UnitTests/AI/ImportarPosicaoTesouroPorImagemHandlerTests.cs`

Usa um stub leve de `IChatClient` (sem depender de rede ou Ollama rodando) para validar todo o pipeline de extração e regras de negócio:

**C#**

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
        // Arrange
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

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
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

### 6. Documentação (`docs/integracao-ia-chatgpt.md`)

**Markdown**

````
# Integração de IA (Ollama / OpenRouter) e ChatGPT Custom GPT

## 1. Inicializando o Modelo Multimodal no Ollama (Docker)

Após subir os containers com `docker compose up -d`, baixe o modelo de visão (`qwen2.5vl:7b`) dentro do container do Ollama. Como o volume `ollama_data` é persistente, esse download ocorre apenas uma vez:

```bash
docker exec -it invest-ollama ollama pull qwen2.5vl:7b

````

Para verificar os modelos instalados:

**Bash**

```
docker exec -it invest-ollama ollama list

```

### Alternando para Nuvem (OpenRouter)

Para usar um modelo na nuvem sem exigir RAM/GPU local, altere o `.env`:

**Snippet de código**

```
AI__Provider=OpenRouter
AI__OpenRouter__ApiKey=sk-or-v1-...
AI__OpenRouter__Model=google/gemini-2.5-flash

```

## 2. Integrando o Invest Dashboard ao ChatGPT Plus (Custom GPT Actions)

### Pré-requisito

A `InvestDashboard.WebAPI` deve estar acessível via **HTTPS público** (ex.: Render, Railway, Azure ou túnel Cloudflare/ngrok).

### Passo A: Habilitar OAuth Server no Supabase

1. Acesse seu projeto no **Supabase** -> **Authentication** -> **OAuth Server**.
2. Ative **Enable OAuth Server** e crie um novo OAuth App (ex.: `ChatGPT Invest Dashboard`).
3. Anote o **Client ID** e o **Client Secret**.

### Passo B: Criar a Action no Custom GPT

1. No ChatGPT Plus, acesse **Explorar GPTs** -> **Criar** -> aba **Configurar**.
2. Clique em **Criar nova ação**.
3. Importe o schema OpenAPI da sua API (`https://<seu-dominio>/swagger/v1/swagger.json`).
   - *Atenção:* Garanta que o campo `servers[0].url` no JSON aponte para a URL HTTPS pública da API.
4. Em **Autenticação**, selecione **OAuth** e preencha:
   - **Client ID:** `<Client ID do Supabase>`
   - **Client Secret:** `<Client Secret do Supabase>`
   - **Authorization URL:** `https://<SEU-PROJETO>.supabase.co/auth/v1/oauth/authorize`
   - **Token URL:** `https://<SEU-PROJETO>.supabase.co/auth/v1/oauth/token`
   - **Scope:** `openid email profile`
   - **Token Exchange Method:** `Default (POST request)`
5. Salve a Action, copie a **Callback URL** exibida pelo ChatGPT (`https://chatgpt.com/aip/<id>/oauth/callback`) e cadastre-a em **Redirect URIs** no OAuth App do Supabase.

### Segurança e Isolamento de Dados

Todos os endpoints expostos ao GPT exigem `[Authorize]` e extraem o `UserId` exclusivamente da claim `sub` do JWT assinado pelo Supabase, garantindo que cada usuário visualize e atualize apenas a própria carteira.

```

<ElicitationsGroup message="Para finalizar a integração na WebAPI:">
  <Elicitation label="Configurar AddSwaggerGen com OAuth2/Bearer para o Custom GPT" query="Mostre como configurar o AddSwaggerGen no .NET 10 para gerar o swagger.json compatível com o parser de Actions do Custom GPT."/>
  <Elicitation label="Adicionar redimensionamento automático de imagem antes da IA" query="Mostre como redimensionar imagens grandes no .NET 10 com SkiaSharp ou ImageSharp antes de enviar ao IChatClient."/>
</ElicitationsGroup>

```

Algo de ml.net útil?

No contexto do **Invest Dashboard** (.NET 10, extração de prints/notas e gestão de carteira), o **ML.NET** não é a melhor ferramenta para fazer OCR de imagens do zero (onde um modelo multimodal como `Qwen2.5-VL` ou `Gemini Flash` via `Microsoft.Extensions.AI` ganha de lavada), **mas** ele brilha em tarefas numéricas, tabulares e de classificação locais dentro da própria API .NET — rodando em microssegundos na CPU, sem custo de tokens e sem depender de GPU ou rede externa.

Onde o **ML.NET** realmente agrega valor ao seu projeto:

### 1. Categorização Automática de Lançamentos e Ativos (Classificação Multiclasse)

Quando você importa extratos, notas de corretagem ou títulos novos que vêm com nomes abreviados ou sujos da corretora (ex.: `"TESOURO SELIC 2029 LFT"`, `"FII MAXI RENDA MXRF11"`, `"CDB BANCO INTER 102% CDI"`):

- **O que faz:** Usa o algoritmo `SdcaMaximumEntropy` ou `LbfgsMaximumEntropy` sobre o texto (`FeaturizeText`) para classificar automaticamente o ativo na classe correta (`TesouroDireto`, `FII`, `Acao`, `RendaFixa`, `Cripto`) antes mesmo de bater em uma API externa.
- **Vantagem:** Resposta instantânea (< 1 ms) e aprende com as correções manuais que os usuários fazem no próprio dashboard.

### 2. Detecção de Anomalias em Cotações e Importações (`DetectIidSpike` / `DetectChangePoint`)

Você já tem um sincronizador de cotações em tempo real (SignalR) e importação de posições:

- **O que faz:** Usando o pacote `Microsoft.ML.TimeSeries`, você detecta picos anômalos (`DetectIidSpike`) ou mudanças bruscas de patamar (`DetectChangePoint`) tanto nas séries de cotações recebidas da API externa (evitando gravar um tick bugado de R$ 0,01 ou R$ 9.999,00 no banco) quanto na extração da IA (sinalizando quando um saldo extraído de um print fugiu muito do histórico daquela posição).
- **Vantagem:** Não exige treino prévio em lote — funciona em *streaming* diretamente sobre uma lista de valores históricos da posição.

### 3. Projeção de Fluxo de Dividendos e Patrimônio (`SSA` — Singular Spectrum Analysis)

Para a aba de análise da carteira:

- **O que faz:** O `ForecastBySsa` (também do `Microsoft.ML.TimeSeries`) analisa a sazonalidade mensal dos proventos recebidos (dividendos/JCP/rendimentos de FIIs) e projeta os próximos 6 a 12 meses com intervalo de confiança (limite inferior e superior).
- **Vantagem:** Muito mais realista que uma projeção linear simples porque captura os meses em que ações pagam dividendos semestrais/trimestrais vs. FIIs mensais.

### Exemplo Prático: Detector de Anomalia para Validar Extração de Print ou Cotação

Este exemplo não exige arquivo `.zip` de modelo treinado — você pode colocá-lo direto na camada `Domain` ou `Application` para barrar valores absurdos vindos da extração por imagem:

**C#**

```
using Microsoft.ML;
using Microsoft.ML.Data;

public sealed class HistoricoSaldo
{
    public float Saldo { get; set; }
}

public sealed class ResultadoAnomalia
{
    // [0] = Alerta (1 = anomalia, 0 = normal), [1] = Valor atual, [2] = P-Value
    [VectorType(3)]
    public double[] Prediction { get; set; } = [];
}

public static class ValidadorAnomaliaPosicao
{
    public static bool EhValorSuspeito(IReadOnlyList<decimal> historicoSaldos, decimal novoSaldoExtraido)
    {
        if (historicoSaldos.Count < 5)
            return false; // Pouco histórico para inferir anomalia estatística

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

        // Prediction[0] == 1.0 indica spike/anomalia fora do padrão histórico
        return ultimaPrevisao.Prediction[0] == 1.0;
    }
}

```

### Resumo de quando usar cada um no seu stack

- **Visão Computacional (ler print/PDF) e Análise Qualitativa:** `Microsoft.Extensions.AI` (`Ollama` / `OpenRouter` / `ChatGPT`).
- **Série Temporal, Detecção de Outliers e Classificação de Texto Tabular:** `ML.NET` (`Microsoft.ML` + `Microsoft.ML.TimeSeries`).

e investimento de renda fixa(cd,ipca, tesouro,selic....)?

Para **Renda Fixa** (CDI, IPCA+, Prefixado, Tesouro Selic, CDB, LCI/LCA), o **ML.NET** tem casos de uso ainda mais precisos do que na Renda Variável, porque esses títulos seguem curvas de juros e regras matemáticas determinísticas — enquanto os **parsers de texto e anomalias** resolvem as dores reais de importação de corretoras.

Aqui está onde o **ML.NET** realmente entrega valor prático para Renda Fixa no seu **Invest Dashboard**:

### 1. Parser Inteligente de Nomenclatura de Títulos (NLP + Classificação)

Cada corretora escreve o mesmo título de um jeito diferente nos extratos ou prints:

- `"CDB BANCO MASTER 118% CDI VENC 15/08/2028"`
- `"LCI INTER IPCA + 6,15% AA"`
- `"TESOURO IPCA+ 2035 COM JUROS SEMESTRAIS"`
- `"LCA RABOBANK PRE 11,40%"`

Com **ML.NET (**`FeaturizeText`**&#x20;+&#x20;**`SdcaMaximumEntropy`**)**, você cria um classificador leve (< 500 KB) que identifica automaticamente:

1. **Produto:** `CDB`, `LCI`, `LCA`, `TesouroDireto`, `Debenture`, `CRI/CRA`.
2. **Indexador:** `CDI_Percentual` (ex.: 110% do CDI), `CDI_Spread` (CDI + 1,5%), `IPCA_Mais` (IPCA + 6%), `Selic`, `Prefixado`.
3. **Liquidez / Isenção IR:** Infere se é isento de IR (`LCI`/`LCA`/`CRI`/`CRA`) ou regressivo (`CDB`/`Tesouro`).

### 2. Projeção de Rendimento com Volatilidade de Indexadores (`ForecastBySsa`)

Enquanto um título **Prefixado** tem curva exata por fórmula matemática, títulos atrelados a **CDI/Selic** e **IPCA+** dependem da trajetória futura da taxa básica e da inflação:

- **O que o ML.NET faz:** Usando `ForecastBySsa` sobre o histórico mensal do **IPCA** ou da **Taxa Selic/CDI** (que você pode buscar na API do Banco Central - SGS), o modelo projeta a curva esperada dos próximos 12 a 36 meses com **intervalo de confiança (cenário otimista, base e conservador)**.
- **Aplicação direta no dashboard:** Na tela de detalhes do Tesouro IPCA+ ou CDB pós-fixado, você consegue mostrar o **valor líquido estimado no vencimento** em três cenários sem precisar hardcodar uma taxa fixa eterna.

### 3. Detector de Divergência na Marcação a Mercado (`DetectIidSpike` / `DetectChangePoint`)

Títulos como **Tesouro IPCA+** e **Tesouro Prefixado** sofrem **marcação a mercado** diária (o preço unitário sobe ou cai conforme a abertura/fechamento da curva de juros futuros), enquanto CDBs pós-fixados sobem de forma quase monotônica todos os dias úteis.

- **O que o ML.NET faz:** Monitora a variação diária do saldo/PU de cada posição importada e separa:
  - **Variação normal de juros/inflação** (oscilação esperada na marcação a mercado);
  - **Anomalia de importação ou erro de leitura da IA** (ex.: o modelo leu `R$ 15.400,00` como `R$ 1.540,00` ou duplicou um aporte).
- Para CDBs 100% CDI, qualquer queda diária no saldo bruto aciona alerta imediato de inconsistência.

### Exemplo Prático: Classificador de Indexador de Renda Fixa em C\#

Treina em milissegundos na inicialização (ou a partir de uma tabela de referência no Postgres) e classifica qualquer descrição importada:

**C#**

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

### Divisão Ideal no seu Backend para Renda Fixa

1. **Cálculo de Juros Compostos, Dias Úteis (ANBIMA 252) e Tabela Regressiva de IR/IOF:** Matemática pura em C# no `InvestDashboard.Domain` (nunca use IA/ML para calcular imposto ou juros exatos).
2. **Extração de Print/PDF da Corretora:** `Microsoft.Extensions.AI` (`Ollama` / `OpenRouter`).
3. **Classificação de Nomenclatura Suja + Projeção de Curva IPCA/Selic + Alerta de Anomalia no Saldo:** `ML.NET`.
