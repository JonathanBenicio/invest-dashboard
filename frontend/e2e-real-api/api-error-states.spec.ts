import { expect, test } from '@playwright/test'
import { apiUrl, createAccessToken, jwtSecret, mockRefreshSession } from './auth'

const unavailableResponse = {
  status: 503,
  contentType: 'application/problem+json',
  body: JSON.stringify({
    type: 'about:blank',
    title: 'Service unavailable',
    detail: 'Serviço temporariamente indisponível.',
    status: 503,
  }),
}

test.beforeEach(async ({ page }) => {
  expect(jwtSecret, 'E2E_JWT_SECRET must be configured for the test browser session').toBeTruthy()
  await mockRefreshSession(page, createAccessToken(jwtSecret!))
})

test('CSV import distinguishes portfolio API failure from an empty portfolio list', async ({ page }) => {
  let requests = 0
  await page.route(`${apiUrl}/api/v1/portfolios*`, async route => {
    requests++
    await route.fulfill(unavailableResponse)
  })

  await page.goto('/importar')

  await expect(page.getByRole('alert')).toContainText('Não foi possível carregar as carteiras.')
  await expect(page.getByText('Crie uma carteira antes de importar operações.')).toHaveCount(0)
  const initialRequestCount = requests
  await page.getByRole('button', { name: 'Tentar novamente' }).click()
  await expect.poll(() => requests).toBeGreaterThan(initialRequestCount)
})

test('dashboard reports summary API failure and can retry', async ({ page }) => {
  let requests = 0
  await page.route(`${apiUrl}/api/v1/investments/summary`, async route => {
    requests++
    await route.fulfill(unavailableResponse)
  })

  await page.goto('/dashboard')

  await expect(page.getByRole('alert')).toContainText('Não foi possível carregar o resumo da carteira.')
  const initialRequestCount = requests
  await page.getByRole('button', { name: 'Tentar novamente' }).click()
  await expect.poll(() => requests).toBeGreaterThan(initialRequestCount)
})

test('variable-income search distinguishes provider failure from no matches and retries', async ({ page }) => {
  const apiRoot = `${apiUrl}/api/v1`
  const emptyPage = {
    dados: [],
    sucesso: true,
    mensagem: null,
    paginacao: {
      pagina: 1,
      itensPorPagina: 100,
      totalItens: 0,
      totalPaginas: 0,
      temProximaPagina: false,
      temPaginaAnterior: false,
    },
  }
  const portfoliosPage = {
    ...emptyPage,
    dados: [{
      id: 'cccccccc-dddd-eeee-ffff-000000000001',
      nome: 'Carteira de teste',
      posicoes: [],
      valorTotal: 0,
      totalInvestido: 0,
      ganhoTotal: 0,
      percentualGanho: 0,
      moeda: 'BRL',
      quantidadeAtivos: 0,
    }],
  }
  await page.route(`${apiRoot}/portfolios**`, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(portfoliosPage),
  }))
  await page.route(`${apiRoot}/investments**`, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(emptyPage),
  }))

  let searchRequests = 0
  await page.route(`${apiRoot}/market-data/search**`, async route => {
    searchRequests++
    await route.fulfill(unavailableResponse)
  })

  await page.goto('/renda-variavel')
  await page.getByRole('button', { name: 'Adicionar Ativo' }).click()
  await page.getByRole('combobox').filter({ hasText: 'Pesquisar ativo...' }).click()
  await page.getByPlaceholder('Digite o ticker (ex: PETR4)...').fill('PETR')

  await expect(page.getByRole('alert')).toContainText('Não foi possível pesquisar ativos.')
  await expect(page.getByText('Nenhum ativo encontrado.')).toHaveCount(0)
  const initialRequestCount = searchRequests
  await page.getByRole('button', { name: 'Tentar novamente' }).click()
  await expect.poll(() => searchRequests).toBeGreaterThan(initialRequestCount)
})
