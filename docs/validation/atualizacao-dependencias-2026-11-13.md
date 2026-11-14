# Relatório de validação — atualização de dependências

- Data: 13/11/2026, horário de São Paulo. Branch `codex/homologacao-autenticacao-real`; SHA base `f85ae79`; havia alterações locais anteriores em documentação e testes, preservadas.
- Issue/PR: não criadas. Plano: pedido aprovado na conversa; trabalho ainda parcial. ADR e história: não criadas nesta revisão.
- Ambiente: Linux/WSL; Node 26.10.0; Bun 1.3.4; SDK .NET 10.0.401 em contêiner; PostgreSQL 15 isolado e dados sintéticos. Builds e testes operaram sobre a árvore local, incluindo alterações preexistentes.

## Resultados

| Verificação | Método | Estado | Evidência ou limite |
|---|---|---|---|
| Instalação frontend | `bun install --frozen-lockfile` | passou | Após as últimas alterações; scripts executados. |
| Tipagem frontend | `tsc --noEmit -p tsconfig.app.json` e `tsc --noEmit -p tsconfig.node.json` | passou | Aplicação verificada após as alterações de componentes; configuração Node verificada antes delas. |
| Lint frontend | `bun run lint` | passou | Dez avisos de Fast Refresh, zero erros. |
| Build frontend | `bun run build` | passou | Aviso de chunk acima de 500 kB; não é teste de comportamento. |
| E2E mock | `bun run test -- --reporter=line --workers=2` | falhou | Antes da última alteração de dois componentes sem consumidores identificados, 15 passaram e dois excederam tempo no carregamento de login; execução anterior passou 17/17 antes da reconstrução do lockfile. |
| Repetição dos dois E2E | `bun run test -- e2e/crud.spec.ts --grep 'configura titular|exige modalidade fiscal antes' --reporter=line --workers=1` | passou | 2/2; a causa do timeout anterior não foi comprovada. |
| CSV contra API | `bun run test:api -- csv-import.spec.ts --reporter=line` | passou | 1/1, API e PostgreSQL isolados, identidade sintética; antes da última atualização de componentes sem consumidores identificados. |
| Backend com PostgreSQL | `dotnet test InvestDashboard.slnx --configuration Release` | passou | SDK em contêiner; 58 unitários passaram e dois externos foram ignorados; 79 integrações passaram sem ignorados, incluindo os dependentes de PostgreSQL. |
| Auditoria NuGet | `dotnet list InvestDashboard.slnx package --vulnerable --include-transitive --no-restore` | passou | Nenhuma vulnerabilidade reportada pelas fontes consultadas. |
| Auditoria Bun | `bun audit` | falhou | 3 alertas moderados: minimatch, glob e postcss-selector-parser, em ferramentas de desenvolvimento. Antes: 105 alertas incluindo dev; contagem não demonstra explorabilidade. |
| Auditoria npm produção | `npm audit --omit=dev` | passou | Nenhum alerta após sincronização do lockfile npm; auditorias npm e Bun usam resoluções distintas. |
| Imagem API | `docker build -t invest-dependency-review-api -f src/InvestDashboard.WebAPI/Dockerfile .` | passou | Publicação em imagem .NET 10. |
| Imagem frontend | `docker build -t invest-dependency-review-frontend -f frontend/Dockerfile frontend` | passou | Build após as últimas alterações de dependências. |
| Android/iOS nativo | Build e inicialização | capacidade ausente | Java e SDK Android não disponíveis no host; iOS indisponível. Capacitor 8 foi atualizado sem validação nativa. |
| Autenticação Supabase real e SignalR ponta a ponta | Testes ao vivo | não executado | Sem evidência de integração ao vivo nesta revisão; o CSV usa sessão sintética. |

Cobertura de código não medida. Os testes E2E não exercitam todos os fluxos, os componentes sem consumidores identificados, acessibilidade ou compatibilidade visual em dispositivos.

## Compatibilidade e pendências

- TypeScript 7 falhou com o typescript-eslint; 6.0.3 está dentro da faixa documentada. `baseUrl` foi removido mantendo os aliases relativos.
- Tailwind 4 exige migração de configuração e revisão visual e de navegadores; mantidos Tailwind 3 e tailwind-merge 2. TanStack Table 9 alterou APIs usadas por tabelas; mantida a versão 8. Recharts 3 gerou erros em gráficos; mantida a versão 2. MSW 3 alterou opções do worker; mantida a versão 2. A migração total dessas famílias permanece pendente.
- DayPicker 10 substituiu a chave `table` por `month_grid`; react-resizable-panels 4 mudou os componentes e atributos de orientação. Os wrappers foram adaptados, mas não tiveram teste interativo dedicado.
- ASP.NET Core 10 requer Microsoft.OpenApi 2.x; 3.11 produziu NU1608 e falha de compilação. EF/Npgsql continuam na linha 10; SDK .NET 11 e EF 11 não foram migrados, pois Npgsql EF disponível na consulta permaneceu na linha 10. Validar suporte e calendário de publicação novamente antes de migrar.
- ESLint e plugin React Hooks permanecem nas linhas 9 e 5: a combinação mais recente ativou regras que apontaram 15 erros em código existente. Não foram desativadas regras para ocultar o resultado.
- Dependências transitivas ainda alertadas exigem atualização upstream ou avaliação pontual de override e testes; não se declara que sejam exploráveis neste produto. Revisar licença de bibliotecas e SDKs separadamente.
- Bun e npm usam lockfiles próprios; as versões declaradas a seguir não substituem as resolvidas. Comparar mudanças dos lockfiles em revisões futuras. O lockfile npm foi gerado com `--legacy-peer-deps`; o dry-run do npm sem essa opção passou após as últimas alterações.

## Fontes oficiais consultadas

- https://typescript-eslint.io/users/dependency-versions/
- https://v8.vite.dev/guide/migration
- https://tailwindcss.com/docs/upgrade-guide
- https://tanstack.com/table/latest/docs/framework/react/guide/migrating
- https://daypicker.dev/upgrading
- https://github.com/bvaughn/react-resizable-panels/blob/main/CHANGELOG.md
- https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/11/microsoft-openapi-3x
- https://capacitorjs.com/docs/updating/8-0

## Inventário frontend

As colunas mostram faixas declaradas antes e depois; dependências não atualizadas foram mantidas por compatibilidade, por ausência de versão mais nova na faixa ou por não demandarem alteração do manifesto. A versão efetiva fica em `frontend/bun.lock` e `frontend/package-lock.json`.

| Dependência | Anterior | Escolhida | Justificativa |
|---|---|---|---|
| @capacitor/camera | ^8.0.0 | ^8.2.6 | Atualizada; ver lockfile para resolução. |
| @hookform/resolvers | ^5.2.2 | ^7.0.6 | Atualizada; ver lockfile para resolução. |
| @ionic/pwa-elements | ^3.3.0 | ^3.4.0 | Atualizada; ver lockfile para resolução. |
| @lightningchart/lcjs | ^8.1.1 | ^9.1.0 | Atualizada; ver lockfile para resolução. |
| @lightningchart/lcjs-trader | ^4.0.1 | ^4.1.3 | Atualizada; ver lockfile para resolução. |
| @microsoft/signalr | ^10.0.0 | ^11.0.0 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-accordion | ^1.2.12 | ^1.3.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-alert-dialog | ^1.1.15 | ^1.2.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-aspect-ratio | ^1.1.8 | ^1.2.0 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-avatar | ^1.1.11 | ^1.3.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-checkbox | ^1.3.3 | ^1.4.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-collapsible | ^1.1.12 | ^1.2.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-context-menu | ^2.2.16 | ^2.4.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-dialog | ^1.1.15 | ^1.3.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-dropdown-menu | ^2.1.16 | ^2.2.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-hover-card | ^1.1.15 | ^1.2.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-label | ^2.1.8 | ^2.1.17 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-menubar | ^1.1.16 | ^1.2.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-navigation-menu | ^1.2.14 | ^1.4.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-popover | ^1.1.15 | ^1.3.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-progress | ^1.1.8 | ^1.2.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-radio-group | ^1.3.8 | ^1.5.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-scroll-area | ^1.2.10 | ^1.4.0 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-select | ^2.2.6 | ^2.4.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-separator | ^1.1.8 | ^1.2.0 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-slider | ^1.3.6 | ^1.6.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-slot | ^1.2.4 | ^1.5.0 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-switch | ^1.2.6 | ^1.4.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-tabs | ^1.1.13 | ^1.2.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-toast | ^1.2.14 | ^1.3.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-toggle | ^1.1.10 | ^1.2.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-toggle-group | ^1.1.11 | ^1.2.1 | Atualizada; ver lockfile para resolução. |
| @radix-ui/react-tooltip | ^1.2.8 | ^1.4.1 | Atualizada; ver lockfile para resolução. |
| @tanstack/react-query | ^5.83.0 | ^5.107.1 | Atualizada; ver lockfile para resolução. |
| @tanstack/react-router | ^1.141.6 | ^1.170.46 | Atualizada; ver lockfile para resolução. |
| @tanstack/react-table | ^8.21.3 | ^8.21.3 | Mantida; linha compatível ou sem atualização declarada. |
| class-variance-authority | ^0.7.1 | ^0.7.1 | Mantida; linha compatível ou sem atualização declarada. |
| clsx | ^2.1.1 | ^2.1.1 | Mantida; linha compatível ou sem atualização declarada. |
| cmdk | ^1.1.1 | ^1.1.1 | Mantida; linha compatível ou sem atualização declarada. |
| date-fns | ^4.1.0 | ^4.4.0 | Atualizada; ver lockfile para resolução. |
| embla-carousel-react | ^8.6.0 | ^8.6.0 | Mantida; linha compatível ou sem atualização declarada. |
| input-otp | ^1.4.2 | ^1.5.0 | Atualizada; ver lockfile para resolução. |
| lucide-react | ^0.556.0 | ^1.72.0 | Atualizada; ver lockfile para resolução. |
| msw | ^2.12.4 | ^2.15.0 | Atualizada; ver lockfile para resolução. |
| next-themes | ^0.4.6 | ^0.4.6 | Mantida; linha compatível ou sem atualização declarada. |
| react | ^19.2.1 | ^19.3.1 | Atualizada; ver lockfile para resolução. |
| react-day-picker | ^9.12.0 | ^10.0.2 | Atualizada; ver lockfile para resolução. |
| react-dom | ^19.2.1 | ^19.3.1 | Atualizada; ver lockfile para resolução. |
| react-hook-form | ^7.68.0 | ^7.92.0 | Atualizada; ver lockfile para resolução. |
| react-resizable-panels | ^3.0.6 | ^4.22.0 | Atualizada; ver lockfile para resolução. |
| react-ts-tradingview-widgets | ^1.2.8 | ^1.2.8 | Mantida; linha compatível ou sem atualização declarada. |
| recharts | 2.15.4 | 2.15.4 | Mantida; linha compatível ou sem atualização declarada. |
| sonner | ^2.0.7 | ^2.0.8 | Atualizada; ver lockfile para resolução. |
| tailwind-merge | ^2.6.0 | ^2.6.1 | Atualizada; ver lockfile para resolução. |
| tailwindcss-animate | ^1.0.7 | ^1.0.7 | Mantida; linha compatível ou sem atualização declarada. |
| vaul | ^1.1.2 | ^1.1.2 | Mantida; linha compatível ou sem atualização declarada. |
| zod | ^4.1.13 | ^4.6.7 | Atualizada; ver lockfile para resolução. |
| zustand | ^5.0.9 | ^5.1.0 | Atualizada; ver lockfile para resolução. |
| @capacitor/android | ^8.0.0 | ^8.6.2 | Atualizada; ver lockfile para resolução. |
| @capacitor/cli | ^8.0.0 | ^8.6.2 | Atualizada; ver lockfile para resolução. |
| @capacitor/core | ^8.0.0 | ^8.6.2 | Atualizada; ver lockfile para resolução. |
| @capacitor/ios | ^8.0.0 | ^8.6.2 | Atualizada; ver lockfile para resolução. |
| @capacitor/splash-screen | ^8.0.0 | ^8.0.2 | Atualizada; ver lockfile para resolução. |
| @capacitor/status-bar | ^8.0.0 | ^8.0.4 | Atualizada; ver lockfile para resolução. |
| @eslint/js | ^9.32.0 | ^9.39.5 | Atualizada; ver lockfile para resolução. |
| @playwright/test | ^1.57.0 | ^1.65.0 | Atualizada; ver lockfile para resolução. |
| @tailwindcss/typography | ^0.5.16 | ^0.5.20 | Atualizada; ver lockfile para resolução. |
| @types/node | ^22.19.3 | ^26.6.6 | Atualizada; ver lockfile para resolução. |
| @types/react | ^19.2.7 | ^19.3.0 | Atualizada; ver lockfile para resolução. |
| @types/react-dom | ^19.2.3 | ^19.3.0 | Atualizada; ver lockfile para resolução. |
| @vitejs/plugin-react-swc | ^3.11.0 | ^4.3.3 | Atualizada; ver lockfile para resolução. |
| autoprefixer | ^10.4.21 | ^10.6.2 | Atualizada; ver lockfile para resolução. |
| eslint | ^9.32.0 | ^9.39.5 | Atualizada; ver lockfile para resolução. |
| eslint-plugin-react-hooks | ^5.2.0 | ^5.2.0 | Mantida; linha compatível ou sem atualização declarada. |
| eslint-plugin-react-refresh | ^0.4.20 | ^0.5.7 | Atualizada; ver lockfile para resolução. |
| globals | ^15.15.0 | ^17.14.0 | Atualizada; ver lockfile para resolução. |
| lovable-tagger | ^1.1.13 | ^1.3.5 | Atualizada; ver lockfile para resolução. |
| patch-package | ^8.0.1 | ^8.0.1 | Mantida; linha compatível ou sem atualização declarada. |
| postcss | ^8.5.6 | ^8.5.32 | Atualizada; ver lockfile para resolução. |
| tailwindcss | ^3.4.17 | ^3.4.19 | Atualizada; ver lockfile para resolução. |
| typescript | ^5.8.3 | ^6.0.3 | Atualizada; ver lockfile para resolução. |
| typescript-eslint | ^8.38.0 | ^8.72.2 | Atualizada; ver lockfile para resolução. |
| vite | ^7.2.7 | ^8.3.8 | Atualizada; ver lockfile para resolução. |

## Inventário backend

| Dependência | Anterior | Escolhida | Justificativa |
|---|---|---|---|
| FluentAssertions | 8.9.0 | 8.11.0 | Atualizada. |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.7 | 10.0.15 | Atualizada. |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.7 | 10.0.15 | Atualizada. |
| Microsoft.AspNetCore.OpenApi | 10.0.7 | 10.0.15 | Atualizada. |
| Microsoft.EntityFrameworkCore.Design | 10.0.7 | 10.0.15 | Atualizada. |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.7 | 10.0.15 | Atualizada. |
| Microsoft.EntityFrameworkCore.Relational | 10.0.7 | 10.0.15 | Atualizada. |
| Microsoft.NET.Test.Sdk | 17.14.1 | 18.12.0 | Atualizada. |
| Microsoft.OpenApi | 2.7.5 | 2.13.0 | Atualizada. |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | 10.0.3 | Atualizada. |
| coverlet.collector | 6.0.4 | 10.1.0 | Atualizada. |
| xunit | 2.9.3 | 2.9.3 | Mantida. |
| xunit.runner.visualstudio | 3.1.4 | 4.0.1 | Atualizada. |
