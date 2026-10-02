import { expect, test } from '@playwright/test'
import { apiUrl, createAccessToken, createGroup, jwtSecret, mockRefreshSession } from './auth'

test('tax edits persist unit and periodicity in the real history', async ({ page, request }) => {
  expect(jwtSecret, 'E2E_JWT_SECRET must match the test API').toBeTruthy()
  const token = createAccessToken(jwtSecret!)
  const authorization = { Authorization: `Bearer ${token}` }
  const group = await createGroup(request, token, `Taxas E2E ${Date.now()}`)
  const symbol = `FX${Date.now().toString().slice(-5)}`
  const referenceDate = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString().slice(0, 10)

  const rateResponse = await request.post(`${apiUrl}/api/v1/taxes?grupoId=${group.id}`, {
    headers: authorization,
    data: {
      nome: 'Câmbio de teste',
      simbolo: symbol,
      valorAtual: 5.25,
      valorAnterior: 5,
      descricao: 'Valor inicial do E2E',
      origem: 'Manual E2E',
      unidade: 'Percentual',
      periodicidade: 'Mensal',
      dataReferencia: referenceDate,
    },
  })
  expect(rateResponse.status()).toBe(201)
  const rate = (await rateResponse.json()).dados as { id: string }

  await mockRefreshSession(page, token)
  await page.goto('/taxas')
  await page.getByLabel('Grupo dos indicadores').selectOption(group.id)
  await expect(page.getByRole('heading', { name: symbol })).toBeVisible()
  await page.getByRole('button', { name: `Editar ${symbol}` }).click()
  await page.locator('#edit-currentValue').fill('5.5')
  await page.locator('#edit-previousValue').fill('5.25')
  await page.locator('#edit-unit').selectOption('R$/US$')
  await page.locator('#edit-periodicity').selectOption('Pontual')
  await page.getByRole('button', { name: 'Atualizar', exact: true }).click()

  await expect(page.getByText('Taxa atualizada com sucesso.', { exact: true })).toBeVisible()
  await expect(page.getByText('Pontual · referência', { exact: false })).toBeVisible()
  await page.getByRole('button', { name: `Histórico de ${symbol}` }).click()
  const historyDialog = page.getByRole('dialog', { name: `Histórico · ${symbol}` })
  await expect(historyDialog).toBeVisible()
  await expect(historyDialog.getByRole('table')).toContainText('5,25%')
  await expect(historyDialog.getByRole('table')).toContainText('Mensal')
  await expect(historyDialog.getByRole('table')).toContainText('5,5 R$/US$')
  await expect(historyDialog.getByRole('table')).toContainText('Pontual')

  const historyResponse = await request.get(`${apiUrl}/api/v1/taxes/${rate.id}/historico?grupoId=${group.id}`, {
    headers: authorization,
  })
  expect(historyResponse.status()).toBe(200)
  const history = (await historyResponse.json()).dados as Array<{
    unidadeAnterior: string
    periodicidadeAnterior: string
    unidadeNova: string
    periodicidadeNova: string
  }>
  expect(history).toContainEqual(expect.objectContaining({
    unidadeAnterior: 'Percentual',
    periodicidadeAnterior: 'Mensal',
    unidadeNova: 'R$/US$',
    periodicidadeNova: 'Pontual',
  }))
})
