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
| US-PORT-003 | [Consolidar investimentos por grupo](#us-port-003--consolidar-investimentos-por-grupo) | Implementada e validada em testes focados | Issue [#70](https://github.com/JonathanBenicio/invest-dashboard/issues/70), [Plano](plan/investimentos-consolidados-por-grupo.md) e [ADR-0005](adrs/0005-consulta-consolidada-investimentos.md). Conferência autenticada pendente. |
| US-SIM-001 | Simulação de investimentos | API/UI integradas; taxa informada manualmente | [SimulacaoController](../src/InvestDashboard.WebAPI/Controllers/SimulacaoController.cs) recebe contrato PT-BR (`valorInicial`, `aporteMensal`, `taxaJurosAnual`) para estratégias determinística e Monte Carlo; frontend carrega estratégias da API (`nome/descricao`), exibe resultado e estado de erro. Seleção automática de taxa configurada ainda não integrada. |
| US-COMP-001 | Análise de posições | Carteira/posições e CDI conectados | [Analysis.tsx](../frontend/src/pages/tools/Analysis.tsx) lê carteira, posições, histórico e CDI diário do BCB SGS 12; normaliza ambas as séries em datas completas comuns e mostra origem/atualização. Ibovespa B3 foi movido a funcionalidade futura por decisão do usuário. |
| US-IMPORT-001 | Importação CSV de operações | Importador genérico implementado; layouts reais aguardam amostras | [Import.tsx](../frontend/src/pages/tools/Import.tsx) lê e valida CSV, envia operações com idempotência e atualiza consultas. [E2E](../frontend/e2e-real-api/csv-import.spec.ts) percorre a UI sem MSW e confirma transações/posições no PostgreSQL. [Plano de análise de CSVs de corretoras](plan/broker-csv-import-plan.md) define o procedimento por layout; faltam amostras reais anonimizadas. |
| US-IMPORT-002 | Reimportação auditável de snapshots HTML | Implementada; validações focadas passaram, com conferência manual pendente | Issues [#55](https://github.com/JonathanBenicio/invest-dashboard/issues/55), [#57](https://github.com/JonathanBenicio/invest-dashboard/issues/57), [#58](https://github.com/JonathanBenicio/invest-dashboard/issues/58), [ADR-0004](adrs/0004-tesouro-reimportacao-snapshots.md) e [plano](plan/tesouro-reimportacao-snapshots.md). |
| US-IA-001 | Consultar carteira e retomar a conversa | Proposta | [Plano](plan/ia-renda-fixa/06-conversas-streaming.md) |
| US-IA-002 | Administrar provedores de IA | Proposta | [Plano](plan/ia-renda-fixa/03-politica-provedores.md) |
| US-IA-003 | Consultar dados permitidos de um grupo | Proposta | [Plano](plan/ia-renda-fixa/09-assistente-grupos.md) |
| US-IA-004 | Revisar um print e confirmar saldo observado | Proposta | [Plano](plan/ia-renda-fixa/05-revisao-saldo.md) |
| US-IA-005 | Distinguir saldo de estimativa em renda fixa | Proposta | [Plano](plan/ia-renda-fixa/12-projecoes-renda-fixa.md) |


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

## US-SIM-001 — Simulação de investimentos

**Como** investidor, **quero** simular aportes futuros com valor, prazo e taxa, **para** projetar resultados antes de investir.

**Estado:** contrato JSON PT-BR da API (`valorInicial`, `aporteMensal`, `anos`, `taxaJurosAnual`, `pontos`, `valorFinal`) é alinhado aos DTOs TS; a tela carrega estratégias (`id`, `nome`, `descricao`) via `/simulation/strategies` e apresenta carregamento, erro, resultado, juros e evolução.

**Gap:** a taxa anual segue informada manualmente. Conectar taxas econômicas persistidas à simulação requer escolher indicador/unidade e é uma etapa futura; nenhuma taxa é inventada.

## US-COMP-001 — Análise de posições

**Como** investidor, **quero** analisar valor, resultado, distribuição e histórico da carteira, **para** acompanhar minhas posições com dados atuais.

**Estado:** CDI diário oficial do BCB é apresentado em comparação normalizada com a carteira usando dias completos comuns. Ibovespa B3 é uma funcionalidade futura; dias sem dados não são interpolados.

## US-IMPORT-001 — Importação CSV de operações

**Como** investidor, **quero** revisar um CSV e registrar suas linhas como operações, **para** atualizar as posições sem redigitar cada operação.

**Estado:** a implementação valida campos, números brasileiros, datas, compra/venda, compra de renda fixa, carteira de destino e idempotência por linha. Operações são enviadas ao endpoint real e as consultas são invalidadas após sucesso. O MSW usado no E2E da interface é uma fixture explícita. O teste PostgreSQL confirma o contrato HTTP, idempotência, rejeição de linha inválida e persistência do ledger entre hosts.

**Gap:** validar parsing com extratos reais anonimizados de corretoras. O procedimento está em [broker-csv-import-plan.md](plan/broker-csv-import-plan.md). XLS/XLSX e conectores diretos de corretora não são suportados.

## US-IMPORT-002 — Reimportação auditável de snapshots HTML

- Estado: implementação e validações focadas concluídas no checkout; a sessão autenticada manual não foi conferida.
- Evidência final: 49/49 integrações Tesouro/consolidação, 105/105 unitários e 11/11 E2E focados passaram em 26/11/2026. Ver relatório de validação atualizado.
- Issues: [#55](https://github.com/JonathanBenicio/invest-dashboard/issues/55), [#57](https://github.com/JonathanBenicio/invest-dashboard/issues/57) e [#58](https://github.com/JonathanBenicio/invest-dashboard/issues/58); iniciativa: [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: [0004 — revisão auditável de divergências entre snapshots](adrs/0004-tesouro-reimportacao-snapshots.md), aceita.
- Plano: [reimportação auditável de snapshots do Tesouro Direto](plan/tesouro-reimportacao-snapshots.md).

**Como** investidor, **quero** importar um snapshot HTML posterior e revisar diferenças de posições, eventos e movimentos manuais, **para** atualizar o saldo sem perder operações ou apagar dados por ausência no arquivo.

### Contexto e limites

O sistema apresenta as linhas da prévia em tabelas, associa eventos HTML a movimentos manuais mediante revisão, compara lotes/eventos com o snapshot anterior e registra o aceite da nova base. Um manual sem par bloqueia até o usuário aceitar explicitamente a divergência; a transação fica preservada e não é reaplicada ao saldo novo. Posição ausente é sinalizada como “não observada neste snapshot”, sem propor saldo zero ou encerrar a base anterior. O HTML não fornece identificador estável e um upload representa um título, portanto ausências não provam exclusão.

### Critérios de aceite verificáveis

- [x] Dado uma base e um lote anterior do mesmo título, quando um snapshot posterior for preparado, então quantidade/saldo anterior e proposto e registros potencialmente ausentes são exibidos para revisão. Evidência: testes direcionados de serviço/API com snapshots sintéticos no commit `3adef8c`.
- [x] Dado evento ambíguo ou duplicado, a conciliação não escolhe nem deduplica automaticamente e preserva alternativas. Coberto por testes do comparador e integração.
- [x] Dado movimento manual sem evento correspondente, quando revisar o snapshot, então a confirmação fica bloqueada até o usuário aceitar explicitamente a nova base; a transação original e o aceite permanecem auditáveis. Evidência: teste PostgreSQL e E2E direcionada passaram em 18/11/2026.
- [x] Dada uma posição anterior e um HTML posterior sem posição interpretável, a tela mostra que não foi observada, não propõe saldo zero e bloqueia a confirmação. Coberto por integração e E2E.
- [x] Dada uma prévia com posições ou movimentações, os campos aparecem em tabelas com estado de interpretação e valores brutos. Playwright verificou tabela e acesso aos dados.
- [x] Dada divergência de snapshot, o registro inclui autor, lote, versão, hash e decisão; revisão obsoleta é invalidada. Integrações PostgreSQL focadas passaram.
- [x] Dado upload de um título, posições fora do ativo em conciliação não são removidas nem classificadas como encerradas. Integração PostgreSQL reimportou um dos dois títulos Tesouro da carteira e verificou que a base e a posição do outro permaneceram inalteradas.
- [x] Dado posições válidas sem movimentos, quando o snapshot for confirmado, então a base pode ser atualizada sem criar movimento fictício. Evidência: teste de integração PostgreSQL em contêiner, 18/11/2026.
- [x] A base anterior é substituída junto com a decisão e rebuild transacionais; rollback mantém a base anterior. Integração PostgreSQL passou.

### Contratos, evidências e gaps

- Autorização: usuário autenticado com permissão de gerenciar a carteira; revisar/reconfirmar a mesma versão.
- Contrato: endpoints de prévia, conciliação, revisão, consulta de auditoria e confirmação expõem comparação/hash de snapshot; aceite manual validado por integração PostgreSQL.
- Persistência: revisão de movimentos e revisão de divergências são append-only; esta última é criada com o hash, autor, decisão, versão da carteira e data.
- Evidências: integração final focada passou 49/49 e unitários 105/105; Playwright focado 11/11. Inclui snapshot sem movimentos, rollback/concorrência, vínculo manual, posição não observada, encerramento por venda e leitura do Extrato compartilhado. Conferência autenticada manual pendente. XLSX adiado.
- XLSX: adiado pelo usuário e fora desta história.

## US-PORT-003 — consolidar investimentos por grupo

- Estado: implementação e validações focadas concluídas no checkout.
- Issue: [#70](https://github.com/JonathanBenicio/invest-dashboard/issues/70).
- ADR: [0005 — consulta consolidada de investimentos](adrs/0005-consulta-consolidada-investimentos.md).
- Plano: [investimentos consolidados por grupo](plan/investimentos-consolidados-por-grupo.md).

**Como** pessoa que acompanha investimentos de várias carteiras do mesmo grupo, **quero** ver o saldo de um mesmo investimento consolidado e poder alternar para uma carteira, **para** entender o total e saber a quem pertence cada parte.

### Critérios de aceite verificáveis

- [x] Dadas duas carteiras acessíveis com o mesmo instrumento no grupo, a lista mostra um total que inclui ambas. O detalhe consolidado mostra as contribuições e permite abrir cada posição; integração e E2E passaram.
- [x] Ao abrir por uma carteira, o detalhe inicia nessa carteira, oferece o consolidado do grupo e permite retornar à posição individual. O E2E cobriu a alternância para uma carteira agrupada e a entrada no detalhe de uma carteira sem grupo.
- [x] O filtro por titular inclui todas as suas carteiras elegíveis. Ao escolher uma carteira, o detalhe exibe somente sua posição. Integração e E2E passaram.
- [x] Carteiras sem acesso ficam fora de totais, opções e detalhes, inclusive em links diretos. Integração HTTP e revisão de segurança confirmaram o escopo.
- [x] Operações e alterações continuam vinculadas à posição de uma carteira com autorização de escrita; o detalhe individual conserva os endpoints e IDs de posição.

### Contratos, evidências e gaps

A consulta consolida posições autorizadas antes de paginar. Integrações em InMemory verificaram agrupamento, paginação, filtros, posições encerradas, carteiras sem grupo e isolamento de acesso; PostgreSQL isolado verificou datas distintas entre bases Tesouro. O Playwright com MSW verificou os filtros e a alternância entre grupo e carteira em 320, 360 e 1920 px. A consulta pública do Tesouro aparece separadamente nos detalhes e em Análise e usa cache em memória da API. A conferência autenticada manual não foi executada.

<a id="us-ia-001"></a>

## US-IA-001 — Consultar carteira e retomar a conversa

- Estado: proposta
- Issue: [#78](https://github.com/JonathanBenicio/invest-dashboard/issues/78)
- Plano: [subplano](plan/ia-renda-fixa/06-conversas-streaming.md)

Como investidor, quero consultar posições da minha carteira e abrir o histórico para retomar a análise com a mesma resposta exibida.

### Critério de aceite verificável

- [ ] Dados de uma carteira própria batem com o serviço financeiro e o histórico preserva o texto final. Evidência pendente.

<a id="us-ia-002"></a>

## US-IA-002 — Administrar provedores de IA

- Estado: proposta
- Issue: [#75](https://github.com/JonathanBenicio/invest-dashboard/issues/75)
- Plano: [subplano](plan/ia-renda-fixa/03-politica-provedores.md)

Como administrador da plataforma, quero ativar, testar e ordenar provedores para manter a capacidade disponível dentro do orçamento.

### Critério de aceite verificável

- [ ] Uma falha elegível antes da primeira saída usa provedor alternativo sem duplicar a resposta ou revelar a credencial. Evidência pendente.

<a id="us-ia-003"></a>

## US-IA-003 — Consultar dados permitidos de um grupo

- Estado: proposta
- Issue: [#81](https://github.com/JonathanBenicio/invest-dashboard/issues/81)
- Plano: [subplano](plan/ia-renda-fixa/09-assistente-grupos.md)

Como membro de grupo, quero analisar as carteiras que posso visualizar para entender o consolidado autorizado.

### Critério de aceite verificável

- [ ] Totais cobrem apenas carteiras visíveis. Revogação barra leitura e continuação da conversa afetada. Evidência pendente.

<a id="us-ia-004"></a>

## US-IA-004 — Revisar um print e confirmar saldo observado

- Estado: proposta
- Issue: [#77](https://github.com/JonathanBenicio/invest-dashboard/issues/77)
- Plano: [subplano](plan/ia-renda-fixa/05-revisao-saldo.md)

Como gestor da carteira, quero corrigir a prévia de um print do Tesouro para registrar o saldo bruto da posição existente.

### Critério de aceite verificável

- [ ] Prévia não altera o patrimônio. Confirmação registra uma avaliação idempotente após revisão. Evidência pendente.

<a id="us-ia-005"></a>

## US-IA-005 — Distinguir saldo de estimativa em renda fixa

- Estado: proposta
- Issue: [#84](https://github.com/JonathanBenicio/invest-dashboard/issues/84)
- Plano: [subplano](plan/ia-renda-fixa/12-projecoes-renda-fixa.md)

Como investidor, quero ver premissas e fontes de uma projeção para interpretar o saldo e a estimativa segundo suas datas e naturezas.

### Critério de aceite verificável

- [ ] Apresenta estimativa com premissas ou motivo da ausência. Evidência pendente.

## Manutenção do catálogo

Não reutilize IDs. Histórias extensas podem ter arquivo próprio em `docs/user-stories/`, criado somente quando necessário e sempre ligado daqui. Ao confirmar ou mudar uma história, registre ator, benefício, critérios verificáveis, contratos, evidências e gaps conforme [templates/user-stories-template.md](../templates/user-stories-template.md). Atualize o estado apenas com evidência compatível.
