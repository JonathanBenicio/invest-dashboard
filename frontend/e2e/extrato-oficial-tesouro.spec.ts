import { expect, test } from '@playwright/test'

test('mostra o extrato e volta ao início se a base mudar durante a paginação', async ({ page }) => {
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    const json = (dados: unknown) => new Response(JSON.stringify({ dados }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    })
    let baseMudou = false
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (url.pathname.endsWith('/auth/login')) {
        const resposta = await original(input, init)
        if (resposta.ok) sessionStorage.setItem('sessao-teste-extrato', await resposta.clone().text())
        return resposta
      }
      if (url.pathname.endsWith('/auth/refresh')) {
        const sessao = sessionStorage.getItem('sessao-teste-extrato')
        if (sessao) return new Response(sessao, { headers: { 'Content-Type': 'application/json' } })
      }
      if (url.pathname.endsWith('/investments/posicao-tesouro')) {
        return json({
          id: 'posicao-tesouro', carteiraId: 'portfolio-1', ativoId: 'ativo-tesouro',
          tipo: 'fixed_income', subtipo: 'TESOURO_DIRETO', ticker: 'TD35',
          nome: 'Tesouro Prefixado 2035', situacao: 'closed', quantidade: 0,
          totalInvestido: 0, precoMedio: 0, precoAtual: 22, valorAtual: 0, ganho: 0, percentualGanho: 0,
        })
      }
      if (url.pathname.includes('/investments/posicao-tesouro/')) {
        return json(url.pathname.endsWith('/projecao-renda-fixa')
          ? { valorObservado: 0, dataVencimento: '2035-01-01', valorProjetadoBruto: null }
          : [])
      }
      if (url.pathname.endsWith('/posicoes/ativo-tesouro/extrato')) {
        const estado = sessionStorage.getItem('estado-teste-extrato')
        if (estado === 'falha') {
          sessionStorage.setItem('estado-teste-extrato', 'vazio')
          return new Response(null, { status: 503 })
        }
        if (estado === 'vazio') {
          return json({ baseAtiva: null, eventos: [], proximoIndice: null, resumoUltimaImportacao: null })
        }
        if (url.searchParams.get('loteId') === 'lote-ativo') {
          baseMudou = true
          return new Response(null, { status: 409 })
        }
        return json({
          baseAtiva: { loteId: baseMudou ? 'lote-novo' : 'lote-ativo', dataReferencia: baseMudou ? '2026-11-16' : '2026-11-15', quantidade: 0 },
          eventos: [
            { indiceEvento: 1, dados: { dadosInterpretados: { data: '2026-11-13', natureza: 'Venda', tipo: 'Débito', valor: 400, quantidade: 1, preco: 400, camposNaoAplicaveis: ['taxaAnual'] } } },
            { indiceEvento: 2, dados: { dadosInterpretados: { data: '2026-11-13', natureza: 'Taxas', tipo: 'Débito', valor: 2 } } },
          ],
          proximoIndice: baseMudou ? null : 2,
          resumoUltimaImportacao: {
            loteAnteriorId: 'lote-anterior', dataReferenciaAnterior: '2026-11-12', dataReferenciaAtual: '2026-11-15', quantidadeAnterior: 1,
            quantidadeAtual: 0, variacaoQuantidade: -1,
            divergencias: [{ tipo: 'evento_novo', descricao: 'Venda observada no novo extrato', anterior: null, atual: null }],
          },
        })
      }
      return original(input, init)
    }
  })
  await page.goto('/login')
  await page.getByLabel('E-mail').fill('admin@investpro.com')
  await page.getByLabel('Senha').fill('password')
  await page.getByRole('button', { name: 'Entrar' }).click()
  await expect(page).not.toHaveURL(/\/login/)
  await page.goto('/investimento/posicao-tesouro')

  const extrato = page.getByRole('table', { name: 'Movimentações oficiais do Tesouro Direto' })
  await expect(extrato.getByText('Valor bruto do resgate')).toBeVisible()
  await expect(extrato.getByText('Cobrança de taxa')).toBeVisible()
  await extrato.getByText('Ver dados').first().click()
  await expect(extrato.getByText('Não aplicável')).toBeVisible()
  await page.getByText('O que mudou na última importação').click()
  await expect(page.getByText('Venda observada no novo extrato')).toBeVisible()

  for (const [width, height] of [[320, 800], [360, 800], [1280, 720], [1366, 768], [1920, 1080]]) {
    await page.setViewportSize({ width, height })
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width)
  }
  await page.setViewportSize({ width: 320, height: 800 })
  const rolagem = page.getByRole('region', { name: 'Rolagem das movimentações oficiais' })
  await rolagem.focus()
  await expect(rolagem).toBeFocused()
  expect(await rolagem.evaluate(elemento => elemento.scrollWidth > elemento.clientWidth)).toBe(true)

  await page.getByRole('button', { name: 'Próxima página' }).click()
  await expect(page.getByRole('alert')).toContainText('Uma nova importação atualizou o extrato.')
  await page.getByRole('button', { name: 'Voltar à primeira página' }).click()
  await expect(page.getByText('16/11/2026', { exact: true })).toBeVisible()

  await page.evaluate(() => sessionStorage.setItem('estado-teste-extrato', 'falha'))
  await page.reload()
  await expect(page.getByRole('alert')).toContainText('Não foi possível carregar o extrato.')
  await page.getByRole('button', { name: 'Tentar novamente' }).click()
  await expect(page.getByText('Esta posição ainda não tem um extrato confirmado.')).toBeVisible()
})
