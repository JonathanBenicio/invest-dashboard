import { expect, test } from '@playwright/test'

test('consulta indicadores oficiais por período e informa falhas sem alterar taxas manuais', async ({ page }) => {
  // Em São Paulo ainda é dia 13, embora UTC já tenha avançado para o dia 14.
  await page.clock.setFixedTime(new Date('2026-11-14T01:30:00Z'))
  await page.addInitScript(() => {
    const fetchOriginal = window.fetch.bind(window)
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (!url.pathname.startsWith('/api/v1/indicadores/')) return fetchOriginal(input, init)

      const selic = url.pathname.endsWith('/selic')
      if (!selic && window.sessionStorage.getItem('falhar-ipca') === 'sim') {
        return new Response(JSON.stringify({ detail: 'Fonte indisponível' }), { status: 503 })
      }
      return new Response(JSON.stringify({ dados: {
        indicador: selic ? 'SELIC' : 'IPCA', codigoSgs: selic ? 11 : 433,
        unidade: selic ? 'percentual ao dia' : 'variação percentual mensal',
        periodicidade: selic ? 'diária' : 'mensal',
        dataDe: url.searchParams.get('dataDe'), dataAte: url.searchParams.get('dataAte'),
        origem: `Banco Central do Brasil — SGS série ${selic ? 11 : 433}`,
        atualizadoEmUtc: '2025-02-03T12:00:00Z',
        pontos: [{ data: url.searchParams.get('dataDe'), valor: selic ? 0.1234 : -0.45 }],
      } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
  })

  await page.goto('/login')
  await page.getByLabel('E-mail').fill('admin@investpro.com')
  await page.getByLabel('Senha').fill('password')
  await page.getByRole('button', { name: 'Entrar' }).click()
  await page.getByRole('link', { name: 'Taxas e Indicadores' }).click()

  await expect(page.getByLabel('Data final')).toHaveValue('2026-11-13')
  await expect(page.getByLabel('Data final')).toHaveAttribute('max', '2026-11-13')
  await expect(page.getByLabel('Data inicial')).toHaveValue('2026-08-15')

  const selic = page.getByRole('region', { name: 'Selic diária' })
  const ipca = page.getByRole('region', { name: 'IPCA mensal' })
  await expect(selic.getByText('0,1234%')).toBeVisible()
  await expect(ipca.getByText('-0,45%')).toBeVisible()
  await expect(selic.getByText(/SGS série 11/)).toBeVisible()
  await expect(ipca.getByText(/SGS série 433/)).toBeVisible()
  await expect(page.getByText(/não alteram as taxas manuais/)).toBeVisible()

  await page.getByLabel('Data inicial').fill('2025-01-01')
  await page.getByLabel('Data final').fill('2025-02-01')
  await page.getByRole('button', { name: 'Consultar período' }).click()
  await expect(selic.getByText('01/01/2025')).toBeVisible()
  await expect(ipca.getByText('01/01/2025')).toBeVisible()

  await page.evaluate(() => window.sessionStorage.setItem('falhar-ipca', 'sim'))
  await page.getByLabel('Data inicial').fill('2025-01-02')
  await page.getByRole('button', { name: 'Consultar período' }).click()
  await expect(ipca.getByRole('alert')).toContainText('Não foi possível consultar IPCA mensal', { timeout: 15_000 })
  await expect(selic.getByText('02/01/2025')).toBeVisible()
})

test('indicadores oficiais permanecem acessíveis sem grupo de taxas manuais', async ({ page }) => {
  await page.addInitScript(() => {
    const fetchOriginal = window.fetch.bind(window)
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (url.pathname === '/api/v1/grupos-carteiras') {
        return new Response(JSON.stringify({ dados: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }
      if (url.pathname.startsWith('/api/v1/indicadores/')) {
        return new Response(JSON.stringify({ dados: {
          indicador: 'SELIC', codigoSgs: 11, unidade: 'percentual ao dia', periodicidade: 'diária',
          dataDe: url.searchParams.get('dataDe'), dataAte: url.searchParams.get('dataAte'),
          origem: 'Banco Central do Brasil — SGS série 11', atualizadoEmUtc: '2025-02-03T12:00:00Z', pontos: [],
        } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }
      return fetchOriginal(input, init)
    }
  })
  await page.goto('/login')
  await page.getByLabel('E-mail').fill('admin@investpro.com')
  await page.getByLabel('Senha').fill('password')
  await page.getByRole('button', { name: 'Entrar' }).click()
  await page.getByRole('link', { name: 'Taxas e Indicadores' }).click()
  await expect(page.getByText('Crie um grupo ou aceite um convite para gerenciar taxas manuais.')).toBeVisible()
  await expect(page.getByRole('region', { name: 'Selic diária' }).getByText('Nenhum dado publicado no período selecionado.')).toBeVisible()
})
