import { expect, test } from '@playwright/test'
import { apiUrl, createAccessToken, createGroup, jwtSecret, mockRefreshSession } from './auth'

test('portfolio creation persists its group, holder, institution and visibility', async ({ page, request }) => {
  expect(jwtSecret, 'E2E_JWT_SECRET must match the test API').toBeTruthy()
  const token = createAccessToken(jwtSecret!)
  const authorization = { Authorization: `Bearer ${token}` }
  const group = await createGroup(request, token, `Workspace E2E ${Date.now()}`)
  const holderName = `Titular E2E ${Date.now()}`
  const institutionName = `Corretora E2E ${Date.now()}`

  const holderResponse = await request.post(`${apiUrl}/api/v1/grupos-carteiras/${group.id}/titulares`, {
    headers: authorization,
    data: { nome: holderName, parentesco: 'Filha' },
  })
  expect(holderResponse.status()).toBe(200)
  const holder = (await holderResponse.json()).dados as { id: string }

  const institutionResponse = await request.post(`${apiUrl}/api/v1/instituicoes-financeiras?grupoId=${group.id}`, {
    headers: authorization,
    data: { nome: institutionName },
  })
  expect(institutionResponse.status()).toBe(201)
  const institution = (await institutionResponse.json()).dados as { id: string }

  await mockRefreshSession(page, token)
  await page.goto('/carteiras')
  await page.getByRole('button', { name: 'Nova carteira' }).click()
  const portfolioName = `Carteira workspace E2E ${Date.now()}`
  await page.locator('#portfolio-name').fill(portfolioName)
  await page.locator('#portfolio-group').selectOption(group.id)
  await expect(page.locator('#portfolio-holder-profile')).toContainText(holderName)
  await page.locator('#portfolio-holder-profile').selectOption(holder.id)
  await page.locator('#portfolio-institution').selectOption(institution.id)
  await page.locator('#portfolio-visibility').selectOption('PublicaDoGrupo')
  await page.getByRole('button', { name: 'Criar carteira', exact: true }).click()

  await expect(page.getByText('A carteira foi salva.', { exact: true })).toBeVisible()
  const portfoliosResponse = await request.get(`${apiUrl}/api/v1/portfolios?grupoId=${group.id}`, {
    headers: authorization,
  })
  expect(portfoliosResponse.status()).toBe(200)
  const portfolios = (await portfoliosResponse.json()).dados as Array<{
    id: string
    nome: string
    grupoId: string
    titularId: string
    instituicaoFinanceiraId: string
    visibilidade: string
  }>
  expect(portfolios).toContainEqual(expect.objectContaining({
    nome: portfolioName,
    grupoId: group.id,
    titularId: holder.id,
    instituicaoFinanceiraId: institution.id,
    visibilidade: 'PublicaDoGrupo',
  }))
})
