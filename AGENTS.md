# AGENTS.md - Invest Dashboard

Monorepo: React 19 + Vite frontend (`frontend/`) + .NET 10 DDD backend (`src/`).

## Commands

| Task | Command |
|------|---------|
| Full stack up | `docker-compose up -d --build` |
| Frontend dev | `cd frontend && bun install && bun run dev` |
| Backend dev | `cd src && dotnet restore && dotnet build InvestDashboard.slnx && dotnet run --project InvestDashboard.WebAPI` |
| Frontend build | `cd frontend && bunx msw init public --save && bun run build` |
| Backend tests | `cd src && dotnet test` |
| Frontend E2E | `cd frontend && bun run test` |
| Mobile Android | `cd frontend && bun run mobile:android` |
| AI validation | `python .agents/scripts/checklist.py .` |

## Critical Gotchas

- **Bun, not npm** — frontend uses `bun@1.3.4`. `npm install` works but `bun` is the package manager.
- **`.slnx` not `.sln`** — solution file is `src/InvestDashboard.slnx` (modern XML format).
- **Port 5000 conflict** — Vite dev server and API both default to 5000. Docker maps API to `5000:8080`. Local dev: API runs on 5000, Vite on 5173.
- **MSW required for builds** — run `bunx msw init public --save` before `bun run build` or the service worker won't be registered.
- **GitHub Pages build** — needs `GITHUB_PAGES=true`, `VITE_USE_MSW=true`, and `cp dist/index.html dist/404.html` for SPA routing.
- **`android/` is gitignored** — Capacitor native dirs are generated. Run `bun run cap:sync` to recreate.
- **Integration tests are empty** — only a placeholder `Test1()` exists. Unit tests cover domain logic only.
- **No backend CI/CD** — only frontend has GitHub Actions workflows.

## Architecture

### Backend (src/) — DDD Clean Architecture

```
Domain (net10.0, no deps)
  ← Application (refs Domain)
    ← Infrastructure (refs Domain + Application, EF Core, SignalR, Workers)
      ← WebAPI (refs Infrastructure + Application, Controllers, JWT, OpenAPI)
```

- **Domain language**: Portuguese (`Carteira`, `Transacao`, `Ativo`, `TaxaEconomica`). Infrastructure/frontend: English.
- **TPH inheritance**: `Ativo` base class with discriminator `TipoAtivo` for `Acao`/`FundoImobiliario`/`Criptoativo`/`RendaFixa`.
- **EF Core**: Fluent API in isolated config classes, snake_case column mapping, private parameterless constructors for deserialization.
- **SignalR**: `DadosMercadoHub` — JWT-authorized, ticker-based group subscriptions via query string `access_token`.
- **Background worker**: `AtualizadorDadosMercadoWorker` — fetches from Brapi API, falls back to random-walk simulation.
- **Auto-asset creation**: `TransacaoAppService` infers asset type from ticker patterns (`*11` = FII, `*3/*4` = Acao).

### Frontend (frontend/)

- **Router**: TanStack Router (programmatic route tree, not file-based). Dynamic base path: `/` for native, `/invest-dashboard/` for GitHub Pages.
- **Auth**: Supabase Auth + Zustand store. `AuthInitializer` calls `checkAuth()` on mount.
- **API client**: Typed `fetch` wrapper in `src/api/client.ts`, 10s timeout, auto-logout on 401.
- **MSW**: 909 lines of mock handlers in `src/mocks/`. Falls back to mock admin user when no auth token (dev convenience).
- **Charts**: Recharts, Chart.js + financial plugin, LightningChart, TradingView widgets, lightweight-charts.

## Known Issues (Do Not Reproduce)

- **Hardcoded portfolio fallbacks** in `CarteirasController` — returns simulated data when DB is null. Leaks into production.
- **JWT secret fallback** — hardcoded `default_very_long_fallback_secret_for_security_compliance` in `Program.cs` if none configured.
- **Supabase credentials** in `src/InvestDashboard.WebAPI/appsettings.json` — should be user secrets or env vars.
- **Random-walk bias** — price simulation uses `0.49` instead of `0.50`, creating slight upward drift.

## .agents/ System

- **20 specialist agents**, **36 skills**, **18 workflows** in `.agents/`
- **GEMINI.md** rules in `.agents/rules/` define agent routing protocol — read appropriate agent file before implementation
- **Global rules**: `.agents/rules/dot-net-standards.md`, `front.md`, `database-rules.md`, `arquitetura-mappers.md`
- **Validation**: `checklist.py` (core checks), `verify_all.py` (full suite with Lighthouse + Playwright)

## Conventions

- Tickers stored as uppercase via `.ToUpperInvariant()`
- All monetary values in BRL
- Nullable reference types enabled in all .NET projects
- shadcn-ui components in `frontend/src/components/ui/`
- Status colors: success=emerald, warning=amber, error=rose, info=blue
