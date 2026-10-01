import { defineConfig, devices } from '@playwright/test'
import { existsSync, readFileSync } from 'node:fs'
import { resolve } from 'node:path'

const localAuthEnv = resolve(process.cwd(), '.env.e2e.local')
if (existsSync(localAuthEnv)) {
  for (const line of readFileSync(localAuthEnv, 'utf8').split(/\r?\n/)) {
    const match = line.match(/^\s*([A-Z][A-Z0-9_]*)\s*=\s*(.*)\s*$/)
    if (!match || match[1] in process.env) continue
    const value = match[2].trim()
    process.env[match[1]] = value.startsWith('"') && value.endsWith('"')
      ? JSON.parse(value)
      : value.startsWith("'") && value.endsWith("'")
        ? value.slice(1, -1)
        : value
  }
}

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
