# Plano — Extrato detalhado do Tesouro Direto

- Estado: implementação e validações automatizadas focadas concluídas; conferência visual autenticada manual não executada.
- Issue: [#69](https://github.com/JonathanBenicio/invest-dashboard/issues/69). Contexto na [iniciativa #53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: [0004 — revisão de divergências entre snapshots](../adrs/0004-tesouro-reimportacao-snapshots.md), usada como referência para o resumo. A consulta usa as bases e os lotes existentes.
- História proposta, sem ID: como investidor, quero consultar o extrato oficial nos detalhes do título para entender vendas, compras e cobranças.
- Baseline desta etapa: branch `codex/tesouro-previa-e-cotacoes`, commit `d7fe081`, com alterações locais preexistentes na prévia e em carteiras compartilhadas.
- Plano canônico: `docs/plan/tesouro-extrato-detalhado.md`.

## Objetivo, baseline e escopo

A tela [InvestmentDetails.tsx](../../frontend/src/pages/investments/InvestmentDetails.tsx) mostra a posição e as transações da carteira. Eventos oficiais do HTML confirmado ficam separados e a consulta atual é por lote. Adicionar a seção **Extrato oficial do Tesouro Direto** nos detalhes do título, mantendo a visão de saldo e as movimentações da carteira.

| Data | Tipo | Valor |
|---|---|---:|
| Data da venda | Venda — valor bruto do resgate | R$ 1.000,00 |
| Data da cobrança | Cobrança de taxa — débito | R$ 2,00 |

Exemplo sintético. A seção mostra data, natureza e tipo, valor em BRL e origem do extrato. Proposta para informações adicionais: quantidade, preço unitário e percentual anual quando presentes, em detalhes da linha. Compras mantêm a distinção entre taxa anual informada e não informada. Uma cobrança conserva o sentido de débito e só recebe identificação de B3 ou instituição se a fonte informar isso explicitamente.

O escopo é consultar o extrato confirmado que serve de base ao saldo atual. A seção não oferece histórico de extratos nem seletor de lotes anteriores. Vendas e cobranças permanecem linhas independentes. Ficam fora do escopo associação automática, cálculo de valor após taxas ou líquido, recálculo de saldo, criação de transações e exclusão de registros já gravados.

### O que mudou na última importação

Decisão de produto aprovada: manter os lotes completos para auditoria e mostrar, nos detalhes, o extrato ativo e um resumo de diferenças em relação à base imediatamente anterior. A primeira importação exibe apenas o extrato ativo. O resumo compara dois lotes confirmados do mesmo título e carteira, com suas datas de referência.

Compare campos observáveis e suas contagens. Uma linha já presente com a mesma contagem fica fora do resumo. Uma venda ou cobrança adicional aparece como observada no novo extrato, em linhas independentes. Uma linha ausente é descrita como “não apareceu no novo extrato”. Quando não houver identidade suficiente, indique a ambiguidade em vez de presumir que um evento foi editado ou cancelado. Mostre a variação da quantidade entre as bases e sinalize quando o lote anterior estiver indisponível. Consulte a [ADR de reimportação](../adrs/0004-tesouro-reimportacao-snapshots.md) para preservar os critérios de comparação e auditoria.

## Etapas e verificações

| Etapa | Dependências | Verificação | Estado |
|---|---|---|---|
| Executar as validações de baseline disponíveis | SDKs do projeto | Registrar resultados e impedimentos antes da implementação | Passou: 1 teste de persistência PostgreSQL e 2 unitários do comparador, segundo o agente backend |
| Definir consulta paginada por carteira e título | Eventos oficiais, lotes confirmados e autorização | Validar contrato, ordenação estável, indexação e mudança de base entre páginas | Implementado; validação de integração em andamento |
| Implementar leitura e teste integrado da API | Contrato aprovado e dados sintéticos | Consultar eventos do título sem incluir outra carteira, título ou lote pendente | Passou em PostgreSQL isolado, incluindo cursor e resposta quando a base muda |
| Implementar resumo da última importação | Bases confirmadas e regra de comparação da ADR | Expor novos registros observados, ausências, ambiguidades e variação de quantidade sem duplicar lançamentos | Implementado. Testes unitários e o fluxo de reimportação no navegador passaram |
| Integrar seção nos detalhes | API e padrão de tabela do frontend | Mostrar valores, origem e estados de carregamento, vazio, erro e paginação | Implementado; Playwright com dados simulados passou, incluindo a troca de base durante a paginação |
| Executar validações automatizadas finais | Implementação completa | Integração, build e Playwright nas dimensões registradas | Passou em execuções focadas. A validação manual da tela continua pendente |

## Critérios de aceite

- [x] Apenas eventos oficiais do lote confirmado da base ativa da carteira e do título consultados são exibidos, com paginação estável. Sem base ativa, apresentar estado vazio.
- [x] Venda é identificada como valor bruto do resgate, e a cobrança aparece como débito próprio. O percentual anual tem apresentação distinta de valores monetários.
- [x] Dados ausentes são apresentados conforme sua classificação. Nenhuma taxa ou valor líquido é inferido.
- [x] Com duas bases confirmadas, o resumo omite linhas repetidas e distingue novas observações, ausências e ambiguidades. Na primeira importação, o extrato ativo aparece sem resumo.
- [x] O saldo e as transações da carteira permanecem consistentes após a consulta. Um vínculo de auditoria existente pode identificar um evento sem virar novo lançamento.
- [x] Interface responsiva com leitura por teclado, incluindo lista vazia, erro recuperável e tabelas largas com rolagem interna.

## Validação

| Verificação | Método e ambiente | Estado |
|---|---|---|
| Baseline de persistência | Validação prévia reportada pelo agente de testes: filtro `TesouroImportPersistenceTests` com PostgreSQL isolado. Um teste passou, sem ignorados. Não cobre o endpoint HTTP de eventos | Passou |
| Baseline de frontend | ESLint em `InvestmentDetails.tsx` em cópia temporária restaurada pelo lockfile, conforme relato do agente de testes. O checkout local apresenta dependências desalinhadas | Passou |
| Integração da API | .NET 10 e PostgreSQL isolado, filtro Tesouro/consolidação | 49/49 testes passaram; inclui leitor autorizado de carteira compartilhada, lote confirmado, bloqueio de lote pendente/sem acesso, isolamento, paginação e resumo da importação |
| Frontend | ESLint, TypeScript e build Vite com Bun 1.3.4 e dependências do lockfile | Passou; lint sem erros e 10 avisos Fast Refresh existentes. Build também reporta aviso de Browserslist antiga, SignalR e chunk >500 kB |
| Navegador com respostas simuladas | `playwright test` em Chromium; cenário `extrato-oficial-tesouro.spec.ts` | Passou em 25/11/2026: venda bruta, cobrança separada, resumo, retorno à primeira página após 409, região de rolagem com foco e larguras de 320, 360, 1280, 1366 e 1920 px |
| Navegador com API e PostgreSQL isolados | `frontend/e2e-real-api/tesouro-import.spec.ts` | Passou em 25/11/2026. Extrato ativo exibiu venda bruta, taxa como débito separado e variação do saldo após reimportação com venda integral. Os dois originais exibiram o extrato inicial. [Validação](../validation/tesouro-importacao-2026-11-25.md) |

## Contrato de consulta

`GET /api/v1/portfolios/{carteiraId}/tesouro-direto/posicoes/{ativoId}/extrato` usa a mesma autorização da consulta de eventos por lote. A resposta contém `baseAtiva`, `eventos`, `proximoIndice` e `resumoUltimaImportacao`. Sem base ativa, retorna um resultado vazio. A paginação aceita `depoisDoIndice` e `limite` de 1 a 100. Depois da primeira página, informe `loteId` para vincular a consulta à base vista. Se a base mudou, a API responde 409 e a tela oferece recarregar a primeira página. A consulta é somente leitura.

## Validação final — 26/11/2026

Playwright focal passou 11/11 com dados sintéticos, incluindo apresentação responsiva em 320, 360, 1280, 1366 e 1920 px. A autorização de leitura compartilhada foi coberta por integração HTTP com PostgreSQL isolado. A tela autenticada com a conta real do usuário não foi conferida nesta rodada.

## Riscos, gaps e pendências

| Item | Impacto | Ação |
|---|---|---|
| Reimportações podem repetir movimentos em lotes distintos | Misturar lotes pode sugerir uma segunda venda ou cobrança | Decisão de produto: exibir só o lote da base ativa. Se uma nova importação substituir a base, a seção passa a mostrar o novo lote. |
| Lotes pertencem ao usuário que os enviou | Leitores autorizados de carteiras compartilhadas não devem perder o extrato confirmado | A API resolve a base ativa por carteira/título e lê lote confirmado da própria carteira; integração PostgreSQL prova isolamento e rejeição de pendentes |
| Eventos antigos podem não conter todos os campos interpretados | Risco de exibir informação incorreta | Preservar valores existentes e sinalizar ausência. Fixture antiga ainda precisa de conferência manual. |

## Entrega e sincronização

- Implementação e testes focados disponíveis no checkout do PR #62. Evidências no [relatório da importação](../validation/tesouro-importacao-2026-11-25.md). A validação manual da tela continua pendente.
- Índice documental atualizado. Rastreabilidade no GitHub pela issue #69.
