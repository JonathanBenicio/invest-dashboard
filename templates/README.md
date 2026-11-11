# Templates de documentação e contribuição

O fluxo e as condições para dispensar artefatos estão em [`conductor/workflow.md`](../conductor/workflow.md). Para trabalho rastreável, siga Issue → ADR (se houver decisão arquitetural) → Story (se houver comportamento de ator) → Plano (para trabalho não trivial) → implementação → validação → PR. Atualize a Issue com os links assim que cada artefato existir.

| Template | Quando usar |
|---|---|
| [issue-template.md](issue-template.md) | Contexto, escopo e critérios verificáveis do trabalho. |
| [adr-template.md](adr-template.md) | Decisão técnica/arquitetural duradoura; guardar um arquivo por ADR em `docs/adrs/`, localização canônica existente; diferencia proposta, aceitação, implementação e validação. |
| [user-stories-template.md](user-stories-template.md) | Comportamento com ator, capacidade e benefício; adicionar ID ao catálogo `docs/USER-STORIES.md`. |
| [plan-template.md](plan-template.md) | Trabalho multi-etapa, transversal ou com dependências/risco; plano canônico em `docs/plan/`. |
| [validation-report-template.md](validation-report-template.md) | Evidências estruturadas de validação e gaps; guardar em `docs/validation/` somente quando precisar de relatório documental separado. |
| [commit-rules.md](commit-rules.md) | Mensagens, trailers e preservação do staging/trabalho existente. |
| [pr-template.md](pr-template.md) | Entrega revisável com rastreabilidade, validação e limitações. |
| [endpoint-template.md](endpoint-template.md) | Documentar contrato HTTP/SignalR/API existente ou aprovado. Confirmar detalhes no código; não presumir contratos. |
| [codex-agent-template.toml](codex-agent-template.toml) | Criar um subagente personalizado do Codex em `.codex/agents/`; preencher os campos e manter o escopo delimitado. |
| [bdd-template.feature](bdd-template.feature) | Estruturar cenários BDD quando solicitado ou necessário; substituir os exemplos de agentes e MCP pelo domínio real. |
| [playwright-structure-template.md](playwright-structure-template.md) | Referência para organizar testes Playwright quando houver necessidade; adaptar à estrutura já existente. |
| [pom-e2e-template.md](pom-e2e-template.md) | Referência para Page Objects quando agregarem reuso aos testes E2E. |
| [pom-nav-menu-example.ts](pom-nav-menu-example.ts) | Exemplo de objeto de componente de navegação; adaptar seletores e comportamento à aplicação. |
| [aom-api-template.md](aom-api-template.md) | Referência para cliente de API de testes quando fizer sentido encapsular chamadas repetidas. |
| [agent-manifest-template.yaml](agent-manifest-template.yaml) | Modelo legado de outro sistema de agentes; não é configuração nativa do Codex. |

Este projeto contém ASP.NET Core WebAPI e controladores em `src/InvestDashboard.WebAPI/Controllers/`, portanto o modelo de endpoint é aplicável. Templates são referências adaptáveis: examine o código e os testes existentes antes de copiar estruturas ou exemplos.

O [manifesto YAML de agente](agent-manifest-template.yaml) descreve outro formato e não é uma configuração nativa de subagente do Codex. Os templates de documentação permanecem aqui e podem ser referenciados por instruções ou skills, sem registro automático.

Índices: [documentação](../docs/INDEX.md), [histórias](../docs/USER-STORIES.md) e [tracks](../conductor/tracks.md). Não crie artefatos de funcionalidades sem solicitação. Use pendências/placeholders claros quando IDs, decisões, requisitos ou evidências não forem conhecidos.
