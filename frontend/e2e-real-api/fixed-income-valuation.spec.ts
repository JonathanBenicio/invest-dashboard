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
    data: { name: portfolioName },
  })
  expect(portfolioResponse.status()).toBe(201)
  const portfolio = (await portfolioResponse.json()).data as { id: string }

  const purchaseResponse = await request.post(`${apiUrl}/api/v1/investments/fixed-income`, {
    headers: authorization,
    data: {
      portfolioId: portfolio.id,
      name: assetName,
      subtype: 'CDB',
      issuer: 'Banco de teste',
      principal: 5000,
      statementValue: 5075,
      interestRate: 110,
      indexer: 'CDI',
      purchaseDate: `${dateOnly(-30)}T12:00:00.000Z`,
      maturityDate: `${dateOnly(365)}T12:00:00.000Z`,
      idempotencyKey: crypto.randomUUID(),
    },
  })
  expect(purchaseResponse.status()).toBe(201)
  const purchase = (await purchaseResponse.json()).data as { id: string; currentValue: number }
  expect(purchase.currentValue).toBe(5075)

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
  const position = (await positionResponse.json()).data as { currentValue: number }
  expect(position.currentValue).toBe(5200)

  const historyResponse = await request.get(
    `${apiUrl}/api/v1/investments/${purchase.id}/history`,
    { headers: authorization },
  )
  expect(historyResponse.status()).toBe(200)
  const history = (await historyResponse.json()).data as Array<{ date: string; price: number; source: string }>
  expect(history).toEqual(expect.arrayContaining([
    expect.objectContaining({ date: expect.stringContaining(valuationDate), price: 1.04, source: 'statement' }),
  ]))

  const transactionsResponse = await request.get(
    `${apiUrl}/api/v1/transactions/portfolio/${portfolio.id}`,
    { headers: authorization },
  )
  expect(transactionsResponse.status()).toBe(200)
  const transactions = (await transactionsResponse.json()).data as Array<{ ticker: string }>
  expect(transactions).toHaveLength(1)
})
