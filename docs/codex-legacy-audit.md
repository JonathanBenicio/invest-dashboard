# Auditoria do legado `.agents`

Auditoria em 11/11/2026, baseada em inventário de caminhos, frontmatter, referências e amostras de conteúdo. A classificação não é uma auditoria linha a linha nem aprovação de segurança. Os arquivos existentes permaneceram no lugar e os conteúdos ignorados pelo Git não são garantidos em um checkout limpo. Para instruções vigentes e fluxo de migração, consulte [codex-migration.md](codex-migration.md).

## Skills e recursos

Havia 51 `SKILL.md` legados, além das quatro skills `invest-*`. Abaixo, os nomes identificam caminhos relativos a `.agents/skills/`, exceto quando observado. Classificação por escopo e evidências; manter um item como candidato não garante correção ou seleção automática apropriada.

A revisão posterior das 17 candidatas e a decisão por skill estão em [codex-skills-review.md](codex-skills-review.md).

| Tratamento | Skills legadas | Critério |
| --- | --- | --- |
| Desativadas localmente por caminho, até revisão | `app-builder`, `behavioral-modes`, `brainstorming`, `clean-code`, `frontend-design`, `intelligent-routing`, `lint-and-validate`, `mobile-design`, `modern-react-nextjs`, `parallel-agents`, `plan-writing`, `tailwind-patterns` | Instruções Antigravity, gatilhos amplos ou regras incompatíveis com stack e processo. Arquivos preservados. |
| Candidatas a avaliar e portar seletivamente | `api-patterns`, `architecture`, `code-review-checklist`, `database-design`, `deployment-procedures`, `documentation-templates`, `dotnet-best-practices`, `i18n-localization`, `nextjs-react-expert`, `performance-profiling`, `supabase-postgres-best-practices`, `systematic-debugging`, `tdd-workflow`, `testing-patterns`, `vulnerability-scanner`, `web-design-guidelines`, `webapp-testing` | Potencial para o projeto; revisar instruções, versões, scripts, licença e duplicidade antes de promover. O nome declarado de `nextjs-react-expert` é `react-best-practices`; separar regras React das específicas de Next.js. |
| Legado fora do foco principal | `app-builder/templates`, `bash-linux`, `geo-fundamentals`, `mcp-builder`, `nodejs-best-practices`, `powershell-windows`, `python-patterns`, `red-team-tactics`, `rust-pro`, `seo-fundamentals`, `server-management`, `game-development`, `game-development/2d-games`, `game-development/3d-games`, `game-development/game-art`, `game-development/game-audio`, `game-development/game-design`, `game-development/mobile-games`, `game-development/multiplayer`, `game-development/pc-games`, `game-development/vr-ar`, `game-development/web-games` | Preservar como legado local; podem ser úteis para pedidos específicos, sem presumir necessidade para o produto atual. Não foram automaticamente desativadas. |

As desativações são entradas `[[skills.config]]` no arquivo de configuração do perfil Codex local, com seleção por **caminho absoluto**, sem mudar skills homônimas em outros caminhos. Foram verificadas em uma nova execução do CLI: as 12 não constavam no catálogo inicial e as quatro `invest-*` constavam. Esse ajuste não se transfere automaticamente para outra máquina, checkout ou `CODEX_HOME`. Sessões já iniciadas podem manter o catálogo anterior. Consulte [Build skills](https://learn.chatgpt.com/docs/build-skills) para configuração e descoberta.

O uso de configuração de projeto com `skills.config` não foi adotado como solução comprovada: com Codex CLI 0.160.0, `config/read` indicou as entradas do projeto, mas `skills/list` e `debug prompt-input` ainda listaram as skills como habilitadas. Além disso, nessa leitura o array do projeto substituiu o array do usuário; uma lista de nomes poderia afetar skills homônimas e ocultar outras preferências. O teste foi removido após a observação.

Recursos relevantes para revisão futura: `api-patterns` tem referências e `api_validator.py`; `database-design` tem referências e `schema_validator.py`; `frontend-design` e `mobile-design` incluem guias e auditorias; `nextjs-react-expert` inclui nove guias e dois scripts; `supabase-postgres-best-practices` inclui referências e pode duplicar skill fornecida por plugin. `app-builder/templates` contém 13 scaffolds de outras stacks, distintos dos templates de documentação da raiz. Não promover uma skill sem seus recursos realmente necessários e validação de compatibilidade.

O frontmatter básico de 50 das 51 skills legadas foi lido com `name` e `description` por PyYAML; `lint-and-validate` apresenta descrição YAML inválida por dois pontos sem aspas. Isso não valida semântica, ferramentas ou licença.

## Scripts e catálogo de design

**Decisão:** preservar os arquivos locais como referência histórica, sem usá-los como gate de qualidade, publicar ou executar automaticamente. Usar os comandos específicos do projeto documentados nos `AGENTS.md`; registrar ausência de ferramentas. Promover um script só após adequá-lo ao monorepo, revisar dependências e licença e testar resultados positivos, negativos e indisponibilidade.

| Caminho legado | Tratamento e evidência |
| --- | --- |
| `scripts/checklist.py`, `scripts/verify_all.py` | Não usar como gate. Ambos podem retornar sucesso quando todos os checks são ignorados; chamam `python`, e o segundo referencia `dependency_analyzer.py` e `bundle_analyzer.py` ausentes. Em amostras isoladas, retornaram código 0 com 0 aprovados e, respectivamente, 6 e 15 ignorados. |
| `scripts/auto_preview.py`, `scripts/session_manager.py` | Não promover: heurísticas procuram `package.json` na raiz; preview usa npm e porta 3000, incompatíveis com a disposição e convenções atuais. Não usar arquivo PID como prova de identidade do processo. |
| `skills/performance-profiling/scripts/lighthouse_audit.py`, `skills/webapp-testing/scripts/playwright_runner.py` | Não usar via wrappers: os wrappers passam caminho do projeto como primeiro argumento, enquanto os scripts o leem como URL; em amostras com dependências ausentes os scripts retornaram JSON de erro e código 0. |
| `skills/nextjs-react-expert/scripts/react_performance_checker.py`, `convert_rules.py` | Não promover: o glob com chaves de `Path.rglob` não expande extensões; `convert_rules.py` escreve arquivos e depende de uma origem não encontrada. |
| `skills/lint-and-validate/scripts/lint_runner.py`, `type_coverage.py`, `skills/testing-patterns/scripts/test_runner.py`, `skills/vulnerability-scanner/scripts/security_scan.py` | Referência experimental, não gate: heurísticas e comandos genéricos não demonstram cobertura deste monorepo .NET/Bun. O scanner pode ler configurações; não divulgar saídas sem revisão. |
| `skills/api-patterns/scripts/api_validator.py`, `skills/database-design/scripts/schema_validator.py` | Não tratá-los como validadores dos controllers .NET e do schema EF Core; os detectores inspecionados são orientados a outras stacks. |
| `skills/frontend-design/scripts/accessibility_checker.py`, `ux_audit.py`; `skills/geo-fundamentals/scripts/geo_checker.py`; `skills/i18n-localization/scripts/i18n_checker.py`; `skills/mobile-design/scripts/mobile_audit.py`; `skills/seo-fundamentals/scripts/seo_checker.py` | Referência experimental de heurísticas; escopo e dependências devem ser verificados antes do uso. O auditor mobile inspecionado é orientado a React Native/Flutter, não prova cobertura Capacitor. |
| `.shared/ui-ux-pro-max/scripts/core.py`, `design_system.py`, `search.py` e 24 CSV em `.shared/ui-ux-pro-max/data/` | Preservar o catálogo local, sem incluí-lo na skill compartilhável ou ativá-lo automaticamente. Uma busca local sem persistência retornou JSON válido; qualidade, adequação e licença não foram avaliadas. `--persist` escreve arquivos e só deve ser usado dentro do escopo autorizado. |

## Limites de validação

A reprodução dos wrappers foi isolada em diretórios temporários, sem validar um aplicativo real. A disponibilidade de um script, configuração TOML válida ou resultado de busca não prova qualidade nem comportamento do produto. Dados antigos como `.agents/sentinel/BRIEFING.md` e `.agents/ARCHITECTURE.md` são históricos e podem estar desatualizados; confirme qualquer afirmação no código atual. `mcp_config.json` é configuração legada de outro cliente, não evidência de conexão Codex ativa.

### Delegação dos subagentes

Em 11/11/2026, uma sessão isolada do Codex CLI 0.160.0 executou `codex -a never exec --json -s read-only -C <projeto-temporário>` com pedido para delegar uma leitura de arquivo sintético aos seis papéis. O projeto temporário foi inicializado em Git e recebeu cópias dos seis TOML de `.codex/agents/`, conferidas por SHA-256 contra o checkout. Os registros dos seis subagentes identificaram o `agent_role`, continham as respectivas `developer_instructions`, mostraram a leitura do arquivo e registraram conclusão. Os seis resultados foram recebidos pelo agente principal. O teste demonstrou descoberta e execução em ambiente isolado; não demonstra qualidade das especializações em tarefas reais do projeto. Uma tentativa anterior com `--ephemeral` falhou no armazenamento de threads e não foi usada como evidência de sucesso.
