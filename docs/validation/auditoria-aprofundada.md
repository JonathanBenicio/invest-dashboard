# Auditoria aprofundada da documentação e dos contratos HTTP

- Data/fuso: 13/11/2026, America/Sao_Paulo. Branch `codex/homologacao-autenticacao-real`, SHA `f85ae79f73e641bdf19be22c9cdd08266268ea9a`, com staging e working tree preexistentes. O SHA não identifica sozinho a árvore analisada.
- Escopo: documentação em `docs` e Markdown da raiz; inventário de actions dos controllers; OpenAPI e testes HTTP isolados. Issue/PR: não criados. ADR/Story: não alteradas.
- Método: leitura estática direcionada, busca em fontes, geração isolada do schema e execução dos testes indicados abaixo. Nenhuma conclusão de correção legal, de segurança completa ou de disponibilidade externa decorre deste relatório.

## Evidências executadas

| Verificação | Método/resultado | Estado | Limitação |
|---|---|---|---|
| OpenAPI | Host temporário `WebApplicationFactory` derivado da fixture existente, `Development`, EF InMemory, workers removidos, Docker `--network none`, credenciais sintéticas; GET `/openapi/v1.json`: HTTP 200. | passou | SDK 10.0.401; o host referencia binários preexistentes. A resposta contém 47 caminhos e 58 operações de controllers. Não verifica execução de cada action. |
| Mapeamento de rotas | Extração do schema e dos atributos HTTP/rotas nos 11 controllers; 58/58 operações correspondem a actions localizadas. | passou | O [inventário](contratos-api-inventario.md) mostra parâmetros e schemas gerados e links para as actions; equivalência de contratos, autorização e efeitos ainda requer análise por fluxo. |
| Testes de integração | `docker run --rm --network none -e DOTNET_CLI_HOME=/tmp -v "$PWD:/workspace:ro" -w /workspace/src mcr.microsoft.com/dotnet/sdk:10.0 dotnet vstest tests/InvestDashboard.IntegrationTests/bin/Debug/net10.0/InvestDashboard.IntegrationTests.dll --TestCaseFilter:'FullyQualifiedName~AuthControllerTests\|FullyQualifiedName~CarteirasControllerTests\|FullyQualifiedName~InvestimentosControllerTests\|FullyQualifiedName~PortfolioGroupAuthorizationTests\|FullyQualifiedName~SimulationControllerTests\|FullyQualifiedName~TaxasControllerTests' --logger:'console;verbosity=minimal'` | passou | 39 passaram, 0 falharam, 0 ignorados; Docker sem rede, fixture InMemory/fakes. Binários já existentes, sem rebuild; timestamps das fontes não demonstram identidade byte a byte com a árvore. Não é evidência de PostgreSQL ou Supabase real. As barras invertidas na tabela representam apenas escapes Markdown; o filtro real usa `|`. |
| Índice e links | Script Python local de existência de destinos relativos Markdown e de arquivos listados no índice; `git diff --check` e `git diff --cached --check`. | passou | Nenhum arquivo de `docs` fora do índice, nenhum caminho relativo Markdown inexistente, sem erro de whitespace. Âncoras, links externos e exemplos não foram executados. |
| Preservação do staging | `git diff --cached --binary \| sha256sum` no início e ao final: `13d66e521e1ce722a8a542c2c293e356b40beb3d2c9dc19905d309ecb1dd071a`. | passou | O hash comprova que o conteúdo do diff staged permaneceu igual; não certifica outras alterações concorrentes no working tree. |
| PostgreSQL descartável e serviços externos | Não executados. | não executado | Não há resultado novo de PostgreSQL, Brapi, BCB, Supabase ou teste E2E. |

## Achados de contrato e OpenAPI

1. **Status HTTP incompletos — impacto em clientes gerados.** O schema lista somente `200` nas 58 operações. O código retorna `201` para criação de [carteira](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs#L98), [instituição](../../src/InvestDashboard.WebAPI/Controllers/InstituicoesFinanceirasController.cs#L25), [renda fixa e variável](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L154), [taxa](../../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs#L66) e [transação](../../src/InvestDashboard.WebAPI/Controllers/TransacoesController.cs#L29). [Cadastro](../../src/InvestDashboard.WebAPI/Controllers/AuthController.cs#L33) também pode retornar `202` quando exige confirmação. Não foram executados todos esses caminhos nesta extração; o status resulta da leitura dos controllers. Recomenda-se anotar e testar respostas de sucesso e erros em mudança própria da API.
2. **Autenticação ausente do schema — impacto em consumidores.** `components.securitySchemes` está ausente e nenhuma operação declara `security`; os controllers usam `[Authorize]` com exceções explícitas no [AuthController](../../src/InvestDashboard.WebAPI/Controllers/AuthController.cs). A ausência da descrição no OpenAPI não demonstra ausência de proteção em execução. Recomenda-se especificar JWT e exceções, com teste de contrato separado.
3. **Requisitos de negócio não cabem integralmente no schema.** `CriarCarteiraDto` declara `nome` como obrigatório no OpenAPI, mas [CreatePortfolioAsync](../../src/InvestDashboard.Application/Services/CarteiraAppService.cs#L56) também exige instituição resolvível e verifica associação e permissões. A obrigatoriedade contextual não deve ser inferida apenas da lista `required`. O exemplo documental foi mantido com instituição e ressalvas.
4. **Cobertura de erros e validação.** Responses geradas não enumeram 400/401/403/404/409/429/503 observáveis em diferentes caminhos; o [middleware](../../src/InvestDashboard.WebAPI/Middleware/ExceptionHandlingMiddleware.cs) e respostas dos controllers precisam ser conferidos por operação. A ausência na especificação não prova que todo endpoint possa retornar todos esses códigos.

## ADRs, nomenclatura e licença: recomendações pendentes

As decisões sobre ADRs, nomenclatura e cenários BDD foram encaminhadas posteriormente para análise nas issues [#59](https://github.com/JonathanBenicio/invest-dashboard/issues/59), [#60](https://github.com/JonathanBenicio/invest-dashboard/issues/60) e [#61](https://github.com/JonathanBenicio/invest-dashboard/issues/61), respectivamente. Sua criação não altera os estados históricos dos registros abaixo.

- **ADRs:** [adoção de ADRs](../0001-adoption-of-adrs.md) e [stack](../adrs/0001-core-tech-stack.md) usam `0001` para decisões diferentes; a [stack histórica](../0002-monorepo-tech-stack.md) e o ADR de stack se sobrepõem e ambos declaram aceitação. O histórico disponível mostra `379205f` (16/05/2026), `329fd3a` (18/05/2026), `4147ea9` (28/09/2026) e `d10f2af` (05/10/2026) entre as alterações do conjunto; não demonstra uma decisão explícita de substituição. Recomenda-se manter caminhos/status, criar mapa de identificadores por caminho e obter decisão antes de qualquer renumeração ou consolidação. [ADR de avaliação por posição](../adrs/0002-position-scoped-valuations.md) continua proposta no registro, independentemente da presença de implementação.
- **Nomenclatura:** o [plano inglês](../plan/opencode/naming-convention.md) propõe classes em inglês, enquanto o [plano PT-BR](../plan/opencode/normalização-models-dtos.md) propõe domínio e DTOs em português. O checkout mistura `Carteira`/`PosicaoInvestimento` com propriedades como `Name`/`Positions`, e rotas em inglês e português. Recomenda-se preservar identificadores e contratos existentes e seguir nomes de domínio em português para adições, conforme `AGENTS.md`; qualquer padronização abrangente exige decisão separada, avaliação de compatibilidade e migração de contratos, clientes, persistência e testes.
- **Licença:** `git log --all -- LICENSE LICENSE.md LICENSE.txt COPYING` não retornou commits, e não há arquivo de licença correspondente rastreado neste checkout. A licença do projeto não foi confirmada; nenhuma licença foi adotada nesta revisão.

## Cenários BDD

| Cenário | Comparação e recomendação |
|---|---|
| Primeira carteira | Criação existe; `IsDefault` não foi encontrado. O DTO não expõe saldo inicial e o serviço requer instituição. Confirmar se carteira principal é requisito; não presumir atendimento. |
| Resumo consolidado | Existe agrupamento por tipo/subtipo no controller; conferir valores e dados de entrada do cenário em teste de ponta a ponta antes de aceitá-lo. |
| Exclusão em cascata | Existe exclusão de carteira e há relacionamentos persistidos; a afirmação sobre metas e toda a cascata não foi demonstrada. Definir requisitos e validar no PostgreSQL descartável. |
| Isenção de ações | Existem constantes e estimativa fiscal limitada; cenário de cobrança/retenção automática não foi comprovado. Revisão fiscal especializada necessária. |
| IR de ações acima do limite | Cálculo estimado não comprova lançamento de imposto ou desconto de saldo. Definir comportamento de produto e validar regras com especialista. |
| IR de FII | Existe cálculo específico; registro ou cobrança automáticos descritos não foram comprovados. Revisar regras e critérios antes de declarar concluído. |
| Login Supabase | Há provider e gateway, mas o cenário usa rota antiga e pressupõe validação direta do token Supabase; a API emite seu JWT. Live Supabase não foi executado. |
| Acesso sem autenticação | Rotas vigentes exigem autenticação; teste de carteira sem autenticação faz parte da suíte selecionada. O caminho `/api/portfolio` do cenário é histórico. |
| Compra de ativos | Serviço e testes de posição existem; custos e taxas alteram o preço médio. Confirmar condições do exemplo, inclusive taxas e autorização. |
| Atualização SignalR | Worker publica `OnPriceUpdate` e hook assina tickers; atualização instantânea de patrimônio na tela não foi executada nem estabelecida. |

Fontes dos cenários: [carteiras](../features/01-gestao-carteiras.feature), [fiscal](../features/02-regras-fiscais-ir.feature), [autenticação](../features/authentication.feature), [portfólio](../features/portfolio.feature); [ações de carteira](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs), [serviço fiscal](../../src/InvestDashboard.Domain/Services/CalculoImpostoService.cs), [worker](../../src/InvestDashboard.Infrastructure/BackgroundWorkers/AtualizadorDadosMercadoWorker.cs).

## Limitações e pendências

- A matriz de documentos abaixo é um inventário por finalidade e não comprova comparação linha a linha de cada afirmação ou tarefa histórica. Uma comparação integral das referências e de todos os fluxos, campos, autorizações, efeitos, erros e clientes por endpoint permanece pendente; não chamar esta auditoria de exaustiva.
- O schema documenta a compilação preexistente carregada no host temporário. A execução de 39 testes também usa DLL preexistente; reproduzir após rebuild em ambiente com pacotes restaurados é necessário para afirmar correspondência com o checkout atual.
- Não foi executada revisão fiscal especializada, auditoria de segurança integral, PostgreSQL descartável nem integração externa. Não há evidência nova de funcionamento live ou de estabilidade.
- O plano original previa comparação exaustiva e matriz por afirmação e por fluxo completo. O [inventário HTTP](contratos-api-inventario.md) cobre todas as rotas e schemas gerados, mas não substitui esse rastreamento; pendências devem ser concluídas antes de declarar o plano integralmente atendido.

## Inventário de documentos

Classificação indica a finalidade documental, não validação automática do conteúdo. Para detalhes de achados e fontes, consulte as seções acima e o [relatório anterior](comparacao-documentacao-codigo.md).

| Documento | Classificação e alcance |
|---|---|
| [README.md](../../README.md) | Navegação/instruções — comparação pontual com estrutura e configuração. |
| [AGENTS.md](../../AGENTS.md) | Navegação/instruções — comparação pontual com estrutura e configuração. |
| [GEMINI.md](../../GEMINI.md) | Navegação/instruções — comparação pontual com estrutura e configuração. |
| [docs/0001-adoption-of-adrs.md](../0001-adoption-of-adrs.md) | Decisão/registro histórico — status preservado; sobreposição analisada acima. |
| [docs/0002-monorepo-tech-stack.md](../0002-monorepo-tech-stack.md) | Decisão/registro histórico — status preservado; sobreposição analisada acima. |
| [docs/API-CONTRACTS-PT-BR.md](../API-CONTRACTS-PT-BR.md) | Referência de produto — comparação pontual descrita neste relatório ou no anterior; revisão integral pendente. |
| [docs/DOMAIN-MODEL.md](../DOMAIN-MODEL.md) | Referência de produto — comparação pontual descrita neste relatório ou no anterior; revisão integral pendente. |
| [docs/INDEX.md](../INDEX.md) | Navegação/instruções — comparação pontual com estrutura e configuração. |
| [docs/README.md](../README.md) | Navegação/instruções — comparação pontual com estrutura e configuração. |
| [docs/TEMPLATE.md](../TEMPLATE.md) | Modelo legado — referência histórica, sem comportamento de produto. |
| [docs/USER-STORIES.md](../USER-STORIES.md) | Referência de produto — comparação pontual descrita neste relatório ou no anterior; revisão integral pendente. |
| [docs/adrs/0001-core-tech-stack.md](../adrs/0001-core-tech-stack.md) | Decisão/registro histórico — status preservado; sobreposição analisada acima. |
| [docs/adrs/0002-position-scoped-valuations.md](../adrs/0002-position-scoped-valuations.md) | Decisão/registro histórico — status preservado; sobreposição analisada acima. |
| [docs/codex/codex-legacy-audit.md](../codex/codex-legacy-audit.md) | Registro de configuração ou auditoria — observações locais e datas devem ser respeitadas. |
| [docs/codex/codex-migration.md](../codex/codex-migration.md) | Registro de configuração ou auditoria — observações locais e datas devem ser respeitadas. |
| [docs/codex/codex-skills-review.md](../codex/codex-skills-review.md) | Registro de configuração ou auditoria — observações locais e datas devem ser respeitadas. |
| [docs/diagrams/architecture.md](../diagrams/architecture.md) | Referência de produto — comparação pontual descrita neste relatório ou no anterior; revisão integral pendente. |
| [docs/features/01-gestao-carteiras.feature](../features/01-gestao-carteiras.feature) | Requisito histórico — classificação por cenário acima. |
| [docs/features/02-regras-fiscais-ir.feature](../features/02-regras-fiscais-ir.feature) | Requisito histórico — classificação por cenário acima. |
| [docs/features/authentication.feature](../features/authentication.feature) | Requisito histórico — classificação por cenário acima. |
| [docs/features/portfolio.feature](../features/portfolio.feature) | Requisito histórico — classificação por cenário acima. |
| [docs/historico/ORIGINAL_REQUEST.md](../historico/ORIGINAL_REQUEST.md) | Histórico — requisito original; não implica comportamento atual. |
| [docs/historico/base.md](../historico/base.md) | Histórico — requisito original; não implica comportamento atual. |
| [docs/plan/api-contracts-ptbr.md](../plan/api-contracts-ptbr.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/autenticacao-real-plano.md](../plan/autenticacao-real-plano.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/broker-csv-import-plan.md](../plan/broker-csv-import-plan.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/carteira.md](../plan/carteira.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/chat-ia.md](../plan/chat-ia.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/complete-active-plan.md](../plan/complete-active-plan.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/dotnet-backend-ddd.md](../plan/dotnet-backend-ddd.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/functional-gaps.md](../plan/functional-gaps.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/future-low-priority.md](../plan/future-low-priority.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/mock-era-parity.md](../plan/mock-era-parity.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/opencode/chat-ia.md](../plan/opencode/chat-ia.md) | Proposta — premissas e execução dependem de confirmação. |
| [docs/plan/opencode/naming-convention.md](../plan/opencode/naming-convention.md) | Proposta — premissas e execução dependem de confirmação. |
| [docs/plan/opencode/normalização-models-dtos.md](../plan/opencode/normalização-models-dtos.md) | Proposta — premissas e execução dependem de confirmação. |
| [docs/plan/plan.md](../plan/plan.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/pr-44-review-fixes.md](../plan/pr-44-review-fixes.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/real-data-integration.md](../plan/real-data-integration.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/supabase-auth-homologation.md](../plan/supabase-auth-homologation.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/plan/tesouro-direto-extrato-plano.md](../plan/tesouro-direto-extrato-plano.md) | Plano — tarefas e premissas exigem conferência antes de concluir seu estado. |
| [docs/validation/autenticacao-real-homologacao.md](../validation/autenticacao-real-homologacao.md) | Histórico — relatório ou inventário; resultados preservados com seus limites. |
| [docs/validation/comparacao-documentacao-codigo.md](../validation/comparacao-documentacao-codigo.md) | Histórico — relatório ou inventário; resultados preservados com seus limites. |
| [docs/validation/contratos-api-inventario.md](../validation/contratos-api-inventario.md) | Histórico — relatório ou inventário; resultados preservados com seus limites. |
| [docs/validation/pr-44-review-fixes.md](../validation/pr-44-review-fixes.md) | Histórico — relatório ou inventário; resultados preservados com seus limites. |
