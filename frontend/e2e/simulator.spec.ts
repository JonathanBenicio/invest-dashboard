import { expect, test } from '@playwright/test'

test.describe('Simulador conectado à API', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel('E-mail').fill('admin@investpro.com')
    await page.getByLabel('Senha').fill('password')
    await page.getByRole('button', { name: 'Entrar' }).click()
    await expect(page.getByText(/Patrim.*Total/).first()).toBeVisible({ timeout: 10_000 })
    await page.getByRole('link', { name: 'Simulador' }).click()
    await expect(page.getByRole('heading', { name: 'Simulador de Investimentos' })).toBeVisible()
  })

  test('loads strategies and renders the simulation response', async ({ page }) => {
    const strategySelect = page.getByRole('combobox')
    await expect(strategySelect).toBeEnabled()
    await strategySelect.click()
    await page.getByRole('option', { name: /Determinístico/ }).click()

    await page.locator('#initialAmount').fill('1000')
    await page.locator('#monthlyContribution').fill('500')
    await page.locator('#years').fill('1')
    await page.locator('#interestRate').fill('10')
    await page.getByRole('button', { name: 'Simular' }).click()

    await expect(page.getByText('R$ 7.000,00', { exact: true })).toBeVisible()
    await expect(page.getByText(/Determinístico/).first()).toBeVisible()
    await expect(page.getByRole('alert')).toHaveCount(0)
  })

  test('shows API validation errors instead of the initial state', async ({ page }) => {
    await page.locator('#initialAmount').fill('-1')
    await page.getByRole('button', { name: 'Simular' }).click()

    await expect(page.getByRole('alert')).toContainText('Parâmetros de simulação inválidos.')
    await expect(page.getByText(/Aguardando simulação/)).toHaveCount(0)
  })
})
