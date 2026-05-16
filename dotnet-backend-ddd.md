# Plano de Implementação: Backend Invest-Dashboard (.NET 10 + DDD + Supabase/PostgreSQL)

Este documento descreve o plano estratégico e estruturado para o desenvolvimento do backend de alta performance do **Invest-Dashboard**. O sistema será construído usando as tecnologias mais modernas de 2026, com foco em resiliência, escalabilidade, design limpo e observabilidade.

---

## 📋 1. Visão Geral (Overview)

O objetivo é criar uma API robusta de investimentos dividida em três contextos delimitados fundamentais:
1. **Contexto de Consolidação de Carteira (Portfolio Consolidation Context):** Responsável por calcular rentabilidade, saldos consolidados, histórico de patrimônio e distribuição de ativos.
2. **Contexto de Negociação (Trading Context):** Responsável por registrar aportes, retiradas, compras e vendas de ativos (Ações, FIIs, Criptomoedas, Tesouro).
3. **Contexto de Market Data (Market Data Context):** Responsável pelo monitoramento e atualização de cotações de ativos em tempo real e de forma agendada em background.

---

## 🎯 2. Tipo do Projeto (Project Type)

- **Tipo:** **BACKEND** (Standalone API + Servidor de Mensagens Realtime + Background Services)
- **Agente Responsável:** `backend-specialist` (com suporte de `database-architect` e `devops-engineer`)
- **Skill Principal:** `dotnet-best-practices`, `database-design`, `api-patterns`

---

## 📈 3. Critérios de Sucesso (Success Criteria)

- [ ] **Cobertura de Testes:** Mínimo de **80%** de cobertura no projeto `Domain` e `Application` utilizando testes unitários estruturados (AAA Pattern).
- [ ] **Autenticação Segura:** Integração com o **Supabase Auth** validando tokens JWT de forma segura e injetando as claims do usuário (`User ID`) nas requisições da API.
- [ ] **Tempo de Resposta (SLA):** < **150ms** para listagem consolidada de carteira com cache inteligente em nível de aplicação/infraestrutura.
- [ ] **Realtime de Cotações:** Hub **SignalR** atualizando preços ativos para os clientes conectados a cada 5 segundos (no horário do mercado) de forma não bloqueante.
- [ ] **Design Limpo (Clean Code):** Zero avisos de linter e compilação no .NET 10.
- [ ] **Dockerização:** Ambiente local 100% reprodutível com um único comando `docker compose up --build`.

---

## 🛠️ 4. Stack Tecnológica (Tech Stack)

| Tecnologia | Escolha | Justificativa |
| :--- | :--- | :--- |
| **Runtime** | .NET 10 (C# 14) | Desempenho extremo, novos recursos de linguagem (`field` keyword, melhorias no compilador e JIT) e suporte nativo a AOT se necessário. |
| **ORM** | Entity Framework Core 10 | Mapeamento relacional avançado, suporte de primeira classe ao PostgreSQL, geração de consultas otimizadas e facilidade no gerenciamento de transações. |
| **Banco de Dados** | PostgreSQL (Supabase Docker) | Banco relacional robusto, extensões prontas (como `pgvector` e `pgjwt`) e escalabilidade nativa. |
| **Autenticação** | JwtBearer Authentication + Supabase Auth | Delegação do gerenciamento de identidade para o Supabase Auth com validação de assinatura de chaves RSA em nível de API .NET. |
| **Realtime** | ASP.NET Core SignalR | Substituição do Supabase Realtime por SignalR, fornecendo uma infraestrutura gerenciada pelo .NET para comunicação bidirecional de baixíssima latência. |
| **Armazenamento** | Supabase Storage (com feature flag) | Abstração flexível (com fallback para Base64/DB) para armazenamento de notas de corretagem ou comprovantes. |
| **Background Tasks** | `IHostedService` / `BackgroundService` | Agendamento interno nativo no .NET para processamento de filos e sincronização de cotações de mercado. |
| **Testes** | xUnit + FluentAssertions | Frameworks modernos, expressivos e integrados ao ecossistema .NET. |

---

## 📁 5. Estrutura de Arquivos Planejada (File Structure)

A estrutura seguirá uma **Clean Architecture** orientada a **DDD** (Domain-Driven Design), isolando as regras de negócio de detalhes de entrega e de infraestrutura.

```plaintext
src/Backend/
├── InvestDashboard.sln                      # Solução .NET
├── docker-compose.yml                       # Docker Compose para a infraestrutura do Supabase + PostgreSQL + API
├── src/
│   ├── InvestDashboard.Domain/              # Camada de Domínio (Sem dependências externas)
│   │   ├── Common/                          # Entidades base, Value Objects, Domain Events
│   │   ├── Aggregates/                      # Agregados com Entidades e Regras de Negócio
│   │   │   ├── Portfolio/                   # Agregado de Consolidação de Carteira (Carteira, PosicaoAtivo)
│   │   │   ├── Trading/                     # Agregado de Negociação (Ordem, Transacao)
│   │   │   └── MarketData/                  # Agregado de Ativos e Cotações (Ativo, CotacaoHistorica)
│   │   ├── Exceptions/                      # Exceções de Domínio customizadas
│   │   └── Repository/                      # Interfaces dos Repositórios (Portas de Saída)
│   │
│   ├── InvestDashboard.Application/         # Camada de Aplicação (Regras de Caso de Uso)
│   │   ├── Services/                        # Serviços de Aplicação (Orquestração de Casos de Uso)
│   │   ├── DTOs/                            # Objetos de Transferência de Dados (Records)
│   │   ├── Interfaces/                      # Abstrações de Serviços Externos (Ex: ISupabaseStorageService)
│   │   └── Mappers/                         # Classes de mapeamento DTO <-> Entidade (Mappers manuais/eficientes)
│   │
│   ├── InvestDashboard.Infrastructure/      # Camada de Infraestrutura (Detalhes de Tecnologia)
│   │   ├── Persistence/
│   │   │   ├── EFCore/
│   │   │   │   ├── Configurations/          # Mapeamento do EF Core (Fluent API)
│   │   │   │   ├── InvestDbContext.cs       # Contexto do Banco de Dados
│   │   │   │   └── Migrations/              # Migrações geradas pelo EF Core
│   │   │   └── RepositoryImpl/              # Implementações de Repositórios
│   │   ├── ExternalServices/
│   │   │   ├── Supabase/
│   │   │   │   ├── SupabaseAuthHandler.cs   # Custom Token Validator para Supabase JWT
│   │   │   │   └── SupabaseStorageService.cs# Implementação do serviço de Storage
│   │   │   └── MarketData/
│   │   │       └── YahooFinanceClient.cs    # Sincronizador com APIs de mercado
│   │   ├── Realtime/
│   │   │   └── SignalR/
│   │   │       ├── MarketDataHub.cs         # Hub do SignalR para cotações
│   │   │       └── RealtimeBroadcaster.cs   # Transmissor de dados para o SignalR
│   │   └── BackgroundWorkers/
│   │       └── MarketDataUpdateWorker.cs    # Worker em Background que atualiza cotações periodicamente
│   │
│   └── InvestDashboard.WebAPI/              # Camada de Apresentação (Minimal APIs ou Controllers)
│       ├── Controllers/                     # Controladores HTTP (Portfolio, Transactions, Assets)
│       ├── Middleware/                      # Middleware de tratamento de erros e logs
│       ├── Extensions/                      # Configurações de Dependency Injection
│       ├── Program.cs                       # Ponto de entrada do sistema (.NET 10)
│       ├── appsettings.json                 # Configurações do sistema
│       └── Dockerfile                       # Dockerfile otimizado de multi-stage build para .NET 10
│
└── tests/
    ├── InvestDashboard.UnitTests/           # Testes Unitários (Domain, Application)
    └── InvestDashboard.IntegrationTests/    # Testes de Integração (WebAPI, Database)
```

---

## 🗺️ 6. Cronograma e Decomposição de Tarefas (Task Breakdown)

### 🧱 Fase 1: Infraestrutura & Dockerização (P0 - Fundação)
Foco em erguer o ambiente local reprodutível utilizando Docker Compose e configurar a espinha dorsal do banco de dados relacional.

| ID | Tarefa | Agente | Skills | Prioridade | Dependências | Descrição |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **T1.1** | Criar Docker Compose Completo | `devops-engineer` | `docker-expert` | Alta | Nenhuma | Configurar o arquivo `docker-compose.yml` para iniciar uma instância local de PostgreSQL (utilizando a imagem recomendada do Supabase `supabase/postgres:15.1.1` ou similar) junto aos serviços necessários de mock do Supabase Auth se necessário, e expor as portas adequadas.<br>**INPUT:** Nenhuma.<br>**OUTPUT:** `src/Backend/docker-compose.yml` configurado.<br>**VERIFY:** Executar `docker compose up -d db` e testar a conexão com o banco de dados. |
| **T1.2** | Estruturação da Solução .NET 10 | `dotnet-expert` | `dotnet-best-practices` | Alta | **T1.1** | Criar a Solution .NET 10 e gerar os projetos `Domain`, `Application`, `Infrastructure`, `WebAPI` e os projetos de teste separados.<br>**INPUT:** dotnet CLI.<br>**OUTPUT:** Estrutura de pastas e arquivos `.csproj` criados e referenciados.<br>**VERIFY:** Executar `dotnet build` na raiz da solução e certificar de que compila com zero erros. |

---

### 🛡️ Fase 2: Domínio do DDD (P1 - Core)
Implementação pura das entidades de negócio, agregados, objetos de valor e contratos de repositório, garantindo total isolamento tecnológico.

| ID | Tarefa | Agente | Skills | Prioridade | Dependências | Descrição |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **T2.1** | Modelo do Domínio: Contexto de Negociação | `backend-specialist` | `clean-code` | Alta | **T1.2** | Criar entidades e agregados do contexto de negociação (`Ativo`, `Ordem` e `Transacao`), contendo regras de validação interna, eventos de domínio e regras de negócio para aportes/vendas.<br>**INPUT:** Regras de negócio de transações de ativos.<br>**OUTPUT:** Classes de entidades dentro de `InvestDashboard.Domain/Aggregates/Trading`.<br>**VERIFY:** Escrita de testes unitários em `TradingTests.cs` validando criação e mutação de estado de transações. |
| **T2.2** | Modelo do Domínio: Contexto de Consolidação | `backend-specialist` | `clean-code` | Alta | **T2.1** | Criar agregado `Carteira` (Portfolio) e as lógicas de negócio para calcular a rentabilidade ponderada pelo tempo (MWRR/TWRR) e a distribuição percentual do patrimônio.<br>**INPUT:** Fórmulas matemáticas de rentabilidade financeira.<br>**OUTPUT:** Entidade `Carteira` e objetos de valor em `InvestDashboard.Domain/Aggregates/Portfolio`.<br>**VERIFY:** Testes unitários com dados simulados calculando rentabilidade com assertividade exata. |
| **T2.3** | Modelo do Domínio: Contexto de Market Data | `backend-specialist` | `clean-code` | Média | **T2.2** | Criar entidades que gerenciam cotações históricas (`CotacaoHistorica`) e os contratos/interfaces para as fontes externas de cotação.<br>**INPUT:** Necessidades de dados de cotação de ativos.<br>**OUTPUT:** Entidades e interfaces em `InvestDashboard.Domain/Aggregates/MarketData`.<br>**VERIFY:** Testes unitários validando criação de cotações com carimbo de data/hora (UTC). |

---

### 💾 Fase 3: Persistência & Integração Supabase Auth (P1 - Core)
Implementação física do EF Core mapeando os agregados, geração das migrações e middleware de autenticação baseado no JWT do Supabase.

| ID | Tarefa | Agente | Skills | Prioridade | Dependências | Descrição |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **T3.1** | Configuração do DbContext & Fluent API | `database-architect` | `database-design` | Alta | **T2.2** | Mapear as entidades do domínio para tabelas do PostgreSQL usando Fluent API do EF Core 10. Configurar relacionamentos, chaves primárias/estrangeiras e indices importantes em colunas de busca rápida (ex: `AtivoId`, `UsuarioId`).<br>**INPUT:** Interfaces e modelos criados na Fase 2.<br>**OUTPUT:** `InvestDbContext.cs` e classes de configuração em `Infrastructure/Persistence/EFCore`.<br>**VERIFY:** Rodar `dotnet ef migrations add InitialCreate` com sucesso. |
| **T3.2** | Integração com Supabase Auth (JWT JwtBearer) | `security-auditor` | `vulnerability-scanner` | Alta | **T1.2** | Configurar o middleware `JwtBearer` na WebAPI para validar tokens gerados pelo Supabase Auth. Garantir extração correta da claim `sub` (User ID do Supabase) para mapear o usuário atual de forma segura na API .NET.<br>**INPUT:** URL do Supabase Project e chave pública JWT.<br>**OUTPUT:** Configuração de serviços de autenticação em `Program.cs` ou classe de extensão.<br>**VERIFY:** Fazer chamada a um endpoint protegido com um token inválido (deve dar 401) e com um token válido (deve dar 200). |
| **T3.3** | Supabase Storage com Feature Flag | `backend-specialist` | `clean-code` | Média | **T1.2** | Criar o serviço `SupabaseStorageService` para persistir arquivos (como PDFs de notas de corretagem) no bucket do Supabase Storage. Implementar uma chave de configuração (Feature Flag) que, se desativada, converte o arquivo para Base64 e armazena diretamente no Postgres (fallback).<br>**INPUT:** SDK ou REST Client do Supabase Storage.<br>**OUTPUT:** Interface `ISupabaseStorageService` e implementação correspondente.<br>**VERIFY:** Testar upload de um arquivo de teste e verificar a persistência correta de acordo com o status da Feature Flag. |

---

### ⚡ Fase 4: Casos de Uso & Realtime SignalR (P2 - Core & UI)
Implementação lógica dos serviços de aplicação que processam transações, consolidam carteiras e atualizam cotações em background via SignalR.

| ID | Tarefa | Agente | Skills | Prioridade | Dependências | Descrição |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **T4.1** | Serviços de Aplicação (Portfolio & Trading) | `backend-specialist` | `clean-code` | Alta | **T3.1** | Desenvolver serviços que recebem dados das transações, salvam através dos repositórios e disparam a consolidação do saldo da Carteira de forma consistente.<br>**INPUT:** DTOs de cadastro de transação.<br>**OUTPUT:** Serviços em `InvestDashboard.Application/Services`.<br>**VERIFY:** Testes de integração simulando um fluxo completo de transação (Aporte -> Compra -> Saldo Consolidado). |
| **T4.2** | SignalR Realtime Hub para Cotações | `backend-specialist` | `clean-code` | Alta | **T1.2** | Criar o `MarketDataHub` do SignalR. Clientes frontend poderão se conectar a esse hub e se inscrever para receber atualizações de cotações em tempo real de ativos que eles possuem na carteira.<br>**INPUT:** Hub Context do SignalR.<br>**OUTPUT:** `MarketDataHub.cs` em `Infrastructure/Realtime/SignalR`.<br>**VERIFY:** Conectar um client de teste ao endpoint `/hubs/market-data` e testar recebimento de payloads em tempo real. |
| **T4.3** | Background Service para Atualização de Cotações | `backend-specialist` | `performance-profiling` | Alta | **T4.2**, **T2.3** | Criar um `BackgroundService` (.NET Hosted Service) que é executado periodicamente (ex: a cada 10 segundos). Ele busca as cotações ativas dos ativos custodiados (via mock de mercado ou API Yahoo Finance) e utiliza o SignalR Broadcaster para notificar todos os usuários conectados interessados.<br>**INPUT:** IHostedService Lifecycle.<br>**OUTPUT:** `MarketDataUpdateWorker.cs` em `Infrastructure/BackgroundWorkers`.<br>**VERIFY:** Iniciar a API e verificar no log da aplicação a execução periódica do worker e despacho de mensagens via SignalR. |

---

### 🌐 Fase 5: Exposição de Endpoints & Middleware (P2 - UI/UX)
Criação dos controladores da WebAPI, tratamento de erros resiliente e documentação com Swagger/OpenAPI.

| ID | Tarefa | Agente | Skills | Prioridade | Dependências | Descrição |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **T5.1** | Controladores HTTP da API | `backend-specialist` | `api-patterns` | Alta | **T4.1** | Criar endpoints RESTful em `WebAPI` expostos para o frontend (ex: `GET /api/portfolio/consolidated`, `POST /api/orders`, `GET /api/assets/quotes`). Todos protegidos por `Authorize` do Supabase Auth.<br>**INPUT:** HTTP Requests.<br>**OUTPUT:** Controladores em `InvestDashboard.WebAPI/Controllers`.<br>**VERIFY:** Requisições via Postman/cURL retornando dados no formato JSON especificado. |
| **T5.2** | Global Exception Handler Middleware | `dotnet-expert` | `dotnet-best-practices` | Alta | **T1.2** | Implementar um middleware global no ASP.NET Core para interceptar exceções de domínio (ex: saldo insuficiente, ativo não encontrado) e retorná-las formatadas em um padrão amigável (como RFC 7807 Problem Details), ocultando erros de infraestrutura internos no ambiente de produção.<br>**INPUT:** HttpContext pipeline.<br>**OUTPUT:** `ExceptionHandlingMiddleware.cs`.<br>**VERIFY:** Forçar um erro de domínio em uma chamada de API e garantir o retorno formatado com status HTTP correto (ex: 400 Bad Request, 422 Unprocessable Entity). |

---

## 🧪 7. Estratégia de Testes (Testing Strategy)

Seguiremos a pirâmide de testes clássica:

1. **Testes Unitários:** Isolamento total com Mocking de dados apenas para serviços externos. O foco de assertividade será nas regras de negócio (MWRR, cálculos tributários, regras de carteira).
2. **Testes de Integração:** Utilizando `WebApplicationFactory` do .NET para levantar a API em memória, executando requisições HTTP reais contra o banco de dados PostgreSQL rodando localmente via Docker.

---

## ⚡ 8. Plano de Recuperação e Rollback (Rollback Plan)

* **Caso de falha na migração do banco:** Toda migração será executada sob uma transação explícita. Se falhar, o script fará rollback automático. O pipeline terá um passo de validação prévia de esquema (`schema_validator.py`).
* **Incompatibilidade de Token Supabase:** Caso o certificado público do Supabase Auth expire ou mude, configuraremos o JWT Bearer com chaves recarregáveis dinamicamente a partir do endpoint do OpenID Connect (`/.well-known/openid-configuration`) do Supabase, evitando paradas de autenticação.
* **Queda do serviço externo de Cotações:** Se o fornecedor de cotações (ex: API Yahoo Finance) falhar ou sofrer rate limit, o `MarketDataUpdateWorker` reterá os últimos valores conhecidos salvos no banco de dados local PostgreSQL e aplicará uma política de retry exponencial (Polly).

---

## 🏁 9. Phase X: Verificação Final (Verification Checklist)

O projeto do Backend só será considerado finalizado e pronto para produção quando todos os itens abaixo forem executados e validados com êxito:

### 1. Auditoria Manual e Regras de Negócio
- [ ] O Socratic Gate foi totalmente respeitado e as decisões acordadas foram aplicadas.
- [ ] Não há vazamento de chaves secretas ou conexões de produção no código fonte (todas estão parametrizadas em variáveis de ambiente).
- [ ] Mapeamento do EF Core otimizado, sem o antipattern de carregar grafos inteiros desnecessários (`AsNoTracking()` utilizado por padrão para leituras).

### 2. Execução dos Scripts Automatizados
Execute os seguintes scripts localizados no diretório de agentes para verificar a conformidade de segurança e qualidade do projeto:

```bash
# Executar análise de segurança de dependências e códigos do projeto
python .agents/skills/vulnerability-scanner/scripts/security_scan.py .

# Executar a verificação de Linter e Análise Estática do código .NET
dotnet build --configuration Release /warnaserror
```

### 3. Build & Runtime
- [ ] O projeto compila em modo Release com zero erros: `dotnet build -c Release`
- [ ] O container de banco de dados e a API sobem perfeitamente em conjunto: `docker compose up --build`
- [ ] Todos os testes passam localmente com sucesso: `dotnet test`

### 4. Relatórios de Qualidade (Adicionar ao final da implementação)
```markdown
## ✅ PHASE X COMPLETE
- Lint/Compiler: ✅ Pass (Zero Warnings / Zero Errors)
- Security: ✅ No critical issues (Security scan passed)
- Database Schema: ✅ Mapped successfully with EF Core Migrations
- Date: [Data Atual]
```
