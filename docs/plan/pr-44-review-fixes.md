# Plano — corrigir os achados da revisão do PR #44

- Estado: commit `d10f2af` publicado no PR #44; quatro checks remotos passaram e os três tópicos de revisão foram respondidos/resolvidos.
- Data: 05/10/2026 (America/Sao_Paulo).
- PR: [#44](https://github.com/JonathanBenicio/invest-dashboard/pull/44).
- Issue: pendente; esta etapa cria somente o plano local, sem publicação externa.
- ADR: pendente de proposta para a persistência de avaliações por posição e migração de registros legados; nenhuma decisão nova é declarada aceita.
- Story: critérios abaixo restauram os comportamentos de isolamento, avaliação por extrato e cotação do [catálogo existente](../USER-STORIES.md); nenhum novo ID foi criado.
- Baseline: `320ba4996ee1ba3b3c6aa8b1e385003dcc3d69d3`, branch `feature/mock-era-parity-coverage`, cópia WSL `/home/jonathan/projetos/GitHub/invest-dashboard`; base `develop` em `fdeba36870dd7b050c9a5f13e25ee7a8824b2a9e`. Alteração preexistente: `.ai-memory.toml` não rastreado, a preservar.

## Objetivo e evidência

Corrigir três defeitos confirmados na revisão, mantendo React 19/Vite/Bun e .NET 10/DDD/EF Core/PostgreSQL. O plano segue o [workflow do projeto](../../conductor/workflow.md) e complementa o [plano de paridade](mock-era-parity.md).

| Achado | Reprodução na baseline | Resultado esperado |
|---|---|---|
| P1 — extratos compartilhados entre contas | Duas contas importam renda fixa com o mesmo ticker; após B avaliar em R$ 1.400, a projeção de A troca seu valor de R$ 1.000 por R$ 1.400. | Avaliações e projeções de A não incorporam dados de B. |
| P2 — cotação e indicadores divergentes | Em `InvestmentDetails.tsx`, 10 unidades a R$ 12 exibem preço 12 e percentual 20%, mas valor 100 e ganho zero. O objeto atualiza `currentValue`/`gain`, enquanto os cards leem `valorAtual`/`ganho`. | Valor 120, ganho 20 e percentual 20%, calculados pela mesma cotação. |
| P2 — extrato retroativo e projeção divergentes | Registrar R$ 1.200 com data recente e depois R$ 1.100 com data anterior deixa a posição em 1.100 e a projeção em 1.200. | Posição e projeção usam a avaliação vigente pelo mesmo critério cronológico. |

Na revisão, testes existentes em Docker SDK .NET 10: 52 unitários e 59 integrações aprovados; 2 live e 11 PostgreSQL ignorados. Duas reproduções adicionais de API/InMemory falharam nos valores acima. A divergência frontend foi reproduzida por execução da transformação em Node, sem navegador. Testes temporários e TRX estão em `/tmp/invest-review-44/`, não versionados; portar os cenários para o repositório antes da correção. A CI existente estava verde na baseline; isso não cobre os achados novos.

## Escopo e desenho proposto

- Avaliações de extrato pertencem à **posição/carteira**, não ao ativo global. Propor entidade/repositório dedicado para renda fixa, incluindo posição, data de observação, valor por unidade e quantidade/valor informado na observação; todas as leituras continuam autorizadas pela carteira. Cotações públicas continuam no histórico de mercado. Verificar o contrato compartilhado de histórico para não expor avaliações manuais de outra carteira.
- A avaliação vigente é a de maior data de observação; inserir um extrato anterior preserva o histórico sem substituir o valor corrente. Para datas iguais, definir critério determinístico de revisão/desempate e documentá-lo antes de implementar. Posição, projeção individual, consolidado e histórico devem usar a mesma fonte, sem perder o valor após compras, vendas ou reconstrução do ledger.
- Migração: atribuir registros `statement` legados somente quando houver associação inequívoca com uma posição. Preservar os registros ambíguos sem usá-los como extrato de outra conta; retornar projeção indisponível com motivo e solicitar nova avaliação quando necessário. Não presumir titularidade por ticker, data ou usuário autor da transação.
- Não inclui homologação externa, parser de corretora, apuração fiscal nova, merge, deploy nem alterações em dados remotos. Essas pendências permanecem nos planos originais.

## Etapas e verificações

| ID / prioridade | Entrega e arquivos principais | Dependências | Verificação observável |
|---|---|---|---|
| T1 [x] / P1 | Reproduções no teste de integração, E2E da cotação e proposta ADR documentados. | Nenhuma | Duas reproduções falharam na baseline; novos casos passaram após as correções. |
| T2 [x] / P1 | Avaliações isoladas por posição, migration/backfill conservador, autorização via position lookup e histórico público separado. | T1 | Isolamento entre usuários, acesso à posição alheia negado; migration PostgreSQL testada com ativo único e ativo compartilhado. |
| T3 [x] / P2 | Posição, gráfico e projeção usam avaliação vigente por data observada, registro e desempate estável; compras iniciais preservam a cotação da própria carteira. | T2 | Extrato 1.200, revisão na mesma data 1.250 e extrato retroativo 1.100 mantêm posição/projeção em 1.250; histórico guarda as três observações. |
| T4 [x] / P2 | Cards de `InvestmentDetails.tsx` usam o mesmo DTO recalculado pela cotação. | T1 | E2E mostra preço R$ 39,75, valor R$ 7.950, ganho R$ 1.450 e percentual 22.31% para a posição PETR4 de fixture. |
| T5 [x] / P1 | Validação local/remota concluída, evidências registradas abaixo e documentação sincronizada. | T2–T4 | Suites locais passaram; quatro checks de CI passaram no commit `d10f2af`; comentários de revisão foram respondidos e resolvidos. |

## Critérios de aceite e validação final

- [x] Avaliações, histórico e projeções isolados por posição; teste de dois usuários com mesmo ticker passou.
- [x] Avaliação vigente mantém a maior data; em datas iguais vence o registro mais novo; retroativos não alteram posição/projeção.
- [x] Migration PostgreSQL passou em banco vazio e dados legados com posição única/compartilhada; históricos legados ambíguos são mantidos, sem importação.
- [x] API/PostgreSQL validou autorização e persistência entre hosts; 75 testes de integração passaram.
- [x] E2E valida os quatro indicadores da cotação atual.
- [x] Backend `dotnet test InvestDashboard.slnx --configuration Release --no-restore` em Docker SDK 10 com PostgreSQL descartável: 52 unitários e 75 integração passaram; 2 testes live externos foram ignorados.
- [x] Typecheck, ESLint, build de produção, 17 E2E MSW e 11 E2E UI/API/PostgreSQL passaram. O E2E live Supabase foi ignorado sem conta dedicada. Bun não está instalado neste WSL; os comandos equivalentes foram executados com Node/Vite/Playwright.
- [x] `git diff --check` passou. `checklist.py` passou segurança, lint, schema, teste e UX; falhou SEO por múltiplos `<h1>` preexistentes em `frontend/src/pages/dashboard/Dashboard.tsx` e `frontend/src/pages/tools/Taxes.tsx` (ver nota abaixo; fora do escopo).
- [x] Resultados detalhados em [relatório de validação](../validation/pr-44-review-fixes.md). CI remota passou nos quatro checks; merge permanece separado desta execução.

## Riscos e conclusão

O risco principal é atribuir histórico privado legado ao titular errado. Usar migração aditiva e testada em banco descartável, preservar dados originais e preparar rollback compatível com a mudança de aplicação; não aplicar migration em ambiente existente durante esta etapa. O novo armazenamento deve sobreviver ao rebuild do ledger e manter a semântica de preço por unidade quando a quantidade mudar.

Homologação Supabase/Brapi será executada pós-merge e antes de deploy, por decisão do usuário em 05/10/2026; amostras de corretoras e requisitos de release também permanecem pós-merge. Requisitos B3 pertencem à funcionalidade futura de Ibovespa. Consulte [fechamento do plano ativo](complete-active-plan.md), [gaps funcionais](functional-gaps.md) e [backlog futuro](future-low-priority.md).
