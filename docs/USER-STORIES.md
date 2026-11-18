# Catálogo de User Stories

## Escopo desta revisão

Revisado em 29/09/2026. Contratos próprios padronizados em `/api/v1`, conforme decisão do usuário. Estado e evidência pós-implementação estão no [roadmap de gaps funcionais](plan/functional-gaps.md) e no [plano ativo de fechamento](plan/complete-active-plan.md). Fluxo de caixa/proventos estão no [plano futuro de baixa prioridade](plan/future-low-priority.md). Validação atual: 33 testes unitários, 41 integrações PostgreSQL, 8 E2E MSW, build backend/frontend, zero advisories no `npm audit` e smoke Compose 200. Supabase real ainda precisa de conta local de homologação; Ibovespa foi movido para funcionalidade futura por decisão do usuário; importação específica de corretoras aguarda amostras conforme [plano de CSV](plan/broker-csv-import-plan.md).

| ID estável | História | Estado observado no código | Evidência e gap principal |
|---|---|---|---|
| US-AUTH-001 | Autenticação segura | JWT/autorização cobertos; Supabase real pendente | JWT curto e refresh rotativo; teste PostgreSQL/InMemory com dois usuários prova isolamento de carteira e posição, inclusive leitura e tentativas de mutação. Login/claims reais Supabase ficam para a fase final de terceiros. |
| US-CONFIG-001 | Indicadores e taxas | CRUD manual integrado; atualização automática futura | [TaxasController](../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs), [modelo de taxa](../src/InvestDashboard.Domain/Aggregates/MarketData/TaxaEconomica.cs), [tela de taxas](../frontend/src/pages/tools/Taxas.tsx). Teste de integração cobre lista inicialmente vazia, criação, leitura/edição dos campos, variação, data da atualização e exclusão. Taxas persistidas ainda não alimentam automaticamente projeções/cálculos; atualização macroeconômica depende da etapa final de terceiros. |
| US-INV-001 | Compra e venda | Implementada; fluxo validado em InMemory e PostgreSQL | Teste integrado cobre compra idempotente, venda parcial, excesso de unidades e historico incompleto. O mesmo fluxo passou contra PostgreSQL 15 local e na CI, incluindo persistência entre hosts de API. |
| US-INV-002 | Vencimentos | Implementada; estado vencido sem liquidação de caixa | Worker horário em `America/Sao_Paulo` persiste status idempotente; UI mostra “Vencido” e preserva quantidade/valor/ledger. Testes de domínio e PostgreSQL passaram. |
| US-TAX-001 | Apuração de lucro e prejuízo | Parcial; testes de domínio passaram | Custo médio e ganho realizado são calculados, mas critérios fiscais completos e testes de casos de borda permanecem pendentes. |
| US-TAX-002 | Imposto de Renda | Estimativa mensal versionada implementada em escopo limitado | Endpoint `GET /api/v1/taxes/estimativa-mensal` e análise exibem estimativa, fonte, versão e limitações. Vendas recentes podem informar modalidade; dados legados/sem modalidade são excluídos e marcados incompletos. Revisão profissional antes de release. |
| US-PORT-001 | Histórico patrimonial | Posições, cotações e avaliações manuais com histórico | API/UI exibem origem/horário, preservam o último valor com aviso e omitem histórico incompleto. Teste live do `BrapiMarketDataClient` cobriu cotação, histórico e busca reais; limites/token da conta contratada seguem pendentes. Avaliação manual persiste no PostgreSQL; autenticação Supabase real não tem conta de homologação. |
| US-PORT-002 | Visualização e filtragem | Implementada; filtros combinados cobertos em teste | Endpoint oferece tipo, subtipo, emissor, setor, status, busca e paginação. Teste cobre filtros combinados, ordenação e paginação em InMemory e PostgreSQL; CI de `a9ad5ac` passou. |
| US-PORT-003 | Avaliação atual de posições do Tesouro Direto | Proposta; rascunho parcial em branch separada | [Issue #64](https://github.com/JonathanBenicio/invest-dashboard/issues/64), [plano](plan/avaliacao-liquida-tesouro-direto.md) e [ADR-0003 proposta](adrs/0003-tesouro-market-valuation.md). PU base × quantidade mostra valor bruto de referência; tributos/taxas do extrato permanecem datados. Líquido não validado. |
| US-SIM-001 | Simulação de investimentos | API/UI integradas; taxa informada manualmente | [SimulacaoController](../src/InvestDashboard.WebAPI/Controllers/SimulacaoController.cs) recebe contrato PT-BR (`valorInicial`, `aporteMensal`, `taxaJurosAnual`) para estratégias determinística e Monte Carlo; frontend carrega estratégias da API (`nome/descricao`), exibe resultado e estado de erro. Seleção automática de taxa configurada ainda não integrada. |
| US-COMP-001 | Análise de posições | Carteira/posições e CDI conectados | [Analysis.tsx](../frontend/src/pages/tools/Analysis.tsx) lê carteira, posições, histórico e CDI diário do BCB SGS 12; normaliza ambas as séries em datas completas comuns e mostra origem/atualização. Ibovespa B3 foi movido a funcionalidade futura por decisão do usuário. |
| US-COMP-002 | Comparação histórica da posição com CDI e outros ativos | Proposta; metodologia não aprovada | [Issue #65](https://github.com/JonathanBenicio/invest-dashboard/issues/65) e [plano](plan/comparacao-historica-tesouro-cdi.md). Método depende de definir fluxos, cupons, custos, período comum e lacunas. |
| US-IMPORT-001 | Importação CSV de operações | Importador genérico implementado; layouts reais aguardam amostras | [Import.tsx](../frontend/src/pages/tools/Import.tsx) lê e valida CSV, envia operações com idempotência e atualiza consultas. [E2E](../frontend/e2e-real-api/csv-import.spec.ts) percorre a UI sem MSW e confirma transações/posições no PostgreSQL. [Plano de análise de CSVs de corretoras](plan/broker-csv-import-plan.md) define o procedimento por layout; faltam amostras reais anonimizadas. |

## US-AUTH-001 — Autenticação segura

**Como** investidor, **quero** realizar login seguro via e-mail/senha ou SSO, **para** acessar meus dados financeiros com privacidade.

**Gap:** Teste multiusuário confirma isolamento de carteira/posição em InMemory e PostgreSQL. O teste Supabase live opt-in está pronto, mas aguarda conta confirmada. Instruções e campos locais: [homologação Supabase](plan/supabase-auth-homologation.md).

## US-CONFIG-001 — Indicadores e taxas

**Como** usuário, **quero** configurar e visualizar variáveis macroeconômicas e fiscais (SELIC, percentual de IR, corretagem e B3), **para** que sejam consideradas nas projeções e cálculos.

**Gap:** CRUD de taxas econômicas é genérico; catálogo/valores específicos e integração com simulação/cálculos permanecem por confirmar. Critérios e validação executada: pendentes.

## US-INV-001 — Compra e venda

**Como** investidor, **quero** registrar operações de compra ou venda de ativos informando ticker, data, preço e taxas, **para** atualizar a custódia da carteira.

**Gap:** O fluxo passou contra PostgreSQL 15 local e na CI, confirmando migrations e persistência. O job repete a cobertura nos PRs e pushes de `main`/`develop`; regras fiscais e vencimento automático continuam separados.

## US-INV-002 — Vencimentos

**Como** investidor, **quero** que o sistema identifique investimentos que chegaram à data de término e atualize seu status, **para** refletir vencimento e retorno na carteira.

**Estado:** concluído para escopo aprovado. Worker marca automaticamente “Vencido” por data de São Paulo, idempotente, mantendo valor e ledger; não lança crédito em caixa. Teste PostgreSQL verifica persistência entre hosts.

## US-TAX-001 — Apuração de lucro e prejuízo

**Como** investidor, **quero** que o sistema calcule preço médio, lucro e prejuízo em operações de venda, **para** acompanhar a performance por ativo.

**Gap:** Os testes executam, mas ainda faltam critérios aprovados para múltiplas compras, taxas, perdas, vendas parciais e apuração fiscal.

## US-TAX-002 — Imposto de Renda

**Como** investidor, **quero** que o sistema calcule ou estime Imposto de Renda sobre resultados, **para** compreender o impacto fiscal no lucro líquido.

**Estado:** primeira estimativa mensal versionada está integrada às vendas e à análise. A regra usa classes/modalidades identificadas, separa perdas por categoria, exibe origem/premissas/exclusões e indica dados incompletos. Revisão fiscal humana ainda é requisito pré-release; não é orientação fiscal.

## US-PORT-001 — Histórico patrimonial

**Como** investidor, **quero** visualizar evolução histórica do patrimônio e rentabilidade, **para** acompanhar a evolução da carteira.

**Gap:** a cotação Brapi e o timestamp têm teste de contrato; falta validar login/isolamento multiusuário com Supabase configurado e a operação sustentada em homologação.

## US-PORT-002 — Visualização e filtragem

**Como** investidor, **quero** visualizar e filtrar ativos por classe, status ou instituição, **para** gerenciar recortes da carteira.

**Gap:** O contrato inclui status e emissor além de tipo/subtipo/setor/busca. Combinação representativa, ordenação e paginação têm teste para InMemory e PostgreSQL; ambos passaram em CI. Cobertura de todas as combinações permanece pendente.

## US-PORT-003 — Avaliação atual de posições do Tesouro Direto

- Estado: proposta; rascunho local de consulta por posição em branch separada, sem commit ou validação.
- Issue: [#64](https://github.com/JonathanBenicio/invest-dashboard/issues/64); iniciativa relacionada: [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: [0003 — cotação pública na avaliação de posições do Tesouro Direto](adrs/0003-tesouro-market-valuation.md), proposta.
- Plano: [avaliação atual de posições do Tesouro Direto](plan/avaliacao-liquida-tesouro-direto.md).

**Como** investidor, **quero** ver o valor bruto da posição do Tesouro Direto calculado com cotação pública e os dados do extrato identificados pela sua data, **para** acompanhar o mercado sem confundir observação histórica com líquido atual.

### Contexto e limites

O usuário aceitou o PU base público × quantidade como referência de valor bruto e os impostos/taxas do HTML como observações datadas. O campo `aliquotaIr` do extrato pode definir a faixa na data do documento; se já estiver em 15%, não é necessário recalcular o prazo desde a compra para escolher essa faixa. Para faixas maiores, a idade pode avançar por dias corridos entre a data do extrato e a cotação. O líquido deve ficar indisponível quando dados atuais de custos, lotes ou eventos forem insuficientes. A hipótese “dias úteis × 22%” não foi aprovada como fórmula; revalidar regra fiscal em fontes oficiais. Cotação pública não garante preço efetivo de venda.

### Critérios de aceite verificáveis

- [ ] Dado título/vencimento associado e posição atual, quando houver cotação oficial, então PU base, quantidade e valor bruto são exibidos com fonte/data sem exigir novo extrato. Evidência: testes API/interface.
- [ ] Dado extrato confirmado, quando impostos e taxas forem exibidos, então seus valores preservam a data de referência e não são rotulados como encargos atuais. Evidência: contrato e revisão da interface.
- [ ] Dado `aliquotaIr` e `diasCorridos` no extrato, quando a cotação for posterior, então 15% permanece como piso e as demais faixas avançam por dias corridos, sem usar dias úteis como prazo fiscal. Evidência: testes de transição por limite.
- [ ] Dado custo ausente, cupom não reconciliado, operação posterior ou quantidade divergente, quando o líquido for solicitado, então fica indisponível com motivo e sem reutilizar taxa antiga como atual. Evidência: testes por cenário.
- [ ] Dada uma compra/venda, quando a posição for consultada, então a quantidade atual é combinada à cotação sem criar transação nem alterar custo de aquisição. Evidência: teste de atualização e ledger.
- [ ] Dado usuário sem acesso à carteira, quando consultar a posição, então nenhum dado pessoal da posição é revelado. Evidência: teste multiusuário.

### Contratos, evidências e gaps

- Ator/autorização: investidor autenticado; consulta limitada a carteiras acessíveis.
- Contrato de API: pendente de validação; proposta registrada em `docs/API-CONTRACTS-PT-BR.md` após implementação.
- Persistência: ADR-0003 propõe manter ledger e dados do extrato separados da cotação. Histórico público e comparação com CDI ficam em plano futuro.
- Compatibilidade: CSV de operações permanece inalterado; XLSX foi adiado.
- Evidências/gaps: revisão somente leitura encontrou risco de cupom e taxa histórica; ambos bloqueiam líquido atual. Frontend ajustou data `DateOnly` e invalidação após movimento. Build backend passou em contêiner .NET 10.0.401. O teste PostgreSQL da criação inicial HTML passou, mas não cobre o novo endpoint de avaliação; validação frontend e da cotação permanecem pendentes.

## US-SIM-001 — Simulação de investimentos

**Como** investidor, **quero** simular aportes futuros com valor, prazo e taxa, **para** projetar resultados antes de investir.

**Estado:** contrato JSON PT-BR da API (`valorInicial`, `aporteMensal`, `anos`, `taxaJurosAnual`, `pontos`, `valorFinal`) é alinhado aos DTOs TS; a tela carrega estratégias (`id`, `nome`, `descricao`) via `/simulation/strategies` e apresenta carregamento, erro, resultado, juros e evolução.

**Gap:** a taxa anual segue informada manualmente. Conectar taxas econômicas persistidas à simulação requer escolher indicador/unidade e é uma etapa futura; nenhuma taxa é inventada.

## US-COMP-001 — Análise de posições

**Como** investidor, **quero** analisar valor, resultado, distribuição e histórico da carteira, **para** acompanhar minhas posições com dados atuais.

**Estado:** CDI diário oficial do BCB é apresentado em comparação normalizada com a carteira usando dias completos comuns. Ibovespa B3 é uma funcionalidade futura; dias sem dados não são interpolados.

## US-COMP-002 — Comparação histórica da posição com CDI e outros ativos

- Estado: proposta; nenhuma metodologia ou implementação aprovada.
- Issue: [#65](https://github.com/JonathanBenicio/invest-dashboard/issues/65); iniciativa relacionada: [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: pendente até escolher o método de retorno e o tratamento dos fluxos.
- Plano: [comparação histórica de posições com CDI e outros ativos](plan/comparacao-historica-tesouro-cdi.md).

**Como** investidor, **quero** comparar a evolução histórica de uma posição com CDI e outros ativos, **para** analisar sua performance usando critérios comparáveis.

### Contexto e limites

A análise atual normaliza valores e alinha observações comuns, mas isso não demonstra retorno ajustado por aportes, retiradas, vendas ou cupons. O método entre retorno ponderado pelo tempo e retorno ponderado por fluxo de caixa está pendente. Não apresentar série pessoal completa quando posição/ledger não cobrir o período. XLSX foi adiado e permanece excluído.

### Critérios de aceite verificáveis

- [ ] Dadas séries de posição e CDI, quando exibidas juntas, então período, base, fonte, frequência e método de retorno são informados. Evidência: contrato e revisão da interface.
- [ ] Dados aportes, retiradas, vendas parciais e cupons, quando incluídos, então o método escolhido ajusta esses fluxos ou declara explicitamente sua exclusão. Evidência: casos sintéticos.
- [ ] Dado período sem cotação/indicador, quando a série for exibida, então o intervalo permanece ausente e é identificado; nenhum ponto é interpolado sem regra aprovada. Evidência: teste de série incompleta.
- [ ] Dada comparação bruta ou líquida, quando resultados forem mostrados, então custos/impostos são tratados de forma coerente e ficam explícitos. Evidência: exemplo reproduzível.
- [ ] XLSX não é importado nem exigido por esta história. Evidência: escopo e plano.

### Contratos, evidências e gaps

- Ator: usuário autenticado com acesso à carteira.
- Fontes candidatas: CDI SGS 12 e histórico CSV do Tesouro Transparente; demais ativos dependem de cobertura e ajustes adequados.
- Persistência e contratos: pendentes da decisão metodológica; nenhuma série histórica pessoal nova será presumida.
- Validação: não executada; apenas a comparação atual normalizada com CDI está implementada, sem comprovação de ajuste a fluxos.

## US-IMPORT-001 — Importação CSV de operações

**Como** investidor, **quero** revisar um CSV e registrar suas linhas como operações, **para** atualizar as posições sem redigitar cada operação.

**Estado:** a implementação valida campos, números brasileiros, datas, compra/venda, compra de renda fixa, carteira de destino e idempotência por linha. Operações são enviadas ao endpoint real e as consultas são invalidadas após sucesso. O MSW usado no E2E da interface é uma fixture explícita. O teste PostgreSQL confirma o contrato HTTP, idempotência, rejeição de linha inválida e persistência do ledger entre hosts.

**Gap:** validar parsing com extratos reais anonimizados de corretoras. O procedimento está em [broker-csv-import-plan.md](plan/broker-csv-import-plan.md). XLS/XLSX e conectores diretos de corretora não são suportados.

## Manutenção do catálogo

Não reutilize IDs. Histórias extensas podem ter arquivo próprio em `docs/user-stories/`, criado somente quando necessário e sempre ligado daqui. Ao confirmar ou mudar uma história, registre ator, benefício, critérios verificáveis, contratos, evidências e gaps conforme [templates/user-stories-template.md](../templates/user-stories-template.md). Atualize o estado apenas com evidência compatível.
