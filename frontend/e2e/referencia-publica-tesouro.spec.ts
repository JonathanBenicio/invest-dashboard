import { expect, test } from '@playwright/test'

test('sugere nome e vencimento correspondentes e consulta preço sob demanda', async ({ page }) => {
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (url.pathname.endsWith('/tesouro-direto/lotes')) {
        if (init?.method === 'GET') return new Response(JSON.stringify({ dados: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
        if (!(init?.body instanceof FormData) || !(init.body.get('arquivo') instanceof File) || url.searchParams.get('dataReferencia') !== '2026-11-14') return new Response(null, { status: 400 })
        const previa = {
          carteiraId: 'portfolio-1', formato: 'html', layout: 'portal', titulo: 'Tesouro Prefixado 2035', vencimento: '2035-01-01',
          posicoes: [], movimentacoes: [], linhasNaoInterpretadas: [], avisos: [], exigeRevisao: true, coberturaCompleta: false, podeConfirmar: false,
        }
        return new Response(JSON.stringify({ dados: { id: 'lote-1', carteiraId: 'portfolio-1', dataReferencia: '2026-11-14', formato: 'html', estado: 'Previa', criadoEmUtc: '2026-11-14T12:00:00Z', confirmadoEmUtc: null, previa } }), { status: 201, headers: { 'Content-Type': 'application/json' } })
      }
      if (url.pathname === '/api/v1/tesouro-direto/titulos') return new Response(JSON.stringify({ dados: {
        origem: 'Tesouro Transparente', urlFonte: 'https://www.tesourotransparente.gov.br/', coletadoEmUtc: '2026-11-14T12:00:00Z', ultimaDataDisponivel: '2026-11-12',
        titulos: [
          { tipoTitulo: 'Tesouro Prefixado', dataVencimento: '2035-01-01', ultimaDataDisponivel: '2026-11-12' },
          { tipoTitulo: 'Tesouro Prefixado', dataVencimento: '2036-01-01', ultimaDataDisponivel: '2026-11-12' },
          { tipoTitulo: 'Tesouro IPCA+', dataVencimento: '2035-01-01', ultimaDataDisponivel: '2026-11-12' },
        ],
      } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      if (url.pathname === '/api/v1/tesouro-direto/cotacoes') return new Response(JSON.stringify({ dados: {
        origem: 'Tesouro Transparente', cotacoes: [{ dataBase: '2026-11-12', puBaseManha: 456.78 }],
      } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      return original(input, init)
    }
  })
  await page.goto('/login')
  await page.getByLabel('E-mail').fill('admin@investpro.com')
  await page.getByLabel('Senha').fill('password')
  await page.getByRole('button', { name: 'Entrar' }).click()
  await page.getByRole('link', { name: /Importar opera/ }).click()
  await page.getByRole('tab', { name: 'Tesouro Direto' }).click()
  await page.getByLabel('Carteira de destino').click()
  await page.getByRole('option', { name: 'Carteira Principal' }).click()
  await page.getByLabel('Arquivo do extrato').setInputFiles({ name: 'sintetico.html', mimeType: 'text/html', buffer: Buffer.from('<html></html>') })
  await page.getByLabel('Data de referência do extrato').fill('2026-11-14')
  await page.getByRole('button', { name: 'Analisar e salvar prévia' }).click()
  await page.getByRole('button', { name: 'Buscar títulos públicos correspondentes' }).click()
  await expect(page.getByRole('button', { name: 'Ver cotação deste título' })).toHaveCount(1)
  await page.getByRole('button', { name: 'Ver cotação deste título' }).click()
  await expect(page.getByText(/PU base R\$\s*456,78 em 12\/11\/2026/)).toBeVisible()
  await expect(page.getByText(/não associa nem altera sua posição/)).toBeVisible()
})
