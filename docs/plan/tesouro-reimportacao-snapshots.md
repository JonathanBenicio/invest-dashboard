# Plano — reimportação auditável de snapshots do Tesouro Direto

- Estado: em andamento; regras de base temporal existem, revisão de divergências entre snapshots está pendente.
- Issues: [#55 — contrato/regras de conciliação](https://github.com/JonathanBenicio/invest-dashboard/issues/55), [#57 — persistência/reconciliação](https://github.com/JonathanBenicio/invest-dashboard/issues/57) e [#58 — validação ponta a ponta](https://github.com/JonathanBenicio/invest-dashboard/issues/58); iniciativa [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: [0004 — revisão auditável de divergências entre snapshots](../adrs/0004-tesouro-reimportacao-snapshots.md), proposta.
- Story: [US-IMPORT-002](../USER-STORIES.md#us-import-002--reimportação-auditável-de-snapshots-html), proposta.
- Baseline: branch `codex/tesouro-previa-e-cotacoes`, HEAD `4fe4d0d`; PR #62 permanece em rascunho.
- Plano canônico: `docs/plan/tesouro-reimportacao-snapshots.md`.

## Objetivo, baseline e escopo

- Problema observado: a confirmação atual troca a base ativa por título e preserva eventos por lote. Ela sugere vínculos entre eventos do novo HTML e movimentos manuais, mas não compara o histórico oficial anterior com o novo snapshot, não apresenta ausências como divergências e bloqueia toda a confirmação quando uma operação manual sem par está no intervalo substituído.
- Resultado esperado: comparar snapshots posteriores do mesmo título, apresentar diferenças como possíveis lacunas (não como prova de ausência), exigir revisão explícita antes de ativar uma nova base e preservar decisões, histórico anterior e movimentos manuais para auditoria.
- Incluído: comparação de linhas de posição e movimentos do lote anterior com o novo HTML para o mesmo título; divergências de quantidade/cobertura; operações manuais não vinculadas; revisão versionada e vinculada ao hash da comparação, usuário, carteira e lote; confirmação apenas após revisão vigente.
- Excluído: inferir que uma posição inteira da carteira foi encerrada porque um único HTML de título não a contém; remover transações ou posições sem decisão explícita; assumir que dois cartões com mesma data/valor são o mesmo evento; importar XLSX.
- Dependências: extrato anterior associado à base ativa, layout/semântica das linhas HTML, identidade natural de eventos/lotes, autorização e versão da carteira; migração EF/PostgreSQL para registrar a revisão de snapshot.

## Regras de produto e limites conhecidos

- O extrato é fonte oficial para o histórico do usuário, mas a leitura HTML pode estar parcial; ausência em um arquivo significa “não observado neste snapshot”, não prova de que o evento ou a posição deixou de existir.
- Uma nova base não é ativada enquanto há divergências não revisadas. A revisão deve mostrar o saldo anterior, o saldo proposto e os registros potencialmente ausentes.
- Revisar e aceitar o snapshot novo é uma ação explícita. A posição anterior permanece ativa até a confirmação transacional. Aceitar nova base não apaga os eventos/lotes anteriores; mantém sua trilha de auditoria.
- Um movimento manual que coincide com evento oficial é vinculado após escolha do usuário. Um movimento manual sem evento correspondente bloqueia a confirmação; preservar o lançamento e apresentar o conflito. Resolução alternativa com auditoria ainda precisa ser definida e implementada.
- O identificador dos cartões HTML não é estável. Comparações por data, natureza, tipo, quantidade, preço e valor são sugestões multiconjunto e podem ser ambíguas; não deduplicar nem excluir automaticamente.
- O upload atual aceita um HTML por lote e os arquivos observados são focados em título. Portanto, ausência de outro título da carteira não pode ser inferida por um arquivo individual; essa verificação exigirá um lote explícito de vários arquivos ou outro extrato com cobertura da carteira inteira.
- Snapshot válido com posição e sem movimento pode atualizar a base, desde que a leitura das posições seja suficiente e não haja conflito manual pendente. Movimento ausente continua sinalizado como limitação de cobertura.

## Etapas e verificações

| Etapa/entrega | Dependências | Verificação observável | Estado e evidência |
|---|---|---|---|
| [ ] Comparar snapshots anteriores e atuais | Base ativa, lote anterior e novo `PreviaTesouroDto` | Serviço produz divergências por título/lote/evento com chaves e limites documentados | Parcial em alterações locais; HTML sempre informa cobertura não comprovada |
| [ ] Persistir revisão de divergências | ADR-0004, Issue #55, nova entidade/configuração e migration EF | Registro append-only inclui hash da comparação, autor, decisão, versão da carteira e data; revisão antiga não valida snapshot alterado | Parcial; migração aplicada em teste PostgreSQL isolado |
| [ ] Expor revisão na prévia e no contrato | Serviço de diff, API de revisão e UI atual | Usuário vê posição/lote/evento ausente, conflito manual e saldo anterior/proposto antes de confirmar | Parcial em alterações locais; frontend ainda precisa de validação |
| [ ] Integrar validação à confirmação | Decisões de eventos e de snapshot | Confirmação bloqueia revisão ausente/obsoleta; aceite atualiza base na mesma transação e preserva registros anteriores | Parcial; teste de dois snapshots passou, cobertura adicional pendente |
| [ ] Permitir posição sem movimentações | Validação do saldo-base e cobertura de posição | Snapshot com lotes válidos e zero movimentos pode ser confirmado, sem fabricar eventos | Teste de integração PostgreSQL passou; E2E de tela pendente |
| [ ] Validar regressão e concorrência | Migração, API, UI e PostgreSQL isolado | Testes de segundo snapshot, ambiguidade, ausência, manual sem par, rollback, replay, concorrência e isolamento | Pendente |

## Critérios de aceite

- [ ] Ao reimportar o mesmo título após uma base ativa, a prévia mostra o saldo anterior e o saldo proposto e lista possíveis linhas/eventos ausentes; evidência: teste de serviço e API com dois HTML sintéticos.
- [ ] Correspondências naturais ambíguas ficam para escolha do usuário e preservam múltiplos eventos iguais; evidência: fixture sintética com eventos duplicados.
- [ ] Movimento manual sem par impede confirmação e a transação original continua preservada; uma resolução auditável para esse caso ainda está pendente.
- [ ] Divergências confirmadas ficam ligadas ao hash exato do snapshot, autor e versão da carteira. Alterar lote/carteira invalida a revisão anterior.
- [ ] A base anterior permanece ativa até confirmação serializável da nova base; falha/rollback não deixa estado parcial.
- [ ] Ausência de posição/evento não apaga dado automaticamente. Um HTML por título não é usado para inferir encerramento de outros títulos da carteira.
- [ ] Snapshot com posição válida e nenhum movimento pode ser revisado e confirmado sem movimentos fictícios.
- [ ] Reimportar o mesmo lote continua idempotente; operações posteriores à nova base continuam aplicadas uma única vez.

## Validação

Testes direcionados passaram em 18/11/2026 com SDK .NET 10.0.401 em contêiner e PostgreSQL 15 temporário. Build Docker das imagens API/frontend e health checks locais passaram. Não acompanhei a CI por decisão do usuário; a validação manual da interface está com o usuário.

| Verificação | Comando/método e ambiente | Estado | Evidência/limitação |
|---|---|---|---|
| Criação inicial HTML com eventos | teste de integração filtrado, PostgreSQL 15 temporário | passou | Upload, revisão, ativo, base, posição e evento oficial; não cobre tela |
| Criação inicial HTML sem eventos | teste de integração filtrado, PostgreSQL 15 temporário | passou | Posição válida sem cartões de movimentação cria base sem evento fictício |
| Reimportação de dois snapshots | teste de integração PostgreSQL `ConfirmacaoTesouroControllerTests` | passou | Ausência de evento é sinalizada, manual sem vínculo bloqueia, vínculo permite atualização, base anterior é desativada e auditoria fica consultável; rollback/concorrência específicos pendentes |
| Diferentes taxas por aplicação e isolamento | testes de integração InMemory/PostgreSQL | passou | Duas linhas com 10,5% e 12% preservam taxas no lote; taxa global da posição Tesouro fica nula e não vaza entre duas carteiras. CDB continua usando taxa contratual. |
| Unit tests direcionados | `ConfirmacaoTesouroCriacaoTests`, `ComparadorSnapshotTesouroTests`, `TesouroDiretoHtmlReaderTests` | passou | 20 testes passaram; inclui linhas com taxa múltipla, snapshot sem cartões, divergências e duplicatas |
| Frontend | Docker Compose build com Bun e ESLint direcionado | passou | Build Vite passou; ESLint da conciliação/tabela passou. E2E manual da reimportação permanece pendente. |
| Migration/rollback/concorrência | não executados | não executado | validar em PostgreSQL isolado depois da migration |
| E2E de reimportação | não executado | não executado | Bun não está instalado no host; CI usa Bun e Playwright |

## Riscos, gaps e pendências

| Item | Impacto | Mitigação/ação | Referência/estado |
|---|---|---|---|
| HTML não tem ID estável e a cobertura temporal não é comprovada | Falso pareamento ou falso alerta de ausência | Mostrar divergência como hipótese, preservar multiconjunto e pedir revisão | Issues #55/#58 abertas |
| Um HTML representa um título, não a carteira inteira | Não é possível detectar automaticamente outros títulos ausentes | Não inferir remoção; planejar lote de vários arquivos se a carteira inteira precisar ser comparada | Limite atual da interface |
| Movimento manual sem par não possui resolução intermediária | Atualização fica bloqueada ou pode perder a intenção do usuário | Requerer decisão auditável “incluído na nova base” ou cancelamento | Issue #57 aberta |
| Revisão pode ficar obsoleta após concorrência | Confirmação aceita dados que o usuário não revisou | Hash do diff + versão da carteira + transação serializável | Pendente |
| Ausência integral de posição em HTML de um título | Não há saldo novo para aplicar nem escopo de carteira completa no arquivo | Manter base anterior e sinalizar fora do escopo daquele título; não inferir encerramento de outros títulos | Limite da fonte/contrato |

## Entrega e sincronização

- Implementação/PR/commits: PR #62 contém upload, prévia, confirmação inicial, comparação de snapshots e revisão auditável no commit `3adef8c`; taxas por aplicação no commit `43d8c14`; testes direcionados adicionais locais ainda aguardam CI/manual.
- Relatório/evidências: resultado da criação inicial está registrado em [Issue #58](https://github.com/JonathanBenicio/invest-dashboard/issues/58).
- Limitações restantes: resolução para manual sem par permanece bloqueante; não se infere ausência integral de outros títulos a partir de um HTML por título; E2E e validação manual pendentes.
- Índices: ligar a Issues #55, #57, #58, Story US-IMPORT-002, ADR-0004, `docs/INDEX.md` e `conductor/tracks.md`.
