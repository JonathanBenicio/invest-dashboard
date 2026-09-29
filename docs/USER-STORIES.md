# Catálogo de User Stories

## Escopo desta revisão

Revisado em 29/09/2026. O commit `605bbc5` integrou análise real e importação CSV, e sua CI passou em backend/PostgreSQL, frontend e Docker smoke. A execução local em Release passou em 23 testes unitários e 30 testes de integração PostgreSQL, sem skips, incluindo proteção de timestamp Brapi e isolamento de carteira/posição entre duas identidades. Os 5 E2E existentes usam MSW; um E2E sem MSW validou importação contra API/PostgreSQL, controlando apenas sessão. Os quatro jobs do commit `919e79a` passaram na CI (https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36546811301). Nova validação de Supabase, Brapi e terceiros foi adiada para a fase final a pedido do usuário. Lint sem erros, com 10 avisos Fast Refresh. Fluxo de caixa/proventos continuam adiados.

| ID estável | História | Estado observado no código | Evidência e gap principal |
|---|---|---|---|
| US-AUTH-001 | Autenticação segura | JWT/autorização cobertos; Supabase real pendente | JWT próprio curto e refresh rotativo; testes de API usam provedor fake. Teste InMemory/PostgreSQL com dois usuários prova que carteira/posições não atravessam tenants. Falta login/claims reais Supabase e validação de homologação, reservados à fase final de terceiros. |
| US-CONFIG-001 | Indicadores e taxas | Parcial; não validada | [TaxasController](../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs), [modelo de taxa](../src/InvestDashboard.Domain/Aggregates/MarketData/TaxaEconomica.cs), [tela de taxas](../frontend/src/pages/tools/Taxas.tsx). CRUD genérico existe; atualização automática e uso integrado em projeções/cálculos não foram comprovados. |
| US-INV-001 | Compra e venda | Implementada; fluxo validado em InMemory e PostgreSQL | Teste integrado cobre compra idempotente, venda parcial, excesso de unidades e historico incompleto. O mesmo fluxo passou contra PostgreSQL 15 local e na CI, incluindo persistência entre hosts de API. |
| US-INV-002 | Vencimentos | Parcial; a transição descrita não foi evidenciada | [RendaFixa](../src/InvestDashboard.Domain/Aggregates/MarketData/RendaFixa.cs) armazena data de vencimento e a UI mostra projeções. Não localizei transição automática de estado para “Vencido” nem liquidação do principal/juros. |
| US-TAX-001 | Apuração de lucro e prejuízo | Parcial; testes de domínio passaram | Custo médio e ganho realizado são calculados, mas critérios fiscais completos e testes de casos de borda permanecem pendentes. |
| US-TAX-002 | Imposto de Renda | Cálculos de domínio presentes; integração e regras pendentes | [CalculoImpostoService](../src/InvestDashboard.Domain/Services/CalculoImpostoService.cs) contém alíquotas/limiar codificados. Não localizei fluxo que aplique o serviço ao registrar venda nem testes específicos no catálogo de testes. As regras fiscais não foram verificadas como orientação vigente. |
| US-PORT-001 | Historico patrimonial | Implementado; qualidade temporal Brapi protegida | API/UI mostram origem e horário, preservam o valor salvo quando não há retorno e omitem períodos incompletos. O provider descarta cotações sem `regularMarketTime`; testes cobrem ações/cripto com e sem timestamp. Consulta pública PETR4 sem token retornou horário de mercado. Homologação de autenticação Supabase e isolamento multiusuário continua pendente. |
| US-PORT-002 | Visualização e filtragem | Implementada; filtros combinados cobertos em teste | Endpoint oferece tipo, subtipo, emissor, setor, status, busca e paginação. Teste cobre filtros combinados, ordenação e paginação em InMemory e PostgreSQL; CI de `a9ad5ac` passou. |
| US-SIM-001 | Simulação de investimentos | Fluxo de simulação presente; integração de taxa externa não comprovada | [SimulacaoController](../src/InvestDashboard.WebAPI/Controllers/SimulacaoController.cs), [estratégias](../src/InvestDashboard.Domain/Services/EstrategiaDeterministica.cs) e [tela](../frontend/src/pages/tools/Simulator.tsx). Recebe aportes mensais e taxa informada; ligação com SELIC configurada e validação não foram comprovadas. |
| US-COMP-001 | Análise de posições | Parcial; carteira, posições, cotações e histórico conectados | [Analysis.tsx](../frontend/src/pages/tools/Analysis.tsx) lê carteira, posições e histórico da API, combinando cotações atuais. Dias incompletos são sinalizados e omitidos. Benchmark CDI/Ibovespa aguarda fonte/contrato real. E2E com fixture passou. |
| US-IMPORT-001 | Importação CSV de operações | Implementada; E2E real e CI validados | [Import.tsx](../frontend/src/pages/tools/Import.tsx) lê e valida CSV, envia operações com idempotência e atualiza consultas. [E2E](../frontend/e2e-real-api/csv-import.spec.ts) percorre a UI sem MSW, confirma transações e posições no PostgreSQL; a sessão é controlada pelo teste. Os quatro jobs passaram na CI `9066682` (https://github.com/JonathanBenicio/invest-dashboard/actions/runs/36544411654). [Teste de contrato PostgreSQL](../src/tests/InvestDashboard.IntegrationTests/Controllers/PostgresCsvImportContractTests.cs) complementa replay, rejeição e persistência entre hosts. |

## US-AUTH-001 — Autenticação segura

**Como** investidor, **quero** realizar login seguro via e-mail/senha ou SSO, **para** acessar meus dados financeiros com privacidade.

**Gap:** Teste multiusuário confirma isolamento de carteira/posição em InMemory e PostgreSQL. Login real, refresh e claims Supabase seguem pendentes para a etapa final de integração com terceiros.

## US-CONFIG-001 — Indicadores e taxas

**Como** usuário, **quero** configurar e visualizar variáveis macroeconômicas e fiscais (SELIC, percentual de IR, corretagem e B3), **para** que sejam consideradas nas projeções e cálculos.

**Gap:** CRUD de taxas econômicas é genérico; catálogo/valores específicos e integração com simulação/cálculos permanecem por confirmar. Critérios e validação executada: pendentes.

## US-INV-001 — Compra e venda

**Como** investidor, **quero** registrar operações de compra ou venda de ativos informando ticker, data, preço e taxas, **para** atualizar a custódia da carteira.

**Gap:** O fluxo passou contra PostgreSQL 15 local e na CI, confirmando migrations e persistência. O job repete a cobertura nos PRs e pushes de `main`/`develop`; regras fiscais e vencimento automático continuam separados.

## US-INV-002 — Vencimentos

**Como** investidor, **quero** que o sistema identifique investimentos que chegaram à data de término e atualize seu status, **para** refletir vencimento e retorno na carteira.

**Gap:** data/projeção de vencimento aparece no domínio/UI; mudança automática de status e crédito de principal/juros não foram encontrados. O comportamento original permanece uma proposta, não capacidade confirmada.

## US-TAX-001 — Apuração de lucro e prejuízo

**Como** investidor, **quero** que o sistema calcule preço médio, lucro e prejuízo em operações de venda, **para** acompanhar a performance por ativo.

**Gap:** Os testes executam, mas ainda faltam critérios aprovados para múltiplas compras, taxas, perdas, vendas parciais e apuração fiscal.

## US-TAX-002 — Imposto de Renda

**Como** investidor, **quero** que o sistema calcule ou estime Imposto de Renda sobre resultados, **para** compreender o impacto fiscal no lucro líquido.

**Gap:** serviço de domínio isolado contém regras codificadas, mas sua integração com vendas não foi localizada. As regras precisam de critérios e fonte validados antes de serem requisito; este registro não é orientação fiscal.

## US-PORT-001 — Histórico patrimonial

**Como** investidor, **quero** visualizar evolução histórica do patrimônio e rentabilidade, **para** acompanhar a evolução da carteira.

**Gap:** a cotação Brapi e o timestamp têm teste de contrato; falta validar login/isolamento multiusuário com Supabase configurado e a operação sustentada em homologação.

## US-PORT-002 — Visualização e filtragem

**Como** investidor, **quero** visualizar e filtrar ativos por classe, status ou instituição, **para** gerenciar recortes da carteira.

**Gap:** O contrato inclui status e emissor além de tipo/subtipo/setor/busca. Combinação representativa, ordenação e paginação têm teste para InMemory e PostgreSQL; ambos passaram em CI. Cobertura de todas as combinações permanece pendente.

## US-SIM-001 — Simulação de investimentos

**Como** investidor, **quero** simular aportes futuros com valor, prazo e taxa, **para** projetar resultados antes de investir.

**Gap:** estratégias determinística e Monte Carlo recebem taxa como entrada; integração com SELIC/CDI configurados e execução validada não foram comprovadas.

## US-COMP-001 — Análise de posições

**Como** investidor, **quero** analisar valor, resultado, distribuição e histórico da carteira, **para** acompanhar minhas posições com dados atuais.

**Gap:** benchmark CDI/Ibovespa não é apresentado enquanto o backend não tiver fonte histórica verificável para esses índices. Dias incompletos são omitidos do gráfico e indicados na tela.

## US-IMPORT-001 — Importação CSV de operações

**Como** investidor, **quero** revisar um CSV e registrar suas linhas como operações, **para** atualizar as posições sem redigitar cada operação.

**Estado:** a implementação valida campos, números brasileiros, datas, compra/venda, compra de renda fixa, carteira de destino e idempotência por linha. Operações são enviadas ao endpoint real e as consultas são invalidadas após sucesso. O MSW usado no E2E da interface é uma fixture explícita. O teste PostgreSQL confirma o contrato HTTP, idempotência, rejeição de linha inválida e persistência do ledger entre hosts.

**Gap:** **Gap:** validar parsing com extratos reais anonimizados de corretoras. XLS/XLSX e conectores diretos de corretora não são suportados.

## Manutenção do catálogo

Não reutilize IDs. Histórias extensas podem ter arquivo próprio em `docs/user-stories/`, criado somente quando necessário e sempre ligado daqui. Ao confirmar ou mudar uma história, registre ator, benefício, critérios verificáveis, contratos, evidências e gaps conforme [templates/user-stories-template.md](../templates/user-stories-template.md). Atualize o estado apenas com evidência compatível.
