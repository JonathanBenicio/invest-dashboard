# Relatório de validação — homologação de autenticação real

- Data/hora e fuso: 2026-11-11 18:54:06 (America/Sao_Paulo)
- Branch/SHA validado: `codex/homologacao-autenticacao-real` / `f85ae79` (árvore de trabalho com alterações não commitadas)
- Issue/Plano/PR: [#48](https://github.com/JonathanBenicio/invest-dashboard/issues/48), [plano](../plan/autenticacao-real-plano.md); PR pendente.
- Ambiente: Linux; Node.js 26.10.0 e npm com dependências já instaladas; .NET SDK 10.0 em container; Playwright 1.57.0. Sem Bun disponível. `frontend/.env.e2e.local` não existe. Após iniciar o serviço `db` existente, o Postgres ficou saudável, a API respondeu HTTP 200 em `/health/ready` na porta 5000 e o frontend respondeu HTTP 200 na porta 8080; o volume não foi recriado.

Os caminhos absolutos locais nos comandos abaixo foram normalizados para `"$PWD"`, com execução a partir da raiz do repositório.

## Resultados

| ID/critério | Cenário e método | Comando exato | Resultado | Estado | Evidência/gap |
|---|---|---|---|---|---|
| #50 — provider Supabase | Mapeamento de login válido, credencial inválida, e-mail não confirmado e indisponibilidade com respostas HTTP simuladas | `docker run --rm --user 1000:1000 -e DOTNET_CLI_HOME=/tmp/invest-dashboard-dotnet-home -e NUGET_PACKAGES=/tmp/invest-dashboard-nuget -v "$PWD":/workspace -w /workspace/src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test InvestDashboard.slnx --filter 'FullyQualifiedName~SupabaseAuthProviderTests|FullyQualifiedName~AuthControllerTests' --logger 'console;verbosity=minimal'` | 7 testes unitários passaram | passou | Simulado via `HttpMessageHandler`; não contata Supabase real. |
| #50/#51 — API de sessão | Login, claims, 503 sem detalhes do provider, revogação upstream chamada pelo serviço, expiração, rotação/replay, logout e cookies com `IAuthProvider` fake | `docker run --rm --user 1000:1000 -e DOTNET_CLI_HOME=/tmp/invest-dashboard-dotnet-home -e NUGET_PACKAGES=/nuget -v /tmp/invest-dashboard-nuget:/nuget -v "$PWD":/workspace -w /workspace/src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test tests/InvestDashboard.IntegrationTests/InvestDashboard.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~AuthControllerTests' --logger 'console;verbosity=minimal'` | 13 passaram | passou | Integração local com provider fake; não valida sessão Supabase real. |
| #51 — cookie no API live | Logout anônimo, sem sessão, para observar apenas a exclusão do cookie | `curl --max-time 3 -sS -D - -o /dev/null -w 'logout-http=%{http_code}\n' -X POST http://127.0.0.1:5000/api/v1/auth/logout` com os headers filtrados para não imprimir valores | HTTP 200; `refresh_token` expirado, `Secure`, `HttpOnly`, `SameSite=Lax` e `Path=/api/v1/auth` | passou | Confirma cabeçalho de exclusão no Compose Production; não executa login nem valida browser sob HTTPS. |
| #51 — isolamento em InMemory | Acesso e tentativas de mutação entre identidades fake em grupos, carteiras e posições | `docker run --rm --user 1000:1000 -e DOTNET_CLI_HOME=/tmp/invest-dashboard-dotnet-home -e NUGET_PACKAGES=/nuget -v /tmp/invest-dashboard-nuget:/nuget -v "$PWD":/workspace -w /workspace/src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test tests/InvestDashboard.IntegrationTests/InvestDashboard.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~PortfolioTenantIsolationTests|FullyQualifiedName~PortfolioGroupAuthorizationTests|FullyQualifiedName~GroupInvitationTests' --logger 'console;verbosity=minimal'` | 6 passaram; variante PostgreSQL ignorada | passou | Confirma comportamento com JWTs de teste; não substitui as duas contas Supabase. |
| #51 — isolamento relacional | Variante de isolamento em PostgreSQL com banco temporário | `docker compose -f docker-compose.yml -f /tmp/invest-dashboard-postgres-test.compose.yml run --rm --no-deps postgres-test-runner` | 1 passou | passou | Fixture criou e removeu um banco `invest_test_*`; consulta posterior confirmou 0 bancos temporários restantes. Os usuários usam JWT fake, não sessões Supabase reais. A connection string foi montada dentro do container com os valores locais do Compose e não foi registrada. |
| #49 — flags públicas de Auth | GET de leitura em `/auth/v1/settings` com a chave publicável carregada localmente; o script não imprime URL/chave | `GET /auth/v1/settings` via Python `urllib` | HTTP 200; provider de e-mail habilitado, confirmação automática desabilitada, cadastro aberto | passou | A resposta pública não tem campos Site URL/allowlist; redirect de convite segue não verificado no Dashboard. |
| Frontend — lint | ESLint completo | `npm run lint` em `frontend/` | 0 erros e 10 avisos Fast Refresh | passou | npm executado porque Bun não está instalado; avisos em componentes/router preexistentes. |
| Smoke live — coleta | Playwright coleta os cenários do filtro | `npm run test:api -- --list --grep "Supabase Auth live smoke"` | 5 cenários listados | passou | Confirma apenas descoberta/sintaxe, não comportamento. |
| #49/#50/#51 — Supabase live | Login Supabase, conta não confirmada, sessão e isolamento com duas contas | `E2E_FRONTEND_URL=http://127.0.0.1:8080 E2E_API_URL=http://127.0.0.1:5000 npm run test:api -- --grep 'Supabase Auth live smoke'` | 5 cenários ignorados pelo Playwright | capacidade ausente | Stack disponível, mas sem credenciais/IDs locais. O guard não enviou login. Nenhuma evidência de autenticação real foi obtida. |
| Cookies sob HTTPS | Comportamento de navegador em origem HTTPS | Não executado | Sem resultado | capacidade ausente | O endpoint live confirmou os atributos do cookie de exclusão; ainda falta uma navegação autenticada sob HTTPS para verificar armazenamento/envio pelo navegador. |
| Diff | Espaços em branco | `git diff --check` | Sem erros | passou | — |

## Falhas e correções

- A primeira execução de `AuthControllerTests` encontrou uma expectativa incorreta no teste de logout: a exclusão produz um `Expires` no início da época, sem `Max-Age=0`. O teste foi ajustado ao cabeçalho realmente emitido pelo controller; a execução final passou 13/13.
- No início da validação, o Postgres estava parado e a API reiniciava com falhas de conexão. `docker compose up -d db` iniciou o serviço existente sem recriar o volume; `/health/ready` passou a responder 200.
- A primeira interpretação da resposta pública de Auth tratou campos ausentes de redirect como allowlist vazia. A inspeção das chaves confirmou que `/auth/v1/settings` não retorna Site URL nem allowlist; a conclusão foi corrigida para “não verificado”. O Dashboard abriu na tela de sign-in e nenhuma autenticação foi automatizada.
- O teste live da API não foi executado porque faltam as contas/recursos de homologação. O Playwright executou apenas os guards e marcou os cinco cenários como ignorados; isso não é evidência de login válido ou inválido no Supabase.

## Resumo e cobertura

- Critérios com evidência local: mapeamento de erros do provider; flags públicas do provider de e-mail; resposta 503 sem eco de senha/detalhes; claims e `/me` com provider fake; validade/expiração local; refresh/replay/logout/cookies; cabeçalho live de exclusão de cookie; isolamento InMemory/PostgreSQL.
- Critérios sem evidência live: credenciais válidas e inválidas no Supabase, conta não confirmada real, rotação e logout ligados a conta Supabase, isolamento com duas identidades Supabase e cookies através de HTTPS.
- Configuração ainda não verificada: Site URL e allowlist; o endpoint público não os retorna e a janela do Dashboard está desconectada.
- Build/testes/análise estática: testes .NET direcionados compilaram os projetos necessários; 7 unitários, 13 testes de controller, 6 testes de isolamento InMemory e 1 teste de isolamento PostgreSQL passaram. Lint frontend passou sem erros, com 10 avisos Fast Refresh preexistentes.
- Cobertura: não medida.

## Limitações e reprodução

1. Preparar duas contas Supabase confirmadas, uma conta dedicada não confirmada e grupos/carteiras/posições sintéticos sem compartilhamento entre as identidades.
2. Preencher `frontend/.env.e2e.local` conforme `frontend/.env.e2e.example`; o arquivo é ignorado pelo Git. Não enviar credenciais, tokens, IDs de usuário ou capturas com dados pessoais.
3. Manter a API e o PostgreSQL Compose saudáveis, sem recriar o volume; depois executar `cd frontend && bun run test:api --grep "Supabase Auth live smoke"`.
4. Preparar uma origem HTTPS para validar o comportamento do navegador.

O relatório não contém credenciais, e-mails de contas, tokens, IDs de usuários, IDs de recursos ou dados financeiros. O contrato entre sessão local e Supabase continua descrito em [supabase-auth-homologation.md](../plan/supabase-auth-homologation.md); não houve mudança arquitetural que exija ADR nesta etapa.

## Conclusão

O harness e a cobertura determinística foram implementados e os testes locais relacionados passaram. A iniciativa #48 continua incompleta e bloqueada: as etapas #49–#51 dependem de duas contas Supabase, dados de teste, sessão autenticada no Dashboard para URL Configuration e uma origem HTTPS; os cenários reais ainda precisam ser executados antes do deploy. Nenhum issue remoto foi alterado e nenhum PR foi criado.
