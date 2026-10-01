import { expect, test } from '@playwright/test'
import { apiUrl, createAccessToken, createPortfolio, dateOnly, jwtSecret, mockRefreshSession } from './auth'

test('fixed-income detail explains why a projection is unavailable without a group rate', async ({ page, request }) => {
  expect(jwtSecret, 'E2E_JWT_SECRET must match the test API').toBeTruthy()
  const token = createAccessToken(jwtSecret!)
  const authorization = { Authorization: `Bearer ${token}` }
  const portfolio = await createPortfolio(request, token, `Premissa E2E ${Date.now()}`)
  const assetName = `CDB sem taxa ${Date.now()}`

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
  const position = (await purchaseResponse.json()).dados as { id: string }

  const projectionResponse = await request.get(
    `${apiUrl}/api/v1/investments/${position.id}/projecao-renda-fixa`,
    { headers: authorization },
  )
  expect(projectionResponse.status()).toBe(200)
  const projection = (await projectionResponse.json()).dados as {
    valorObservado: number
    valorProjetadoBruto: number | null
    motivo: string
  }
  expect(projection.valorObservado).toBe(5075)
  expect(projection.valorProjetadoBruto).toBeNull()
  expect(projection.motivo).toMatch(/taxa/i)

  await mockRefreshSession(page, token)
  await page.goto(`/investimento/${position.id}`)
  await expect(page.getByRole('heading', { name: assetName })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Estimativa até o vencimento' })).toBeVisible()
  await expect(page.getByText(projection.motivo)).toBeVisible()
  await expect(page.getByText('Estimativa bruta:')).toHaveCount(0)
  await expect(page.getByText(/não lança rendimento nem resgate no caixa/)).toBeVisible()
})
