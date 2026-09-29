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

Este projeto contém ASP.NET Core WebAPI e controladores em `src/InvestDashboard.WebAPI/Controllers/`, portanto o modelo de endpoint é aplicável. Outros templates que já existem neste diretório (BDD, E2E e agentes) são mantidos; esta tabela cobre apenas o fluxo acima.

Índices: [documentação](../docs/INDEX.md), [histórias](../docs/USER-STORIES.md) e [tracks](../conductor/tracks.md). Não crie artefatos de funcionalidades sem solicitação. Use pendências/placeholders claros quando IDs, decisões, requisitos ou evidências não forem conhecidos.
