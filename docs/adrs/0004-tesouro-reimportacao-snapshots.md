# ADR-0004 — revisão auditável de divergências entre snapshots do Tesouro Direto

- Data: 2026-11-18
- Estado da decisão: aceita pelo usuário; as regras de negócio foram confirmadas durante a iniciativa #53
- Implementação: comparação, revisão persistida, confirmação transacional, criação inicial, encerramento comprovado por venda integral e Extrato detalhado implementados no checkout
- Validação: integração focal PostgreSQL 15 passou em 26/11/2026; confira resultados e limites nos planos vinculados
- Issue: [#55 — contrato e regras de conciliação](https://github.com/JonathanBenicio/invest-dashboard/issues/55), [#57 — persistir e reconciliar posições](https://github.com/JonathanBenicio/invest-dashboard/issues/57)
- Story: [US-IMPORT-002](../USER-STORIES.md#us-import-002--reimportação-auditável-de-snapshots-html)
- Plano: [reimportação auditável de snapshots](../plan/tesouro-reimportacao-snapshots.md)

> Esta proposta não declara uma divergência como verdadeira. O HTML pode não apresentar cobertura temporal completa e não oferece identificador estável de movimento; o sistema deve chamar a ausência de “não observada no snapshot” até o usuário revisá-la.

## Contexto e problema

O sistema persiste cada importação HTML como lote, permite revisar eventos por lote e usa uma base ativa por posição. O fluxo compara a prévia com o lote anterior e registra o aceite da nova base. Um manual sem vínculo bloqueia até que o usuário revise e aceite explicitamente o snapshot; a transação original permanece preservada e não é reaplicada ao saldo substituído.

O HTML é interpretado preliminarmente e não prova, por si só, cobertura completa. A posição pode conter várias linhas de aquisição, e os cartões de movimento não expõem ID genérico estável. Comparar só valor/data pode confundir operações idênticas.

## Decisão

- Comparar a prévia nova com o lote apontado pela base ativa do mesmo título/vencimento; comparar somente os escopos que o arquivo declara e não inferir posições de outros títulos.
- Produzir um diff de linhas/eventos com correspondência multiconjunto por campos observáveis. Marcar registros ausentes como potenciais divergências, não removê-los nem deduplicá-los automaticamente.
- Se houver divergências, bloquear a confirmação até o usuário revisar e aceitar a nova base. Um manual sem par pode ter seu efeito substituído pela nova base somente quando o usuário aceita explicitamente a comparação; a transação e os IDs dos movimentos no período ficam preservados para auditoria. Sem aceite, a confirmação permanece bloqueada.
- Persistir a decisão de forma append-only com carteira, lote, autor, versão da carteira, hash do diff, ação e instante UTC. Uma nova prévia, diferença ou alteração da carteira invalida a revisão anterior.
- Manter a base anterior ativa até que a nova base, eventos, decisão e rebuild sejam gravados na mesma transação serializável. Em falha, rollback mantém o estado anterior.
- Permitir snapshot com posição válida e zero movimentos, desde que os dados da posição sejam interpretáveis e divergências/manuais tenham revisão explícita. Não inventar movimentos para satisfazer o fluxo.
- Um único HTML por título não permite concluir que outro título da carteira está ausente. Comparação de carteira inteira exigirá um lote explícito com múltiplos arquivos ou fonte com cobertura da carteira.
- Quando o HTML posterior não apresentar posição interpretável, a comparação informa que ela não foi observada, mantém a quantidade proposta indisponível e bloqueia a troca da base. O dado ausente não vira posição zerada nem encerrada.

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
- PostgreSQL é requisito deste fluxo. EF InMemory não prova constraints, transação serializável, unique indexes ou rollback; migrations, persistência, rollback durante confirmação e concorrência foram exercitados em PostgreSQL temporário.
- A API preserva histórico manual e oficial. A revisão registra a comparação aceita e os movimentos no período. A base nova passa a ser o saldo efetivo, portanto transações manuais anteriores à data-base não são reaplicadas; seus registros permanecem no ledger/auditoria.
- Migração: tabela nova sem preenchimento de decisões antigas; lotes existentes continuam sem decisão de snapshot e só exigem revisão se forem reimportados depois da migration. O rollback transacional da confirmação foi validado em PostgreSQL; o rollback da migration permanece sem validação nesta rodada.
- Não há inferência de posição ausente em outro título quando o upload contém só um título.

## Implementação

- Estado e escopo implementado: diff de snapshots, hash, revisão persistida, aceite na confirmação e prévia tabular; branch `codex/tesouro-previa-e-cotacoes`.
- Referências/PR/commits: Issues #55/#57/#58; PR #62; commits `3adef8c`, `43d8c14`, `26aa337` e `aa21a9c`.
- Restante: validação autenticada manual não executada nesta rodada. A posição ausente continua sinalizada; encerramento ocorre somente quando a venda integral é reconciliada e revisada.

## Validação e gaps

- Critérios de validação: comparações multiconjunto reproduzíveis; hash revalidado; revisão obsoleta recusada; rollback deixa base antiga ativa; auditoria mantém decisões anteriores; operação manual nunca é removida automaticamente.
- Execuções e evidências: filtro final de integrações Tesouro/consolidação passou em PostgreSQL 15 descartável (.NET 10.0.404), 49/49; suite unitária relacionada passou 105/105; E2E focal passou 11/11 em Chromium 1255 com dados sintéticos. Builds, typecheck e lint passaram conforme os planos.
- Limitações: cobertura incompleta do HTML e falta de ID estável impedem tratar o diff como prova. Um upload por título não permite reconciliar a coleção completa da carteira. O aceite autoriza a nova base, não prova que a leitura do HTML esteja completa.
