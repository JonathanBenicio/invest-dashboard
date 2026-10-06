import { expect, test } from '@playwright/test'
import { apiUrl, createAccessToken, createPortfolio, dateOnly, jwtSecret, mockRefreshSession } from './auth'

test('stock sale requires fiscal modality and persists the selected category', async ({ page, request }) => {
  expect(jwtSecret, 'E2E_JWT_SECRET must match the test API').toBeTruthy()
  const token = createAccessToken(jwtSecret!)
  const authorization = { Authorization: `Bearer ${token}` }
  const portfolio = await createPortfolio(request, token, `Venda fiscal E2E ${Date.now()}`)
  const ticker = `E2E${Date.now().toString().slice(-6)}3`
  const transactionUrl = `${apiUrl}/api/v1/transactions`

  const purchaseResponse = await request.post(transactionUrl, {
    headers: authorization,
    data: {
      carteiraId: portfolio.id,
      ticker,
      tipo: 'Buy',
      classeAtivo: 'ACAO',
      nome: 'WEG',
      quantidade: 5,
      precoUnitario: 10,
      taxas: 0,
      dataTransacao: dateOnly(-1),
      chaveIdempotencia: crypto.randomUUID(),
    },
  })
  expect(purchaseResponse.status()).toBe(201)

  const positionsResponse = await request.get(`${apiUrl}/api/v1/investments?carteiraId=${portfolio.id}`, {
    headers: authorization,
  })
  expect(positionsResponse.status()).toBe(200)
  const position = ((await positionsResponse.json()).dados as Array<{ id: string; ticker: string }>)
    .find(item => item.ticker === ticker)
  expect(position).toBeDefined()

  const rejectedSale = await request.post(transactionUrl, {
    headers: authorization,
    data: {
      carteiraId: portfolio.id,
      ticker,
      tipo: 'Sell',
      classeAtivo: 'ACAO',
      quantidade: 1,
      precoUnitario: 11,
      taxas: 0,
      dataTransacao: dateOnly(0),
      chaveIdempotencia: crypto.randomUUID(),
    },
  })
  expect(rejectedSale.status()).toBe(400)

  await mockRefreshSession(page, token)
  await page.goto('/renda-variavel')
  const stockRow = page.getByRole('row').filter({ hasText: ticker })
  await expect(stockRow).toBeVisible()
  await page.goto(`/investimento/${position!.id}`)
  await expect(page.getByRole('heading', { name: 'WEG' })).toBeVisible()
  await page.getByRole('button', { name: 'Vender' }).click()

  const modality = page.locator('#trade-tax-modality')
  await expect(modality).toHaveAttribute('required', '')
  await page.locator('#trade-quantity').fill('1')
  await page.locator('#trade-price').fill('11')
  const submitSale = page.getByRole('button', { name: 'Confirmar venda' })
  await expect(submitSale).toBeDisabled()
  await modality.selectOption('Comum')
  await expect(submitSale).toBeEnabled()
  await submitSale.click()
  await expect(page.getByText('Venda registrada.', { exact: true })).toBeVisible()

  const ledgerResponse = await request.get(`${apiUrl}/api/v1/transactions/portfolio/${portfolio.id}`, {
    headers: authorization,
  })
  expect(ledgerResponse.status()).toBe(200)
  const ledger = (await ledgerResponse.json()).dados as Array<{
    tipo: string
    modalidadeFiscal: string
  }>
  expect(ledger).toHaveLength(2)
  expect(ledger.find(transaction => transaction.tipo === 'Sell')?.modalidadeFiscal).toBe('Comum')
})
