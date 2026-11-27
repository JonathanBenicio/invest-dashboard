> Exportação histórica de [Assistente IA](https://chatgpt.com/space/page_7cbec027e11881918d57ebb49028a8a2). Para o escopo e as decisões vigentes, consulte o [épico e os subplanos](../epico.md).

[https://github.com/JonathanBenicio/portfolio-management/tree/main](https://github.com/JonathanBenicio/portfolio-management/tree/mainArquitetura)\
[https://github.com/JonathanBenicio/base-ia/tree/feat/ChatRoleAssistant/agent-Invest](https://github.com/JonathanBenicio/base-ia/tree/feat/ChatRoleAssistant/agent-Invest)\
[https://github.com/JonathanBenicio/Agent-System/compare/develop](https://github.com/JonathanBenicio/Agent-System/compare/develop)\
\
[Arquite](https://github.com/JonathanBenicio/portfolio-management/tree/mainArquitetura)tura técnica — Invest Dashboard: Assistente IA próprio

\
\
\

Arquitetura proposta

&#x20;Versão 1.0 · .NET / C#



## 1. Objetivo do projeto

Desenvolver um assistente de inteligência artificial integrado diretamente ao Invest Dashboard.

O usuário poderá conversar com o assistente dentro do próprio sistema, utilizando linguagem natural para consultar sua carteira, analisar investimentos, realizar cálculos e preparar operações.

**O acesso não dependerá do plano ChatGPT do usuário.**

O assistente utilizará a API de inteligência artificial contratada pelo Invest Dashboard. O custo da utilização será controlado pelo próprio sistema.

## Experiência do usuário

[Suivi de Portefeuille Actions, ETF & Multi-Actifs | Anantys](https://images.openai.com/static-rsc-4/Qa2dgndZqPu_b9rCO66qQMgug5vZY3kn7nTHln7ItIy7xNFHAGV-g-afruA0_ah4usd-9d_dcWJTzHUc2BWcxQIkmYftpsCqPicxIwsjuAfWr2Oa20Hmh437rdeIBbIFbXLTo4mADy0sPhMjTp67Ep7K1qkqYovrizHs7Xt_IGo?purpose=inline)

[anantys.com](https://anantys.com/fr?utm_source=chatgpt.com)

O usuário acessa o Invest Dashboard e encontra uma área de conversa integrada ao sistema.

Exemplos de solicitações:

-   Quanto tenho investido atualmente?
-
    Qual é minha rentabilidade líquida?
-
    Compare meus investimentos de 2029 e 2032.
-
    Quanto receberei de juros nos próximos meses?
-
    Analise minha carteira.
-
    Prepare a importação desta movimentação.

## 2. Arquitetura geral

A proposta é aproveitar integralmente o backend existente em .NET/C#.

Não será necessário criar outro sistema financeiro, outro banco de dados ou duplicar regras de negócio.

**Frontend — Invest Dashboard**

Interface de conversa, histórico e visualizações

**ASP.NET Core — API existente**

Autenticação, autorização e controle de requisições

**AI Orchestrator — Novo módulo**

Interpretação, seleção de ferramentas e gerenciamento das conversas

**OpenAI API**

Modelo de IA e function calling

**Application Services**

Consultas e operações financeiras

**Invest Dashboard — Domain / Database**

Dados reais, cálculos e regras financeiras

O componente de IA não terá acesso direto ao banco de dados. Ele solicitará informações através de ferramentas controladas pelo backend.

## 3. Tecnologias recomendadas

| <p>Componente</p> | <p>Tecnologia</p> |
| - | - |
| Backend | ASP.NET Core / C# |
| Inteligência artificial | OpenAI API |
| Integração com IA | Responses API |
| Comunicação com IA | SDK oficial OpenAI para .NET |
| Execução de funções | Function Calling |
| Interface | Frontend atual do dashboard |
| Streaming | Server-Sent Events ou SignalR |
| Persistência | Banco de dados atual |
| Autenticação | Autenticação existente |
| Monitoramento | Logs e métricas de utilização |

### Por que Responses API?

Utilizaria a Responses API como base da integração.

Ela permite que o modelo:

-   Interprete as perguntas do usuário.
-
    Solicite funções do Invest Dashboard.
-
    Receba os resultados dessas funções.
-
    Combine informações de diferentes consultas.
-
    Produza respostas contextualizadas.

A OpenAI disponibiliza o SDK oficial para .NET, incluindo `ResponsesClient`, chamadas assíncronas e integração com ferramentas.

GitHub+2



Não iniciaria uma implementação nova baseada na antiga Assistants API, que foi descontinuada em 26 de agosto de 2026.

OpenAI API



## 4. Organização dos projetos

Aproveitando a solução atual, adicionaria um módulo específico para inteligência artificial.

```
InvestDashboard.sln
│
├── InvestDashboard.Domain/
│
├── InvestDashboard.Application/
│
├── InvestDashboard.Infrastructure/
│
├── InvestDashboard.Api/
│   └── Controllers/
│       └── AiController.cs
│
├── InvestDashboard.AI/
│   ├── Orchestration/
│   │   ├── AiOrchestrator.cs
│   │   └── AiConversationService.cs
│   │
│   ├── Providers/
│   │   └── OpenAiProvider.cs
│   │
│   ├── Tools/
│   │   ├── PortfolioTools.cs
│   │   ├── AssetTools.cs
│   │   ├── TransactionTools.cs
│   │   ├── FinancialCalculationTools.cs
│   │   └── ImportTools.cs
│   │
│   ├── Prompts/
│   │   └── InvestmentAssistantPrompt.cs
│   │
│   ├── Models/
│   │
│   └── Configuration/
│
└── tests/
    ├── InvestDashboard.AI.Tests/
    └── InvestDashboard.AI.IntegrationTests/
```

O módulo `InvestDashboard.AI` será responsável pela inteligência artificial, enquanto as regras financeiras continuarão nos serviços existentes.

## 5. Ferramentas disponíveis para a IA

Esta é uma das partes mais importantes da arquitetura.

Em vez de permitir que a inteligência artificial acesse diretamente o banco de dados, forneceremos funções específicas.

### Módulo de carteira

| <p>Função</p> | <p>Responsabilidade</p> |
| - | - |
| `get_portfolio_summary` | Resumo geral da carteira |
| `list_positions` | Listar investimentos |
| `get_asset_details` | Detalhes de determinado ativo |
| `get_asset_allocation` | Distribuição dos investimentos |
| `get_portfolio_performance` | Rentabilidade por período |

### Módulo financeiro

| <p>Função</p> | <p>Responsabilidade</p> |
| - | - |
| `calculate_net_return` | Rentabilidade líquida |
| `calculate_gross_return` | Rentabilidade bruta |
| `calculate_compound_interest` | Juros compostos |
| `calculate_fixed_income_projection` | Projeções de renda fixa |
| `compare_investments` | Comparação entre investimentos |

### Módulo de movimentações

| <p>Função</p> | <p>Responsabilidade</p> |
| - | - |
| `list_transactions` | Consultar movimentações |
| `get_transaction_details` | Detalhar movimentações |
| `get_monthly_cash_flow` | Fluxo financeiro mensal |
| `create_import_draft` | Preparar importação |
| `validate_import_draft` | Validar importação |

### Módulo de conhecimento

Este módulo permitirá que o assistente responda perguntas sobre o funcionamento do próprio Invest Dashboard.

Exemplos:

-   Como funciona o cálculo de rentabilidade?
-
    Como cadastrar um investimento?
-
    Qual a diferença entre saldo bruto e líquido?
-
    Como importar uma movimentação?

Inicialmente, essas informações podem vir de documentação interna e instruções do sistema. Não é necessário implementar um banco vetorial logo no início.

## 6. Como a inteligência artificial executará as funções

Exemplo: o usuário pergunta:

"Quanto tenho investido no Tesouro Direto e qual é minha rentabilidade líquida?"

O funcionamento será:

1.  **Usuário faz a pergunta**

    Envia mensagem pela interface do dashboard.
2.
    **AI Orchestrator recebe a solicitação**

    Carrega as instruções e o contexto autorizado do usuário.
3.
    **OpenAI interpreta a solicitação**

    Identifica que precisa consultar os investimentos e calcular a rentabilidade.
4.
    **Executa as ferramentas**

    Chama list_positions e calculate_net_return.
5.
    **Backend busca os dados**

    Utiliza os serviços financeiros existentes e retorna os resultados.
6.
    **IA formula a resposta**

    Explica os valores, apresenta os resultados e informa a data de referência.

O mecanismo de Function Calling é justamente o recurso que permite ao modelo solicitar a execução de funções externas e receber os respectivos resultados. A execução efetiva fica sob responsabilidade da aplicação.

OpenAI API+1



**Regra fundamental:** a IA interpreta e explica os resultados; os cálculos financeiros continuam sendo responsabilidade do Invest Dashboard.

## 7. Segurança e isolamento dos usuários

Como estamos tratando de informações financeiras, esta camada deve ser obrigatória desde a primeira versão.

| <p>Controle</p> | <p>Implementação</p> |
| - | - |
| Identificação | Usuário autenticado |
| Isolamento | Consultas limitadas ao usuário logado |
| Autorização | Verificada no backend |
| Chaves da IA | Armazenadas somente no servidor |
| Histórico | Separado por usuário |
| Ferramentas | Lista de funções permitidas |
| Operações de escrita | Revisão e confirmação humana |
| Auditoria | Registro das operações realizadas |

Um detalhe essencial: o modelo nunca poderá decidir qual usuário consultar.

O `UserId` será obtido da autenticação do backend, nunca de um parâmetro fornecido pela IA.

## 8. Controle financeiro da IA

O custo da OpenAI API será pago pelo proprietário do Invest Dashboard, e não pela assinatura ChatGPT dos usuários.

Isso significa que precisamos de uma camada de gerenciamento de consumo.

**AI Usage Management**

| <p>Controle</p> | <p>Finalidade</p> |
| - | - |
| Limite diário | Evitar consumo excessivo |
| Limite mensal | Controlar orçamento |
| Limite por usuário | Distribuir utilização |
| Limite de mensagens | Evitar abuso |
| Seleção de modelo | Otimizar custo |
| Histórico de tokens | Mensurar consumo |
| Rate limiting | Controlar requisições simultâneas |

**Exemplo de política inicial, configurável:**

-   Usuário gratuito: limite diário de mensagens.
-
    Usuário premium: limite maior.
-
    Consultas simples: modelo de menor custo adequado.
-
    Análises mais complexas: modelo com maior capacidade.

Esses limites são decisões comerciais do Invest Dashboard, não restrições do plano ChatGPT.

A cobrança da API é independente da assinatura ChatGPT e depende dos tokens e recursos utilizados.

OpenAI Help Center+1



## 9. Histórico e memória do assistente

Eu separaria três tipos de memória:

| <p>Tipo</p> | <p>Exemplo</p> |
| - | - |
| Histórico de conversa | Perguntas e respostas anteriores |
| Contexto financeiro | Carteira atual e preferências autorizadas |
| Preferências | Formato de respostas, análises favoritas |

Para a primeira versão, armazenaria o histórico no banco de dados do Invest Dashboard.

A cada nova mensagem, o backend selecionaria somente o contexto relevante para enviar ao modelo.

Também consideraria utilizar `store: false` nas chamadas à Responses API e manter o histórico sob controle da aplicação. É importante observar que isso não elimina os registros de monitoramento de abuso previstos pela plataforma. A documentação detalha as opções de retenção e armazenamento.

OpenAI API+1



## 10. Interface do assistente

A interface seria integrada ao dashboard, sem exigir que o usuário abra outro aplicativo.

[Finance Virtual Assistant Dashboard & Chatbot by ElifTech on Dribbble](https://images.openai.com/static-rsc-4/-16TCpUKwdkZ9-aCQC585HGAPGCwUcBl7nd-vLcT-D9As1RDLKU2lqUHTaH2SJ_fuyCKNNbFhSUhm0rYxVMtdI9qOPXxplLSsOt0GjRDxjBXXYo0ZHY8DZscNFFXTc8qVtVpIJ2NEUcEZZVF_dFZBIv9zKOehJOP5Kd0gm6Hs1A?purpose=inline)

**Chat lateral**

Assistente acessível em todas as telas.

[Build a Fullstack Stock Portfolio Agent with Mastra and AG-UI - DEV Community](https://images.openai.com/static-rsc-4/C8ATRDoxvOmrtFfj5E6Ctk3pLuzjC44WlLlSTwFORNWa0ABBUPmBF7PAh0L7U4I924GX6vUjIpVY1Sp_UuI0S_TenH2Sm_hblKXIKIKM6wsTrcQsefiqKOCaLhFfFO-uxa_WvUsTWuO0Cj_vmbVWJm7YpKA8qRxHQxXyY3a4YqY?purpose=inline)

**Análise integrada**

Respostas com indicadores, gráficos e tabelas.

Funcionalidades previstas:

-   Conversas independentes.
-
    Histórico de conversas.
-
    Respostas com Markdown.
-
    Tabelas financeiras.
-
    Gráficos de investimentos.
-
    Acesso ao contexto da tela atual.
-
    Anexos e documentos, em uma etapa posterior.
-
    Botão para interromper uma resposta.
-
    Indicador de ferramentas utilizadas pelo assistente.

## 11. Endpoints da API

Criaria os seguintes endpoints no backend existente:

| <p>Endpoint</p> | <p>Método</p> | <p>Finalidade</p> |
| - | - | - |
| `/api/ai/conversations` | POST | Criar conversa |
| `/api/ai/conversations` | GET | Listar conversas |
| `/api/ai/conversations/{id}` | GET | Consultar histórico |
| `/api/ai/chat` | POST | Enviar mensagem |
| `/api/ai/chat/stream` | POST | Resposta em streaming |
| `/api/ai/usage` | GET | Consultar consumo |
| `/api/ai/import-drafts` | GET | Listar rascunhos |

O backend deverá validar a propriedade da conversa e dos rascunhos em todas as requisições.

## 12. Configuração inicial

Pacote oficial:

```
dotnet add package OpenAI
```

Configuração:

```
{
  "AI": {
    "Provider": "OpenAI",
    "Model": "MODELO_CONFIGURADO",
    "MaxToolCalls": 5,
    "MaxConversationMessages": 20,
    "EnableStreaming": true,
    "EnableFinancialTools": true
  }
}
```

A chave da API não deve ficar no `appsettings.json` versionado. Deve ser armazenada em variável de ambiente ou gerenciador de segredos.

A implementação utilizará `ResponsesClient` e um serviço próprio para orquestrar o ciclo de chamadas das ferramentas.

Documentação: [OpenAI .NET SDK](https://github.com/openai/openai-dotnet?utm_source=chatgpt.com).

## 13. Plano de implementação

Minha sugestão é desenvolver em entregas independentes, validando cada uma antes de avançar.

## Roadmap do projeto

1.

   MVP

   **Fundação do assistente**

   - Criar `InvestDashboard.AI`.

   - Integrar OpenAI API.

   - Criar AI Orchestrator.

   - Criar interface de conversa.

   - Implementar histórico.

2. **Consultas financeiras**

   - Consultar carteira.

   - Listar investimentos.

   - Consultar movimentações.

   - Calcular rentabilidade.

   - Comparar investimentos.

3. **Análise inteligente**

   - Análises de carteira.

   - Projeções financeiras.

   - Comparações entre títulos.

   - Indicadores e gráficos.

4. **Importação inteligente**

   - Interpretar documentos e informações.

   - Preparar rascunhos.

   - Validar lançamentos.

   - Encaminhar para confirmação.

5. **Controle e otimização**

   - Limites de consumo.

   - Monitoramento de custos.

   - Cache de consultas.

   - Otimização dos modelos.

6. **Evolução**

   - Upload de documentos.

   - Memória personalizada.

   - Mais provedores de IA.

   - Integração MCP opcional.

## 14. Decisões arquiteturais

| <p>Decisão</p> | <p>Escolha</p> |
| - | - |
| Onde ficará a IA? | Dentro do Invest Dashboard |
| Quem paga pela IA? | Proprietário do sistema |
| Quem controla o consumo? | Invest Dashboard |
| Backend | .NET / C# existente |
| Banco de dados | Banco atual |
| Comunicação com IA | Responses API |
| Execução de operações | Ferramentas controladas pelo backend |
| Cálculos financeiros | Serviços existentes |
| Plugin ChatGPT | Opcional, não obrigatório |
| MCP | Opcional, para futuras integrações |

**Uma vantagem adicional:** manteria a integração com OpenAI desacoplada. Assim, futuramente será possível adicionar outro provedor de IA sem reescrever as ferramentas financeiras e as regras de negócio.

## 15. Escopo que recomendo implementar primeiro

Para evitar desenvolver muitas funcionalidades antes de validar a arquitetura, começaria com:

**Primeira versão funcional**

MVP recomendado

-   Interface de conversa integrada.
-
    Integração com OpenAI API.
-
    Histórico de conversas.
-
    Identificação do usuário autenticado.
-
    Consulta do resumo da carteira.
-
    Listagem de investimentos.
-
    Cálculo de rentabilidade líquida.
-
    Comparação entre investimentos.
-
    Controle de consumo da API.
-
    Testes de isolamento de dados.

## Para o Invest Dashboard, minha recomendação é começar com Microsoft.Extensions.AI (MEAI) e deixar o Microsoft Agent Framework (MAF) como uma evolução futura.

Mas existe uma diferença importante: eles não são concorrentes diretos. Você pode utilizar os dois juntos.

## 1. Diferença entre eles

**Microsoft.Extensions.AI**

Recomendado agora

Camada de integração com modelos de inteligência artificial.

Permite:

-   Conectar à OpenAI e outros provedores.
-
    Gerenciar conversas.
-
    Executar Function Calling.
-
    Disponibilizar ferramentas C# para a IA.
-
    Trabalhar com streaming, cache e telemetria.

É a base para adicionar inteligência artificial a uma aplicação .NET.

[Documentação Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai?utm_source=chatgpt.com)

**Microsoft Agent Framework (MAF)**

Framework para agentes e orquestração de processos complexos.

Permite:

-   Agentes especializados.
-
    Planejamento de tarefas com várias etapas.
-
    Orquestração de ferramentas.
-
    Workflows com condições e ramificações.
-
    Coordenação entre agentes.
-
    Gerenciamento de estado e aprovações.

É mais indicado quando o assistente precisa executar processos complexos e encadeados.

[Documentação Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/overview?utm_source=chatgpt.com)

A própria Microsoft recomenda começar com MEAI para a maioria dos recursos de IA em aplicações e adicionar MAF quando houver necessidade real de orquestração multi-etapas.

Microsoft Learn+1



## 2. Aplicando ao Invest Dashboard

Imagine três situações:

| <p>Solicitação</p> | <p>Tecnologia</p> |
| - | - |
| Quanto tenho investido? | MEAI |
| Compare a rentabilidade de dois títulos. | MEAI |
| Analise minha carteira e explique os resultados. | MEAI |
| Analise minha carteira, identifique oportunidades e gere um relatório com várias etapas. | MAF pode ajudar |
| Conduza uma importação com validações, decisões e aprovação. | MAF pode ajudar |

O MEAI já oferece Function Calling. Portanto, não precisamos de um framework de agentes completo para o assistente consultar os serviços financeiros.

## 3. Arquitetura que eu adotaria

**Invest Dashboard — Frontend**

Interface de conversa

**InvestDashboard.AI**

Orquestrador próprio em C#

**Microsoft.Extensions.AI**

## IChatClient

Abstração de comunicação com a IA

**OpenAI API**

Modelo de inteligência artificial

**Ferramentas disponíveis para a IA**

**PortfolioTools**

Carteira e investimentos

**FinancialTools**

Cálculos e projeções

**TransactionTools**

Movimentações

**ImportTools**

Importações e validações

**Application Services existentes**

Regras financeiras e banco de dados

Futuramente, o MAF poderá ser adicionado acima dessa estrutura, utilizando as ferramentas e serviços existentes.

## 4. Pacotes .NET

Para começar:

```
dotnet add package Microsoft.Extensions.AI
dotnet add package Microsoft.Extensions.AI.OpenAI
```

O MEAI disponibiliza `IChatClient`, `AIFunction` e `FunctionInvokingChatClient`, que permitem conectar o modelo às funções C# da aplicação.

Microsoft Learn+2



## 5. Minha decisão técnica

| <p>Critério</p> | <p>Escolha</p> |
| - | - |
| Primeira versão | Microsoft.Extensions.AI |
| Provedor | OpenAI API |
| Comunicação | IChatClient |
| Function Calling | AIFunction |
| Orquestração | Serviço próprio C# |
| Processos complexos futuros | Microsoft Agent Framework |
| Banco e regras financeiras | Application Services existentes |
| Integração externa futura | MCP |

**Um cuidado importante:** não deixaria o Function Calling executar livremente operações financeiras. Consultas podem ser automatizadas; alterações devem passar por validação e confirmação explícita no dashboard.

### Conclusão

Eu escolheria **Microsoft.Extensions.AI agora**.

É uma arquitetura mais simples para o primeiro assistente, mantém o código desacoplado do provedor e permite adicionar MAF posteriormente.

Referência adicional: [Ecossistema oficial .NET + IA — Microsoft](https://learn.microsoft.com/pt-br/dotnet/ai/dotnet-ai-ecosystem?utm_source=chatgpt.com).

Não vejo vantagem em introduzir o MAF antes de termos necessidades concretas de agentes e workflows complexos.

\
\
Minha orientação final

Eu não começaria pelo desenvolvimento de várias ferramentas MCP ou pela criação de um plugin.

Começaria pelo **AI Orchestrator**, que será o núcleo da inteligência artificial do Invest Dashboard.

Ele deverá permitir que o modelo compreenda a pergunta, escolha a ferramenta correta, execute a consulta no backend e formule uma resposta usando dados reais.

Depois disso, novas funcionalidades poderão ser adicionadas sem alterar a estrutura central.

**O resultado será um assistente próprio, acessível pelo navegador, independente do plano ChatGPT dos usuários e integrado diretamente às funcionalidades financeiras do Invest Dashboard.**
