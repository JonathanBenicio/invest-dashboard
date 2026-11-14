# Configuração Codex do Invest Dashboard

O repositório usa `AGENTS.md` na raiz, `frontend/AGENTS.md` e `src/AGENTS.md` para regras por escopo. Os caminhos abaixo são relativos à raiz. O Codex descobre `.agents/skills/` e `.codex/agents/`; skills são invocadas explicitamente por `$nome` ou selecionadas conforme descrição. Subagentes são delegados quando a sessão e suas instruções permitem. Consulte [instruções de projeto](https://learn.chatgpt.com/docs/agent-configuration/agents-md), [subagentes](https://learn.chatgpt.com/docs/agent-configuration/subagents?surface=app) e [skills](https://learn.chatgpt.com/docs/build-skills).

## Arquivos compartilháveis e legado local

Historicamente `.agents/*` era ignorado e seus arquivos não estavam no Git. O [`.gitignore`](../../.gitignore) libera os diretórios de skills `invest-*` compartilháveis do projeto. Outros arquivos locais de skills, agentes, regras, scripts e workflows podem continuar ignorados; não se deve depender deles em um checkout limpo.

**Atenção:** o Git ignore não desativa skills. Nesta máquina, 13 skills locais com conflitos identificados, incluindo `dotnet-best-practices`, foram desativadas por caminho absoluto no `config.toml` do Codex usado na sessão; outras skills legadas continuam descobertas e precisam de revisão antes de serem compartilhadas. Essas entradas locais não acompanham o repositório nem afetam skills homônimas em outros caminhos. Em outro checkout ou perfil Codex, repita a verificação antes de aplicar bloqueios equivalentes. O inventário, critérios e riscos dos scripts estão em [auditoria do legado](codex-legacy-audit.md).

## Regras

| Origem local | Destino Codex |
| --- | --- |
| `rules/GEMINI.md` | Protocolo Antigravity não é importado; as regras úteis e específicas estão nos `AGENTS.md` por escopo. |
| `rules/front.md`, `rules/arquitetura-mappers.md` | `frontend/AGENTS.md` e `$invest-mapped-module`. |
| `rules/dot-net-standards.md`, `rules/database-rules.md` | `src/AGENTS.md` e `$invest-ef-migration`, com revisão dos padrões reais antes de impor regras genéricas. |

## Agentes

Os agentes TOML compartilháveis são `invest-backend`, `invest-frontend`, `invest-api`, `invest-database`, `invest-test`, `invest-review`, `invest-security`, `invest-msbuild` e `invest-dotnet-performance`. São papéis delimitados e não copiam integralmente as personas legadas. Eles herdam permissões e conexões da sessão; instruções de revisão não criam sandbox separado.

| Agentes locais anteriores | Equivalência ou tratamento |
| --- | --- |
| `backend-specialist`, `dotnet-expert` | `invest-backend`. |
| `frontend-specialist`, `react-nextjs-expert` | `invest-frontend`; instruções específicas de Next.js não se aplicam ao frontend Vite. |
| `database-architect` | `invest-database`. |
| `test-engineer` | `invest-test`. |
| `security-auditor` | `invest-security`. |
| `explorer-agent`, `debugger`, `performance-optimizer` | Delegação delimitada a agente nativo ou a um dos papéis acima quando pertinente; não há conversão literal. |
| `orchestrator`, `project-planner`, `documentation-writer`, `ide-assistant`, `devops-engineer` | Capacidades do agente principal e instruções específicas da solicitação; não há agente dedicado nesta migração. |

O [template TOML](../../templates/codex-agent-template.toml) mostra o formato nativo. O template YAML anterior permanece como artefato separado, sem ser carregado como subagente.

## Workflows

Os nomes abaixo se referem aos arquivos locais legados em `.agents/workflows/`. Não são comandos de barra do Codex.

As skills do projeto atuais abrangem API HTTP (`invest-aspnetcore-api`), consultas EF (`invest-ef-query-review`), testes .NET (`invest-dotnet-tests`) e sintomas MSBuild (`invest-msbuild`). Para API HTTP, o agente especializado é `invest-api`; SignalR não é roteado por essa skill.

| Workflow legado | Destino ou tratamento |
| --- | --- |
| `create-mapped-module` | `$invest-mapped-module`. |
| `migration` | `$invest-ef-migration`. |
| `create-page` | `$invest-ui-page`, adaptado ao TanStack Router. |
| `pr-review` | `$invest-pr-review`. |
| `development_standards` | `frontend/AGENTS.md` e `src/AGENTS.md`. |
| `brainstorm`, `plan`, `enhance`, `create` | `invest-feature-discovery` para concepção e planejamento; `invest-screen-concept` somente quando interface faz parte do escopo. Sem comandos de barra legados. |
| `debug`, `test` | `invest-debug` para diagnóstico transversal; `invest-dotnet-tests` e ferramentas de frontend para testes pertinentes. Sem comandos de barra legados. |
| `orchestrate` | Delegação nativa quando solicitada ou autorizada pelas instruções aplicáveis; sem número mínimo artificial de agentes. |
| `deploy`, `preview`, `status` | Consultar comandos e workflows reais do projeto; execução depende da solicitação e das permissões aplicáveis. |
| `verify-design`, `ui-ux-pro-max` | Verificação de interface quando pertinente; não depende do catálogo ou de ferramentas não verificadas no legado. |
| `create-modern-feature` | Específico de Next.js; não transplantado para a aplicação Vite. |

Os templates genéricos em `templates/` continuam no local original. Os templates de scaffolding dentro da skill local `app-builder` permanecem legados e não são dependência das skills compartilháveis do projeto.

## Verificação

Verifique a descoberta a partir da raiz e das áreas específicas, a sintaxe TOML, as skills e seus caminhos em uma nova sessão. Uma configuração pode ser válida sem que uma skill seja invocada em todo prompt. Rode build e testes da aplicação quando o escopo da mudança justificar e as ferramentas estiverem disponíveis; verificação estrutural da configuração não prova comportamento de aplicação nem execução de subagentes.
