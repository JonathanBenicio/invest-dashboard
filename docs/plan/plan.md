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
- A validação local atual confirmou 21 testes unitários e 24 de integração; o teste PostgreSQL foi pulado sem `INVEST_TEST_POSTGRES_CONNECTION`. Na CI, passaram 21 testes unitários e 25 de integração, sem testes pulados, incluindo o fluxo PostgreSQL. Frontend: typecheck, build e 2 E2E passaram. O smoke Compose confirmou readiness e o proxy com 401 sem token. O checklist Python não pôde iniciar localmente porque o interpretador não está disponível; o lint global ainda tem pendências.

### Próxima etapa de acompanhamento de posições

1. Validar autenticação Supabase e dados Brapi em um ambiente de homologação configurado; comprovar isolamento entre usuários, origem e data das cotações, além da indicação de indisponibilidade sem preços simulados.
2. [x] Integrar o carregamento de posições com a API de cotações: exibir origem e horário da observação Brapi/SignalR; se o provedor não retornar preço, conservar o último valor persistido e avisar que está indisponível. CI local/remota do commit `df2bae7` validou essa etapa.
3. [x] Cobrir filtros combinados de classe, subtipo, setor, status e busca, além de ordenação/paginação, com testes InMemory e PostgreSQL; CI do commit `a9ad5ac` passou.
4. [x] Cobrir o contrato de importação até o PostgreSQL: compra de ação e renda fixa, replay idempotente, rejeição de linha inválida e persistência após reinício do host.
5. [x] E2E Playwright sem MSW para importar CSV pela interface contra API/PostgreSQL reais em ambiente de teste; sessão de teste controlada, operações financeiras reais. Job dedicado adicionado à CI; execução remota pendente.
6. Tratar o lint global em alteração própria, preservando o foco desta entrega e registrando os checks que ainda falham.
7. Preparar a homologação de frontend/API/banco após configurar credenciais Supabase/Brapi. Produção, fluxo de caixa e proventos continuam fora desta etapa.

### Plano para a próxima alteração de código

1. Executar a CI remota do job `csv-import-e2e` ao enviar as alterações e corrigir qualquer diferença de ambiente Linux.
2. Confirmar que o job encerra a API e o PostgreSQL descartável em sucesso e falha.
3. Validar parsing com extratos reais anonimizados de corretoras e documentar formatos aceitos; manter isso separado do contrato atual.
4. Não implementar nesta alteração coleta Tesouro, caixa, proventos ou produção.