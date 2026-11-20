# Plano — reimportação auditável de snapshots do Tesouro Direto

- Estado: em andamento; comparação, auditoria, aceite explícito de movimentos sem vínculo e sinalização de posição não observada estão implementados no código local. Builds passaram; validação manual, concorrência/rollback e decisão sobre encerramento explícito continuam pendentes.
- Issues: [#55 — contrato/regras de conciliação](https://github.com/JonathanBenicio/invest-dashboard/issues/55), [#57 — persistência/reconciliação](https://github.com/JonathanBenicio/invest-dashboard/issues/57) e [#58 — validação ponta a ponta](https://github.com/JonathanBenicio/invest-dashboard/issues/58); iniciativa [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: [0004 — revisão auditável de divergências entre snapshots](../adrs/0004-tesouro-reimportacao-snapshots.md), proposta.
- Story: [US-IMPORT-002](../USER-STORIES.md#us-import-002--reimportação-auditável-de-snapshots-html), em andamento.
- Baseline: branch `codex/tesouro-previa-e-cotacoes`, última implementação no commit `aa21a9c`; PR #62 permanece em rascunho.
- Plano canônico: `docs/plan/tesouro-reimportacao-snapshots.md`.

## Objetivo, baseline e escopo

- Problema observado: a confirmação troca a base ativa por título e preserva eventos por lote. O fluxo compara snapshots e registra o aceite da nova base; movimentos manuais sem vínculo só são substituídos no saldo após aceite explícito da comparação.
- Resultado esperado: comparar snapshots posteriores do mesmo título, apresentar diferenças como possíveis lacunas (não como prova de ausência), exigir revisão explícita antes de ativar uma nova base e preservar decisões, histórico anterior e movimentos manuais para auditoria.
- Incluído: comparação de linhas de posição e movimentos do lote anterior com o novo HTML para o mesmo título; divergências de quantidade/cobertura; operações manuais não vinculadas; revisão versionada e vinculada ao hash da comparação, usuário, carteira e lote; confirmação apenas após revisão vigente.
- Excluído: inferir que uma posição inteira da carteira foi encerrada porque um único HTML de título não a contém; remover transações ou posições sem decisão explícita; assumir que dois cartões com mesma data/valor são o mesmo evento; importar XLSX.
- Dependências: extrato anterior associado à base ativa, layout/semântica das linhas HTML, identidade natural de eventos/lotes, autorização e versão da carteira; migration da revisão já existe e requer validação PostgreSQL para concorrência/rollback.

## Regras de produto e limites conhecidos

- O extrato é fonte oficial para o histórico do usuário, mas a leitura HTML pode estar parcial; ausência em um arquivo significa “não observado neste snapshot”, não prova de que o evento ou a posição deixou de existir.
- Uma nova base não é ativada enquanto há divergências não revisadas. A revisão deve mostrar o saldo anterior, o saldo proposto e os registros potencialmente ausentes.
- Revisar e aceitar o snapshot novo é uma ação explícita. A posição anterior permanece ativa até a confirmação transacional. Aceitar nova base não apaga os eventos/lotes anteriores; mantém sua trilha de auditoria.
- Um movimento manual que coincide com evento oficial é vinculado após escolha do usuário. Se não houver vínculo, o usuário pode aceitar explicitamente a nova base após revisar a divergência; o efeito anterior não é reaplicado, mas a transação original e o aceite ficam registrados na auditoria. Sem aceite, a confirmação é bloqueada.
- O identificador dos cartões HTML não é estável. Comparações por data, natureza, tipo, quantidade, preço e valor são sugestões multiconjunto e podem ser ambíguas; não deduplicar nem excluir automaticamente.
- O upload atual aceita um HTML por lote e os arquivos observados são focados em título. Portanto, ausência de outro título da carteira não pode ser inferida por um arquivo individual; essa verificação exigirá um lote explícito de vários arquivos ou outro extrato com cobertura da carteira inteira.
- Snapshot válido com posição e sem movimento pode atualizar a base, desde que a leitura das posições seja suficiente e não haja conflito manual pendente. Movimento ausente continua sinalizado como limitação de cobertura.

## Etapas e verificações

| Etapa/entrega | Dependências | Verificação observável | Estado e evidência |
|---|---|---|---|
| [x] Comparar snapshots anteriores e atuais | Base ativa, lote anterior e novo `PreviaTesouroDto` | Serviço produz divergências por título/lote/evento com chaves e limites documentados | Implementado no commit `3adef8c`; testes direcionados do comparador passaram; HTML informa cobertura não comprovada |
| [x] Persistir revisão de divergências | ADR-0004, Issue #55, entidade/configuração e migration EF | Registro append-only inclui hash da comparação, autor, decisão, versão da carteira e data; revisão antiga não valida snapshot alterado | Implementado no commit `3adef8c`; migration aplicada em teste PostgreSQL isolado |
| [x] Expor revisão na prévia e no contrato | Serviço de diff, API de revisão e UI atual | Usuário vê divergências e saldo anterior/proposto; aceite habilita confirmação apenas após revisão | API e tela implementadas; E2E direcionada passou (2 cenários) |
| [x] Integrar validação à confirmação | Decisões de eventos e de snapshot | Confirmação bloqueia revisão ausente/obsoleta; aceite atualiza base na mesma transação e preserva registros anteriores | Integração PostgreSQL passou com bloqueio sem aceite, hash, troca de base e auditoria |
| [x] Permitir posição sem movimentações | Validação do saldo-base e cobertura de posição | Snapshot com lotes válidos e zero movimentos pode ser confirmado, sem fabricar eventos | Teste de integração PostgreSQL passou no commit `7d7207a` |
| [ ] Revisar posição não observada sem zerar saldo | Snapshot anterior e HTML sem linhas de posição interpretáveis | API retorna quantidade proposta indisponível, exibe divergência e mantém a confirmação bloqueada; base anterior não é desativada | Implementado no commit `aa21a9c`; builds passaram; validação manual pendente |
| [ ] Exibir linhas da prévia em tabelas | Campos brutos por posição e movimentação | Tabelas preservam valores originais e destacam linhas que precisam de revisão; rolagem horizontal em telas estreitas | Implementado no commit `aa21a9c`; ESLint e build Vite passaram; validação manual pendente |
| [ ] Validar regressão e concorrência | Migração, API, UI e PostgreSQL isolado | Testes de segundo snapshot, ambiguidade, ausência, manual sem par, rollback, replay, concorrência e isolamento | Pendente |

## Critérios de aceite

- [x] Ao reimportar o mesmo título após uma base ativa, a prévia mostra o saldo anterior e o saldo proposto e lista possíveis linhas/eventos ausentes; evidência: testes direcionados de serviço/API com dois snapshots sintéticos, commit `3adef8c`.
- [x] Correspondências naturais ambíguas ficam para escolha do usuário e preservam múltiplos eventos iguais; evidência: `ComparadorSnapshotTesouroTests.ApresentaCadaEventoAnteriorNaoObservadoSemApagarDuplicatas`, 21 testes direcionados passaram em 18/11/2026.
- [x] Movimento manual sem par bloqueia a confirmação até o usuário revisar e aceitar explicitamente a nova base; transação e decisão ficam preservadas para auditoria. Evidência: integração PostgreSQL e E2E direcionada passaram em 18/11/2026.
- [ ] Dada uma base ativa e um HTML sem posição interpretável para o título, quando comparar, então o sistema informa “não observada neste arquivo”, não propõe quantidade zero, mantém a base ativa e bloqueia a confirmação. Evidência: build passou; validação manual pendente.
- [ ] Divergências confirmadas ficam ligadas ao hash exato do snapshot, autor e versão da carteira. Alterar lote/carteira invalida a revisão anterior.
- [ ] A base anterior permanece ativa até confirmação serializável da nova base; falha/rollback não deixa estado parcial.
- [ ] Ausência de posição/evento não apaga dado automaticamente. Um HTML por título não é usado para inferir encerramento de outros títulos da carteira.
- [x] Snapshot com posição válida e nenhum movimento pode ser revisado e confirmado sem movimentos fictícios. Evidência: teste PostgreSQL no commit `7d7207a`.
- [x] Reimportar o mesmo lote continua idempotente; operações posteriores à nova base continuam aplicadas uma única vez. Evidência: `Confirmacao_RevalidaRevisoesEPreservaMovimentacaoPosteriorSemDuplicarEventos`.

## Validação

Os testes direcionados anteriores passaram em 18/11/2026. Para a alteração atual de posição não observada, o build completo .NET 10.0.404 e o build Vite passaram; não foram executados testes automatizados por orientação do usuário. A validação manual deve confirmar a divergência exibida e que a base anterior continua ativa.

| Verificação | Comando/método e ambiente | Estado | Evidência/limitação |
|---|---|---|---|
| Criação inicial HTML com eventos | teste de integração filtrado, PostgreSQL 15 temporário | passou | Upload, revisão, ativo, base, posição e evento oficial; não cobre tela |
| Criação inicial HTML sem eventos | teste de integração filtrado, PostgreSQL 15 temporário | passou | Posição válida sem cartões de movimentação cria base sem evento fictício |
| Reimportação de dois snapshots | `dotnet test tests/InvestDashboard.IntegrationTests/InvestDashboard.IntegrationTests.csproj --filter FullyQualifiedName~Confirmacao_RevalidaRevisoesEPreservaMovimentacaoPosteriorSemDuplicarEventos` em SDK .NET 10.0.404, PostgreSQL 15 isolado | passou | Bloqueio sem aceite/hash obsoleto, aceite explícito, preservação do manual, troca da base e consulta de auditoria; rollback de falha no segundo snapshot permanece pendente |
| Diferentes taxas por aplicação e isolamento | testes de integração InMemory/PostgreSQL | passou | Duas linhas com 10,5% e 12% preservam taxas no lote; taxa global da posição Tesouro fica nula e não vaza entre duas carteiras. CDB continua usando taxa contratual. |
| Unit tests direcionados | `dotnet test tests/InvestDashboard.UnitTests/InvestDashboard.UnitTests.csproj --filter 'FullyQualifiedName~ConfirmacaoTesouroCriacaoTests|FullyQualifiedName~ComparadorSnapshotTesouroTests|FullyQualifiedName~TesouroDiretoHtmlReaderTests'` em SDK .NET 10.0.404 | passou | 21 testes passaram; inclui criação, comparação multiconjunto, snapshot sem cartões, divergências e duplicatas |
| Frontend | ESLint direcionado e `./node_modules/.bin/playwright test e2e/conciliacao-tesouro.spec.ts --config node_modules/.cache/codex-playwright.config.mjs --workers=1` | passou | Dois cenários passaram; o segundo verifica que o aceite libera o botão e envia hash/aceite. Config temporária usou Vite na porta 5174; Docker Compose e a porta 5173 não foram alterados. |
| Build atual do backend | `dotnet build InvestDashboard.slnx` com SDK .NET 10.0.404 em contêiner | passou | Solução compilou sem warnings/erros; nenhum teste foi iniciado. |
| Build atual do frontend | `./node_modules/.bin/vite build` | passou | Bundling passou; Vite reportou o aviso já existente de chunk maior que 500 kB. Nenhum E2E foi iniciado nesta alteração. |
| ESLint dos componentes de prévia e conciliação | `./node_modules/.bin/eslint src/components/PreviaTesouroDireto.tsx src/components/ConciliacaoTesouro.tsx src/api/services/tesouro-direto.service.ts` | passou | Nenhum teste automatizado foi criado ou executado nesta alteração. |
| Migration e concorrência da confirmação inicial | integração PostgreSQL da classe `ConfirmacaoTesouroControllerTests` | passou | Banco temporário aplicou a migration e o teste confirmou que duas solicitações paralelas geram no máximo uma confirmação efetiva |
| Rollback e concorrência ao trocar snapshot posterior | não executados | não executado | Falta provocar falha durante a troca da base e disputar confirmações concorrentes do mesmo snapshot |
| E2E de reimportação | `frontend/e2e/conciliacao-tesouro.spec.ts`, Playwright 1.65.0, Chromium 1255, Vite na porta 5174 | passou | Dois cenários passaram; a validação manual com a conta real do usuário ainda está pendente |

## Riscos, gaps e pendências

| Item | Impacto | Mitigação/ação | Referência/estado |
|---|---|---|---|
| HTML não tem ID estável e a cobertura temporal não é comprovada | Falso pareamento ou falso alerta de ausência | Mostrar divergência como hipótese, preservar multiconjunto e pedir revisão | Issues #55/#58 abertas |
| Um HTML representa um título, não a carteira inteira | Não é possível detectar automaticamente outros títulos ausentes | Não inferir remoção; planejar lote de vários arquivos se a carteira inteira precisar ser comparada | Limite atual da interface |
| Movimento manual sem par pode ter o efeito substituído sem ciência | A nova base pode retirar o efeito da operação do saldo atual | Exigir aceite explícito da nova base, registrar os IDs dos manuais e manter a transação original | Integração PostgreSQL e E2E passaram |
| Revisão pode ficar obsoleta após concorrência | Confirmação aceita dados que o usuário não revisou | Hash do diff + versão da carteira + transação serializável | Pendente |
| Ausência de posição no HTML pode parecer saldo zero | O usuário poderia interpretar ausência como encerramento | Mostrar quantidade indisponível e divergência `posicao_nao_observada`; bloquear troca da base. Encerramento exige decisão explícita futura | Implementado no diff atual; validação manual pendente |
| Um HTML representa um título, não a carteira inteira | Não é possível detectar automaticamente outros títulos ausentes | Não inferir remoção de outros títulos a partir de um arquivo individual | Limite atual da fonte/contrato |

## Entrega e sincronização

- Implementação/PR/commits: PR #62 contém upload, prévia tabular, confirmação inicial, comparação de snapshots, revisão auditável e sinalização de posição não observada nos commits `3adef8c`, `43d8c14`, `26aa337` e `aa21a9c`.
- Relatório/evidências: resultado da criação inicial está registrado em [Issue #58](https://github.com/JonathanBenicio/invest-dashboard/issues/58).
- Limitações restantes: validação manual do usuário, rollback de falha durante reimportação, concorrência no snapshot posterior e ação explícita para encerrar uma posição; ausência não desativa a base automaticamente.
- Índices: ligar a Issues #55, #57, #58, Story US-IMPORT-002, ADR-0004, `docs/INDEX.md` e `conductor/tracks.md`.
