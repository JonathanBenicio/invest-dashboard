import { expect, test } from '@playwright/test'
import { apiUrl, createAccessToken, dateOnly, jwtSecret, mockRefreshSession } from './auth'

test('fixed-income statement valuation persists through the real UI and API', async ({ page, request }) => {
  expect(jwtSecret, 'E2E_JWT_SECRET must match the test API').toBeTruthy()
  const token = createAccessToken(jwtSecret!)
  const portfolioName = `Extrato E2E ${Date.now()}`
  const assetName = `CDB extrato ${Date.now()}`
  const authorization = { Authorization: `Bearer ${token}` }

  await mockRefreshSession(page, token)

  const portfolioResponse = await request.post(`${apiUrl}/api/v1/portfolios`, {
    headers: authorization,
    data: {
      nome: portfolioName,
      instituicaoFinanceiraId: '10000000-0000-4000-8000-000000000001',
    },
  })
  const createdPortfolio = await portfolioResponse.json()
  expect(portfolioResponse.status(), JSON.stringify(createdPortfolio)).toBe(201)
  const portfolio = createdPortfolio.dados as { id: string }

  const purchaseResponse = await request.post(`${apiUrl}/api/v1/investments/fixed-income`, {
    headers: authorization,
    data: {
      carteiraId: portfolio.id,
      nome: assetName,
      subtipo: 'CDB',
      emissor: 'Banco de teste',
      valorPrincipal: 5000,
      valorExtrato: 5075,
      taxaJuros: 110,
      indexador: 'CDI',
      dataCompra: new Date(`${dateOnly(0)}T00:00:00`).toISOString(),
      dataVencimento: `${dateOnly(365)}T12:00:00.000Z`,
      chaveIdempotencia: crypto.randomUUID(),
    },
  })
  expect(purchaseResponse.status()).toBe(201)
  const purchase = (await purchaseResponse.json()).dados as { id: string; valorAtual: number }
  expect(purchase.valorAtual).toBe(5075)

  await page.goto('/renda-fixa')
  await expect(page.getByRole('heading', { name: 'Renda Fixa' })).toBeVisible()
  const assetRow = page.getByRole('row', { name: new RegExp(assetName) })
  await expect(assetRow).toBeVisible()
  await assetRow.getByRole('button').click()
  await page.getByRole('menuitem', { name: 'Editar' }).click()

  const valuationDate = dateOnly(0)
  await page.locator('#valuation-total').fill('5200')
  await page.locator('#valuation-date').fill(valuationDate)
  await page.getByRole('button', { name: 'Salvar avaliação' }).click()

  await expect(page.getByText('Ativo atualizado', { exact: true })).toBeVisible()
  await expect(assetRow.getByText(/5\.200,00/)).toBeVisible()

  const positionResponse = await request.get(
    `${apiUrl}/api/v1/investments/${purchase.id}`,
    { headers: authorization },
  )
  expect(positionResponse.status()).toBe(200)
  const position = (await positionResponse.json()).dados as { valorAtual: number }
  expect(position.valorAtual).toBe(5200)

  const historyResponse = await request.get(
    `${apiUrl}/api/v1/investments/${purchase.id}/history`,
    { headers: authorization },
  )
  expect(historyResponse.status()).toBe(200)
  const history = (await historyResponse.json()).dados as Array<{ data: string; preco: number; origem: string }>
  expect(history).toEqual(expect.arrayContaining([
    expect.objectContaining({ data: expect.stringContaining(valuationDate), preco: 1.04, origem: 'statement' }),
  ]))

  const transactionsResponse = await request.get(
    `${apiUrl}/api/v1/transactions/portfolio/${portfolio.id}`,
    { headers: authorization },
  )
  expect(transactionsResponse.status()).toBe(200)
  const transactions = (await transactionsResponse.json()).dados as Array<{ ticker: string }>
  expect(transactions).toHaveLength(1)
})
