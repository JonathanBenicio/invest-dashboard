import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  testMatch: [
    'tesouro-direto.spec.ts',
    'referencia-publica-tesouro.spec.ts',
    'conciliacao-tesouro.spec.ts',
    'extrato-oficial-tesouro.spec.ts',
    'investimentos-consolidados.spec.ts',
  ],
  fullyParallel: false,
  workers: 1,
  reporter: 'list',
  use: {
    ...devices['Desktop Chrome'],
    baseURL: 'http://127.0.0.1:5174',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  webServer: {
    command: 'bun run dev -- --host 127.0.0.1 --port 5174',
    url: 'http://127.0.0.1:5174',
    reuseExistingServer: false,
    env: { VITE_USE_MSW: 'true', VITE_API_URL: '' },
  },
})
