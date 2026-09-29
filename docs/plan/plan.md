# Plano de Implementação - Wealth Management Platform

Este documento detalha o plano de ação para implementar as funcionalidades de Gestão de Patrimônio, com foco nas regras fiscais brasileiras e simulações avançadas, integrando o Frontend (React) e o Backend (.NET).


> Atualizado em 29/09/2026: as fases abaixo registram o plano original. A entrega de acompanhamento de posições foi concluída; consulte o [catálogo de user stories](../USER-STORIES.md) e o [plano vigente de integração com dados reais](real-data-integration.md) para evidências e próximos passos. A coleta do Tesouro Transparente — incluindo visualizações informativas e comparação segura com posições compatíveis — está registrada como fase futura, ainda sem implementação.

---

## 🔍 Revisão do Estado Atual

### Frontend (React 19 / Vite)
- **Estrutura de Páginas:** Muito avançada. Já existem páginas para `Dashboard`, `Simulator` e `Taxas` (Taxas e Indicadores).
- **Estado Atual:** As páginas estão funcionais visualmente, mas utilizam dados mockados ou estado local (`useState`).
- **Oportunidade:** Conectar a página `Taxas` a uma API para persistir as configurações (SELIC, IPCA, etc.) e a página `Simulator` para usar o motor do backend (incluindo Monte Carlo).

### Backend (.NET 10 / DDD)
- **Domínio:** Já possui entidades como `Portfolio`, `AssetPosition` e `Transaction` bem estruturadas.
- **Controladores:** `InvestmentsController`, `PortfoliosController` e `TransactionController` já existem.
- **Lacunas:** 
  - Falta implementar as regras fiscais brasileiras (Isenção de 20k, FIIs) no domínio.
  - Falta implementar o `InvestmentSimulatorService` com suporte a Monte Carlo.
  - Falta expor APIs para gerenciamento de taxas econômicas e simulações.

---

## 🗺️ Plano de Ação

### Fase 0: Login / Cadastro (Supabase)
Foco em garantir a segurança e multi-tenancy.

1. **Configurar Supabase Auth no Backend**
   - Integrar o middleware de validação de JWT do Supabase.
   - Garantir que as rotas de API extraiam o `UserId` do token.

2. **Ajustar Fluxo de Autenticação no Frontend**
   - Garantir que as páginas de login/cadastro usem o cliente do Supabase.
   - Redirecionar para o Dashboard após o login.

### Fase 1: Expansão do Domínio e Serviços (Backend)
Foco em implementar a inteligência financeira no C#.

1. **Implementar `TaxCalculationService`**
   - Criar o serviço de domínio para cálculo de IR.
   - Implementar regra de isenção de R$ 20.000 para Swing Trade de Ações.
   - Implementar alíquota de 20% para FIIs.
   - Adicionar suporte a compensação de prejuízos.

2. **Implementar `InvestmentSimulatorService`**
   - Criar interface `ISimulationStrategy`.
   - Implementar `DeterministicStrategy` (Juros Compostos simples).
   - Implementar `MonteCarloStrategy` (Simulação probabilística com volatilidade).

3. **Criar Entidade `EconomicRate`**
   - Mapear taxas como SELIC, IPCA, CDI para serem persistidas e usadas nas simulações.

### Fase 2: APIs e Integração (Backend -> WebAPI)
Foco em expor as novas funcionalidades.

1. **Criar `TaxesController`**
   - CRUD para taxas econômicas (permitindo que o usuário configure manualmente por enquanto).
2. **Criar `SimulationController`**
   - Endpoint que recebe os parâmetros e retorna a série temporal da simulação (com suporte a escolher a estratégia).

### Fase 3: Conexão e Refatoração (Frontend)
Foco em remover os mocks e usar o Backend.

1. **Refatorar `Taxas.tsx`**
   - Substituir o uso de `mock-data` por chamadas à API do `TaxesController`.
2. **Refatorar `Simulator.tsx`**
   - Adicionar opção na UI para escolher entre "Matemático (Determínistico)" e "Estatístico (Monte Carlo)".
   - Chamar a API de simulação do backend para plotar os gráficos de área.

---

## Próximos passos — estado em 29/09/2026

O plano vigente para remover mocks de produto e fechar a integração frontend/backend está em [real-data-integration.md](real-data-integration.md). A colisão de migrations foi corrigida em `aa31fc9`. A análise/importação e substituição dos mocks de produto foram integradas em `605bbc5`; backend/PostgreSQL, frontend e Docker smoke passaram na CI.

- [x] Renomear o helper local e confirmar que os nomes legados não aparecem em `src/` e `frontend/src/`.
- [x] Executar o fluxo de ledger contra PostgreSQL 15 em Compose efêmero: migrations, compra idempotente, venda parcial, sobre-venda, histórico incompleto e persistência entre hosts.
- [x] Validar Compose: `/health/ready` retornou 200 e o proxy frontend → API retornou 401 sem token, conforme esperado; containers e volume temporários removidos.
- [x] Atualizar `docs/USER-STORIES.md` com evidências e gaps atuais.
- [x] Fechar a issue #42 no GitHub usando o PR #43 merged como evidência (concluída em 29/09/2026).
- [x] Sincronizar `develop` e confirmar a [CI do commit dc82a07](https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36534836526): backend/PostgreSQL, frontend e smoke Compose passaram.
- [x] Substituir mocks das telas de análise/importação por API e registrar a coleta futura do Tesouro Transparente; CI de `605bbc5` passou nos três jobs.
- [x] Revisar os PRs #14 e #4 separadamente e encerrá-los sem merge em 29/09/2026, conforme decisão do usuário de manter `develop` como linha de trabalho.

Fluxo de caixa e proventos seguem adiados. Nenhum deploy de produção faz parte desta etapa.

### Revisão dos PRs antigos

- **#4 (`develop_jules` → `develop`): não integrar o diff atual.** As cinco alterações usam caminhos anteriores à separação do monorepo, `package-lock.json`, seleção de carteiras mockadas e projeções com CDI/IPCA fixos. Seleção de carteira e compra/venda já estão presentes nas telas atuais com contratos tipados e API. Eventuais projeções devem usar indicadores configurados e ter critérios próprios.
- **#14 (`develop` → `develop_jules`): encerrado sem merge.** É uma sincronização inversa, com 327 arquivos e conflitos, incluindo configuração local, log e artefatos de build. O usuário confirmou o encerramento dos dois PRs antigos e `develop` como linha de trabalho. As branches foram preservadas.
- Snapshot histórico da revisão dos PRs #4/#14: à época passaram 21 testes unitários e 24 de integração localmente e 21 unitários/25 integrações na CI; os resultados atuais e a contagem de lint estão registrados em `docs/USER-STORIES.md`.

### Próxima etapa de acompanhamento de posições

1. Adiada para a fase final por orientação do usuário: validar autenticação Supabase e comunicação sustentada com Brapi/outros serviços externos. A consulta pública Brapi e os contratos locais já foram verificados; não iniciar nova comunicação externa antes de concluir os demais testes locais.
2. [x] Integrar o carregamento de posições com a API de cotações: exibir origem e horário da observação Brapi/SignalR; se o provedor não retornar preço, conservar o último valor persistido e avisar que está indisponível. CI local/remota do commit `df2bae7` validou essa etapa.
3. [x] Cobrir filtros combinados de classe, subtipo, setor, status e busca, além de ordenação/paginação, com testes InMemory e PostgreSQL; CI do commit `a9ad5ac` passou.
4. [x] Cobrir o contrato de importação até o PostgreSQL: compra de ação e renda fixa, replay idempotente, rejeição de linha inválida e persistência após reinício do host.
5. [x] E2E Playwright sem MSW para importar CSV pela interface contra API/PostgreSQL reais; sessão de teste controlada e operações financeiras reais. Os quatro jobs passaram na CI de `9066682` (https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36544411654).
6. [x] Corrigir o lint padrão do frontend; `bun run lint` passa sem erros e mantém 10 avisos Fast Refresh em componentes existentes.
7. Na fase final, validar homologação de autenticação Supabase e comunicação/limites de Brapi e demais serviços terceiros; só então avaliar deploy. Fluxo de caixa e proventos seguem fora desta etapa.

### Próximos passos de acompanhamento

### Limpeza de gráficos de demonstração sem uso (concluída)

- [x] Confirmado por busca que `ChartJSExamples.tsx`, `LightningChartTrader.tsx` e `TraderChart.tsx` não estavam no roteador nem eram importados.
- [x] Removidos os três componentes: os dois primeiros geravam preços aleatórios e o terceiro exibia uma série OHLC fixa como gráfico trader.
- [x] Removidas as dependências `chart.js`, `chartjs-adapter-date-fns`, `chartjs-chart-financial`, `lightweight-charts` e `react-chartjs-2`, exclusivas desses arquivos.
- [x] `bun install --frozen-lockfile`, `bunx tsc --noEmit`, build e 5 E2E passaram. `Math.random` só permanece no dimensionamento visual do skeleton; dados de negócio continuam nos fixtures MSW opt-in.

**Aceite:** nenhuma série financeira de demonstração permanece em componente frontend órfão e a aplicação compila e passa os E2E atuais.
1. Validar parsing com extratos reais anonimizados de corretoras e documentar formatos aceitos; manter isso separado do contrato atual.
2. [x] Corrigir o lint padrão do frontend; `bun run lint` passa sem erros e mantém 10 avisos Fast Refresh em componentes existentes.
3. Fase final: após concluir as tarefas locais, validar autenticação Supabase e comunicação com Brapi e demais serviços externos; deploy depende dessa homologação.
4. Manter a coleta e visualização do Tesouro Transparente na fase futura descrita em `real-data-integration.md`.


### Correção do lint padrão do frontend (concluída)

- [x] Excluídos artefatos gerados (`android`, `build`, `dist` e `public/mockServiceWorker.js`) da análise ESLint.
- [x] Tipados os handlers, navegação e callbacks administrativos que usavam `any`; handlers MSW continuam opt-in e compatíveis com os DTOs.
- [x] Migrado o plugin `tailwindcss-animate` de `require()` para import ESM.
- [x] `bun run lint` conclui com 0 erros e 10 avisos de Fast Refresh; `bunx tsc --noEmit`, build e 5 E2E passam. Os quatro jobs da CI do commit `919e79a` também passaram (https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36546811301).

**Aceite:** lint padrão sem erros; avisos remanescentes não bloqueiam a compilação nem representam valores simulados de negócio.
### Integridade do horário de observação das cotações Brapi (concluída)

A documentação do [schema oficial Brapi](https://brapi.dev/docs/openapi) define `requestedAt` como horário da requisição e `regularMarketTime` como horário de mercado do ativo. Não usar o horário da requisição nem o relógio local como substituto de observação.

1. [x] Client descarta preços sem `regularMarketTime` parseável; o endpoint não promove horário de consulta a cotação recente.
2. [x] Testes HTTP simulados cobrem ações e cripto: timestamp válido usa o horário de mercado e timestamp ausente resulta em lista vazia mesmo com `requestedAt`.
3. [x] Suíte .NET Release atual: 23 testes unitários e 32 integrações PostgreSQL, sem skips; o fluxo conserva preço e informa indisponibilidade sem cotação válida.

**Aceite:** nenhum preço sem horário de observação válido é apresentado como cotação atual. A validação Supabase ponta a ponta permanece pendente.
### Cobertura de isolamento multiusuário (concluída)

- [x] Adicionadas ao ambiente de teste uma segunda identidade e sessão JWT independentes.
- [x] Cenário executado em InMemory e PostgreSQL isolado: usuário B não lista/lê carteira, resumo, histórico, posições ou transações de A; tentativa de registrar, reavaliar ou excluir dados alheios retorna NotFound e não altera os dados do proprietário.
- [x] Suíte .NET Release na validação multiusuário: 23 unitários e 30 integrações PostgreSQL, sem skips; testes provam que a identidade B não lê nem altera carteira/posição da identidade A.

**Aceite:** isolamento de tenant provado com tokens de teste; autenticação e claims reais Supabase permanecem para a fase final de serviços terceiros.
### Avaliação de renda fixa por extrato (concluída)

- [x] Compra com `statementValue` e avaliação posterior usam os endpoints reais da API.
- [x] A posição atual reflete o novo valor; histórico contém preço/fonte `statement` e nenhuma transação extra é criada.
- [x] InMemory e PostgreSQL (incluindo recriação do host) passaram; suíte .NET Release: 23 unitários e 32 integrações PostgreSQL, sem skips.

**Aceite:** valor de extrato informado pelo usuário atualiza a posição e persiste no histórico, sem serviço externo nem preço simulado.
### E2E da avaliação manual pela UI (concluída)

- [x] Usado o harness de API/PostgreSQL real e sessão JWT controlada, sem MSW nem chamadas externas.
- [x] E2E cria carteira/contrato, altera pela tela de Renda Fixa o valor e data do extrato e valida a confirmação visual.
- [x] Confirma pela API que valor atual, data e histórico `statement` foram persistidos e que permanece somente a transação original.
- [x] E2E de importação CSV e avaliação manual passaram juntos (2 testes); avaliação foi repetida após incluir verificação da data. Typecheck/lint direcionado e 5 E2E MSW passaram.

**Aceite:** caminho da tela de Renda Fixa ao endpoint de avaliação grava valor/histórico no PostgreSQL sem adicionar transação e sem depender de terceiros.