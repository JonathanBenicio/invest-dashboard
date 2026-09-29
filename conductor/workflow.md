# Fluxo de documentação e implementação

Este arquivo define o processo do repositório. Os modelos estão em [`templates/`](../templates/README.md); o catálogo documental está em [`docs/INDEX.md`](../docs/INDEX.md). O repositório é um monorepo com frontend React 19/Vite/TypeScript/Bun e backend .NET 10, organizado em camadas DDD, com EF Core/PostgreSQL, WebAPI e SignalR. O fluxo deve refletir apenas fatos verificados no código e nos documentos existentes.

## Rastreabilidade

Para trabalho planejado, use a cadeia **Issue → ADR → User Story → Plano → Implementação → Validação → PR**. Os artefatos são ligados por links relativos no repositório e por links da plataforma de Issues/PRs quando disponíveis. A Issue é o ponto de entrada e deve ser atualizada com os links para ADR, Story, Plano e PR imediatamente após cada artefato ser criado. Se ainda não houver URL/ID, registre `pendente` e substitua assim que conhecido.

Não invente decisões, requisitos, critérios satisfeitos ou evidências. Marque informação indisponível como `Pendente: ...`, hipótese como hipótese, e ausência de aplicação como `N/A — justificativa`. Não crie Issue, ADR, plano nem história para funcionalidade específica sem solicitação. Documentos históricos do Invest Dashboard permanecem preservados; correções de índices não alteram seu significado.

## Quando cada artefato é necessário

| Artefato | Usar quando | N/A permitido quando |
|---|---|---|
| Issue | Mudança rastreável, defeito, iniciativa, trabalho multi-etapa ou entrega via PR. | Apenas edição editorial mínima/local sem necessidade de rastreamento externo; registre a justificativa no PR ou no plano aplicável. |
| ADR | Há escolha técnica/arquitetural duradoura, trade-off, impacto em contratos, dados, segurança, integração ou operação. Registrar proposta antes de aceitar. | A mudança segue decisão vigente e não introduz decisão nova; cite o ADR aplicável ou explique por que não há decisão arquitetural. |
| User Story | Há comportamento/capacidade percebido por usuário ou ator de sistema. | Trabalho interno/documental/técnico sem comportamento de ator; descreva o resultado técnico na Issue/Plano. |
| Plano | Trabalho com múltiplas etapas, dependências, risco, mudança transversal ou validação significativa. | Correção pontual e autocontida; declare N/A e seu escopo na Issue/PR. |
| Relatório de validação | Mudanças com verificações executáveis, risco relevante, múltiplas verificações, ou quando resultados/gaps precisam ser preservados. | Mudança documental simples revisada por diff; ainda informe no PR o que foi revisado e o que não foi executado. |

Um ADR aceito registra uma decisão, não prova implementação nem validação. Registre esses estados separadamente e atualize evidências após execução.

## Preparação e execução

1. Registre branch, SHA-base, estado do working tree e arquivos/alterações preexistentes antes de editar. Preserve trabalho alheio; não use staging amplo (`git add .`/`git add -A`) e não misture mudanças não relacionadas.
2. Abra/identifique a Issue e registre o contexto observado, objetivo, escopo, exclusões e critérios verificáveis. Atualize os links da Issue imediatamente após criar cada artefato.
3. Registre ADR proposta se aplicável em `docs/adrs/`, a localização canônica existente; obtenha decisão pelos mecanismos de revisão do projeto antes de tratá-la como aceita. Escreva Story no catálogo ou em arquivo próprio ligado a ele quando houver ator e comportamento; crie Plano canônico em `docs/plan/` para trabalho não trivial.
4. Implemente em etapas ligadas aos critérios e ao plano. Respeite a arquitetura existente: Domain sem dependências, Application sobre Domain, Infrastructure sobre Domain/Application e WebAPI sobre Application/Infrastructure; frontend em `frontend/`. Confirme qualquer detalhe incerto no código/configuração em vez de assumir.
5. Execute verificações pertinentes ao escopo e registre comandos exatos, ambiente, SHA quando relevante, resultado observado e evidência. Comandos de referência deste projeto incluem backend `cd src; dotnet build InvestDashboard.slnx` e `dotnet test`, frontend `cd frontend; bun run build` (após inicializar MSW com `bunx msw init public --save`) e `bun run test`. Só reporte comandos realmente executados e confirme pré-requisitos no checkout atual.
6. Sincronize Issue, ADR/Story/Plano, relatórios, índices e PR. Se um relatório de validação separado for necessário, crie-o em `docs/validation/`; não mantenha pastas vazias. Atualize `conductor/tracks.md` para tracks acompanhados e `docs/USER-STORIES.md` quando histórias forem criadas, alteradas ou retiradas.

## Validação, evidências e gaps

Use no relatório um estado por verificação: **passou**, **falhou**, **ignorado** (intencionalmente excluído, com razão), **não executado** (não tentado) ou **capacidade ausente** (ambiente/ferramenta/recurso indisponível). Não confunda esses estados. Inclua ambiente e versões relevantes, comando ou método, resultado real, saída/link sanitizado e critério coberto. Evidência de build não substitui teste comportamental; teste unitário não demonstra integração ou isolamento que não exercita.

Registre limitações, falhas, cobertura não medida, mocks, configurações e gaps com impacto e reprodução possível. Não inclua segredos, tokens, dados financeiros pessoais ou logs sem sanitização. Relacione um gap a Issue existente quando houver; caso contrário, registre como pendência, sem inventar Issue. Pendência não pode ser descrita como resolvida. Não reduza critérios para acomodar validação indisponível.

## Índices e entrega em PR

- `docs/INDEX.md` lista documentos e aponta para suas localizações reais; `docs/USER-STORIES.md` mantém IDs estáveis, links e status/evidência sem declarar requisitos não verificados como fatos.
- `conductor/tracks.md` lista iniciativas em andamento/concluídas e aponta para o plano canônico em `docs/plan/`; não copia o conteúdo do plano. Itens sem plano conhecido devem dizer isso explicitamente.
- Atualize `README.md` para apontar ao processo e aos índices. Atualize `CONSOLIDATED_DOCS.md` somente se existir ou se uma convenção estabelecida passar a exigir o arquivo; atualmente sua existência não foi identificada.
- O PR usa [`templates/pr-template.md`](../templates/pr-template.md), lista Issue/ADR/Story/Plano/validação, escopo, evidências, limitações e pendências, e declara prontidão para revisão. Use `Refs #ID` para trabalho parcial/em andamento; `Closes #ID` apenas se todos os critérios da Issue foram satisfeitos e há evidência. Não declare prontidão ou fechamento apenas porque ADR foi aceita.
- Commits seguem [`templates/commit-rules.md`](../templates/commit-rules.md). Sincronize links e status antes de pedir revisão. Merge/deploy seguem autorização e processo próprios do projeto; este fluxo documental não os autoriza.
