# Plano — homologação de autenticação real

- Estado: bloqueado — harness e testes locais implementados; homologação live aguarda contas e acesso autenticado à URL Configuration
- Issue: [#48](https://github.com/JonathanBenicio/invest-dashboard/issues/48); etapas [#49](https://github.com/JonathanBenicio/invest-dashboard/issues/49), [#50](https://github.com/JonathanBenicio/invest-dashboard/issues/50), [#51](https://github.com/JonathanBenicio/invest-dashboard/issues/51) e [#52](https://github.com/JonathanBenicio/invest-dashboard/issues/52)
- ADR: pendente de análise caso seja proposta mudança duradoura na gestão de sessões.
- Story: [US-AUTH-001](../USER-STORIES.md#us-auth-001--autenticação-segura)
- Baseline: `develop`, `f85ae79`, em 11/11/2026; árvore sem alterações rastreadas ou não rastreadas antes desta edição. O plano anterior foi registrado na raiz no contexto de `84241c7`.
- Plano canônico: `docs/plan/autenticacao-real-plano.md`

## Objetivo, baseline e escopo

Homologar o fluxo real Supabase → API → sessão local em ambiente controlado. A [issue #46](https://github.com/JonathanBenicio/invest-dashboard/issues/46) registra entrega e uso de convite; isso não comprova login por senha, refresh, logout ou isolamento entre contas. O [roteiro](supabase-auth-homologation.md) descreve as contas, os recursos descartáveis e as variáveis locais. O [teste live](../../frontend/e2e-real-api/supabase-auth-live.spec.ts) agora contém cenários para login válido/inválido/não confirmado, claims, rotação/replay/logout e isolamento entre recursos de duas identidades; esses cenários ainda dependem de contas, IDs de teste e API local disponíveis.

Inclui testes positivos e negativos, duas identidades, sessão local, cookies e isolamento. Mudanças na relação entre sessão local e upstream dependem de análise e decisão; não presumir uma nova arquitetura. Dependências: contas controladas, dados de teste, credenciais fora do Git, aplicação executável e ambiente HTTPS para testar atributos aplicáveis.

## Etapas e verificações

| Etapa/entrega | Dependências | Verificação observável | Estado e evidência |
|---|---|---|---|
| Preparar ambiente, duas contas e dados de teste ([#49](https://github.com/JonathanBenicio/invest-dashboard/issues/49)) | Acesso às contas e configuração | Identidades confirmadas e configuração conferida sem divulgar segredos | Bloqueada — `.env.e2e.local`, contas/recursos e login manual no Dashboard não estão disponíveis |
| Validar login, `/me`, claims e falhas ([#50](https://github.com/JonathanBenicio/invest-dashboard/issues/50)) | Ambiente preparado | Login válido/inválido, conta não confirmada e indisponibilidade registrados | Parcial — provider/API determinísticos passaram; cenários Supabase live aguardam contas |
| Validar refresh, logout e isolamento ([#51](https://github.com/JonathanBenicio/invest-dashboard/issues/51)) | Duas identidades; HTTPS para atributos seguros | Rotação, reutilização, revogação, cookies e tentativas entre usuários verificadas | Parcial — sessão/cookies passaram; isolamento fake passou em InMemory e PostgreSQL isolado; live aguarda contas/HTTPS |
| Consolidar resultados e tratar gaps ([#52](https://github.com/JonathanBenicio/invest-dashboard/issues/52)) | Cenários executados | Relatório sanitizado com ambiente, resultados, limitações e eventual ADR | Parcial — [relatório local](../validation/autenticacao-real-homologacao.md); publicação/PR e cenários live pendentes |

## Critérios de aceite

- [ ] Cenários positivos e negativos de login, claims e indisponibilidade comprovados — provider/API determinísticos passaram; login Supabase válido, inválido e não confirmado live pendentes.
- [ ] Sessão local, refresh anterior, logout e cookies verificados; relação com sessão upstream documentada — testes locais passaram e a relação local/upstream está descrita; sessão real Supabase pendente.
- [ ] Isolamento de grupos, carteiras e posições entre duas identidades, inclusive mutações, demonstrado — testes com identidades fake passaram em InMemory e PostgreSQL isolado; teste live requer duas contas e seis IDs de recursos.
- [ ] Resultados e limitações publicados sem dados pessoais ou credenciais — relatório preparado nesta branch; publicação depende do futuro PR.

## Validação

Ambiente observado: branch `codex/homologacao-autenticacao-real`, HEAD `f85ae79` com alterações não commitadas. O serviço `db` foi iniciado sem recriar o volume; a API e o Postgres agora respondem, e `/health/ready` retorna 200. Não existe `frontend/.env.e2e.local`. O relatório registra o alcance e os limites de cada verificação.

| Verificação | Comando/método e ambiente | Estado | Evidência/limitação |
|---|---|---|---|
| Provider Supabase — credenciais, e-mail não confirmado e indisponibilidade | .NET SDK 10 em container: `dotnet test InvestDashboard.slnx --filter 'FullyQualifiedName~SupabaseAuthProviderTests|FullyQualifiedName~AuthControllerTests'` | passou | 7 testes unitários; respostas HTTP simuladas, sem contatar Supabase real. |
| API de autenticação — login, sessão, expiração, replay, logout e cookies | .NET SDK 10 em container: `dotnet test tests/InvestDashboard.IntegrationTests/InvestDashboard.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~AuthControllerTests'` | passou | 13 testes; fake provider, incluindo revogação upstream chamada pelo serviço e 503. |
| Isolamento em InMemory | .NET SDK 10: `dotnet test tests/InvestDashboard.IntegrationTests/InvestDashboard.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~PortfolioTenantIsolationTests|FullyQualifiedName~PortfolioGroupAuthorizationTests|FullyQualifiedName~GroupInvitationTests'` | passou | 6 testes com JWT fake; a variante PostgreSQL desse comando foi ignorada por falta de conexão configurada. |
| Isolamento relacional em PostgreSQL | Compose com runner temporário: `docker compose -f docker-compose.yml -f /tmp/invest-dashboard-postgres-test.compose.yml run --rm --no-deps postgres-test-runner` | passou | 1 teste; fixture criou e removeu banco isolado. Usa JWT fake, não autenticação Supabase. |
| Flags públicas de Auth | GET somente de leitura em `/auth/v1/settings`, com chave publicável local | passou | HTTP 200; e-mail habilitado, confirmação automática desabilitada, cadastro aberto. O endpoint não retorna Site URL nem allowlist. |
| Site URL e allowlist de redirect | URL Configuration do Dashboard do projeto | capacidade ausente | O navegador conectado está desconectado; a allowlist não foi verificada. |
| Lint frontend | `npm run lint` em `frontend/` | passou | 0 erros; 10 avisos Fast Refresh preexistentes. Executado por npm porque Bun não está instalado. |
| Smoke Supabase live | `E2E_FRONTEND_URL=http://127.0.0.1:8080 E2E_API_URL=http://127.0.0.1:5000 npm run test:api -- --grep 'Supabase Auth live smoke'` | capacidade ausente | Playwright encontrou 5 cenários e ignorou-os por falta de credenciais/IDs locais; nenhum login foi tentado. A API local está saudável. |
| Cookies de sessão no navegador HTTPS | Origem HTTPS e duas contas de teste | capacidade ausente | O endpoint live confirmou a exclusão segura do cookie; não houve login autenticado nem navegação HTTPS. |

## Riscos, gaps e pendências

| Item | Impacto | Mitigação/ação | Referência/estado |
|---|---|---|---|
| Sessão local versus upstream | Expiração e revogação podem divergir | Contrato atual documentado em [supabase-auth-homologation.md](supabase-auth-homologation.md); só propor ADR se a homologação exigir mudança duradoura | Sem mudança de arquitetura proposta |
| Contas, dados de teste e segredos | Testes live podem atingir identidades/dados inadequados | Exigir duas contas controladas, recursos sintéticos separados e arquivo local ignorado pelo Git; não imprimir dados/tokens | Pendente de preparação local |
| Redirect de convite | Supabase pode ignorar `redirectTo` se o destino não estiver permitido | Conferir URL Configuration no Dashboard e comparar com o redirect local, sem registrar a URL em relatório público | Não verificado; Dashboard conectado está desconectado |
| Contas e recursos live ausentes | Sem identidades/IDs preparados não há prova de login real nem isolamento | Preparar duas contas confirmadas, uma não confirmada e recursos sintéticos separados conforme o roteiro | Pendente de preparação local |

## Entrega e sincronização

- Branch de trabalho: `codex/homologacao-autenticacao-real`; alterações não commitadas; PR pendente. Este plano não autoriza deploy.
- Relatório/evidências: [relatório local](../validation/autenticacao-real-homologacao.md); convite registrado na #46 tem escopo limitado. Atualização/publicação das issues ainda pendente.
- Índices: `docs/INDEX.md` e `conductor/tracks.md`; sincronização dos links remotos depende da publicação deste plano.
