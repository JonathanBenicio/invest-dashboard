# Validação — correções da revisão do PR #44

- Data: 05/10/2026 (America/Sao_Paulo).
- Branch de trabalho: `feature/mock-era-parity-coverage`.
- Base revisada: `fdeba36870dd7b050c9a5f13e25ee7a8824b2a9e`.
- Estado: commit `d10f2af` publicado no PR #44; CI remoto passou nos quatro checks; os três tópicos inline foram respondidos e resolvidos.

## Evidências

| Verificação | Método e ambiente | Estado | Resultado |
|---|---|---|---|
| Reproduções backend na baseline | Docker .NET SDK 10, EF InMemory | passou (falharam como esperado antes das correções) | Reproduziram extrato compartilhado (A R$ 1.000 → R$ 1.400 após avaliação de B) e avaliação retroativa alterando R$ 1.200 para R$ 1.100. |
| Testes .NET Release | `dotnet test src/InvestDashboard.slnx --configuration Release --no-restore` dentro de Docker SDK 10, PostgreSQL 15 descartável e `INVEST_TEST_POSTGRES_CONNECTION` configurada | passou | 52 unitários passaram; 2 testes live externos ignorados; 75 de integração passaram, incluindo execução das variantes PostgreSQL e migration legada. |
| Migration de avaliações | PostgreSQL 15 descartável; atualização do último schema anterior, carga sintética, migração e leitura do novo modelo | passou | Migrou avaliação com ativo em posição única; manteve avaliações de ativo compartilhado no histórico legado; redefiniu baseline das posições ambíguas para custo, preservando o preço anterior para rollback. |
| Isolamento e avaliação retroativa | API/InMemory e API/PostgreSQL, em `PositionValuationIsolationTests` | passou | Duas contas com ticker/ativo igual mantêm valores e históricos próprios; histórico de posição alheia retorna 404; avaliação na mesma data usa o registro mais novo; registro retroativo não substitui posição/projeção vigentes. |
| Typecheck frontend | `frontend/node_modules/.bin/tsc -p tsconfig.app.json --noEmit` | passou | Sem erros de tipos. |
| ESLint direcionado | `frontend/node_modules/.bin/eslint src/pages/investments/InvestmentDetails.tsx e2e/crud.spec.ts` | passou | Sem erros. |
| Build de produção | Em `frontend/`: `VITE_USE_MSW=false VITE_API_URL=/ ./node_modules/.bin/vite build --outDir /tmp/invest-review-build` | passou | Bundle produzido. Permaneceram aviso de Browserslist desatualizado, anotação PURE de SignalR e chunk JS principal acima de 500 KB. |
| E2E frontend com MSW | Playwright 1.57 Chromium em Docker, suite completa `frontend/e2e` | passou | 17 testes passaram, incluindo cotação R$ 39,75, posição R$ 7.950, ganho R$ 1.450 e percentual 22.31%. |
| E2E real API/PostgreSQL | Playwright 1.57, API local Testing e PostgreSQL 15 descartável; suite `frontend/e2e-real-api` | passou | 11 passaram; 1 teste live Supabase ignorado por exigir uma conta de homologação. Nenhum serviço externo foi usado. |
| Checklist do projeto | `python3 .agents/scripts/checklist.py .` com alias temporário `python` para o Python 3 disponível | falhou parcialmente | Segurança, lint, schema, testes e UX passaram. SEO falhou em dois arquivos preexistentes: `frontend/src/pages/dashboard/Dashboard.tsx` e `frontend/src/pages/tools/Taxas.tsx` contêm múltiplos `<h1>`. Essas telas não foram alteradas nesta correção. Lighthouse foi ignorado por ausência de URL. |
| `git diff --check` | Checkout WSL do PR | passou | Sem erros de whitespace no diff. |
| CI do PR #44 | Execução 37403017029 no commit `d10f2af` | passou | `backend`, `docker-smoke`, `csv-import-e2e` e `frontend` passaram. |

## Limitações

O teste live Supabase e a homologação Supabase/Brapi continuam pós-merge conforme decisão do usuário. As amostras de corretoras e os requisitos de release também continuam pós-merge. O ADR de avaliações por posição permanece como proposta até revisão do PR.
