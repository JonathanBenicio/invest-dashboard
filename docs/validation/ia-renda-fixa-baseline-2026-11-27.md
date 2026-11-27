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
| FE-03 | `cd frontend && CI=1 bun run test e2e/groups.spec.ts e2e/tesouro-direto.spec.ts e2e/conciliacao-tesouro.spec.ts --reporter=line --workers=1 --retries=0` | Na execução inicial, Vite falhou com `listen EPERM` em `127.0.0.1:5173`. Os testes não iniciaram. | não executado |
| BE-01 | `cd src && dotnet build InvestDashboard.slnx` | SDK Linux não encontrado no sandbox. Execução do SDK Windows falhou no socket WSL. | capacidade ausente |
| BE-02 | Testes focados de carteira, grupos e Tesouro em PostgreSQL isolado | SDK ausente e acesso inicial ao socket Docker negado. | capacidade ausente |

## Reprodução e gaps

Executar BE-01 com SDK 10 e rerodar os testes de integração com `INVEST_TEST_POSTGRES_CONNECTION` apontando a PostgreSQL descartável com permissão de criar banco temporário. O relatório deve registrar o subconjunto InMemory separadamente de PostgreSQL. Casos de UI usam fixtures sintéticas/MSW.
