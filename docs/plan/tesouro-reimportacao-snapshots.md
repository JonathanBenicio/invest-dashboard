# Plano — reimportação auditável de snapshots do Tesouro Direto

- Estado: implementação concluída e validações focadas aprovadas. A criação direta bloqueia vínculos manuais antigos com orientação de recuperação; a sessão autenticada manual do usuário ainda não foi conferida.
- Issues: [#55 — contrato/regras de conciliação](https://github.com/JonathanBenicio/invest-dashboard/issues/55), [#57 — persistência/reconciliação](https://github.com/JonathanBenicio/invest-dashboard/issues/57) e [#58 — validação ponta a ponta](https://github.com/JonathanBenicio/invest-dashboard/issues/58); iniciativa [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: [0004 — revisão auditável de divergências entre snapshots](../adrs/0004-tesouro-reimportacao-snapshots.md), aceita.
- Story: [US-IMPORT-002](../USER-STORIES.md#us-import-002--reimportação-auditável-de-snapshots-html), implementação e validações focadas concluídas.
- Baseline desta etapa: branch `codex/tesouro-previa-e-cotacoes`, commit `d7fe081`, com alterações locais da prévia e de carteiras compartilhadas preservadas. PR [#62](https://github.com/JonathanBenicio/invest-dashboard/pull/62).
- Plano canônico: `docs/plan/tesouro-reimportacao-snapshots.md`.

## Objetivo, baseline e escopo

- Problema observado: a confirmação troca a base ativa por título e preserva eventos por lote. O fluxo compara snapshots e registra o aceite da nova base; movimentos manuais sem vínculo só são substituídos no saldo após aceite explícito da comparação.
- Resultado esperado: comparar snapshots posteriores do mesmo título, apresentar diferenças como possíveis lacunas (não como prova de ausência), exigir revisão explícita antes de ativar uma nova base e preservar decisões, histórico anterior e movimentos manuais para auditoria.
- Incluído: comparação de linhas de posição e movimentos do lote anterior com o novo HTML para o mesmo título; divergências de quantidade/cobertura; operações manuais não vinculadas; revisão versionada e vinculada ao hash da comparação, usuário, carteira e lote; confirmação apenas após revisão vigente.
- Excluído: inferir que uma posição inteira da carteira foi encerrada porque um único HTML de título não a contém; remover transações ou posições sem decisão explícita; assumir que dois cartões com mesma data/valor são o mesmo evento; importar XLSX.
- Dependências: extrato anterior associado à base ativa, layout/semântica das linhas HTML, identidade natural de eventos/lotes, autorização e versão da carteira; migration da revisão já existe e requer validação PostgreSQL para concorrência/rollback.

## Regras de produto e limites conhecidos

- **Instituição do extrato e da carteira, decisão aprovada:** extrair e preservar o nome do HTML separado da taxa. A prévia mostra o nome extraído e a instituição vinculada à carteira. Se a identificação for segura e divergir, bloquear a importação e solicitar outra carteira. A API revalida ao gravar a prévia e confirmar lotes pendentes. O cadastro da carteira não muda. Identificação ausente ou inconclusiva não é tratada como correspondência.

- **Mês e ano declarados no HTML:** a amostra do portal traz “Posição em Novembro/2026”, sem dia no cabeçalho. Quando o leitor reconhecer um mês e ano únicos nesse campo, mostrar o calendário limitado ao primeiro e último dia do mês e pedir a data de referência do saldo. Validar no servidor que a data pertence ao mês e ano do HTML. Sem cabeçalho reconhecível ou com valores conflitantes, manter o calendário sem limite por mês e indicar que a referência foi informada pelo usuário. Datas de movimentação, vencimento e modificação do arquivo não substituem a referência do saldo. Para extratos do mesmo mês, a ordem ainda depende do dia declarado.
- **Mensagens propostas:** se a referência divergir, exibir “O extrato informa posição em novembro de 2026. Escolha uma data desse mês e ano.” Se faltar o dia, exibir “O extrato informa o mês e o ano da posição. Informe o dia de referência do saldo.”

- **Ordem dos extratos:** para a mesma carteira e título, somente um HTML com data de referência estritamente posterior à da base ativa pode substituí-la. A data de referência corresponde ao saldo do extrato. Data igual ou anterior deve bloquear a confirmação e preservar a base ativa. Revalide a ordem dentro da confirmação, inclusive se a base mudar depois da prévia.

- **Encerramento por venda integral (decisão aprovada):** partir da última base confirmada da mesma carteira e título. Reconciliar as compras e vendas posteriores à data-base até a nova data de referência, sem contar lançamentos vinculados em duplicidade. Se houver venda registrada e a quantidade reconciliada chegar exatamente a zero com evidência suficiente, a confirmação do lote encerra a posição e preserva o histórico. Se a posição apenas não aparecer, se o saldo for residual ou negativo, ou se houver dados incompletos ou movimentos ambíguos, preservar a base anterior e indicar o motivo para revisão. Cobranças monetárias não alteram quantidade.

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
| [x] Validar rollback e concorrência antes das novas alterações | PostgreSQL isolado e testes em `d7fe081` | Falha na troca mantém a base anterior. Confirmações concorrentes não duplicam base ou eventos. Movimentos posteriores são preservados | 3 integrações passaram no baseline de 25/11/2026 (agente backend) |
| [x] Reconhecer mês, ano e instituição antes de salvar a prévia | Estrutura dos dois HTMLs observados | Mês/ano lidos, calendário limitado ao mês e instituição mostrada | Os dois HTMLs originais foram importados em carteiras de teste compatíveis. Divergência sintética bloqueou na UI; integração HTTP correspondente foi adicionada, sem reexecução a pedido do usuário |
| [x] Confirmar encerramento comprovado por venda integral | Base ativa, seção de posições reconhecida, eventos completos e revisados | Encerramento conserva extrato e histórico. Ausência sem venda preserva o saldo. Compra posterior é reaplicada pelo ledger | Testes focados de API/PostgreSQL e E2E sintético passaram. Não alegar que o filtro agregado completo passou |
| [x] Revisar posição não observada sem zerar saldo | Snapshot anterior e HTML sem linhas de posição interpretáveis | API retorna quantidade proposta indisponível e mantém confirmação bloqueada | Teste de integração do caso passou no filtro Tesouro anterior; conferir novamente manualmente após as mudanças recentes |
| [x] Exibir linhas da prévia em tabelas | Campos brutos por posição e movimentação | Preservar valores originais, marcar leitura e conter rolagem | Playwright validou a prévia em telas móveis e desktop; execução de referência em 24/11/2026 |
| [x] Validar fluxo integrado após as alterações mais recentes | API, UI e PostgreSQL isolado | Cobrir criação direta, revisões antigas, exclusão, vínculo manual, rollback, replay e concorrência | Filtro final Tesouro/consolidação passou 49/49 em PostgreSQL descartável. Playwright focal passou 11/11, incluindo criação, exclusão, instituição divergente, calendário, conciliação e extrato. Sessão autenticada real não conferida |

## Critérios de aceite

- [x] A prévia distingue instituição do HTML, instituição da carteira e valor de taxa. Divergência identificada bloqueia a gravação e pede outra carteira; a API aplica a mesma regra. A integração cobre XP versus Banco do Brasil e preserva o cadastro da carteira; testes não foram reexecutados nesta solicitação.

- [x] Com mês e ano reconhecidos no HTML, o calendário restringe datas ao mês e o servidor recusa outro mês/ano. O caso sem mês/ano reconhecido permite entrada manual. HTMLs originais e teste HTTP do parser/data passaram em validações anteriores.

- [x] Extratos com data igual ou anterior são recusados, e a confirmação revalida a base dentro da transação. Testes PostgreSQL de rollback/concorrência passaram antes da alteração recente de criação direta.

- [x] Ao reimportar o mesmo título após uma base ativa, a prévia mostra o saldo anterior e o saldo proposto e lista possíveis linhas/eventos ausentes; evidência: testes direcionados de serviço/API com dois snapshots sintéticos, commit `3adef8c`.
- [x] Correspondências naturais ambíguas ficam para escolha do usuário e preservam múltiplos eventos iguais; evidência: `ComparadorSnapshotTesouroTests.ApresentaCadaEventoAnteriorNaoObservadoSemApagarDuplicatas`, 21 testes direcionados passaram em 18/11/2026.
- [x] Movimento manual sem par bloqueia a confirmação até o usuário revisar e aceitar explicitamente a nova base; transação e decisão ficam preservadas para auditoria. Evidência: integração PostgreSQL e E2E direcionada passaram em 18/11/2026.
- [x] Dada uma base ativa e uma seção de posições ausente/inválida, o sistema informa que a posição não foi observada, preserva a base e bloqueia a confirmação. Caso coberto por integração PostgreSQL anterior.
- [x] Revisões de snapshot ficam vinculadas a hash, autor, lote e versão; alteração de conteúdo invalida o aceite. Testes PostgreSQL e E2E direcionados anteriores passaram.
- [x] A base anterior permanece ativa até confirmação serializável; falhas durante a transação preservam atomicidade. Testes PostgreSQL de baseline passaram antes dos últimos ajustes da criação direta.
- [x] Ausência de posição/evento não apaga dados automaticamente nem encerra outros títulos a partir de um arquivo individual.
- [x] A venda integral comprovada encerra e preserva trilha. Testados venda integral, ausência sem venda, seção inválida, compra manual posterior e manual sem vínculo em testes anteriores. A revisão antiga vinculada é bloqueada e teve teste de regressão aprovado.
- [x] Snapshot com posição válida e nenhum movimento pode ser revisado e confirmado sem movimentos fictícios. Evidência: teste PostgreSQL no commit `7d7207a`.
- [x] Reimportar o mesmo lote continua idempotente; operações posteriores à nova base continuam aplicadas uma única vez. Evidência: `Confirmacao_RevalidaRevisoesEPreservaMovimentacaoPosteriorSemDuplicarEventos`.

## Validação

As linhas históricas abaixo preservam execuções anteriores. A validação final mais recente consta na seção de 26/11/2026 e passou no filtro focado; não confundir com a execução agregada antiga que falhou por timeout e assertions desatualizadas.

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
| Rollback e concorrência ao trocar snapshot posterior (baseline) | `dotnet test` filtrado em `ConfirmacaoTesouroControllerTests`, SDK em Docker e PostgreSQL 15 isolado | passou | 3 testes de rollback, concorrência e movimentação posterior passaram em 25/11/2026 antes da implementação de encerramento. A reexecução final após o encerramento permanece pendente |
| E2E de reimportação | `frontend/e2e/conciliacao-tesouro.spec.ts`, Playwright 1.65.0, Chromium 1255, Vite na porta 5174 | passou | Dois cenários passaram; a validação manual com a conta real do usuário ainda está pendente |
| Dois HTMLs originais pela interface | Playwright com API autenticada e PostgreSQL 15 isolado, sessão de teste e capturas desativadas | passou | Identificou mês/ano e instituição e confirmou os dois originais em carteiras vazias separadas em 25/11/2026. A reimportação foi exercitada com dados sintéticos do mesmo título |
| Reimportação com encerramento pela interface | `e2e-real-api/tesouro-import.spec.ts` com API autenticada e PostgreSQL 15 isolado | passou | Confirmação inicial seguida por reimportação posterior com venda integral. Posição ficou encerrada e extrato ativo mostrou venda bruta e cobrança separadas. O ledger permaneceu sem transações duplicadas. [Validação](../validation/tesouro-importacao-2026-11-25.md) |
| Filtro agregado após alterações de encerramento | .NET 10.0.404 em cópia temporária e PostgreSQL isolado, filtro `FullyQualifiedName~Tesouro` | falhou parcialmente | 105 unitários passaram. Na execução agregada 33 integrações passaram e 3 falharam, por um timeout no cleanup PostgreSQL durante carga concorrente e duas asserções que esperavam nomes JSON incorretos. As asserções foram corrigidas, mas o filtro não foi reexecutado a pedido do usuário |

## Validação final — 26/11/2026

Integração .NET 10.0.404 com PostgreSQL 15 descartável: 49/49 aprovados, 0 falhas e 0 ignorados no filtro Tesouro/consolidação. Unitários Tesouro, snapshot e conciliação: 105/105 aprovados. Playwright sintético em Chromium: 11/11 aprovados. Inclui leitura por membro de carteira compartilhada apenas para lote confirmado. Build e typecheck são registrados no plano de importação inicial. O navegador isolado permaneceu na tela de login, portanto não houve conferência manual autenticada.

Após essa rodada, o cenário `ReimportarUmTituloPreservaOutroTituloDaMesmaCarteira` passou 1/1 em PostgreSQL descartável. Ele verifica que a reimportação de um título preserva a base ativa, o lote, o saldo e o preço do outro título Tesouro na mesma carteira. A primeira execução exigiu ajuste da fixture para incluir o aceite explícito do snapshot.

## Riscos, gaps e pendências

| Item | Impacto | Mitigação/ação | Referência/estado |
|---|---|---|---|
| HTML não tem ID estável e a cobertura temporal não é comprovada | Falso pareamento ou falso alerta de ausência | Mostrar divergência como hipótese, preservar multiconjunto e pedir revisão | Issues #55/#58 abertas |
| Um HTML representa um título, não a carteira inteira | Não é possível detectar automaticamente outros títulos ausentes | Não inferir remoção; planejar lote de vários arquivos se a carteira inteira precisar ser comparada | Limite atual da interface |
| Movimento manual sem par pode ter o efeito substituído sem ciência | A nova base pode retirar o efeito da operação do saldo atual | Exigir aceite explícito da nova base, registrar os IDs dos manuais e manter a transação original | Integração PostgreSQL e E2E passaram |
| Revisão antiga já vinculada em prévia pendente | Ignorar o vínculo pode manter o efeito da operação manual no ledger e duplicar o saldo; um bloqueio sem recuperação pode prender a prévia | API detecta `TransacaoId` legado e bloqueia a criação direta. A UI deve oferecer revisão compatível ou instrução para recriar a prévia depois de corrigir o vínculo. Adicionar regressão | API bloqueia e informa caminho para revisar a operação; teste de regressão passou. Conferência manual pendente |
| Revisão pode ficar obsoleta após concorrência | Confirmação aceita dados que o usuário não revisou | Hash do diff + versão da carteira + transação serializável | Filtro final passou em PostgreSQL isolado, incluindo os cenários de concorrência da suíte focada |
| Ausência de posição no HTML pode parecer saldo zero | O usuário poderia interpretar ausência como encerramento | Manter `posicao_nao_observada` sem saldo zero quando faltar evidência; habilitar encerramento apenas pela regra de venda integral aprovada | Implementado e validado em cenários sintéticos; conferir no navegador |
| Um HTML representa um título, não a carteira inteira | Não é possível detectar automaticamente outros títulos ausentes | Não inferir remoção de outros títulos a partir de um arquivo individual | Limite atual da fonte/contrato |

## Entrega e sincronização

- Implementação/PR/commits: PR #62 contém upload, prévia tabular, confirmação inicial, comparação de snapshots, revisão auditável e sinalização de posição não observada nos commits `3adef8c`, `43d8c14`, `26aa337` e `aa21a9c`.
- Relatório/evidências: resultado da criação inicial está registrado em [Issue #58](https://github.com/JonathanBenicio/invest-dashboard/issues/58).
- Limitações atuais: o encerramento por venda integral foi coberto com extrato sintético do mesmo título. Os dois HTMLs originais disponíveis são títulos diferentes e validam a importação inicial, não uma reimportação real do mesmo título. A validação autenticada manual da conta do usuário permanece pendente.
- Índices: ligar a Issues #55, #57, #58, Story US-IMPORT-002, ADR-0004, `docs/INDEX.md` e `conductor/tracks.md`.
