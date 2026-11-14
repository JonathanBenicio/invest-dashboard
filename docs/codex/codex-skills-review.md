# Revisão das skills candidatas

Revisão em 11/11/2026 das 17 candidatas de [codex-legacy-audit.md](codex-legacy-audit.md), contra o projeto React 19/Vite/Bun e .NET 10 e amostras do [awesome-copilot no commit `eaaffd5`](https://github.com/github/awesome-copilot/tree/eaaffd5d020b756a2777a4b1b161d2bdc126d9a0). A classificação considera instruções, metadados, scripts presentes, proveniência local e sobreposição com capacidades disponíveis nesta sessão. Não é auditoria exaustiva de segurança nem avaliação jurídica de licença.

**Decisão inicial:** não promover cópias literais. A rodada de adaptação abaixo criou duas skills originais do projeto com base nas lacunas concretas encontradas. As cópias legadas permanecem locais e ignoradas pelo Git. Skills e plugins da sessão não são necessariamente instalados em outros ambientes.

| Candidata | Avaliação e destino |
| --- | --- |
| `api-patterns` | Guia genérico e referências; `api_validator.py` não valida controllers C#. Não portar scripts; contratos existentes e template de endpoint são a referência do projeto. Versão e licença da cópia local não identificadas. |
| `architecture` | Orientações genéricas sobre trade-offs; sobrepõe decisões e ADRs do projeto. Sem versão/licença local identificada; não copiar literalmente. |
| `database-design` | Pode informar modelagem, mas `schema_validator.py` procura Prisma/Drizzle, não EF Core. Preferir instruções de `src/AGENTS.md` e `invest-ef-migration`; origem/licença não identificadas. |
| `deployment-procedures` | Instruções genéricas de deploy; conferir workflows reais e autorização da tarefa. Sem versão/licença local identificada; não promover. |
| `dotnet-best-practices` | O conteúdo local coincide com [a skill upstream](https://github.com/github/awesome-copilot/blob/eaaffd5d020b756a2777a4b1b161d2bdc126d9a0/skills/dotnet-best-practices/SKILL.md) após normalizar finais de linha. Ela pressupõe .NET 8, MSTest, Semantic Kernel e convenções que não são padrão deste projeto; não portar literalmente. A identidade textual não comprova o histórico da cópia. A licença MIT do repositório upstream exige preservar seus avisos se houver redistribuição de cópias ou partes substanciais. |
| `supabase-postgres-best-practices` | A cópia local declara Supabase, MIT e v1.1.0, mas não acompanha arquivo de licença; o plugin Supabase disponível nesta sessão declara v1.1.1 e tem conteúdo diferente. Preferir o plugin quando disponível; não assumir que exista em todos os ambientes. |
| `nextjs-react-expert` | Declara `react-best-practices`; mistura recomendações React com Next/SSR, inclusive `next/image`, inadequadas ao Vite. `react_performance_checker.py` usa glob com chaves não expandido por `pathlib`; `convert_rules.py` escreve arquivos. Não portar scripts nem copiar o guia sem identificar licença e separar escopo. |
| `performance-profiling` | A ideia de medir antes/depois é útil, porém o runner Lighthouse legado pode retornar código 0 em erro. Sem versão/licença local identificada; usar ferramentas reais conforme tarefa e não promover o script. |
| `i18n-localization` | Guia pressupõe bibliotecas de i18n que o projeto não declara. Em teste isolado com 60 arquivos, o checker leu 50, mas informou ter analisado 60; não prova ausência de strings. Sem versão/licença local identificada; não impor internacionalização ou portar o script. |
| `web-design-guidelines` | Declara referência Vercel e depende de busca externa; há skill homônima disponível nesta sessão. Não duplicar sem comprovar origem/licença e necessidade no checkout. |
| `webapp-testing` | Runner legado pode retornar código 0 em erro e usa suposições diferentes do Playwright do frontend. `invest-ui-page`, `invest-test` e ferramentas Playwright disponíveis cobrem casos relacionados; não portar runner. A [referência upstream de teste web](https://github.com/github/awesome-copilot/blob/eaaffd5d020b756a2777a4b1b161d2bdc126d9a0/skills/webapp-testing/SKILL.md) é alternativa para comparação, não prova a origem da cópia local. |
| `code-review-checklist` | Lista genérica sobrepõe `invest-pr-review` e `invest-review`; versão/licença local não identificadas. Não promover cópia. |
| `documentation-templates` | Sobrepõe os modelos em `templates/`; não presumir licença do projeto com base em exemplos. Sem versão/licença local identificada; não promover. |
| `systematic-debugging` | Declara fonte `obra/superpowers`; o [upstream atual é MIT](https://github.com/obra/superpowers/blob/main/LICENSE), mas o commit e a proveniência da cópia local não foram comprovados. Métodos genéricos não justificam duplicação sem necessidade específica. |
| `tdd-workflow` | Prescreve TDD de forma universal; adequar testes ao pedido e risco. Sem versão/licença local identificada; não promover literalmente. |
| `testing-patterns` | Orientação genérica; `test_runner.py` supõe npm/npx ou pytest e não demonstra cobertura .NET/Bun do monorepo. Preferir `invest-test` e comandos reais do projeto; sem versão/licença local identificada. |
| `vulnerability-scanner` | Scanner legado depende de heurísticas e npm audit, sem demonstrar cobertura Bun/.NET; sobrepõe ferramentas especializadas disponíveis nesta sessão. Sem versão/licença local identificada; não promover script ou afirmar cobertura de segurança. |

## Base externa e limitações

O [awesome-copilot](https://github.com/github/awesome-copilot/tree/eaaffd5d020b756a2777a4b1b161d2bdc126d9a0) é um catálogo comunitário; seus exemplos precisam ser adaptados ao produto. Foram examinadas amostras para .NET, Playwright, PostgreSQL, React 19 e segurança, não uma comparação exaustiva do catálogo com cada arquivo legado. Por exemplo, a skill upstream PostgreSQL contém recomendações prescritivas que exigem análise contextual antes de aplicação; a de React 19 é dirigida a migração. A licença [MIT do repositório awesome-copilot](https://github.com/github/awesome-copilot/blob/eaaffd5d020b756a2777a4b1b161d2bdc126d9a0/LICENSE) não comprova automaticamente a licença ou origem dos demais arquivos locais.

Os scripts foram analisados por inspeção, pelas reproduções isoladas descritas na auditoria anterior e pelo teste sintético do checker de i18n acima; não houve varreduras do projeto ou instalação de dependências nesta revisão. Não foram executados testes comportamentais das 17 skills, e a classificação não prova que os agentes sempre escolherão a capacidade correta.

## Referência adicional: dotnet/skills

Amostras do [repositório dotnet/skills no commit `96c3d68`](https://github.com/dotnet/skills/tree/96c3d68dad2855e3ab5178aa1a262789d86a1466) oferecem alternativas específicas, distribuídas sob [MIT](https://github.com/dotnet/skills/blob/96c3d68dad2855e3ab5178aa1a262789d86a1466/LICENSE). Sua documentação distingue skills portáteis de agentes `.agent.md` do Copilot; estes não se tornam automaticamente agentes TOML do Codex.

| Recurso examinado | Aplicabilidade e cuidados |
| --- | --- |
| [optimizing-ef-core-queries](https://github.com/dotnet/skills/blob/96c3d68dad2855e3ab5178aa1a262789d86a1466/plugins/dotnet-data/skills/optimizing-ef-core-queries/SKILL.md) | Adaptada em `invest-ef-query-review`: separa inspeção estática de medição e preserva contratos do projeto. O arquivo completo não foi instalado. |
| [run-tests](https://github.com/dotnet/skills/blob/96c3d68dad2855e3ab5178aa1a262789d86a1466/plugins/dotnet-test/skills/run-tests/SKILL.md) | Adaptada em `invest-dotnet-tests` para .NET 10, xUnit 2, testes InMemory/PostgreSQL e conexão de teste. O arquivo completo, overlays e dependências extras não foram instalados. |
| [dotnet-webapi](https://github.com/dotnet/skills/blob/96c3d68dad2855e3ab5178aa1a262789d86a1466/plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md) | Adaptada em `invest-aspnetcore-api`: preserva controllers, rotas versionadas, DTOs existentes, naming JSON português, wrappers e middleware ProblemDetails. Regras universais de Minimal API/DTO não foram importadas. |
| [csharp-expert](https://github.com/dotnet/skills/blob/96c3d68dad2855e3ab5178aa1a262789d86a1466/plugins/dotnet/skills/csharp-expert/SKILL.md) | Roteador dependente de especialistas instalados e referência de marketplace; sobrepõe roteamento do projeto e requer adaptação ao host. Não instalar como substituto automático. |
| [analyzing-dotnet-performance](https://github.com/dotnet/skills/blob/96c3d68dad2855e3ab5178aa1a262789d86a1466/plugins/dotnet-diag/skills/analyzing-dotnet-performance/SKILL.md) | Acionar somente em pedido de desempenho; suas referências e heurísticas ainda não foram validadas no produto. Sempre medir gargalo antes de propor ganhos. |

Essas são referências de adaptação, não instalações dos plugins. Os arquivos abaixo foram escritos para este repositório, resumem práticas observadas e não copiam texto ou recursos do upstream. Mantêm links, commit e licença MIT para rastreabilidade.

## Skills adaptadas e validadas no projeto

O diagnóstico da solução encontrou necessidade prática em duas áreas. As novas skills em `.agents/skills/` foram testadas com o Codex em cópias temporárias dos arquivos reais e passaram pelo validador de Agent Skills:

| Skill do projeto | Base considerada | Resultado da validação |
| --- | --- | --- |
| [`invest-ef-query-review`](../../.agents/skills/invest-ef-query-review/SKILL.md) | `optimizing-ef-core-queries`, adaptada a EF 10/Npgsql, DDD, autorização por grupo/usuário, migrations/configurações e risco de confundir InMemory com PostgreSQL. | Em revisão real do `CarteiraRepository`, encontrou caminhos para investigar e declarou limites por falta de consumidores/SQL. Em controle negativo com catálogo pequeno e ordenação contratual, não inventou problema de tracking. |
| [`invest-dotnet-tests`](../../.agents/skills/invest-dotnet-tests/SKILL.md) | `run-tests`, adaptada a solução `.slnx`, .NET 10, xUnit 2, projetos unit/integration, `RequiresPostgresFact` e conexão de teste. | Em pedidos de comando sem execução, escolheu filtro xUnit pelo namespace real e identificou que testes `[RequiresPostgresFact]` dependem da variável PostgreSQL; não executou comandos quando o prompt proibiu executar. |

Validação do checkout, em container `mcr.microsoft.com/dotnet/sdk:10.0` com PostgreSQL 15 descartável e variável de teste dedicada:

- `dotnet test src/InvestDashboard.slnx --configuration Release`: build concluído; 56 testes unitários passaram, 2 foram ignorados por serem verificações externas, e 75 testes de integração passaram com PostgreSQL habilitado.
- Um teste PostgreSQL filtrado, `PositionFilteringTests.CombinedFiltersAndPagination_ReturnExpectedPositions_Postgres`, passou isoladamente antes da suite.
- Não se usou banco de desenvolvimento: a suíte rodou em container temporário separado, que foi removido após validação.

O .NET SDK instalado no Windows também foi identificado, mas a execução de teste nesse host não propagou `INVEST_TEST_POSTGRES_CONNECTION`; o método filtrado foi corretamente marcado como ignorado. Por isso, para comprovar integração, o teste foi repetido em container Linux com a variável presente. O primeiro ensaio no container apontou a porta interna errada (`55432`); corrigido para `5432`, a execução filtrada passou. O conjunto completo seguinte confirmou os resultados acima.

## Workflows e instruções ajustados

`src/AGENTS.md` agora direciona controllers HTTP para `invest-aspnetcore-api`, revisão de consultas para `invest-ef-query-review` e escolha/execução de testes para `invest-dotnet-tests`. Os agentes `invest-api`, `invest-database`, `invest-backend` e `invest-test` foram ligados às especialidades pertinentes. Não foram instalados agentes `.agent.md` do GitHub Copilot porque não são o formato de subagente nativo do Codex; os TOML locais continuam responsáveis pela delegação.

## Agentic workflows do GitHub

Os quatro caminhos em [`agentic-workflows/`](https://github.com/dotnet/skills/tree/96c3d68dad2855e3ab5178aa1a262789d86a1466/agentic-workflows) são pacotes GitHub Agentic Workflows (`gh-aw`) que geram Actions, `.lock.yml`, agentes Copilot, safe outputs e, em alguns casos, jobs que publicam comentários ou PRs. Não são workflows locais que o Codex descubra ou execute diretamente.

| Workflow upstream | Avaliação do estado atual |
| --- | --- |
| `build-failure-analysis` | Depende de rollup Azure Pipelines público e binlogs como artefatos. O CI atual usa GitHub Actions nativas; usar `invest-msbuild` para logs de build fornecidos/observáveis, sem baixar logs Azure nem publicar comentário automaticamente. |
| `msbuild-quality-review` | Revisão read-only de alterações MSBuild em PR com `pull_request_target` e GitHub tools. Para revisão Codex, `invest-pr-review` pode encaminhar arquivos MSBuild a `invest-msbuild`. Não instalar um workflow de publicação externa como configuração local. |
| `test-failure-analysis` | Requer collector CI próprio, esquema normalizado e artefato. O CI atual não publica esse contrato histórico. `invest-dotnet-tests` analisa a execução atual/saída fornecida; tendências exigem collector e decisão operacional separados. |
| `unskip-closed-tests` | Workflow mutável: consulta issues/PRs, altera `[Ignore]`, executa testes e abre draft PR. Não automatizar nem portar para acionamento implícito. |

Todos declaram MIT no repositório upstream. A licença não torna os fluxos GitHub Actions compatíveis com Codex nem dispensa a adaptação de execução e dependências.

## Agentes `.agent.md` encontrados dentro dos plugins

Confirmação do inventário do commit upstream: arquivos de agente estão mesmo aninhados em plugins, junto às skills. São agentes no formato de GitHub Copilot, com handoffs, listas de agentes subordinados e campos de ferramentas próprios do host; o README upstream diz que não são instalados pelo plugin como agentes nativos do Codex. A contagem abaixo reflete os plugins do marketplace examinados, não o número de custom-agent TOMLs fornecidos por eles.

| Plugin | Arquivos em `agents/` | Mapeamento para Codex deste projeto |
| --- | --- | --- |
| `dotnet-test` | `code-testing-builder`, `code-testing-fixer`, `code-testing-implementer`, `code-testing-linter`, `code-testing-planner`, `code-testing-researcher`, `code-testing-tester`, `test-engineer`, `test-quality-auditor`, `testability-migration` | `test-engineer`/`test-quality-auditor` se sobrepõem a `invest-test`; o pipeline de nove workers depende de agente Copilot estático e não foi copiado. `invest-test` e `invest-dotnet-tests` cobrem escolha e validação do projeto com escala apropriada. Migração de testabilidade só para pedido explícito. |
| `dotnet-test-migration` | `test-migration` | Migração de framework é tarefa separada; manter sob pedido explícito, sem roteamento automático porque a solução usa xUnit 2. |
| `dotnet-diag` | `optimizing-dotnet-performance` | Adaptado conceitualmente a `invest-dotnet-performance`, restrito a gargalo observado, baseline e medição. O scan obrigatório de duas passagens e referências ao agente de Copilot não foram transplantados. |
| `dotnet-msbuild` | `msbuild`, `build-perf`, `msbuild-code-review` | Adaptados a `invest-msbuild` e `$invest-msbuild`; MCP Binlog não está instalado. Binlogs permanecem opt-in e exigem revisão para dados sensíveis. |
| `dotnet-template-engine` | `template-engine` | Não convertido: especializado em `dotnet new`; os templates existentes do projeto são documentais e não templates .NET. |

Os outros plugins examinados não tinham diretório `agents/` no commit fixado. Foram adicionados dois TOML locais, `invest-msbuild` e `invest-dotnet-performance`; não são cópias dos agentes upstream e usam instruções próprias ao repositório.

## Inventário dos plugins .NET relevantes

O catálogo no commit examinado contém mais recursos do que as cinco skills inicialmente amostradas. A lista abaixo usa os nomes completos obtidos nos manifests e diretórios dos plugins. Nem toda skill foi lida em profundidade; decisões sobre grupos que ainda não precisaram ser promovidos são classificações de escopo, não validação comportamental.

| Plugin e versão no commit | Recursos e decisão para este repositório |
| --- | --- |
| `dotnet` 1.0.1 | `csharp-expert`, `csharp-refactoring`, `dotnet-breaking-changes`, `msbuild`, `setup-local-sdk`. O roteador/LSP duplicaria especialização local e declarações de Copilot; adaptar conceitos de C# sob demanda, sem LSP automático. Breaking changes e SDK local são tarefas pontuais. |
| `dotnet-data` 0.1.x | `create-datadriven-aspnetcore`, `optimizing-ef-core-queries`. A segunda originou a adaptação validada; a primeira é scaffolding que não corresponde a alteração vigente. |
| `dotnet-aspnetcore` 0.1.x | `dotnet-webapi`, `convert-blazor-server-to-webapp`. A primeira originou `invest-aspnetcore-api`; regras universais de DTO/Minimal API foram omitidas. Blazor é fora de escopo (frontend Vite). |
| `dotnet-test` 1.0.1 | 24 skills: `assertion-quality`, `code-testing-agent`, `code-testing-extensions`, `code-testing`, `coverage-analysis`, `crap-score`, `detect-static-dependencies`, `filter-syntax`, `find-untested-sources`, `generate-testability-wrappers`, `grade-tests`, `migrate-static-to-wrapper`, `mtp-hot-reload`, `platform-detection`, `run-tests`, `scaffold-dotnet-test-project`, `test-analysis-extensions`, `test-anti-patterns`, `test-gap-analysis`, `test-smell-detection`, `test-tagging`, `test-value-evidence`, `testability-obstacle`, `writing-mstest-tests`. `run-tests` foi adaptada. Análises de assertions e gaps podem ser ativadas quando solicitadas; geração agentic, wrappers e MTP dependem de recursos/escopos não presumidos. MSTest e scaffolding duplicam ou contradizem o estado atual (xUnit 2.9.3; projetos de teste já configurados). Migração xUnit v2→v3 somente em tarefa dedicada. |
| `dotnet-test-migration` 0.1.x | `migrate-mstest-v1v2-to-v3`, `migrate-mstest-v3-to-v4`, `migrate-nunit-to-mstest`, `migrate-vstest-to-mtp`, `migrate-xunit-to-mstest`, `migrate-xunit-to-xunit-v3`. Usar apenas quando houver pedido específico de migração de runner/framework. |
| `dotnet-diag` 0.1.x | `analyzing-dotnet-performance`, `dotnet-trace-collect`, `microbenchmarking` podem ajudar em investigação solicitada, com baseline. Android/Apple dumps e CLR activation dump não têm cenário atual comprovado. Não capturar traces automaticamente. |
| `dotnet-msbuild` 0.1.12 | 21 skills: `binlog-failure-analysis`, `binlog-generation`, `build-parallelism`, `build-perf-baseline`, `build-perf-diagnostics`, `check-bin-obj-clash`, `copy-to-output-directory`, `directory-build-organization`, `eliminate-redundant-restores`, `eval-performance`, `extension-points`, `find-repeated-work-within-one-build`, `including-generated-files`, `incremental-build`, `item-management`, `msbuild-antipatterns`, `msbuild-modernization`, `property-patterns`, `resolve-project-references`, `target-authoring`, `troubleshoot-binlog-mcp`. Especialistas para sintomas concretos de build, não regras sempre ativas. `directory-build-organization` é opção futura: há seis projetos e versões diferentes de pacotes, mas centralizar versões exige decisão da equipe. Binlogs podem capturar propriedades sensíveis; MCP de troubleshooting não está instalado. A modernização para SDK-style não se aplica: os projetos já usam esse formato. |
| `dotnet-nuget` 0.1.x | `convert-to-cpm`: avaliar se a equipe decidir centralizar versões de pacotes; não converter automaticamente. |
| `dotnet-upgrade` 0.1.x | `dotnet-aot-compat`, `migrate-dotnet10-to-dotnet11`, `migrate-dotnet8-to-dotnet9`, `migrate-dotnet9-to-dotnet10`, `migrate-nullable-references`, `thread-abort-migration`. O backend já está em .NET 10 com nullable habilitado; upgrades apenas sob pedido específico. |
| `dotnet-template-engine` 0.1.x | `template-authoring`, `template-comparison`, `template-discovery`, `template-instantiation`, `template-smart-defaults`, `template-validation`. Para tarefas sobre templates `dotnet new`; não substitui os modelos de documentação existentes. |
| `dotnet-ai`, `dotnet-blazor`, `dotnet-maui`, `dotnet-winforms`, `dotnet11`, `dotnet-experimental` | Plugins do marketplace fora do foco atual: tecnologia ausente do produto ou versão futura/experimental. |

Versões são do `plugin.json`/`version.json` no commit fixado; não significam que cada skill tenha versão independente. O repositório declara MIT. Seu README diz que agentes `.agent.md` e declarações LSP são específicos do Copilot/hosts e não são instalados como agentes nativos Codex. Mantêm-se os TOML próprios do projeto.

## Limites e próximo passo

A suite completa cobre o baseline atual, mas não prova desempenho de consultas nem todos os fluxos possíveis de delegação das skills. Para alegações de ganho EF, ainda é necessário SQL/plano e medição sobre volume representativo. O inventário cobre nomes/versões de todos os plugins relevantes; não foi feita auditoria linha a linha das referências auxiliares das 80 skills catalogadas. As duas skills locais são deliberadamente específicas e remetem às fontes upstream quando necessário.
