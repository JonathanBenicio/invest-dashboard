import { defineConfig, devices } from '@playwright/test'

const apiUrl = process.env.E2E_API_URL ?? 'http://127.0.0.1:5051'
const frontendUrl = process.env.E2E_FRONTEND_URL ?? 'http://127.0.0.1:5174'
const frontendPort = new URL(frontendUrl).port || '5174'

export default defineConfig({
  testDir: './e2e-real-api',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: 'html',
  use: {
    ...devices['Desktop Chrome'],
    baseURL: frontendUrl,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  webServer: {
    command: `bun run dev -- --host 127.0.0.1 --port ${frontendPort}`,
    url: frontendUrl,
    reuseExistingServer: !process.env.CI,
    env: {
      VITE_USE_MSW: 'false',
      VITE_API_URL: apiUrl,
    },
  },
})
