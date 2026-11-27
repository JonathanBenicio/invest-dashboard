# Baseline de IA e renda fixa

- Data: 27/11/2026, America/Sao_Paulo
- Branch e SHA: `codex/tesouro-previa-e-cotacoes`, `d683cfe16401d8e5ce6b42b9a966fe9bb2032295`
- Plano: [EP-00](../plan/ia-renda-fixa/00-rastreabilidade.md)
- Ambiente: WSL, Bun 1.3.4, Vite 7.2.7, Playwright 1.57.0, Docker Compose v5.5.1
- Working tree inicial: `.codex-attachments/` preexistente sem rastreamento.

## Resultados

| ID | Método | Resultado | Estado |
|---|---|---|---|
| FE-01 | `cd frontend && bun run lint` | Código de saída 0. Dez avisos de Fast Refresh e zero erros. | passou |
| FE-02 | `cd frontend && bun run build --outDir /tmp/invest-ep00-build.v2i60T` | Código de saída 0. Avisos preexistentes de Browserslist, Rollup e chunks. | passou |
| FE-03 | `cd frontend && CI=1 bun run test e2e/groups.spec.ts e2e/tesouro-direto.spec.ts e2e/conciliacao-tesouro.spec.ts --reporter=line --workers=1 --retries=0` | A execução inicial não iniciou com `listen EPERM`. Na repetição autorizada, 12 de 12 testes passaram em 1,5 minuto usando MSW e Chromium 148 já disponível. | passou |
| BE-01 | `cd src && dotnet build InvestDashboard.slnx` | SDK Windows 10.0.201 / MSBuild 18.3.0 em cópia limpa de `git archive d683cfe src` no Temp do Windows. Build passou com zero avisos e zero erros. | passou |
| BE-02 | Testes focados de carteira, grupos e Tesouro em PostgreSQL isolado | `postgres:15` descartável sem volume, com tmpfs e porta local. Passaram 31 de 31 testes focados, zero ignorados. | passou |
| BE-03 | `dotnet test InvestDashboard.UnitTests.csproj --no-build --no-restore --filter "FullyQualifiedName~Tesouro\|FullyQualifiedName~CarteiraTests"` | 114 de 114 passaram, zero ignorados. | passou |

## Reprodução e gaps

A execução direta do SDK Windows sobre o checkout WSL via UNC falhou com CS0006/CS2012. A cópia no disco temporário do Windows contornou essa limitação. O container PostgreSQL e a cópia temporária foram removidos. A conexão de testes foi fornecida via `INVEST_TEST_POSTGRES_CONNECTION`, omitida aqui por conter credencial. Os casos de UI usam fixtures sintéticas/MSW. O Chromium esperado pelo Playwright 1.57.0 não estava instalado. A repetição usou uma cópia temporária da configuração Playwright, com o mesmo perfil e um `launchOptions.executablePath` apontando ao Chromium 148 existente. A tentativa de instalar o Chromium padrão encontrou incompatibilidade com o sistema, seguida de instalação incompleta do build 1200 no cache. O resultado coberto é o Chromium 148.

Comando da repetição de FE-03 em `frontend/`: `BUN_TMPDIR=/tmp CI=1 bun run test --config /tmp/invest-playwright-config.6xoEPm/playwright.config.cjs e2e/groups.spec.ts e2e/tesouro-direto.spec.ts e2e/conciliacao-tesouro.spec.ts --reporter=line --workers=1 --retries=0 --output /tmp/invest-ep00-e2e.DQS5dR`.

Filtro de integração: `PortfolioGroupAuthorizationTests`, `PostgresGroupWorkspaceTests`, `PostgresInvestmentPositionTests`, `ConfirmacaoTesouroControllerTests`, `LotesTesouroControllerTests`, `ConciliacaoTesouroControllerTests` e `ExtratoTesouroControllerTests` como seletores `FullyQualifiedName~` em `dotnet test InvestDashboard.IntegrationTests.csproj --no-build --no-restore --filter ...`.
