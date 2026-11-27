# Plano — teste de integração e fluxo guiado da importação inicial do Tesouro Direto

- Estado: implementação completa; validações automatizadas focadas aprovadas. A sessão autenticada com conta real não foi conferida nesta etapa.
- Issue: [#68](https://github.com/JonathanBenicio/invest-dashboard/issues/68); iniciativa relacionada [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: N/A — teste do comportamento existente, sem decisão arquitetural nova identificada.
- Story: N/A — validação técnica de fluxo existente.
- Baseline: branch `codex/tesouro-previa-e-cotacoes`, HEAD `d7fe081`; o working tree já continha alterações locais da importação e de outro fluxo de carteiras compartilhadas. Essas mudanças foram preservadas e não houve commit nesta etapa.
- Plano canônico: `docs/plan/tesouro-teste-integracao-importacao-inicial.md`

## Objetivo, baseline e escopo

Verificar pela API a importação inicial de um título do Tesouro ainda sem posição compatível na carteira. A decisão atual permite criar a posição nova sem revisão individual dos movimentos, mesmo que a carteira contenha outros títulos. A confirmação ainda valida o saldo, a completude dos movimentos e a ausência de lançamentos manuais anteriores que pudessem duplicar o efeito.

Inclui fixture HTML sintética, conciliação, confirmação e verificação de posição e eventos oficiais, além de bloqueios relevantes. A ausência de posição compatível para o título habilita criar a posição sem revisar cada movimento, mesmo que a carteira tenha outros títulos. O servidor continua exigindo seção de posição válida, saldo e movimentos completos e ausência de movimentos manuais anteriores do mesmo ativo que possam duplicar o saldo. Não inclui dados pessoais nem alterações na carteira real.

A regra de taxa anual não informada em compra foi integrada no backend e no frontend. O backend sinaliza `camposNaoInformados`, preserva o traço e permite a confirmação se os demais campos essenciais estiverem válidos. A prévia mostra “Não informado: taxa do título (% a.a.)” sem classificar a ausência como erro.

Fluxo guiado aprovado para esta tela: etapa 1 exige HTML e carteira para iniciar a leitura, e exige data de referência válida para salvar a prévia. Quando o HTML informa mês e ano, o calendário limita as datas a esse mês. A etapa 2 resume título e vencimento, e exige escolha explícita de uma posição compatível ou da criação de nova posição quando autorizada. A etapa 3 apresenta comparação e revisão antes da confirmação. Posições existentes são apresentadas por nome completo e vencimento. Detalhes extensos ficam recolhidos até serem solicitados.

## Etapas e verificações

| Etapa/entrega | Dependências | Verificação observável | Estado e evidência |
|---|---|---|---|
| Examinar testes e fixtures existentes, inclusive o arquivo não rastreado | Nenhuma | Identificar cobertura do caminho direto e preservar alterações existentes | Concluído; adicionada cobertura de integração do caminho inicial |
| Validar a prévia atual antes da reorganização | Dependências frontend do lockfile | Executar o E2E da prévia sintética existente | Passou: teste `apresenta prévia de arquivo privado` aprovado em cópia temporária |
| Preparar HTML sintético com posição, compra, venda e taxa, incluindo traço não aplicável | Contrato real do leitor | Fixture reproduz os formatos relevantes sem dados pessoais | Concluído |
| Testar criação direta em carteira que contém outros ativos | PostgreSQL isolado e API autenticada | Criar posição sem posição compatível, preservar os outros ativos e não duplicar eventos | Teste focalizado passou para revisão legada sem vínculo e bloqueio de operação manual anterior. Teste adicional para revisão antiga vinculada passou em PostgreSQL isolado (3 testes de criação) |
| Verificar saldo, eventos oficiais vinculados e ausência de transações duplicadas | Confirmação no banco de teste | Estado persistido corresponde à fixture | Passou; teste informado pelo agente backend |
| Testar bloqueios da criação direta | Dados essenciais inválidos, posição correspondente existente, operação manual anterior e revisão legada vinculada | API bloqueia saldo incompleto, duplicação manual e vínculo antigo inseguro | Casos focados existem para saldo e manual anterior. A API bloqueia vínculo vigente; a conciliação mostra o motivo e orienta conferir a operação manual. Regressão PostgreSQL passou |
| Alinhar semântica da taxa ausente em compra | Regra aprovada pelo usuário | Leitor, DTO, prévia e confirmação usam “não informado”, sem converter para zero | Implementado; testes unitários informados como aprovados |
| Restringir opções à posição do Tesouro com título e vencimento compatíveis | Dados da prévia e posições da carteira | Resultado da conciliação lista somente candidatos compatíveis e apresenta nome, vencimento e ticker | Implementado no backend; 3 testes unitários informados pelo agente backend como aprovados em Docker |
| Implementar sequência guiada de três etapas | E2E sintético e contrato de conciliação | Leitura exige arquivo/carteira; salvar exige data; escolher posição existente leva à revisão; escolher posição nova quando permitido leva à confirmação direta, sem revisão individual; voltar refaz a busca | Concluído no fluxo da interface; conferência manual do usuário pendente |
| Recolher dados extensos e avisos da prévia | Componente de prévia existente | Tabelas, linhas não interpretadas e explicações ficam em seção expansível | Concluído; exercitado no E2E |
| Validar responsividade e navegação da prévia | Playwright com fixture sintética via harness do projeto | Conferir conteúdo e tabela em 320, 360, 767, 768, 1280, 1366 e 1920 px; menus móvel e desktop | Passou em 24/11/2026; 5 testes E2E da suíte passaram |
| Executar testes direcionados, lint, tipos e build | Dependências frontend do lockfile; runtime .NET 10 para backend | Registrar comandos, resultados, avisos e limitações | Frontend passou em instalação limpa temporária; teste unitário backend aprovado em execução anterior do agente, não reexecutado neste ambiente (dotnet ausente) |

## Critérios de aceite

- [x] A criação sem revisão individual ignora revisões antigas sem vínculo, mas bloqueia revisões antigas que vincularam uma transação manual. A UI explica como conferir a operação e, se o vínculo estiver incorreto, excluir a prévia e reenviar. API/PostgreSQL e Playwright focados passaram.
- [x] Posição e eventos oficiais persistidos verificados, sem compras/vendas duplicadas.
- [x] Histórico existente e dados essenciais inválidos não permitem confirmação inicial indevida.
- [x] Regra de taxa ausente em compra refletida na API e na prévia frontend.
- [x] Leitura bloqueada até selecionar carteira e arquivo; salvar a prévia exige uma referência válida. Criação e continuação exigem escolha explícita quando não existe posição compatível.
- [x] Posição existente não é pré-selecionada automaticamente; nome, vencimento e ticker identificam cada opção compatível.
- [x] Para posição existente, comparação/revisão aparece após continuar; voltar refaz a busca. Para posição nova sem vínculo antigo, a confirmação direta aparece após escolha explícita.
- [x] Tabelas, avisos e explicações detalhadas começam recolhidos e podem ser abertos para consulta.
- [x] Prévia tabular verificada nos viewports 320, 360, 767, 768, 1280, 1366 e 1920 via Playwright; sem overflow da página e com rolagem da tabela em telas estreitas.
- [x] Calendário de referência limita os dias ao mês/ano lidos do HTML. E2E verifica `min=2026-11-01` e `max=2026-11-30` no HTML de novembro de 2026.
- [x] Resultados conhecidos e limitações registrados; testes ignorados não apresentados como aprovados.

## Validação

| Verificação | Comando/método e ambiente | Estado | Evidência/limitação |
|---|---|---|---|
| Integração direcionada | `dotnet test tests/InvestDashboard.IntegrationTests/InvestDashboard.IntegrationTests.csproj --filter FullyQualifiedName~ConfirmacaoTesouro --verbosity minimal`, em `src/` | passou | SDK 10.0.404 em Docker, PostgreSQL 17 isolado: 3 passaram; agente reporta teste atualizado do fluxo direto passando (1 teste); 22/11/2026 |
| Testes unitários do leitor e confirmação | `dotnet test` filtrado nos projetos unitários | passou | Agente backend reporta 20 testes direcionados aprovados após alinhar expectativa e regra; não reexecutados pelo agente principal |
| Build backend | `dotnet build InvestDashboard.slnx --no-restore --verbosity minimal`, em `src/` | passou | SDK 10.0.404 em Docker: 0 erros e 0 avisos; agente backend reportou build atualizado aprovado |
| ESLint frontend | `./node_modules/.bin/eslint src/api/services/tesouro-direto.service.ts src/components/ConciliacaoTesouro.tsx src/components/PreviaTesouroDireto.tsx e2e/tesouro-direto.spec.ts`, em `/tmp/invest-groups-validation` | passou | Código executado em cópia temporária com dependências instaladas pelo lockfile; exit code 0 |
| TypeScript frontend | `./node_modules/.bin/tsc -b --noEmit`, em `/tmp/invest-groups-validation` | passou | Cópia temporária com versões compatíveis ao lockfile; exit code 0 |
| Build frontend | `npm run build`, em `/tmp/invest-groups-validation` | passou | Vite 7.3.6; avisos preexistentes de comentário puro de SignalR e bundle acima de 500 kB, sem falha |
| Fluxo guiado e responsividade | `./node_modules/.bin/playwright test --config playwright.groups.config.ts`, em `/tmp/invest-groups-validation` | passou | 5 E2E, com mocks sintéticos, sem chamar API real nem alterar carteira. Cobre requisitos de análise, seleção explícita de posição existente/criação inicial, retorno à escolha com nova consulta, revisão, tabela rolável e ausência de overflow em 320, 360, 767, 768, 1280, 1366 e 1920 px; menu móvel aberto e desktop nos dois estados. Execução intermediária encontrou o seletor de criação sumindo antes de avançar; foi corrigido. Outra execução inicial tentou abrir o menu móvel em largura desktop; o harness foi ajustado para 360 px. 24/11/2026 |
| Filtro Tesouro consolidado | .NET 10.0.404 em Docker, cópia temporária e PostgreSQL isolado | falhou parcialmente | 105 unitários passaram. 33 integrações passaram e 3 falharam em uma execução agregada: um timeout ao limpar banco durante carga concorrente e duas asserções de teste com nomes JSON incorretos. As asserções foram ajustadas. O filtro não foi reexecutado a pedido do usuário |
| Importação inicial dos HTMLs originais | Playwright em Chromium, API autenticada com sessão de teste e PostgreSQL isolado. Arquivos lidos localmente e artefatos de captura desativados | passou | Dois HTMLs reais foram importados em carteiras vazias isoladas e seus extratos oficiais apareceram nos detalhes. O ensaio usou um dia de referência dentro do mês do arquivo. [Validação](../validation/tesouro-importacao-2026-11-25.md) |

## Validação final — 26/11/2026

| Verificação | Ambiente e resultado |
|---|---|
| Integração HTTP Tesouro, Extrato e consolidação | 49 testes passaram, 0 falhas/ignorados, .NET 10.0.404 em Docker com PostgreSQL 15 descartável; cobre criação direta, data/instituição, rollback, concorrência, extrato de leitor compartilhado, encerramento, filtro consolidado e carteiras sem grupo |
| Integrações consolidadas pós-fixture | 7/7 `InvestimentosConsolidadosTests`: seis usam InMemory e um usa PostgreSQL descartável. Inclui datas diferentes entre bases Tesouro em PostgreSQL e carteiras sem grupo, vencimentos distintos e compras em anos diferentes em InMemory |
| Unitários Tesouro | 105 passaram, 0 falhas/ignorados |
| Playwright focal | 11/11 passaram com MSW e dados sintéticos em Chromium; seletores e viewport cobrem calendário, fluxo guiado, instituição divergente, exclusão de prévias, Extrato e agrupamento. O cenário de carteira sem grupo foi repetido isoladamente após a última alteração e passou 1/1 |
| Playwright com API real isolada | 1/1 `e2e-real-api/tesouro-import.spec.ts`; API .NET Testing, PostgreSQL descartável e sessão sintética. Confirma importação, venda integral e extrato sem transações duplicadas |
| Frontend lint, tipos e build | Lint 0 erros e 10 avisos preexistentes de Fast Refresh; typecheck passou com Bun/lockfile; build Vite passou com avisos de Browserslist, SignalR e chunk acima de 500 kB |
| Build API | Docker/.NET 10 passou. A compilação também foi exercitada pelas integrações |

Os testes usaram dados sintéticos, carteiras criadas em banco descartável ou interceptação MSW. Os dois HTMLs originais haviam sido processados anteriormente em carteiras de teste isoladas, mas eram de títulos diferentes e não provam reimportação real do mesmo título. A tela autenticada não foi conferida nesta rodada porque o navegador disponível permaneceu na tela de login.

## Riscos, gaps e pendências

| Item | Impacto | Mitigação/ação | Referência/estado |
|---|---|---|---|
| Fixture sintética não cobre todas as variantes do HTML real | Teste verde não prova que toda importação real funciona | Usar formatos observados sem dados privados e investigar diferenças separadamente | Pendente |
| Limite da validação com HTML original | O ensaio cobriu a importação inicial em duas carteiras vazias isoladas | Validar a troca de base com outro extrato do mesmo título | Reimportação passou com fixture sintética. Os dois originais são de títulos diferentes |
| Revisão antiga vinculada a transação manual | Criar posição sem reaplicar vínculo pode contar a operação manual e o saldo do extrato duas vezes. O bloqueio mantém a prévia pendente e orienta conferir a operação manual | Ao reabrir a conciliação, reconhecer vínculos antigos e permitir revisão compatível ou orientar recriar o lote após resolver o vínculo | Bloqueado pela API; teste PostgreSQL passou. Conferência manual pendente |
| A instalação `node_modules` do checkout está desalinhada dos manifests e contém arquivos sem permissão de escrita | Validações locais podem usar versões incompatíveis; `npm ci` direto falhou com EACCES | Dependências corretas foram restauradas numa cópia temporária limpa do projeto; reinstalação da pasta local requer corrigir ownership/limpeza com cuidado | Limitação local; checkout preservado |

## Entrega e sincronização

- Implementação: backend e frontend registrados na branch do PR #62.
- PR relacionado: #62; issue de validação: #68.
- Evidências e limitações: integrações, unitários, E2E, ESLint, typecheck e builds da rodada final estão registrados acima. Essa rodada usou dados sintéticos; a sessão autenticada pessoal não foi conferida.
- Índices e rastreamento: índice documental, track da iniciativa, Issue #68 e PR #62 relacionados.
