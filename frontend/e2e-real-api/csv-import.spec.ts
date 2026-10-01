import { expect, test } from '@playwright/test'
import { apiUrl, createAccessToken, dateOnly, jwtSecret, mockRefreshSession, testUser } from './auth'

test('CSV import creates persisted transactions through the real API', async ({ page, request }) => {
  expect(jwtSecret, 'E2E_JWT_SECRET must match the test API').toBeTruthy()
  const token = createAccessToken(jwtSecret!)
  const portfolioName = `CSV E2E ${Date.now()}`
  const authorization = { Authorization: `Bearer ${token}` }

  await mockRefreshSession(page, token)

  const portfolioResponse = await request.post(`${apiUrl}/api/v1/portfolios`, {
    headers: authorization,
    data: {
      nome: portfolioName,
      instituicaoFinanceiraId: '10000000-0000-4000-8000-000000000001',
    },
  })
  expect(portfolioResponse.status()).toBe(201)
  const portfolio = (await portfolioResponse.json()).dados as { id: string }

  await page.goto('/importar')
  await expect(page.getByRole('heading', { name: 'Importar operações' })).toBeVisible()
  await page.getByRole('combobox', { name: 'Carteira de destino' }).click()
  await page.getByRole('option', { name: portfolioName }).click()
  await page.locator('input[type="file"]').setInputFiles({
    name: 'operacoes.csv',
    mimeType: 'text/csv',
    buffer: Buffer.from([
      'ticker;type;quantity;unitPrice;fees;transactionDate;assetClass;name;sector;issuer;subtype;indexer;interestRate;maturityDate;initialStatementValue',
      `PETR4;Buy;10;35,50;1;${dateOnly(-1)};ACAO;Petrobras;Energia;;;;;;`,
      `RFABC1;Buy;1000;1;0;${dateOnly(-1)};RENDA_FIXA;CDB;;Banco de teste;CDB;CDI;110;${dateOnly(730)};1010`,
      `INVALID1;Buy;0;10;0;${dateOnly(-1)};ACAO;Linha inválida;;;;;;;;`,
      '',
    ].join('\n')),
  })

  await expect(page.getByText('Quantidade deve ser maior que zero.')).toBeVisible()
  await page.getByRole('button', { name: 'Importar 2 operações' }).click()
  await expect(page.getByRole('cell', { name: 'Operação registrada.' })).toHaveCount(2)
  await expect(page.getByText('2 operações registradas; 1 linha precisa de correção.', { exact: true })).toBeVisible()

  const transactionsResponse = await request.get(
    `${apiUrl}/api/v1/transactions/portfolio/${portfolio.id}`,
    { headers: authorization },
  )
  expect(transactionsResponse.status()).toBe(200)
  const transactions = (await transactionsResponse.json()).dados as Array<{ ticker: string }>
  expect(transactions.map(transaction => transaction.ticker).sort()).toEqual(['PETR4', 'RFABC1'])

  const positionsResponse = await request.get(
    `${apiUrl}/api/v1/investments?carteiraId=${portfolio.id}`,
    { headers: authorization },
  )
  expect(positionsResponse.status()).toBe(200)
  const positions = (await positionsResponse.json()).dados as Array<{
    ticker: string
    quantidade: number
    valorAtual: number
  }>
  expect(positions).toHaveLength(2)
  expect(positions.find(position => position.ticker === 'PETR4')).toMatchObject({ quantidade: 10 })
  expect(positions.find(position => position.ticker === 'RFABC1')).toMatchObject({ valorAtual: 1010 })
})
