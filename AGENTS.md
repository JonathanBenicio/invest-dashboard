# Invest Dashboard — instruções para agentes

Monorepo com frontend React 19, Vite, TypeScript e Bun em `frontend/`, e backend .NET 10 em `src/`. Leia também o `AGENTS.md` da área alterada. Confirme detalhes no código antes de fazer suposições.

## Comandos

- Stack: `docker compose up -d --build`.
- Frontend: `cd frontend && bun install && bun run dev`; build: `cd frontend && bun run build`; lint: `cd frontend && bun run lint`; E2E: `cd frontend && bun run test`.
- Backend: `cd src && dotnet restore && dotnet build InvestDashboard.slnx`; executar API: `cd src && dotnet run --project InvestDashboard.WebAPI`; testes: `cd src && dotnet test`.
- Android: `cd frontend && bun run mobile:android`; diretório nativo é gerado e ignorado pelo Git.
- Escolha as verificações proporcionais aos arquivos alterados; informe as que não puder executar e a razão. Não execute instalações ou serviços sem necessidade para a tarefa.

## Convenções gerais

- O frontend usa Bun (`bun@1.3.4` em `frontend/package.json`); a solução backend é `.slnx`.
- O domínio usa termos em português; preserve os nomes existentes e armazene tickers em maiúsculas com `ToUpperInvariant()` onde aplicável. Valores monetários do produto são em BRL.
- Não introduza novos segredos, credenciais, fallbacks inseguros ou dados simulados como se fossem dados reais. Há débitos existentes nesse sentido; verifique o código antes de alterá-los.
- `frontend/vite.config.ts` configura a porta 5000, que pode conflitar com a API local. Confira a porta efetiva antes de iniciar serviços.
- Para GitHub Pages, consulte `.github/workflows/deploy.yml`: build com `GITHUB_PAGES=true`, `VITE_USE_MSW=false`, variável `VITE_API_URL` e cópia de `dist/index.html` para `dist/404.html`.
- Para criar um artefato documental solicitado ou pertinente ao trabalho, consulte `templates/README.md` e leia o modelo correspondente antes de escrevê-lo. Use `conductor/workflow.md` para critérios e destinos; adapte o modelo ao escopo, sem inventar IDs, decisões ou evidências. O modelo não autoriza criar issues, PRs ou outros recursos externos por si só.

## Seleção de templates

- Plano, decisão arquitetural, história de usuário ou issue: consulte, respectivamente, `templates/plan-template.md`, `templates/adr-template.md`, `templates/user-stories-template.md` ou `templates/issue-template.md`.
- Descrição de PR, relatório de validação ou documentação de endpoint: consulte `templates/pr-template.md`, `templates/validation-report-template.md` ou `templates/endpoint-template.md`, respectivamente. Para commits, consulte `templates/commit-rules.md` e escreva o assunto e o corpo em português brasileiro (pt-BR).
- Cenários BDD, organização Playwright, Page Objects e clientes de testes de API: consulte os modelos correspondentes em `templates/README.md` e adapte os exemplos à estrutura e aos contratos reais do repositório.
- Para criar um subagente personalizado do Codex, use `templates/codex-agent-template.toml`; o manifesto YAML legado atende outro formato.

## Configuração de agentes

- Instruções específicas ficam em `frontend/AGENTS.md` e `src/AGENTS.md`; workflows reutilizáveis estão em `.agents/skills/invest-*/` e agentes personalizados em `.codex/agents/`.
- Aplique automaticamente as instruções e skills pertinentes à intenção do pedido, mesmo sem menção explícita. Consulte os templates vinculados quando o trabalho envolver o respectivo artefato; preserve as permissões da sessão e a prioridade das instruções do usuário.
- Ao receber pedido de revisão de PR ou de mudanças, aplique `invest-pr-review` e, quando a sessão permitir delegação e houver análises independentes, delegue revisão de corretude a `invest-review`, riscos de segurança a `invest-security` e cobertura a `invest-test`. Aguarde os resultados, verifique os achados e consolide-os; para mudanças pequenas ou sem delegação disponível, faça a revisão diretamente e informe limitações relevantes.
- Para páginas React, módulos mapeados e migrações EF, aplique respectivamente `invest-ui-page`, `invest-mapped-module` e `invest-ef-migration` quando pertinentes. Para trabalho independente relevante, delegue a `invest-frontend`, `invest-backend` ou `invest-database` conforme a área, se as ferramentas e instruções da sessão permitirem; não crie delegações desnecessárias para tarefas simples.
- O material Antigravity legado em `.agents/agents`, `.agents/rules` e `.agents/workflows`, quando presente localmente, é referência histórica, não protocolo de roteamento do Codex. Não pressuponha que seus comandos `/...` sejam comandos do Codex.
- O histórico, limites e mapeamento de migração estão em `docs/codex-migration.md`. A configuração não concede permissões adicionais.
