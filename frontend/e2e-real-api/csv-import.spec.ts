import { createHmac } from 'node:crypto'
import { expect, test } from '@playwright/test'

const apiUrl = process.env.E2E_API_URL ?? 'http://127.0.0.1:5051'
const authSecret = process.env.E2E_JWT_SECRET
const issuer = process.env.E2E_JWT_ISSUER ?? 'test-issuer'
const audience = process.env.E2E_JWT_AUDIENCE ?? 'test-audience'
const userId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001'
const sessionId = 'bbbbbbbb-cccc-dddd-eeee-ffffffff0001'
const userEmail = 'test@investdashboard.com'
const userName = 'Test User'

function createAccessToken(secret: string) {
  const nowSeconds = Math.floor(Date.now() / 1000)
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url')
  const claims = Buffer.from(JSON.stringify({
    sub: userId,
    email: userEmail,
    name: userName,
    role: 'user',
    sid: sessionId.replaceAll('-', ''),
    iss: issuer,
    aud: audience,
    iat: nowSeconds,
    exp: nowSeconds + 600,
  })).toString('base64url')
  const unsignedToken = `${header}.${claims}`
  const signature = createHmac('sha256', secret).update(unsignedToken).digest('base64url')
  return `${unsignedToken}.${signature}`
}

function dateOnly(offsetDays: number) {
  return new Date(Date.now() + offsetDays * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)
}

test('CSV import creates persisted transactions through the real API', async ({ page, request }) => {
  expect(authSecret, 'E2E_JWT_SECRET must match the test API').toBeTruthy()
  const token = createAccessToken(authSecret!)
  const portfolioName = `CSV E2E ${Date.now()}`
  const authorization = { Authorization: `Bearer ${token}` }

  await page.route(`${apiUrl}/api/v1/auth/refresh`, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      data: {
        accessToken: token,
        expiresIn: 600,
        user: { id: userId, name: userName, email: userEmail, role: 'user' },
        requiresEmailConfirmation: false,
      },
      success: true,
      message: null,
    }),
  }))

  const portfolioResponse = await request.post(`${apiUrl}/api/v1/portfolios`, {
    headers: authorization,
    data: { name: portfolioName },
  })
  expect(portfolioResponse.status()).toBe(201)
  const portfolio = (await portfolioResponse.json()).data as { id: string }

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
  const transactions = (await transactionsResponse.json()).data as Array<{ ticker: string }>
  expect(transactions.map(transaction => transaction.ticker).sort()).toEqual(['PETR4', 'RFABC1'])

  const positionsResponse = await request.get(
    `${apiUrl}/api/v1/investments?portfolioId=${portfolio.id}`,
    { headers: authorization },
  )
  expect(positionsResponse.status()).toBe(200)
  const positions = (await positionsResponse.json()).data as Array<{
    ticker: string
    quantity: number
    currentValue: number
  }>
  expect(positions).toHaveLength(2)
  expect(positions.find(position => position.ticker === 'PETR4')).toMatchObject({ quantity: 10 })
  expect(positions.find(position => position.ticker === 'RFABC1')).toMatchObject({ currentValue: 1010 })
})
