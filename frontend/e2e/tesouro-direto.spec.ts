import { expect, test, type Page } from '@playwright/test'

test.describe.configure({ mode: 'serial' })

async function login(page: Page) {
  await page.goto('/login')
  await page.getByLabel('E-mail').fill('admin@investpro.com')
  await page.getByLabel('Senha').fill('password')
  await page.getByRole('button', { name: 'Entrar' }).click()
}

test('apresenta prévia de arquivo privado sem confirmar movimentações', async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 800 })
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    const previa = {
      carteiraId: 'portfolio-1', formato: 'html', layout: 'portal', titulo: 'Tesouro Prefixado 2035', vencimento: '2035-01-01',
      mesReferencia: 11, anoReferencia: 2026, instituicaoHtml: 'XP Investimentos CCTVM S/A',
      posicoes: [{ numeroLinha: 1, camposBrutos: { valor_liquido: 'valor não reconhecido' }, dadosInterpretados: { camposNaoInterpretados: ['valorLiquido'] } }], movimentacoes: [{
        campos: [
          { rotulo: 'Tipo:', valor: 'Compra' }, { rotulo: 'Preço:', valor: '1.23' },
          { rotulo: 'Taxa (% a.a.):', valor: '-' }, { rotulo: 'Data da aplicação:', valor: '02/03/2024' },
          { rotulo: 'Valor bruto:', valor: '1.234,56' }, { rotulo: 'Taxa da instituição devida (R$):', valor: '0,00' },
        ],
        dadosInterpretados: { data: null, natureza: 'Compra', tipo: 'Crédito', quantidade: 1, preco: null, taxaAnual: null, valor: 1, camposNaoInterpretados: ['data', 'preco'], camposNaoInformados: ['taxaAnual'] },
      }],
      linhasNaoInterpretadas: [7, 12], avisos: ['A leitura pode não identificar todos os dados do extrato.'], exigeRevisao: true, coberturaCompleta: false, podeConfirmar: false,
    }
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (url.pathname.endsWith('/tesouro-direto/previa')) {
        return new Response(JSON.stringify({ dados: previa }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }
      if (url.pathname.endsWith('/tesouro-direto/lotes/lote-1/conciliacao')) {
        const pedido = JSON.parse(String(init?.body ?? '{}')) as { ativoId: string | null }
        const ativoId = pedido.ativoId
        return new Response(JSON.stringify({ dados: {
          carteiraVersao: 3,
          dataReferencia: '2026-11-14',
          ativoId,
          ativos: [{ ativoId: 'asset-1', ticker: 'TD2035', nome: 'Tesouro Prefixado 2035', vencimento: '2035-01-01' }],
          eventos: [{ indice: 0, dadosInterpretados: previa.movimentacoes[0].dadosInterpretados, candidatos: [] }],
          transacoes: [],
          podeConfirmar: false,
          dataBaseAtiva: null,
          podeCriarPosicao: false,
          criarPosicao: false,
          comparacaoSnapshot: null,
          posicaoPropostaDisponivel: true,
          motivosCriacaoPosicao: [],
          importacaoInicialSemRevisao: false,
        } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }
      if (!url.pathname.endsWith('/tesouro-direto/lotes')) return original(input, init)
      if (init?.method === 'GET') return new Response(JSON.stringify({ dados: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      if (!(init?.body instanceof FormData) || !(init.body.get('arquivo') instanceof File) || url.searchParams.get('dataReferencia') !== '2026-11-14') return new Response(null, { status: 400 })
      return new Response(JSON.stringify({ dados: { id: 'lote-1', carteiraId: 'portfolio-1', dataReferencia: '2026-11-14', formato: 'html', estado: 'Previa', criadoEmUtc: '2026-11-14T12:00:00Z', confirmadoEmUtc: null, previa } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
  })
  await login(page)
  await page.getByRole('button', { name: 'Toggle Sidebar' }).click()
  const overflowMobileMenu = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)
  expect(overflowMobileMenu, 'menu móvel aberto não deve expandir a página').toBe(false)
  await page.getByRole('link', { name: /Importar opera/ }).click()
  await page.keyboard.press('Escape')
  await page.getByRole('tab', { name: 'Tesouro Direto' }).click()
  const analyzeButton = page.getByRole('button', { name: 'Analisar extrato' })
  await expect(analyzeButton).toBeDisabled()
  await page.getByLabel('Carteira de destino').click()
  await page.getByRole('option', { name: 'Carteira Principal' }).click()
  await expect(analyzeButton).toBeDisabled()
  await page.getByLabel('Arquivo do extrato').setInputFiles({ name: 'extrato-sintetico.html', mimeType: 'text/html', buffer: Buffer.from('<html>teste</html>') })
  await expect(analyzeButton).toBeEnabled()
  await analyzeButton.click()
  await expect(page.getByText(/Posição em novembro de 2026/)).toBeVisible()
  await expect(page.getByText('XP Investimentos CCTVM S/A')).toBeVisible()
  const salvarPrevia = page.getByRole('button', { name: 'Salvar e continuar' })
  await expect(salvarPrevia).toBeDisabled()
  const dataReferencia = page.getByLabel('Data de referência do saldo')
  await expect(dataReferencia).toHaveAttribute('min', '2026-11-01')
  await expect(dataReferencia).toHaveAttribute('max', '2026-11-30')
  await dataReferencia.fill('2026-10-31')
  await expect(salvarPrevia).toBeDisabled()
  await dataReferencia.fill('2026-11-14')
  await salvarPrevia.click()
  await expect(page.getByText('Tesouro Prefixado 2035')).toBeVisible()
  await expect(page.getByText(/Carteira Principal.*Referência 14\/11\/2026/)).toBeVisible()
  const continuePosition = page.getByRole('button', { name: 'Continuar para escolha da posição' })
  await expect(continuePosition).toBeEnabled()
  await continuePosition.click()
  const matchingPosition = page.getByLabel('Posição correspondente ao título e vencimento do extrato')
  await expect(matchingPosition).toContainText('Selecione uma opção')
  const continueReview = page.getByRole('button', { name: 'Continuar para revisão' })
  await expect(continueReview).toBeDisabled()
  await matchingPosition.click()
  const posicaoCompativel = page.getByRole('option', { name: /Tesouro Prefixado 2035.*01\/01\/2035.*TD2035/ })
  await expect(posicaoCompativel).toBeInViewport({ ratio: 1 })
  await posicaoCompativel.click()
  await expect(continueReview).toBeEnabled()
  await continueReview.click()
  await expect(page.getByText('Etapa 3 · Revise as movimentações')).toBeVisible()
  await expect(page.getByRole('button', { name: /confirmar importação/i })).toBeDisabled()
  await page.getByRole('button', { name: 'Voltar à escolha da posição' }).click()
  await expect(page.getByRole('heading', { name: 'Etapa 2 · Escolha a posição' })).toBeVisible()
  await expect(continueReview).toBeDisabled()
  await matchingPosition.click()
  await page.getByRole('option', { name: /Tesouro Prefixado 2035.*01\/01\/2035.*TD2035/ }).click()
  await continueReview.click()
  await expect(page.getByText('Etapa 3 · Revise as movimentações')).toBeVisible()
  const detalhes = page.getByText('Ver dados completos e avisos do arquivo')
  await detalhes.click()
  await expect(page.getByText('Compra', { exact: true })).toBeVisible()
  const linhaMovimentacao = page.getByRole('row', { name: /Revisar: data, preço.*Não informado: taxa do título/ })
  await expect(linhaMovimentacao).toBeVisible()
  await expect(page.getByText('1.23', { exact: true })).toBeVisible()
  await expect(page.getByText('Revisar: valor líquido', { exact: true })).toBeVisible()
  await expect(page.getByText('valor não reconhecido', { exact: true })).toBeVisible()
  await expect(page.getByText(/A leitura pode não identificar todos os dados/i)).toBeVisible()
  await expect(page.getByText('Linhas: 7, 12.')).toBeVisible()
  for (const width of [320, 360, 767, 768, 1280, 1366, 1920]) {
    await page.setViewportSize({ width, height: 800 })
    const dimensions = await page.evaluate(() => ({ viewport: window.innerWidth, document: document.documentElement.scrollWidth }))
    expect(dimensions.document, `documento não deve estourar a viewport de ${width}px`).toBeLessThanOrEqual(dimensions.viewport)
    const scroll = await page.locator('table[aria-label="Movimentações identificadas no extrato"]').evaluate(table => ({
      clientWidth: table.parentElement?.clientWidth ?? 0,
      scrollWidth: table.parentElement?.scrollWidth ?? 0,
    }))
    if (width < 768) expect(scroll.scrollWidth).toBeGreaterThan(scroll.clientWidth)
  }
  for (const estado of ['expandido', 'recolhido']) {
    await page.getByRole('button', { name: 'Toggle Sidebar' }).click()
    const overflowMenuDesktop = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)
    expect(overflowMenuDesktop, `menu desktop ${estado} não deve expandir a página`).toBe(false)
  }
  await page.getByRole('button', { name: 'Trocar arquivo ou carteira' }).click()
  await expect(page.getByRole('heading', { name: 'Etapa 1 · Arquivo e destino' })).toBeVisible()
  await page.getByLabel('Arquivo do extrato').setInputFiles({ name: 'outro-extrato.html', mimeType: 'text/html', buffer: Buffer.from('<html>outro</html>') })
  await expect(page.getByText('Etapa 1 concluída · Extrato analisado')).toHaveCount(0)
})

test('exige escolha explícita antes de confirmar a criação de uma posição nova', async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 800 })
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    const previa = {
      carteiraId: 'portfolio-1', formato: 'html', layout: 'portal', titulo: 'Tesouro Selic 2029', vencimento: '2029-03-01',
      posicoes: [{ numeroLinha: 1, camposBrutos: { titulo: 'Tesouro Selic 2029' }, dadosInterpretados: { camposNaoInterpretados: [] } }],
      movimentacoes: [], linhasNaoInterpretadas: [], avisos: [], exigeRevisao: false, coberturaCompleta: true, podeConfirmar: true,
    }
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (url.pathname.endsWith('/tesouro-direto/previa')) {
        return new Response(JSON.stringify({ dados: previa }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }
      if (url.pathname.endsWith('/tesouro-direto/lotes/lote-2/conciliacao')) {
        return new Response(JSON.stringify({ dados: {
          carteiraVersao: 1, dataReferencia: '2026-11-14', ativoId: null, ativos: [], eventos: [], transacoes: [],
          podeConfirmar: true, podeCriarPosicao: true, criarPosicao: true, posicaoPropostaDisponivel: true,
          importacaoInicialSemRevisao: true, criacaoPosicaoSemRevisao: true, motivosCriacaoPosicao: [], comparacaoSnapshot: null,
        } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }
      if (!url.pathname.endsWith('/tesouro-direto/lotes')) return original(input, init)
      if (init?.method === 'GET') return new Response(JSON.stringify({ dados: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      if (!(init?.body instanceof FormData) || !(init.body.get('arquivo') instanceof File)) return new Response(null, { status: 400 })
      return new Response(JSON.stringify({ dados: {
        id: 'lote-2', carteiraId: 'portfolio-1', dataReferencia: '2026-11-14', formato: 'html', estado: 'Previa',
        criadoEmUtc: '2026-11-14T12:00:00Z', confirmadoEmUtc: null, previa,
      } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
  })
  await login(page)
  await page.getByRole('button', { name: 'Toggle Sidebar' }).click()
  await page.getByRole('link', { name: /Importar opera/ }).click()
  await page.keyboard.press('Escape')
  await page.getByRole('tab', { name: 'Tesouro Direto' }).click()
  await page.getByLabel('Carteira de destino').click()
  await page.getByRole('option', { name: 'Carteira Principal' }).click()
  await page.getByLabel('Arquivo do extrato').setInputFiles({ name: 'extrato-vazio-sintetico.html', mimeType: 'text/html', buffer: Buffer.from('<html>teste</html>') })
  await page.getByRole('button', { name: 'Analisar extrato' }).click()
  await page.getByLabel('Data de referência do extrato').fill('2026-11-14')
  await page.getByRole('button', { name: 'Salvar e continuar' }).click()
  await page.getByRole('button', { name: 'Continuar para escolha da posição' }).click()
  const continueReview = page.getByRole('button', { name: 'Continuar para revisão' })
  await expect(continueReview).toBeDisabled()
  const matchingPosition = page.getByLabel('Posição correspondente ao título e vencimento do extrato')
  await matchingPosition.click()
  await page.getByRole('option', { name: /Criar posição: Tesouro Selic 2029.*01\/03\/2029/ }).click()
  await expect(continueReview).toBeEnabled()
  await continueReview.click()
  await expect(page.getByText('Etapa 3 · Confirme a posição inicial')).toBeVisible()
  await expect(page.getByText(/Este título ainda não tem uma posição correspondente/)).toBeVisible()
  await expect(page.getByRole('button', { name: /confirmar importação/i })).toBeEnabled()
})

test('bloqueia a gravação quando a instituição diverge e permite trocar a carteira', async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 800 })
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    const previa = {
      carteiraId: 'portfolio-bb', formato: 'html', layout: 'portal', titulo: 'Tesouro Prefixado 2035',
      vencimento: '2035-01-01', mesReferencia: 11, anoReferencia: 2026,
      instituicaoHtml: 'XP Investimentos CCTVM S/A', posicoes: [], movimentacoes: [],
      linhasNaoInterpretadas: [], avisos: [], exigeRevisao: false, coberturaCompleta: true, podeConfirmar: false,
    }
    const carteiras = [
      { id: 'portfolio-bb', nome: 'Carteira Banco do Brasil', instituicaoFinanceiraId: 'bank-bb', instituicaoFinanceira: 'Banco do Brasil' },
      { id: 'portfolio-xp', nome: 'Carteira XP', instituicaoFinanceiraId: 'broker-xp', instituicaoFinanceira: 'XP Investimentos' },
    ]
    let criacoes = 0
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (url.pathname.endsWith('/portfolios') && init?.method === 'GET') {
        return new Response(JSON.stringify({ dados: carteiras, sucesso: true, paginacao: { pagina: 1, itensPorPagina: 50, totalItens: 2, totalPaginas: 1, temProximaPagina: false, temPaginaAnterior: false } }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }
      if (url.pathname.endsWith('/instituicoes-financeiras')) {
        return new Response(JSON.stringify({ dados: [
          { id: 'bank-bb', nome: 'Banco do Brasil', categoria: 'Banco', personalizada: false },
          { id: 'broker-xp', nome: 'XP Investimentos', categoria: 'Corretora', personalizada: false },
        ] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }
      if (url.pathname.endsWith('/tesouro-direto/previa')) {
        return new Response(JSON.stringify({ dados: previa }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }
      if (url.pathname.endsWith('/tesouro-direto/lotes') && init?.method === 'GET') {
        return new Response(JSON.stringify({ dados: [] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }
      if (url.pathname.endsWith('/tesouro-direto/lotes') && init?.method === 'POST') {
        criacoes++
        if (url.pathname.includes('portfolio-bb')) {
          return new Response(JSON.stringify({ detail: 'O extrato identifica XP Investimentos CCTVM S/A, mas a carteira está vinculada a Banco do Brasil. Escolha uma carteira vinculada à instituição identificada no extrato.', status: 409 }), { status: 409, headers: { 'Content-Type': 'application/problem+json' } })
        }
        const lotePrevia = { ...previa, carteiraId: 'portfolio-xp' }
        return new Response(JSON.stringify({ dados: {
          id: 'lote-xp', carteiraId: 'portfolio-xp', dataReferencia: '2026-11-14', formato: 'html',
          estado: 'Previa', criadoEmUtc: '2026-11-25T12:00:00Z', confirmadoEmUtc: null, previa: lotePrevia,
        } }), { status: 201, headers: { 'Content-Type': 'application/json' } })
      }
      return original(input, init)
    }
    window.sessionStorage.setItem('tesouro-e2e-carregador', '0')
  })
  await login(page)
  await page.setViewportSize({ width: 360, height: 800 })
  await page.getByRole('button', { name: 'Toggle Sidebar' }).click()
  await page.getByRole('link', { name: /Importar opera/ }).click()
  await page.keyboard.press('Escape')
  await page.getByRole('tab', { name: 'Tesouro Direto' }).click()
  await page.getByLabel('Carteira de destino').click()
  await page.getByRole('option', { name: 'Carteira Banco do Brasil' }).click()
  await page.getByLabel('Arquivo do extrato').setInputFiles({ name: 'extrato-instituicao-sintetico.html', mimeType: 'text/html', buffer: Buffer.from('<html>teste</html>') })
  await page.getByRole('button', { name: 'Analisar extrato' }).click()
  await page.getByLabel('Data de referência do saldo').fill('2026-11-14')
  await expect(page.getByText('A instituição do extrato difere da carteira.')).toBeVisible()
  await expect(page.getByRole('alert')).toContainText('Importação bloqueada')
  const salvarPrevia = page.getByRole('button', { name: 'Salvar e continuar' })
  await expect(salvarPrevia).toBeDisabled()
  await expect(page.getByRole('button', { name: 'Escolher outra carteira' })).toBeVisible()

  await page.getByLabel('Carteira de destino').click()
  await page.getByRole('option', { name: 'Carteira XP' }).click()
  await page.getByRole('button', { name: 'Analisar extrato' }).click()
  await page.getByLabel('Data de referência do saldo').fill('2026-11-14')
  await expect(page.getByText('A instituição corresponde à carteira.')).toBeVisible()
  await page.getByRole('button', { name: 'Salvar e continuar' }).click()
  await expect(page.getByText('Carteira: Carteira XP · Referência 14/11/2026')).toBeVisible()
})

test('exclui somente prévias pendentes pela lista e pela prévia aberta', async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 800 })
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    let exclusaoListaFalhou = false
    let lotes = [
      { id: 'lote-lista', carteiraId: 'portfolio-1', dataReferencia: '2026-11-24', formato: 'html', estado: 'Pendente', criadoEmUtc: '2026-11-24T12:00:00Z', confirmadoEmUtc: null },
      { id: 'lote-aberto', carteiraId: 'portfolio-1', dataReferencia: '2026-11-23', formato: 'html', estado: 'Pendente', criadoEmUtc: '2026-11-23T12:00:00Z', confirmadoEmUtc: null },
      { id: 'lote-confirmado', carteiraId: 'portfolio-1', dataReferencia: '2026-11-22', formato: 'html', estado: 'Confirmado', criadoEmUtc: '2026-11-22T12:00:00Z', confirmadoEmUtc: '2026-11-22T13:00:00Z' },
    ]
    const json = (dados: unknown, status = 200) => new Response(JSON.stringify({ dados }), { status, headers: { 'Content-Type': 'application/json' } })
    const previa = { carteiraId: 'portfolio-1', formato: 'html', titulo: 'Tesouro Prefixado 2032', vencimento: '2032-01-01', posicoes: [], movimentacoes: [], linhasNaoInterpretadas: [], avisos: [], exigeRevisao: true, coberturaCompleta: false, podeConfirmar: false }
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      const loteMatch = url.pathname.match(/\/tesouro-direto\/lotes\/([^/]+)$/)
      if (url.pathname.endsWith('/tesouro-direto/lotes') && init?.method === 'GET') return json(lotes)
      if (loteMatch && init?.method === 'GET') {
        const resumo = lotes.find(item => item.id === loteMatch[1])
        return resumo ? json({ ...resumo, hashPrevia: 'hash-pendente', previa }) : new Response(null, { status: 404 })
      }
      if (loteMatch && init?.method === 'DELETE') {
        const loteId = loteMatch[1]
        if (loteId === 'lote-confirmado') return json({ sucesso: false, mensagem: 'Somente prévias pendentes podem ser excluídas.' }, 409)
        if (loteId === 'lote-lista' && !exclusaoListaFalhou) {
          exclusaoListaFalhou = true
          return json({ sucesso: false, mensagem: 'Não foi possível excluir esta prévia.' }, 500)
        }
        const lote = lotes.find(item => item.id === loteId)
        if (!lote || lote.estado !== 'Pendente') return new Response(null, { status: 404 })
        lotes = lotes.filter(item => item.id !== loteId)
        return new Response(null, { status: 204 })
      }
      return original(input, init)
    }
  })
  await login(page)
  await page.getByRole('button', { name: 'Toggle Sidebar' }).click()
  await page.getByRole('link', { name: /Importar opera/ }).click()
  await page.keyboard.press('Escape')
  await page.getByRole('tab', { name: 'Tesouro Direto' }).click()
  await page.getByLabel('Carteira de destino').click()
  await page.getByRole('option', { name: 'Carteira Principal' }).click()

  const linhaLista = page.locator('li').filter({ hasText: 'Referência 24/11/2026' })
  await linhaLista.getByRole('button', { name: 'Excluir prévia' }).click()
  const dialogoCancelar = page.getByRole('alertdialog')
  await expect(dialogoCancelar).toBeVisible()
  await page.setViewportSize({ width: 320, height: 720 })
  await expect(page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).resolves.toBe(true)
  await dialogoCancelar.getByRole('button', { name: 'Cancelar' }).click()
  await expect(linhaLista).toBeVisible()

  await linhaLista.getByRole('button', { name: 'Excluir prévia' }).click()
  await page.getByRole('alertdialog').getByRole('button', { name: 'Excluir prévia' }).click()
  await expect(page.getByRole('alert')).toContainText('Não foi possível excluir a prévia')
  await expect(linhaLista).toBeVisible()

  await linhaLista.getByRole('button', { name: 'Excluir prévia' }).click()
  await page.getByRole('alertdialog').getByRole('button', { name: 'Excluir prévia' }).click()
  await expect(linhaLista).toHaveCount(0)

  const linhaAberta = page.locator('li').filter({ hasText: 'Referência 23/11/2026' })
  await linhaAberta.getByRole('button', { name: 'Revisar prévia' }).click()
  const excluirAberta = page.getByRole('button', { name: 'Excluir prévia' })
  await expect(excluirAberta).toBeVisible()
  await excluirAberta.click()
  await page.getByRole('alertdialog').getByRole('button', { name: 'Excluir prévia' }).click()
  await expect(page.getByRole('heading', { name: 'Etapa 1 · Arquivo e destino' })).toBeVisible()
  await expect(page.getByText(/Referência 23\/11\/2026/)).toHaveCount(0)

  const linhaConfirmada = page.locator('li').filter({ hasText: 'Referência 22/11/2026' })
  await expect(linhaConfirmada.getByRole('button', { name: 'Ver extrato confirmado' })).toBeVisible()
  await expect(linhaConfirmada.getByRole('button', { name: 'Excluir prévia' })).toHaveCount(0)
  for (const width of [320, 360, 1280]) {
    await page.setViewportSize({ width, height: 800 })
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width)
  }
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
  await page.getByText('Ver dados completos e avisos do arquivo').click()
  await expect(page.getByText('Referência do saldo informada: 14/11/2026')).toBeVisible()
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
