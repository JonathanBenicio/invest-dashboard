import { expect, test } from '@playwright/test'
import { apiUrl, createAccessToken, jwtSecret, mockRefreshSession } from './auth'

const formatCurrency = (value: number) => new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
}).format(value)

test('simulator sends its selected parameters to the real API and displays the response', async ({ page, request }) => {
  expect(jwtSecret, 'E2E_JWT_SECRET must match the test API').toBeTruthy()
  const token = createAccessToken(jwtSecret!)
  const authorization = { Authorization: `Bearer ${token}` }
  const parameters = {
    valorInicial: 1000,
    aporteMensal: 500,
    anos: 1,
    taxaJurosAnual: 10,
    estrategia: 'deterministic',
  }

  await mockRefreshSession(page, token)
  const expectedResponse = await request.post(`${apiUrl}/api/v1/simulation`, {
    headers: authorization,
    data: parameters,
  })
  expect(expectedResponse.status()).toBe(200)
  const expected = (await expectedResponse.json()).dados as {
    nomeEstrategia: string
    valorFinal: number
    totalInvestido: number
    totalJuros: number
  }

  await page.goto('/simulador')

  await expect(page.getByRole('heading', { name: 'Simulador de Investimentos' })).toBeVisible()
  const strategySelect = page.getByRole('combobox')
  await expect(strategySelect).toBeEnabled()
  await strategySelect.click()
  await page.getByRole('option', { name: /Determinístico/ }).click()

  await page.locator('#initialAmount').fill('1000')
  await page.locator('#monthlyContribution').fill('500')
  await page.locator('#years').fill('1')
  await page.locator('#interestRate').fill('10')
  await page.getByRole('button', { name: 'Simular' }).click()

  await expect(page.getByText(expected.nomeEstrategia, { exact: false }).first()).toBeVisible()
  await expect(page.getByText(formatCurrency(expected.totalInvestido), { exact: true })).toBeVisible()
  await expect(page.getByText(formatCurrency(expected.totalJuros), { exact: true })).toBeVisible()
  await expect(page.getByText(formatCurrency(expected.valorFinal), { exact: true })).toBeVisible()

  const invalidResponse = await request.post(`${apiUrl}/api/v1/simulation`, {
    headers: authorization,
    data: {
      valorInicial: -1,
      aporteMensal: 500,
      anos: 1,
      taxaJurosAnual: 10,
      estrategia: 'deterministic',
    },
  })
  expect(invalidResponse.status()).toBe(400)
})

test('simulator shows API failures without pretending the simulation is idle', async ({ page }) => {
  expect(jwtSecret, 'E2E_JWT_SECRET must match the test API').toBeTruthy()
  const token = createAccessToken(jwtSecret!)
  await mockRefreshSession(page, token)
  await page.goto('/simulador')

  await expect(page.getByRole('combobox')).toBeEnabled()
  await page.route(`${apiUrl}/api/v1/simulation`, route => route.fulfill({
    status: 503,
    contentType: 'application/problem+json',
    body: JSON.stringify({ title: 'Service unavailable', detail: 'Simulação indisponível no momento.', status: 503 }),
  }))

  await page.getByRole('button', { name: 'Simular' }).click()

  await expect(page.getByRole('alert')).toContainText('Simulação indisponível no momento.')
  await expect(page.getByText(/Aguardando simulação/)).toHaveCount(0)
})
