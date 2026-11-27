# Validação da importação do Tesouro Direto em 25 e 26/11/2026

- Branch `codex/tesouro-previa-e-cotacoes`, baseline `d7fe081` e alterações locais da iniciativa.
- Iniciativa [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53), testes de importação [#68](https://github.com/JonathanBenicio/invest-dashboard/issues/68), Extrato detalhado [#69](https://github.com/JonathanBenicio/invest-dashboard/issues/69) e PR [#62](https://github.com/JonathanBenicio/invest-dashboard/pull/62).
- Planos de [importação inicial](../plan/tesouro-teste-integracao-importacao-inicial.md), [reimportação](../plan/tesouro-reimportacao-snapshots.md) e [Extrato detalhado](../plan/tesouro-extrato-detalhado.md).
- Ambiente local WSL, Chromium 1255, API .NET em Docker e PostgreSQL 15 exclusivo dos testes. A suíte com API usa uma sessão de teste assinada conforme o método já adotado para o CSV. Dados de teste de navegador simulado e carteira isolada são diferentes da carteira do usuário.

## Resultados

Em 26/11/2026, foram executados cenários adicionais com PostgreSQL isolado e Chromium. A exclusão de prévia passou em 3 testes de API, incluindo autorização, lote confirmado e concorrência com confirmação. A criação de título novo em carteira com outros ativos passou em 2 testes de API, incluindo revisões antigas e bloqueio de lançamento manual anterior. O Playwright passou no fluxo de exclusão pela lista e pela prévia aberta, cancelamento, erro recuperável e ausência da opção em lote confirmado, nas larguras de 320, 360 e 1280 px. O filtro anterior do Tesouro passou com 105 testes unitários e 37 de integração. Os testes foram executados com dados sintéticos.

| Verificação | Método | Estado | Evidência |
|---|---|---|---|
| Importação dos dois HTMLs originais | Playwright com a API e o PostgreSQL isolados, conta de teste e uma carteira vazia por arquivo | passou | A tela reconheceu a referência e a instituição, exigiu o dia, confirmou a importação e exibiu o extrato oficial nos detalhes da posição criada. O dia usado pertence ao mês do HTML e foi escolhido para o ensaio. |
| Gravação e reimportação | `playwright test` com `e2e-real-api/tesouro-import.spec.ts` | passou | Carteira vazia recebeu uma posição e eventos oficiais. Um extrato sem linhas de posição e sem venda manteve o saldo e a confirmação bloqueada. Uma venda integral zerou o saldo após revisão explícita. A consulta mostrou venda bruta e cobrança separadas, variação de quantidade e posição encerrada. A API de transações retornou lista vazia. |
| Criação direta quando a carteira contém outros títulos | Código e container atualizados para decidir pela falta de posição compatível | validação manual pendente | A confirmação direta não exige revisão individual dos movimentos. O API ainda bloqueia caso encontre movimentações manuais anteriores do mesmo ativo que possam duplicar o saldo. O usuário fará a conferência. |
| Criação de novo título em carteira com outras posições | Fluxo direto agora considera a existência de posição compatível, não o estado global da carteira | não reexecutado | O código e o fluxo da interface foram atualizados. O usuário pediu validar manualmente antes de outra rodada automatizada. |
| Regressão CSV | `playwright test` com `e2e-real-api/csv-import.spec.ts` | passou | As operações válidas foram gravadas e a linha inválida foi identificada. |
| Fluxo guiado e Extrato detalhado com API simulada | `playwright test --config playwright.tesouro.config.ts --workers=1`, configuração temporária sobre os cenários `*tesouro*.spec.ts` | passou antes da mudança do campo de data | 10 cenários no Chromium. Incluiu bloqueio de instituição divergente com troca para carteira compatível, revisão, retorno à escolha e atualização do extrato após mudança de base. |
| Tipos e lint do frontend | `tsc -b --noEmit` e ESLint direcionado aos componentes e testes alterados em cópia com dependências do lockfile | passou | Verificação de tipos e ESLint sem erros. |
| Build do frontend | `docker compose build frontend` | passou | Imagem compilada. Avisos conhecidos de Browserslist, SignalR e pacote final acima de 500 kB. |
| Calendário da referência mensal | E2E `tesouro-direto.spec.ts`, input HTML `type=date` | passou | A fixture de novembro de 2026 verificou `min=2026-11-01` e `max=2026-11-30`. |
| Build da API isolada | `docker build -t invest-dashboard-tesouro-e2e:local -f src/InvestDashboard.WebAPI/Dockerfile .` | passou | API iniciou no ambiente de teste e respondeu ao health check antes da validação em navegador. |
| Rollback e concorrência antes da alteração de encerramento | Filtro de `ConfirmacaoTesouroControllerTests` em PostgreSQL isolado | passou | 3 integrações de baseline, incluindo preservação de lançamentos posteriores. |

## Falhas reproduzidas e corrigidas

- Nos HTMLs originais, a primeira leitura reconheceu a instituição, mas o mês e o ano ficaram ausentes. O elemento do período incluía um elemento filho com o valor do saldo. A correção limitou a leitura ao texto do cabeçalho e os dois arquivos passaram na repetição.
- Após receber 409 ao paginar um extrato cuja base havia mudado, voltar à primeira página ainda podia mostrar a versão guardada na memória da tela. A ação agora refaz a consulta e passou no cenário de navegador.
- Três cenários de regressão usavam textos da tela anterior. As rotas e seletores foram alinhados ao fluxo em etapas e os cenários passaram.

## Limites e pendências

- A decisão está implementada. O teste HTTP de divergência de instituição passou no filtro de integração final, sem criar lote nem alterar carteira.
- O caso de encerramento usa uma amostra sintética com a estrutura reconhecível de um extrato e eventos controlados. Os dois originais são de títulos diferentes e não demonstram uma sequência real de reimportação do mesmo título.
- Os arquivos originais ficaram fora dos artefatos versionados. As capturas de tela e traces foram desativados para o ensaio com os arquivos privados.
- A regra de produto bloqueia salvar e confirmar quando a instituição identificada diverge da carteira. O cenário passou em integração e Playwright sintético. A sessão autenticada manual com a conta do usuário permanece pendente.
- A criação direta sem revisão depende da ausência de posição compatível, mesmo quando a carteira contém outros ativos. Integração e Playwright sintético passaram.
- Alterações desta etapa permanecem no checkout local. Esta página ainda está pendente de publicação no PR.

## Validação focada de importação e agrupamento — 26/11/2026

| Verificação | Comando/ambiente | Estado | Resultado |
|---|---|---|---|
| Integrações Tesouro e consolidação | .NET 10.0.404 em Docker, PostgreSQL 15 descartável, filtro `FullyQualifiedName~Tesouro\|FullyQualifiedName~InvestimentosConsolidadosTests` | passou | 49 passaram, 0 falhas e 0 ignorados. O conjunto inclui testes InMemory e testes PostgreSQL |
| Integrações de consolidação após adicionar posições sem grupo e cenários de datas | `dotnet test tests/InvestDashboard.IntegrationTests/InvestDashboard.IntegrationTests.csproj --configuration Release --filter FullyQualifiedName~InvestimentosConsolidadosTests --verbosity minimal`, PostgreSQL 15 descartável | passou | 7 passaram, 0 falhas/ignorados. São seis testes InMemory, incluindo posições sem grupo e datas de vencimento/compra, e um em PostgreSQL com datas distintas entre bases do Tesouro |
| Unitários de leitor, conciliação e snapshots | .NET 10.0.404 em container, filtro Tesouro/Snapshot/Concilia | passou | 105 passaram, 0 falhas/ignorados |
| Playwright focado | `PLAYWRIGHT_BROWSERS_PATH=/tmp/invest-playwright-browsers bunx playwright test --config playwright.invest.config.ts --workers=1`, Chromium 1255, MSW sintético, Vite 5174 | passou | 11/11; depois da inclusão do cenário de carteira sem grupo, o spec consolidado também passou isoladamente 1/1 |
| Playwright com API real isolada | `bun run test:api -- e2e-real-api/tesouro-import.spec.ts --workers=1`, API Testing e PostgreSQL 15 descartável | passou | 1/1 com sessão e JWT sintéticos. Importa, confirma venda integral e consulta o Extrato sem duplicar transações |
| ESLint | `bun run lint`, frontend, dependências do lockfile | passou | 0 erros e 10 avisos Fast Refresh preexistentes |
| TypeScript e build frontend | Bun 1.3.4, dependências instaladas do `bun.lock`, container temporário | passou | `bunx tsc -b --noEmit` e build Vite passaram; avisos de Browserslist, anotação PURE da SignalR e chunk acima de 500 kB |
| Build da API | `docker compose up -d --build --no-deps api frontend` | passou | As imagens da API e do frontend foram compiladas e os containers iniciaram |
| Segurança/contratos | Revisões `invest-security`, `invest-api`, `invest-database` e `invest-review` | passou com limite | Sem vulnerabilidade ou falha funcional adicional encontrada. Risco de consultas por carteira e agregação completa sem impacto medido. |

Os testes automatizados usaram fixtures sintéticas, MSW e bancos descartáveis; nenhuma carteira real foi modificada. A importação inicial dos dois HTMLs originais em carteiras de teste distintas foi validada anteriormente. Não houve reimportação real do mesmo título nem conferência autenticada com a conta do usuário nesta rodada.
