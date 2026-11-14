import { expect, test, type Page } from '@playwright/test'

test.describe.configure({ mode: 'serial' })

async function login(page: Page) {
  await page.goto('/login')
  await page.getByLabel('E-mail').fill('admin@investpro.com')
  await page.getByLabel('Senha').fill('password')
  await page.getByRole('button', { name: 'Entrar' }).click()
}

test('apresenta prévia de arquivo privado sem confirmar movimentações', async ({ page }) => {
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (!url.pathname.endsWith('/tesouro-direto/previa')) return original(input, init)
      if (!(init?.body instanceof FormData) || !(init.body.get('arquivo') instanceof File)) return new Response(null, { status: 400 })
      return new Response(JSON.stringify({ dados: {
        carteiraId: 'portfolio-1', formato: 'html', layout: 'portal', titulo: 'Título sintético', vencimento: '2035-01-01',
        posicoes: [], movimentacoes: [{ campos: [{ rotulo: 'Tipo:', valor: 'Compra' }] }],
        linhasNaoInterpretadas: [], avisos: ['Revise os dados.'], exigeRevisao: true, coberturaCompleta: false, podeConfirmar: false,
      } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
  })
  await login(page)
  await page.getByRole('link', { name: /Importar opera/ }).click()
  await page.getByRole('tab', { name: 'Tesouro Direto' }).click()
  await page.getByLabel('Carteira de destino').click()
  await page.getByRole('option', { name: 'Carteira Principal' }).click()
  await page.getByLabel('Arquivo do extrato').setInputFiles({ name: 'extrato-sintetico.html', mimeType: 'text/html', buffer: Buffer.from('<html>teste</html>') })
  await page.getByRole('button', { name: 'Analisar arquivo' }).click()
  await expect(page.getByText('Título sintético')).toBeVisible()
  await expect(page.getByText('Compra', { exact: true })).toBeVisible()
  await expect(page.getByText(/não registra operações nem altera a carteira/)).toBeVisible()
  await expect(page.getByText(/confirmação e a conciliação ainda não estão disponíveis/i)).toBeVisible()
  await expect(page.getByRole('button', { name: /confirmar/i })).toHaveCount(0)
})

test('consulta histórico público com a data e a origem do Tesouro', async ({ page }) => {
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (!url.pathname.startsWith('/api/v1/tesouro-direto/')) return original(input, init)
      const metadados = { origem: 'Tesouro Transparente', urlFonte: 'https://www.tesourotransparente.gov.br/', coletadoEmUtc: '2026-11-13T12:00:00Z', ultimaDataDisponivel: '2026-11-12' }
      const dados = url.pathname.endsWith('/titulos')
        ? { ...metadados, titulos: [{ tipoTitulo: 'Tesouro Prefixado', dataVencimento: '2032-01-01', ultimaDataDisponivel: '2026-11-12' }] }
        : { ...metadados, cotacoes: [{ dataBase: '2026-11-12', puBaseManha: 456.78, puVendaManha: 455.55, taxaVendaManha: 10.25 }] }
      return new Response(JSON.stringify({ dados }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
  })
  await login(page)
  await page.getByRole('link', { name: 'Taxas e Indicadores' }).click()
  await page.getByRole('button', { name: 'Consultar cotações do Tesouro' }).click()
  await page.getByLabel('Título e vencimento').click()
  await page.getByRole('option', { name: /Tesouro Prefixado/ }).click()
  await expect(page.getByText('R$ 456,78')).toBeVisible()
  await expect(page.getByText(/última data disponível 12\/11\/2026/)).toBeVisible()
  await expect(page.getByText(/não representa sua posição pessoal/)).toBeVisible()
})
