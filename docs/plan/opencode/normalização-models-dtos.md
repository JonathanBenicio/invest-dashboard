# Normalização de Models, DTOs e Entidades — Conceitos PT-BR

> **Objetivo:** Padronizar todos os nomes de entidades, DTOs, models e classes do projeto seguindo os **conceitos de domínio em português brasileiro**, alinhados com as rotas do frontend.

> **Motivação:** Existem muitas discrepâncias entre backend e frontend. O frontend usa nomes como `InvestmentDto` enquanto o backend usa `AssetPositionDto`. O frontend tem rotas em PT-BR (`/carteiras`, `/renda-fixa`, `/taxas`) mas o backend usa nomes em inglês. A padronização em PT-BR elimina confusão cognitiva e alinha código com o domínio de negócio brasileiro.

---

## Convenção Definida

| Contexto | Padrão | Exemplos |
|----------|--------|----------|
| **Entidades de Domínio** (C#) | Português (`Carteira`) | `Carteira`, `Ativo`, `Transacao`, `TaxaEconomica` |
| **DTOs** (C# e TypeScript) | Português (`CarteiraDto`) | `CarteiraDto`, `PosicaoInvestimentoDto`, `TransacaoDto` |
| **Tipos/Interfaces** (TypeScript) | Português (`CarteiraDto`) | `CarteiraDto`, `CriarCarteiraRequest`, `InvestimentoFiltros` |
| **Endpoints da API** | Manter inglês (URLs) | `/api/v1/portfolios`, `/api/v1/investments` |
| **DTOs genéricos de infra** | Manter inglês | `ApiResponse`, `PaginatedResponse`, `BaseEntity` |
| **UI visível** (labels, toasts) | Português | `"Nova Carteira"`, `"Carteira criada"` |
| **Pastas de código** | Manter inglês | `pages/portfolio/`, `services/portfolio.service.ts` |
| **Variáveis/parâmetros** | Português | `carteira`, `posicoes`, `taxas` |

---

## Mapeamento Completo de Renomeação

### 1. Domínio Carteira (Portfolio)

| Camada | Nome Atual | Novo Nome | Arquivo |
|--------|------------|-----------|---------|
| Domain | `Portfolio` | `Carteira` | `src/.../Domain/Aggregates/Portfolio/Portfolio.cs` |
| Domain | `AssetPosition` | `PosicaoInvestimento` | `src/.../Domain/Aggregates/Portfolio/AssetPosition.cs` |
| DTO | `PortfolioDto` | `CarteiraDto` | `src/.../Application/DTOs/Portfolio/PortfolioDto.cs` |
| DTO | `CreatePortfolioDto` | `CriarCarteiraDto` | `src/.../Application/DTOs/Portfolio/CreatePortfolioDto.cs` |
| DTO | `PortfolioSummaryDto` | `ResumoCarteiraDto` | `src/.../Application/DTOs/Portfolio/PortfolioDto.cs` |
| DTO | `AssetAllocationDto` | `AlocacaoAtivoDto` | `src/.../Application/DTOs/Portfolio/PortfolioDto.cs` |
| DTO | `PerformancePointDto` | `PontoPerformanceDto` | `src/.../Application/DTOs/Portfolio/PortfolioDto.cs` |
| Frontend DTO | `PortfolioDto` | `CarteiraDto` | `frontend/src/api/dtos/portfolio.dto.ts` |
| Frontend DTO | `CreatePortfolioRequest` | `CriarCarteiraRequest` | `frontend/src/api/dtos/portfolio.dto.ts` |
| Frontend DTO | `UpdatePortfolioRequest` | `AtualizarCarteiraRequest` | `frontend/src/api/dtos/portfolio.dto.ts` |
| Frontend DTO | `PortfolioSummaryDto` | `ResumoCarteiraDto` | `frontend/src/api/dtos/portfolio.dto.ts` |
| Frontend DTO | `AssetAllocationDto` | `AlocacaoAtivoDto` | `frontend/src/api/dtos/portfolio.dto.ts` |
| Frontend DTO | `PerformancePointDto` | `PontoPerformanceDto` | `frontend/src/api/dtos/portfolio.dto.ts` |
| Frontend DTO | `PortfolioFilters` | `CarteiraFiltros` | `frontend/src/api/dtos/portfolio.dto.ts` |
| Service | `IPortfolioAppService` | `ICarteiraAppService` | `src/.../Application/Interfaces/IPortfolioAppService.cs` |
| Service | `PortfolioAppService` | `CarteiraAppService` | `src/.../Application/Services/PortfolioAppService.cs` |
| Repository | `IPortfolioRepository` | `ICarteiraRepository` | `src/.../Domain/Repository/IPortfolioRepository.cs` |
| Repository | `PortfolioRepository` | `CarteiraRepository` | `src/.../Infrastructure/Persistence/RepositoryImpl/PortfolioRepository.cs` |
| Controller | `PortfoliosController` | `CarteirasController` | `src/.../WebAPI/Controllers/PortfoliosController.cs` |
| Config | `PortfolioConfiguration` | `CarteiraConfiguration` | `src/.../Infrastructure/Persistence/EFCore/Configurations/PortfolioConfiguration.cs` |
| Config | `AssetPositionConfiguration` | `PosicaoInvestimentoConfiguration` | `src/.../Infrastructure/Persistence/EFCore/Configurations/AssetPositionConfiguration.cs` |

### 2. Domínio Investimento (Investment / Asset / AssetPosition)

| Camada | Nome Atual | Novo Nome | Arquivo |
|--------|------------|-----------|---------|
| Domain | `Asset` | `Ativo` | `src/.../Domain/Aggregates/MarketData/Asset.cs` |
| Domain | `AssetType` | `TipoAtivo` | `src/.../Domain/Aggregates/MarketData/AssetType.cs` |
| Domain | `StockAsset` | `Acao` | `src/.../Domain/Aggregates/MarketData/StockAsset.cs` |
| Domain | `FiiAsset` | `FundoImobiliario` | `src/.../Domain/Aggregates/MarketData/FiiAsset.cs` |
| Domain | `FixedIncomeAsset` | `RendaFixa` | `src/.../Domain/Aggregates/MarketData/FixedIncomeAsset.cs` |
| Domain | `CryptoAsset` | `Criptoativo` | `src/.../Domain/Aggregates/MarketData/CryptoAsset.cs` |
| Domain | `HistoricalPrice` | `PrecoHistorico` | `src/.../Domain/Aggregates/MarketData/HistoricalPrice.cs` |
| DTO | `AssetPositionDto` | `PosicaoInvestimentoDto` | `src/.../Application/DTOs/Portfolio/AssetPositionDto.cs` |
| Frontend DTO | `InvestmentDto` | `PosicaoInvestimentoDto` | `frontend/src/api/dtos/investment.dto.ts` |
| Frontend DTO | `InvestmentType` | `TipoInvestimento` | `frontend/src/api/dtos/investment.dto.ts` |
| Frontend DTO | `FixedIncomeType` | `TipoRendaFixa` | `frontend/src/api/dtos/investment.dto.ts` |
| Frontend DTO | `VariableIncomeType` | `TipoRendaVariavel` | `frontend/src/api/dtos/investment.dto.ts` |
| Frontend DTO | `FixedIncomeDto` | `RendaFixaDto` | `frontend/src/api/dtos/investment.dto.ts` |
| Frontend DTO | `VariableIncomeDto` | `RendaVariavelDto` | `frontend/src/api/dtos/investment.dto.ts` |
| Frontend DTO | `CreateFixedIncomeRequest` | `CriarRendaFixaRequest` | `frontend/src/api/dtos/investment.dto.ts` |
| Frontend DTO | `CreateVariableIncomeRequest` | `CriarRendaVariavelRequest` | `frontend/src/api/dtos/investment.dto.ts` |
| Frontend DTO | `UpdateInvestmentRequest` | `AtualizarInvestimentoRequest` | `frontend/src/api/dtos/investment.dto.ts` |
| Frontend DTO | `InvestmentFilters` | `InvestimentoFiltros` | `frontend/src/api/dtos/investment.dto.ts` |
| Frontend DTO | `InvestmentSummaryDto` | `ResumoInvestimentoDto` | `frontend/src/api/dtos/investment.dto.ts` |
| Controller DTO | `CreateFixedIncomeRequestDto` | `CriarRendaFixaDto` | `src/.../WebAPI/Controllers/InvestmentsController.cs` (inline) |
| Controller DTO | `CreateVariableIncomeRequestDto` | `CriarRendaVariavelDto` | `src/.../WebAPI/Controllers/InvestmentsController.cs` (inline) |
| Controller DTO | `UpdateInvestmentRequestDto` | `AtualizarInvestimentoDto` | `src/.../WebAPI/Controllers/InvestmentsController.cs` (inline) |
| Repository | `IAssetRepository` | `IAtivoRepository` | `src/.../Domain/Repository/IAssetRepository.cs` |
| Repository | `AssetRepository` | `AtivoRepository` | `src/.../Infrastructure/Persistence/RepositoryImpl/AssetRepository.cs` |
| Repository | `IHistoricalPriceRepository` | `IPrecoHistoricoRepository` | `src/.../Domain/Repository/IHistoricalPriceRepository.cs` |
| Repository | `HistoricalPriceRepository` | `PrecoHistoricoRepository` | `src/.../Infrastructure/Persistence/RepositoryImpl/HistoricalPriceRepository.cs` |
| Config | `AssetConfiguration` | `AtivoConfiguration` | `src/.../Infrastructure/Persistence/EFCore/Configurations/AssetConfiguration.cs` |
| Config | `StockAssetConfiguration` | `AcaoConfiguration` | `src/.../Infrastructure/Persistence/EFCore/Configurations/StockAssetConfiguration.cs` |
| Config | `FiiAssetConfiguration` | `FundoImobiliarioConfiguration` | `src/.../Infrastructure/Persistence/EFCore/Configurations/FiiAssetConfiguration.cs` |
| Config | `FixedIncomeAssetConfiguration` | `RendaFixaConfiguration` | `src/.../Infrastructure/Persistence/EFCore/Configurations/FixedIncomeAssetConfiguration.cs` |
| Config | `CryptoAssetConfiguration` | `CriptoativoConfiguration` | `src/.../Infrastructure/Persistence/EFCore/Configurations/CryptoAssetConfiguration.cs` |
| Config | `HistoricalPriceConfiguration` | `PrecoHistoricoConfiguration` | `src/.../Infrastructure/Persistence/EFCore/Configurations/HistoricalPriceConfiguration.cs` |

### 3. Domínio Transação (Transaction)

| Camada | Nome Atual | Novo Nome | Arquivo |
|--------|------------|-----------|---------|
| Domain | `Transaction` | `Transacao` | `src/.../Domain/Aggregates/Trading/Transaction.cs` |
| Domain | `TransactionType` | `TipoTransacao` | `src/.../Domain/Aggregates/Trading/TransactionType.cs` |
| DTO | `TransactionDto` | `TransacaoDto` | `src/.../Application/DTOs/Trading/TransactionDto.cs` |
| DTO | `RegisterTransactionDto` | `RegistrarTransacaoDto` | `src/.../Application/DTOs/Trading/RegisterTransactionDto.cs` |
| Frontend DTO | _(usando `any`)_ | `TransacaoDto` | `frontend/src/api/dtos/transacao.dto.ts` (novo) |
| Frontend DTO | _(não existe)_ | `RegistrarTransacaoRequest` | `frontend/src/api/dtos/transacao.dto.ts` (novo) |
| Frontend DTO | _(não existe)_ | `TransacaoFiltros` | `frontend/src/api/dtos/transacao.dto.ts` (novo) |
| Service | `ITransactionAppService` | `ITransacaoAppService` | `src/.../Application/Interfaces/ITransactionAppService.cs` |
| Service | `TransactionAppService` | `TransacaoAppService` | `src/.../Application/Services/TransactionAppService.cs` |
| Repository | `ITransactionRepository` | `ITransacaoRepository` | `src/.../Domain/Repository/ITransactionRepository.cs` |
| Repository | `TransactionRepository` | `TransacaoRepository` | `src/.../Infrastructure/Persistence/RepositoryImpl/TransactionRepository.cs` |
| Controller | `TransactionController` | `TransacoesController` | `src/.../WebAPI/Controllers/TransactionController.cs` |
| Config | `TransactionConfiguration` | `TransacaoConfiguration` | `src/.../Infrastructure/Persistence/EFCore/Configurations/TransactionConfiguration.cs` |

### 4. Domínio Simulação (Simulation)

| Camada | Nome Atual | Novo Nome | Arquivo |
|--------|------------|-----------|---------|
| Domain | `SimulationParameters` | `SimulacaoParametros` | `src/.../Domain/Services/SimulationParameters.cs` |
| Domain | `SimulationResult` | `SimulacaoResultado` | `src/.../Domain/Services/SimulationResult.cs` |
| Domain | `SimulationPoint` | `SimulacaoPonto` | `src/.../Domain/Services/SimulationResult.cs` |
| Domain | `ISimulationStrategy` | `IEstrategiaSimulacao` | `src/.../Domain/Services/ISimulationStrategy.cs` |
| Domain | `DeterministicStrategy` | `EstrategiaDeterministica` | `src/.../Domain/Services/DeterministicStrategy.cs` |
| Domain | `MonteCarloStrategy` | `EstrategiaMonteCarlo` | `src/.../Domain/Services/MonteCarloStrategy.cs` |
| DTO | `SimulationRequestDto` | `SimulacaoRequestDto` | `src/.../Application/DTOs/Simulation/SimulationDtos.cs` |
| DTO | `SimulationResponseDto` | `SimulacaoResponseDto` | `src/.../Application/DTOs/Simulation/SimulationDtos.cs` |
| DTO | `SimulationPointDto` | `SimulacaoPontoDto` | `src/.../Application/DTOs/Simulation/SimulationDtos.cs` |
| Frontend DTO | `SimulationRequest` | `SimulacaoRequest` | `frontend/src/api/dtos/simulation.dto.ts` |
| Frontend DTO | `SimulationResponse` | `SimulacaoResponse` | `frontend/src/api/dtos/simulation.dto.ts` |
| Frontend DTO | `SimulationPointDto` | `SimulacaoPontoDto` | `frontend/src/api/dtos/simulation.dto.ts` |
| Frontend DTO | `SimulationStrategy` | `SimulacaoEstrategia` | `frontend/src/api/dtos/simulation.dto.ts` |
| Controller | `SimulationController` | `SimulacaoController` | `src/.../WebAPI/Controllers/SimulationController.cs` |

### 5. Domínio Taxa (EconomicRate)

| Camada | Nome Atual | Novo Nome | Arquivo |
|--------|------------|-----------|---------|
| Domain | `EconomicRate` | `TaxaEconomica` | `src/.../Domain/Aggregates/MarketData/EconomicRate.cs` |
| DTO | `EconomicRateDto` | `TaxaEconomicaDto` | `src/.../Application/DTOs/Taxes/EconomicRateDto.cs` |
| DTO | `CreateEconomicRateDto` | `CriarTaxaEconomicaDto` | `src/.../Application/DTOs/Taxes/EconomicRateDto.cs` |
| DTO | `UpdateEconomicRateDto` | `AtualizarTaxaEconomicaDto` | `src/.../Application/DTOs/Taxes/EconomicRateDto.cs` |
| Frontend DTO | `EconomicRateDto` | `TaxaEconomicaDto` | `frontend/src/api/dtos/taxes.dto.ts` |
| Frontend DTO | `CreateEconomicRateRequest` | `CriarTaxaEconomicaRequest` | `frontend/src/api/dtos/taxes.dto.ts` |
| Frontend DTO | _(não existe)_ | `AtualizarTaxaEconomicaRequest` | `frontend/src/api/dtos/taxes.dto.ts` (novo) |
| Service | `ITaxesAppService` | `ITaxasAppService` | `src/.../Application/Interfaces/ITaxesAppService.cs` |
| Service | `TaxesAppService` | `TaxasAppService` | `src/.../Application/Services/TaxesAppService.cs` |
| Repository | `IEconomicRateRepository` | `ITaxaEconomicaRepository` | `src/.../Domain/Repository/IEconomicRateRepository.cs` |
| Repository | `EconomicRateRepository` | `TaxaEconomicaRepository` | `src/.../Infrastructure/Persistence/RepositoryImpl/EconomicRateRepository.cs` |
| Controller | `TaxesController` | `TaxasController` | `src/.../WebAPI/Controllers/TaxesController.cs` |
| Config | `EconomicRateConfiguration` | `TaxaEconomicaConfiguration` | `src/.../Infrastructure/Persistence/EFCore/Configurations/EconomicRateConfiguration.cs` |

### 6. Outros (Infraestrutura e Serviços)

| Camada | Nome Atual | Novo Nome | Arquivo |
|--------|------------|-----------|---------|
| Domain | `ITaxCalculationService` | `ICalculoImpostoService` | `src/.../Domain/Services/ITaxCalculationService.cs` |
| Domain | `TaxCalculationService` | `CalculoImpostoService` | `src/.../Domain/Services/TaxCalculationService.cs` |
| Infra | `MarketDataUpdateWorker` | `AtualizadorDadosMercadoWorker` | `src/.../Infrastructure/BackgroundWorkers/MarketDataUpdateWorker.cs` |
| Infra | `MarketDataHub` | `DadosMercadoHub` | `src/.../Infrastructure/Realtime/SignalR/MarketDataHub.cs` |
| Infra | `CurrentUserService` | `UsuarioAtualService` | `src/.../Infrastructure/Services/CurrentUserService.cs` |
| Infra | `SupabaseStorageService` | `SupabaseStorageService` (manter) | `src/.../Infrastructure/Services/SupabaseStorageService.cs` |
| Infra | `InvestDbContext` | `InvestDbContext` (manter) | `src/.../Infrastructure/Persistence/EFCore/InvestDbContext.cs` |
| Infra | `UnitOfWork` | `UnitOfWork` (manter) | `src/.../Infrastructure/Persistence/UnitOfWork.cs` |
| Infra | `ICurrentUserService` | `IUsuarioAtualService` | `src/.../Application/Interfaces/ICurrentUserService.cs` |
| Infra | `ISupabaseStorageService` | `ISupabaseStorageService` (manter) | `src/.../Application/Interfaces/ISupabaseStorageService.cs` |
| Infra | `IUnitOfWork` | `IUnitOfWork` (manter) | `src/.../Application/Interfaces/IUnitOfWork.cs` |
| Infra | `ExceptionHandlingMiddleware` | Manter (infra genérico) | `src/.../WebAPI/Middleware/ExceptionHandlingMiddleware.cs` |
| Infra | `InvestmentsController` | `InvestimentosController` | `src/.../WebAPI/Controllers/InvestmentsController.cs` |

---

## Resumo de Impacto por Camada

| Camada | Arquivos Estimados | Complexidade |
|--------|-------------------|--------------|
| **Domain** (entidades, interfaces, services) | ~20 arquivos | Alta — base de tudo |
| **Application** (DTOs, interfaces, services) | ~15 arquivos | Média — depende do domain |
| **Infrastructure** (EF Core configs, repos, workers) | ~20 arquivos | Média — depende do domain |
| **WebAPI** (controllers) | ~5 arquivos | Baixa — depende dos DTOs |
| **Tests** (unit e integration) | ~5 arquivos | Média — segue renomeações |
| **Frontend DTOs** | ~6 arquivos | Baixa — renomeação direta |
| **Frontend Services** | ~5 arquivos | Baixa — atualizar imports |
| **Frontend Pages/Components/Hooks** | ~20 arquivos | Baixa — atualizar imports |
| **Migrations** | Manter nomes atuais | Nenhuma — não alterar |

**Total estimado:** ~96 arquivos afetados

---

## Ordem de Execução

### Fase 1: Backend Domain (base de tudo)
1. Renomear entidades de domínio (`Portfolio` → `Carteira`, `Asset` → `Ativo`, etc.)
2. Renomear enums (`AssetType` → `TipoAtivo`, `TransactionType` → `TipoTransacao`)
3. Renomear interfaces de repositório (`IPortfolioRepository` → `ICarteiraRepository`, etc.)
4. Renomear interfaces de serviço de domínio (`ITaxCalculationService` → `ICalculoImpostoService`, etc.)
5. Renomear classes de estratégia de simulação

### Fase 2: Backend DTOs e Application
1. Renomear todos os DTOs (`PortfolioDto` → `CarteiraDto`, etc.)
2. Criar DTOs faltantes (`AtualizarCarteiraDto`, `AtualizarTaxaEconomicaDto`)
3. Extrair DTOs inline do `InvestmentsController` para arquivos dedicados
4. Renomear interfaces de AppService (`IPortfolioAppService` → `ICarteiraAppService`, etc.)
5. Renomear implementações de AppService

### Fase 3: Backend Infrastructure
1. Renomear configurações EF Core (`PortfolioConfiguration` → `CarteiraConfiguration`, etc.)
2. Renomear implementações de repositório
3. Renomear workers e hubs
4. Atualizar `InvestDbContext` com novos `DbSet` names
5. Atualizar `UnitOfWork` se necessário

### Fase 4: Backend Controllers
1. Renomear controllers (`PortfoliosController` → `CarteirasController`, etc.)
2. Atualizar todas as referências de DTOs
3. Atualizar `Program.cs` para registrar serviços com novos nomes

### Fase 5: Backend Tests
1. Atualizar todos os testes de domínio
2. Atualizar testes de integração

### Fase 6: Frontend DTOs
1. Renomear todos os DTOs existentes
2. Criar `transacao.dto.ts` com tipagem completa
3. Criar `AtualizarTaxaEconomicaRequest`
4. Atualizar `index.ts` com exports renomeados

### Fase 7: Frontend Services
1. Atualizar imports em todos os services
2. Tipar transações (remover `any`)
3. Atualizar tipos de retorno

### Fase 8: Frontend Pages/Components/Hooks
1. Atualizar imports em todas as páginas
2. Atualizar imports em todos os componentes
3. Atualizar imports em todos os hooks
4. Atualizar mocks e handlers

---

## Trade-offs e Decisões

### O que MANTER em inglês:
- **Endpoints da API** (`/api/v1/portfolios`, `/api/v1/investments`) — URLs são contratos públicos, mudar quebra clientes existentes
- **DTOs genéricos de infraestrutura** (`ApiResponse`, `PaginatedResponse`, `BaseEntity`) — padrões técnicos universais
- **Patterns de infra** (`UnitOfWork`, `InvestDbContext`, `ExceptionHandlingMiddleware`) — nomes de padrões de arquitetura
- **Serviços de terceiros** (`SupabaseStorageService`) — nome próprio do serviço
- **Pastas de código** (`pages/portfolio/`, `services/portfolio.service.ts`) — convenção de organização

### O que MUDAR para português:
- **Todas as entidades de domínio** — refletem o negócio brasileiro
- **Todos os DTOs** — alinhados com o conceito de domínio
- **Todas as interfaces** — consistência com domínio
- **Todas as implementações** — consistência com interfaces
- **Configurações EF Core** — refletem as entidades
- **Variáveis e parâmetros** — legibilidade no contexto de negócio

---

## Verificação Pós-Implementação

Após a normalização, executar:

1. **Build do backend:** `dotnet build` — garantir que compila sem erros
2. **Tests do backend:** `dotnet test` — garantir que testes passam
3. **Build do frontend:** `npm run build` — garantir que compila sem erros
4. **Typecheck do frontend:** `npx tsc --noEmit` — garantir que tipos estão corretos
5. **Verificar imports:** buscar por nomes antigos no código para garantir que nada foi esquecido

---

## Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Migrations do EF Core quebrarem | Alto | Não renomear tabelas/colunas no banco, apenas no código C# |
| Referências perdidas em testes | Médio | Rodar testes após cada fase |
| Imports quebrados no frontend | Baixo | TypeScript compiler detecta imediatamente |
| Conflitos de merge | Médio | Fazer em branch dedicada, commits pequenos por fase |
