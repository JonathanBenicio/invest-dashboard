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
      if (!url.pathname.endsWith('/tesouro-direto/lotes')) return original(input, init)
      if (init?.method === 'GET') return new Response(JSON.stringify({ dados: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      if (!(init?.body instanceof FormData) || !(init.body.get('arquivo') instanceof File) || url.searchParams.get('dataReferencia') !== '2026-11-14') return new Response(null, { status: 400 })
      const previa = {
        carteiraId: 'portfolio-1', formato: 'html', layout: 'portal', titulo: 'Título sintético', vencimento: '2035-01-01',
        posicoes: [{ numeroLinha: 1, camposBrutos: { valor_liquido: 'valor não reconhecido' }, dadosInterpretados: { camposNaoInterpretados: ['valorLiquido'] } }], movimentacoes: [{
          campos: [{ rotulo: 'Tipo:', valor: 'Compra' }, { rotulo: 'Preço:', valor: '1.23' }],
          dadosInterpretados: { data: null, natureza: 'Compra', tipo: 'Crédito', quantidade: 1, preco: null, taxaAnual: 10, valor: 1, camposNaoInterpretados: ['data', 'preco'] },
        }],
        linhasNaoInterpretadas: [7, 12], avisos: ['Revise os dados.'], exigeRevisao: true, coberturaCompleta: false, podeConfirmar: false,
      }
      return new Response(JSON.stringify({ dados: { id: 'lote-1', carteiraId: 'portfolio-1', dataReferencia: '2026-11-14', formato: 'html', estado: 'Previa', criadoEmUtc: '2026-11-14T12:00:00Z', confirmadoEmUtc: null, previa } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
  })
  await login(page)
  await page.getByRole('link', { name: /Importar opera/ }).click()
  await page.getByRole('tab', { name: 'Tesouro Direto' }).click()
  await page.getByLabel('Carteira de destino').click()
  await page.getByRole('option', { name: 'Carteira Principal' }).click()
  await page.getByLabel('Arquivo do extrato').setInputFiles({ name: 'extrato-sintetico.html', mimeType: 'text/html', buffer: Buffer.from('<html>teste</html>') })
  await expect(page.getByRole('button', { name: 'Analisar e salvar prévia' })).toBeDisabled()
  await page.getByLabel('Data de referência do extrato').fill('2026-11-14')
  await page.getByRole('button', { name: 'Analisar e salvar prévia' }).click()
  await expect(page.getByText('Data de referência informada: 14/11/2026')).toBeVisible()
  await expect(page.getByText('Título sintético')).toBeVisible()
  await expect(page.getByText('Compra', { exact: true })).toBeVisible()
  await expect(page.getByText('Não foi possível interpretar: data, preço. Confira os valores originais abaixo.')).toBeVisible()
  await expect(page.getByText('1.23', { exact: true })).toBeVisible()
  await expect(page.getByText('Não foi possível interpretar: valor líquido. Confira os valores originais abaixo.')).toBeVisible()
  await expect(page.getByText('valor não reconhecido', { exact: true })).toBeVisible()
  await expect(page.getByText(/valores abaixo vêm do arquivo enviado/i)).toBeVisible()
  await expect(page.getByText(/leitura pode não identificar todos os dados/i)).toBeVisible()
  await expect(page.getByText(/não registra operações nem altera a carteira/)).toBeVisible()
  await expect(page.getByText('Linhas: 7, 12.')).toBeVisible()
  await expect(page.getByRole('button', { name: /confirmar/i })).toHaveCount(0)
  await page.getByLabel('Arquivo do extrato').setInputFiles({ name: 'outro-extrato.html', mimeType: 'text/html', buffer: Buffer.from('<html>outro</html>') })
  await expect(page.getByText('Título sintético')).toHaveCount(0)
})

test('retoma uma prévia salva da carteira selecionada', async ({ page }) => {
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (!url.pathname.includes('/tesouro-direto/lotes')) return original(input, init)
      const resumo = { id: 'lote-1', carteiraId: 'portfolio-1', dataReferencia: '2026-11-14', formato: 'html', estado: 'Previa', criadoEmUtc: '2026-11-14T12:00:00Z', confirmadoEmUtc: null }
      const previa = { carteiraId: 'portfolio-1', formato: 'html', titulo: 'Prévia salva sintética', vencimento: null, posicoes: [], movimentacoes: [], linhasNaoInterpretadas: [], avisos: [], exigeRevisao: true, coberturaCompleta: false, podeConfirmar: false }
      const dados = url.pathname.endsWith('/lotes') ? [resumo] : { ...resumo, previa }
      return new Response(JSON.stringify({ dados }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
  })
  await login(page)
  await page.getByRole('link', { name: /Importar opera/ }).click()
  await page.getByRole('tab', { name: 'Tesouro Direto' }).click()
  await page.getByLabel('Carteira de destino').click()
  await page.getByRole('option', { name: 'Carteira Principal' }).click()
  await expect(page.getByText(/Referência 14\/11\/2026/)).toBeVisible()
  await page.getByRole('button', { name: 'Revisar prévia' }).click()
  await expect(page.getByText('Prévia salva sintética')).toBeVisible()
  await expect(page.getByText('Data de referência informada: 14/11/2026')).toBeVisible()
})

test('navega pelas páginas de prévias salvas', async ({ page }) => {
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (!url.pathname.endsWith('/tesouro-direto/lotes') || init?.method !== 'GET') return original(input, init)
      const pagina = url.searchParams.get('pagina') ?? '1'
      const resumos = Array.from({ length: pagina === '1' ? 50 : 1 }, (_, indice) => ({
        id: `lote-${pagina}-${indice}`, carteiraId: 'portfolio-1', dataReferencia: pagina === '1' ? '2026-11-14' : '2025-01-02', formato: 'html', estado: 'Pendente', criadoEmUtc: '2026-11-14T12:00:00Z', confirmadoEmUtc: null,
      }))
      return new Response(JSON.stringify({ dados: resumos }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
  })
  await login(page)
  await page.getByRole('link', { name: /Importar opera/ }).click()
  await page.getByRole('tab', { name: 'Tesouro Direto' }).click()
  await page.getByLabel('Carteira de destino').click()
  await page.getByRole('option', { name: 'Carteira Principal' }).click()
  await expect(page.getByText('Página 1', { exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Próxima página' }).click()
  await expect(page.getByText('Página 2', { exact: true })).toBeVisible()
  await expect(page.getByText(/Referência 02\/01\/2025/)).toBeVisible()
  await expect(page.getByRole('button', { name: 'Próxima página' })).toBeDisabled()
  await page.getByRole('button', { name: 'Página anterior' }).click()
  await expect(page.getByText('Página 1', { exact: true })).toBeVisible()
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
  await expect(page.getByText(/seleção apenas consulta preços públicos/i)).toBeVisible()
  await expect(page.getByText(/associação exigirá sua confirmação/i)).toBeVisible()
})
