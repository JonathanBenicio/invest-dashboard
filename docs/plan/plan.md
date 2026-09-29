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

1. Validar autenticação Supabase e dados Brapi em um ambiente de homologação configurado; comprovar isolamento entre usuários, origem e data das cotações, além da indicação de indisponibilidade sem preços simulados.
2. [x] Integrar o carregamento de posições com a API de cotações: exibir origem e horário da observação Brapi/SignalR; se o provedor não retornar preço, conservar o último valor persistido e avisar que está indisponível. CI local/remota do commit `df2bae7` validou essa etapa.
3. [x] Cobrir filtros combinados de classe, subtipo, setor, status e busca, além de ordenação/paginação, com testes InMemory e PostgreSQL; CI do commit `a9ad5ac` passou.
4. [x] Cobrir o contrato de importação até o PostgreSQL: compra de ação e renda fixa, replay idempotente, rejeição de linha inválida e persistência após reinício do host.
5. [x] E2E Playwright sem MSW para importar CSV pela interface contra API/PostgreSQL reais; sessão de teste controlada e operações financeiras reais. Os quatro jobs passaram na CI de `9066682` (https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36544411654).
6. [x] Corrigir o lint padrão do frontend; `bun run lint` passa sem erros e mantém 10 avisos Fast Refresh em componentes existentes.
7. Preparar a homologação de frontend/API/banco após configurar credenciais Supabase/Brapi. Produção, fluxo de caixa e proventos continuam fora desta etapa.

### Próximos passos de acompanhamento

### Limpeza de gráficos de demonstração sem uso (concluída)

- [x] Confirmado por busca que `ChartJSExamples.tsx`, `LightningChartTrader.tsx` e `TraderChart.tsx` não estavam no roteador nem eram importados.
- [x] Removidos os três componentes: os dois primeiros geravam preços aleatórios e o terceiro exibia uma série OHLC fixa como gráfico trader.
- [x] Removidas as dependências `chart.js`, `chartjs-adapter-date-fns`, `chartjs-chart-financial`, `lightweight-charts` e `react-chartjs-2`, exclusivas desses arquivos.
- [x] `bun install --frozen-lockfile`, `bunx tsc --noEmit`, build e 5 E2E passaram. `Math.random` só permanece no dimensionamento visual do skeleton; dados de negócio continuam nos fixtures MSW opt-in.

**Aceite:** nenhuma série financeira de demonstração permanece em componente frontend órfão e a aplicação compila e passa os E2E atuais.
1. Validar parsing com extratos reais anonimizados de corretoras e documentar formatos aceitos; manter isso separado do contrato atual.
2. [x] Corrigir o lint padrão do frontend; `bun run lint` passa sem erros e mantém 10 avisos Fast Refresh em componentes existentes.
3. Preparar a homologação de frontend/API/banco após configurar credenciais Supabase/Brapi. Produção, fluxo de caixa e proventos continuam fora desta etapa.
4. Manter a coleta e visualização do Tesouro Transparente na fase futura descrita em `real-data-integration.md`.


### Correção do lint padrão do frontend (concluída)

- [x] Excluídos artefatos gerados (`android`, `build`, `dist` e `public/mockServiceWorker.js`) da análise ESLint.
- [x] Tipados os handlers, navegação e callbacks administrativos que usavam `any`; handlers MSW continuam opt-in e compatíveis com os DTOs.
- [x] Migrado o plugin `tailwindcss-animate` de `require()` para import ESM.
- [x] `bun run lint` conclui com 0 erros e 10 avisos de Fast Refresh; `bunx tsc --noEmit`, build e 5 E2E passam.

**Aceite:** lint padrão sem erros; avisos remanescentes não bloqueiam a compilação nem representam valores simulados de negócio.