# ADR-0004 — revisão auditável de divergências entre snapshots do Tesouro Direto

- Data: 2026-11-18
- Estado da decisão: proposta
- Implementação: parcial no PR #62; comparação e auditoria nos commits `3adef8c`/`43d8c14`; aceite explícito de manual sem vínculo no commit `26aa337`, com integração PostgreSQL e E2E aprovadas em 18/11/2026
- Validação: integração PostgreSQL em SDK .NET 10.0.404 e E2E Playwright 1.65.0 passaram em 18/11/2026; ver plano vinculado
- Issue: [#55 — contrato e regras de conciliação](https://github.com/JonathanBenicio/invest-dashboard/issues/55), [#57 — persistir e reconciliar posições](https://github.com/JonathanBenicio/invest-dashboard/issues/57)
- Story: [US-IMPORT-002](../USER-STORIES.md#us-import-002--reimportação-auditável-de-snapshots-html)
- Plano: [reimportação auditável de snapshots](../plan/tesouro-reimportacao-snapshots.md)

> Esta proposta não declara uma divergência como verdadeira. O HTML pode não apresentar cobertura temporal completa e não oferece identificador estável de movimento; o sistema deve chamar a ausência de “não observada no snapshot” até o usuário revisá-la.

## Contexto e problema

O sistema persiste cada importação HTML como lote, permite revisar eventos por lote e usa uma base ativa por posição. O fluxo compara a prévia com o lote anterior e registra o aceite da nova base. Um manual sem vínculo bloqueia até que o usuário revise e aceite explicitamente o snapshot; a transação original permanece preservada e não é reaplicada ao saldo substituído.

O HTML é interpretado preliminarmente e não prova, por si só, cobertura completa. A posição pode conter várias linhas de aquisição, e os cartões de movimento não expõem ID genérico estável. Comparar só valor/data pode confundir operações idênticas.

## Decisão proposta

- Comparar a prévia nova com o lote apontado pela base ativa do mesmo título/vencimento; comparar somente os escopos que o arquivo declara e não inferir posições de outros títulos.
- Produzir um diff de linhas/eventos com correspondência multiconjunto por campos observáveis. Marcar registros ausentes como potenciais divergências, não removê-los nem deduplicá-los automaticamente.
- Se houver divergências, bloquear a confirmação até o usuário revisar e aceitar a nova base. Um manual sem par pode ter seu efeito substituído pela nova base somente quando o usuário aceita explicitamente a comparação; a transação e os IDs dos movimentos no período ficam preservados para auditoria. Sem aceite, a confirmação permanece bloqueada.
- Persistir a decisão de forma append-only com carteira, lote, autor, versão da carteira, hash do diff, ação e instante UTC. Uma nova prévia, diferença ou alteração da carteira invalida a revisão anterior.
- Manter a base anterior ativa até que a nova base, eventos, decisão e rebuild sejam gravados na mesma transação serializável. Em falha, rollback mantém o estado anterior.
- Permitir snapshot com posição válida e zero movimentos, desde que os dados da posição sejam interpretáveis e divergências/manuais tenham revisão explícita. Não inventar movimentos para satisfazer o fluxo.
- Um único HTML por título não permite concluir que outro título da carteira está ausente. Comparação de carteira inteira exigirá um lote explícito com múltiplos arquivos ou fonte com cobertura da carteira.

## Alternativas e trade-offs

| Alternativa | Benefícios | Custos/riscos | Motivo da avaliação |
|---|---|---|---|
| Remover divergências/lançamentos ausentes automaticamente | Fluxo curto | Pode apagar posição legítima por HTML parcial e perde proveniência | Rejeitada conforme regra de produto do usuário |
| Deduplicar eventos por data e valor | Implementação simples | Operações legítimas podem ser idênticas | Rejeitada |
| Bloquear todo snapshot que contenha divergência sem mecanismo de revisão | Evita alteração incorreta | Deixa importação permanentemente travada em casos legítimos | Insuficiente |
| Revisão explícita, versionada e persistida por hash do diff | Usuário controla a decisão e há trilha auditável; snapshots anteriores são preservados | Exige novo contrato/modelo/tabela, UI e migration PostgreSQL | Recomendada para revisão |

## Consequências e migração

- Application calcula diff sem escolher pares automaticamente; WebAPI expõe divergências e recebe a revisão; frontend destaca campos e exige ação antes de confirmar.
- A entidade/tabela de revisão de snapshot ligada ao lote/carteira e seus índices estão implementados no PR #62. O histórico anterior não é reescrito; decisões são append-only.
- O controller de confirmação revalida hash, autor, versão e revisões sob a transação serializável que troca a base ativa.
- PostgreSQL é requisito deste fluxo. EF InMemory não prova constraints, transação serializável, unique indexes ou rollback; migrations e persistência foram exercitadas em PostgreSQL temporário, mas rollback de falha durante a troca ainda precisa de teste específico.
- A API preserva histórico manual e oficial. A revisão registra a comparação aceita e os movimentos no período. A base nova passa a ser o saldo efetivo, portanto transações manuais anteriores à data-base não são reaplicadas; seus registros permanecem no ledger/auditoria.
- Migração: tabela nova sem preenchimento de decisões antigas; lotes existentes continuam sem decisão de snapshot e só exigem revisão se forem reimportados depois da migration. O rollback da migration e da confirmação ainda precisa de validação PostgreSQL; a remoção da tabela não deve apagar lotes/eventos/base existentes.
- Não há inferência de posição ausente em outro título quando o upload contém só um título.

## Implementação

- Estado e escopo implementado: diff de snapshots, hash, revisão persistida e aceite na confirmação; branch `codex/tesouro-previa-e-cotacoes`.
- Referências/PR/commits: Issues #55/#57/#58; PR #62; commits `3adef8c`, `43d8c14` e `26aa337`.
- Restante: validação manual do usuário; cobrir ausência integral e rollback de falha durante a reimportação. O teste atual inclui concorrência na confirmação inicial, mas não na troca da base posterior.

## Validação e gaps

- Critérios de validação: comparações multiconjunto reproduzíveis; hash revalidado; revisão obsoleta recusada; rollback deixa base antiga ativa; auditoria mantém decisões anteriores; operação manual nunca é removida automaticamente.
- Execuções e evidências reais: o teste direcionado de reimportação passou em PostgreSQL 15 isolado/SDK .NET 10.0.404; dois cenários E2E da conciliação passaram com Playwright 1.65.0 e Chromium 1255; ESLint direcionado passou em 18/11/2026. Criação inicial, posição sem movimentos e comparador têm evidências anteriores no plano. Build anterior da aplicação passou em Docker.
- Limitações: cobertura incompleta do HTML e falta de ID estável impedem tratar o diff como prova. Um upload por título não permite reconciliar a coleção completa da carteira. O aceite autoriza a nova base, não prova que a leitura do HTML esteja completa.
