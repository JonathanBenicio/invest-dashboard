import { expect, test } from '@playwright/test'

const carteiraPessoaA = 'carteira-a'

type TestContribution = {
  posicaoId: string
  carteiraId: string
  carteiraNome: string
  titularId: string
  titular: string
  instituicaoFinanceiraId: string
  instituicaoFinanceira: string
  quantidade: number
  totalInvestido: number
  valorAtual: number
  ganho: number
  dataAvaliacao: string
  situacao: string
}

type TestConsolidated = Record<string, string | number | null>

declare global {
  interface Window {
    __contributionFixture: () => TestContribution[]
    __consolidatedFixture: (holderId?: string) => TestConsolidated
  }
}

test('agrupa posições, filtra situação e titular, apresenta datas e alterna a visão por carteira', async ({ page }) => {
  await page.addInitScript(() => {
    const originalFetch = window.fetch.bind(window)
    const json = (dados: unknown, status = 200) => new Response(JSON.stringify({ dados, sucesso: true }), {
      status,
      headers: { 'Content-Type': 'application/json' },
    })
    const portfolio = (id: string, nome: string, titularId: string, titular: string, instituicaoId: string, instituicao: string) => ({
      id,
      nome,
      grupoId: 'grupo-familia',
      titularId,
      titular,
      titularVinculado: true,
      instituicaoFinanceiraId: instituicaoId,
      instituicaoFinanceira: instituicao,
      visibilidade: 'PublicaDoGrupo',
      posicoes: [],
      valorTotal: 15500,
      totalInvestido: 15000,
      ganhoTotal: 500,
      percentualGanho: 3.33,
      moeda: 'BRL',
      quantidadeAtivos: 1,
    })
    const session = {
      tokenAcesso: 'e2e-token',
      expiraEmSegundos: 900,
      requerConfirmacaoEmail: false,
      usuario: { id: 'usuario-teste', nome: 'Teste', email: 'teste@example.test', perfil: 'user' },
    }

    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      const path = url.pathname

      if (path.endsWith('/auth/refresh')) return json(session)
      if (path.endsWith('/grupos-carteiras')) return json([{ id: 'grupo-familia', nome: 'Família', papel: 'Investidor' }])
      if (path === '/api/v1/portfolios') {
        const semGrupo = {
          ...portfolio('carteira-solo', 'Carteira sem grupo', '', '', 'banco-solo', 'Banco solo'),
          grupoId: null,
          titularId: null,
          titular: null,
          titularVinculado: false,
          visibilidade: 'Privada',
        }
        const portfolios = [
          portfolio('carteira-a', 'Carteira Pessoa A', 'titular-a', 'Pessoa A', 'banco-xp', 'XP Investimentos'),
          portfolio('carteira-ana', 'Carteira Ana', 'titular-ana', 'Ana', 'banco-itau', 'Itaú'),
          semGrupo,
        ]
        return new Response(JSON.stringify({ dados: portfolios, sucesso: true, paginacao: {
          pagina: 1, itensPorPagina: 100, totalItens: portfolios.length, totalPaginas: 1,
          temProximaPagina: false, temPaginaAnterior: false,
        } }), { headers: { 'Content-Type': 'application/json' } })
      }
      if (path === '/api/v1/portfolios/projecao-renda-fixa') {
        return json({ valorObservado: 31000, valorProjetadoBruto: null, quantidadePosicoes: 2, estaCompleta: false, posicoesSemProjecao: [] })
      }
      if (path === '/api/v1/investments/consolidated') {
        const situation = url.searchParams.get('situacao')
        const holderId = url.searchParams.get('titularId')
        const portfolioId = url.searchParams.get('carteiraId')
        const selected = holderId || (portfolioId && portfolioId !== 'carteira-solo')
          ? { ...window.__consolidatedFixture(holderId), escopo: portfolioId ? 'carteira' : 'titular' }
          : window.__consolidatedFixture()
        const ungroupedPosition = {
          ...selected,
          id: 'posicao-solo', chaveConsolidacao: 'posicao:posicao-solo', posicaoId: 'posicao-solo',
          grupoId: null, grupoNome: null, titularId: null, titular: null,
          escopo: 'carteira', carteiraId: 'carteira-solo', carteiraNome: 'Carteira sem grupo',
          quantidadeCarteiras: 1, ativoId: 'ativo-solo', nome: 'CDB individual sem grupo', ticker: 'CDB-SOLO',
          dataAvaliacaoMaisAntiga: null, dataAvaliacaoMaisRecente: null,
          datasAvaliacaoDiferentes: false, datasAvaliacaoCompletas: false,
        }
        const encerrada = {
          ...selected,
          id: 'grupo-familia:ativo-fimgr1', chaveConsolidacao: 'grupo-familia:ativo-fimgr1',
          posicaoId: null, ativoId: 'ativo-fimgr1', ticker: 'FIMGR1', nome: 'Ativo encerrado',
          quantidadeCarteiras: 1, situacao: 'closed', quantidade: 0, precoMedio: 0, precoAtual: null,
          totalInvestido: 0, valorAtual: 0, ganho: 0, percentualGanho: 0,
        }
        const normalRows = portfolioId === 'carteira-solo'
          ? [ungroupedPosition]
          : portfolioId || holderId || url.searchParams.has('grupoId')
            ? [selected]
            : [selected, ungroupedPosition]
        const rows = situation === 'closed' ? [encerrada] : situation === 'all' ? [...normalRows, encerrada] : normalRows
        return new Response(JSON.stringify({ dados: rows, sucesso: true, paginacao: {
          pagina: 1, itensPorPagina: 10, totalItens: rows.length, totalPaginas: 1,
          temProximaPagina: false, temPaginaAnterior: false,
        } }), { headers: { 'Content-Type': 'application/json' } })
      }
      const groupDetail = path.match(/\/investments\/consolidated\/grupo-familia\/ativo-cdb-2028$/)
      if (groupDetail) {
        const holderId = url.searchParams.get('titularId')
        const institutionId = url.searchParams.get('instituicaoFinanceiraId')
        let rows = window.__contributionFixture()
        if (holderId) rows = rows.filter(row => row.titularId === holderId)
        if (institutionId) rows = rows.filter(row => row.instituicaoFinanceiraId === institutionId)
        const selectedHolder = holderId || (institutionId ? rows[0]?.titularId : undefined)
        return json({ consolidado: window.__consolidatedFixture(selectedHolder), carteiras: rows })
      }
      if (path === '/api/v1/investments/posicao-a') {
        return json({
          id: 'posicao-a',
          carteiraId: 'carteira-a',
          carteiraNome: 'Carteira Pessoa A',
          grupoId: 'grupo-familia',
          titularId: 'titular-a',
          titular: 'Pessoa A',
          instituicaoFinanceira: 'XP Investimentos',
          ativoId: 'ativo-cdb-2028',
          situacao: 'open',
          nome: 'CDB Banco Alfa 2028',
          ticker: 'CDB-ALFA-2028',
          tipo: 'fixed_income',
          subtipo: 'CDB',
          quantidade: 100,
          precoMedio: 150,
          precoAtual: 155,
          totalInvestido: 15000,
          valorAtual: 15500,
          ganho: 500,
          percentualGanho: 3.33,
          moeda: 'BRL',
          emissor: 'Banco Alfa',
          taxaJuros: 12,
          indexador: 'CDI',
          dataVencimento: '2028-12-31',
        })
      }
      if (path === '/api/v1/investments/posicao-a/transactions') {
        return new Response(JSON.stringify({ dados: [], sucesso: true, paginacao: {
          pagina: 1, itensPorPagina: 10, totalItens: 0, totalPaginas: 0,
          temProximaPagina: false, temPaginaAnterior: false,
        } }), { headers: { 'Content-Type': 'application/json' } })
      }
      if (path === '/api/v1/investments/posicao-a/projecao-renda-fixa') {
        return json({ posicaoId: 'posicao-a', ticker: 'CDB-ALFA-2028', valorObservado: 15500, valorProjetadoBruto: null, dataVencimento: '2028-12-31', estadoProjecao: 'Indisponivel' })
      }
      if (path === '/api/v1/investments/posicao-a/history') return json([])
      if (path === '/api/v1/investments/posicao-solo') {
        return json({
          id: 'posicao-solo',
          carteiraId: 'carteira-solo',
          carteiraNome: 'Carteira sem grupo',
          grupoId: null,
          titularId: null,
          titular: null,
          instituicaoFinanceira: 'Banco solo',
          ativoId: 'ativo-solo',
          situacao: 'open',
          nome: 'CDB individual sem grupo',
          ticker: 'CDB-SOLO',
          tipo: 'fixed_income',
          subtipo: 'CDB',
          quantidade: 100,
          precoMedio: 150,
          precoAtual: 155,
          totalInvestido: 15000,
          valorAtual: 15500,
          ganho: 500,
          percentualGanho: 3.33,
          moeda: 'BRL',
          emissor: 'Banco solo',
          taxaJuros: 12,
          indexador: 'CDI',
          dataVencimento: '2028-12-31',
        })
      }
      if (path === '/api/v1/investments/posicao-solo/transactions') return new Response(JSON.stringify({
        dados: [], sucesso: true, paginacao: { pagina: 1, itensPorPagina: 10, totalItens: 0, totalPaginas: 0, temProximaPagina: false, temPaginaAnterior: false },
      }), { headers: { 'Content-Type': 'application/json' } })
      if (path === '/api/v1/investments/posicao-solo/projecao-renda-fixa') return json({
        posicaoId: 'posicao-solo', ticker: 'CDB-SOLO', valorObservado: 15500, valorProjetadoBruto: null, dataVencimento: '2028-12-31', estadoProjecao: 'Indisponivel',
      })
      if (path === '/api/v1/investments/posicao-solo/history') return json([])

      return originalFetch(input, init)
    }

    window.__contributionFixture = () => [
      {
        posicaoId: 'posicao-a', carteiraId: 'carteira-a', carteiraNome: 'Carteira Pessoa A',
        titularId: 'titular-a', titular: 'Pessoa A', instituicaoFinanceiraId: 'banco-xp',
        instituicaoFinanceira: 'XP Investimentos', dataAvaliacao: '2026-11-20', quantidade: 100, totalInvestido: 15000,
        valorAtual: 15500, ganho: 500, situacao: 'open',
      },
      {
        posicaoId: 'posicao-ana', carteiraId: 'carteira-ana', carteiraNome: 'Carteira Ana',
        titularId: 'titular-ana', titular: 'Ana', instituicaoFinanceiraId: 'banco-itau',
        instituicaoFinanceira: 'Itaú', dataAvaliacao: '2026-11-21', quantidade: 100, totalInvestido: 15000,
        valorAtual: 15500, ganho: 500, situacao: 'open',
      },
    ]
    window.__consolidatedFixture = (holderId?: string) => {
      const rows = window.__contributionFixture().filter(row => !holderId || row.titularId === holderId)
      const quantity = rows.reduce((total, row) => total + row.quantidade, 0)
      const invested = rows.reduce((total, row) => total + row.totalInvestido, 0)
      const current = rows.reduce((total, row) => total + row.valorAtual, 0)
      const position = holderId ? rows[0] : undefined
      return {
        id: position?.posicaoId ?? 'grupo-familia:ativo-cdb-2028',
        chaveConsolidacao: 'grupo-familia:ativo-cdb-2028',
        posicaoId: position?.posicaoId ?? null,
        grupoId: 'grupo-familia',
        grupoNome: 'Família',
        titularId: position?.titularId ?? null,
        titular: position?.titular ?? null,
        carteiraId: position?.carteiraId ?? null,
        carteiraNome: position?.carteiraNome ?? null,
        escopo: holderId ? 'titular' : 'grupo',
        quantidadeCarteiras: rows.length,
        dataAvaliacaoMaisAntiga: rows.map(row => row.dataAvaliacao).sort()[0],
        dataAvaliacaoMaisRecente: (() => {
          const datas = rows.map(row => row.dataAvaliacao).sort()
          return datas[datas.length - 1]
        })(),
        datasAvaliacaoDiferentes: new Set(rows.map(row => row.dataAvaliacao)).size > 1,
        datasAvaliacaoCompletas: rows.every(row => Boolean(row.dataAvaliacao)),
        ativoId: 'ativo-cdb-2028',
        nome: 'CDB Banco Alfa 2028',
        ticker: 'CDB-ALFA-2028',
        tipo: 'fixed_income',
        subtipo: 'CDB',
        situacao: 'open',
        quantidade: quantity,
        precoMedio: invested / quantity,
        precoAtual: current / quantity,
        totalInvestido: invested,
        valorAtual: current,
        ganho: current - invested,
        percentualGanho: invested ? ((current - invested) / invested) * 100 : 0,
        moeda: 'BRL',
        emissor: 'Banco Alfa',
        taxaJuros: 12,
        indexador: 'CDI',
        dataVencimento: '2028-12-31',
      }
    }
  })

  await page.goto('/renda-fixa')
  await expect(page.getByRole('heading', { name: 'Renda Fixa' })).toBeVisible()
  await page.locator('#investment-portfolio-filter').click()
  await page.getByRole('option', { name: /Carteira sem grupo/ }).click()
  const ungroupedRow = page.getByRole('row').filter({ hasText: 'CDB individual sem grupo' })
  await expect(ungroupedRow).toBeVisible()
  await ungroupedRow.getByRole('button').click()
  await page.getByRole('menuitem', { name: 'Ver Detalhes' }).click()
  await expect(page.getByRole('heading', { name: 'CDB individual sem grupo' })).toBeVisible()
  await expect(page.getByText('Carteira sem grupo')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Ver consolidado do grupo' })).toHaveCount(0)
  await page.goto('/renda-fixa')
  await page.locator('#investment-portfolio-filter').click()
  await page.getByRole('option', { name: 'Todas as carteiras' }).click()
  await page.locator('#investment-group-filter').click()
  await page.getByRole('option', { name: 'Família' }).click()
  const groupRow = page.getByRole('row').filter({ hasText: 'CDB Banco Alfa 2028' })
  await expect(groupRow).toBeVisible()
  await expect(groupRow).toContainText('2 carteiras')
  await expect(groupRow).toContainText('Datas de avaliação diferentes: 20/11/2026 a 21/11/2026')
  await expect(page.getByRole('row').filter({ hasText: 'Ativo encerrado' })).toHaveCount(0)

  await page.locator('#investment-holder-filter').click()
  await page.getByRole('option', { name: 'Pessoa A' }).click()
  await expect(groupRow).toContainText('Pessoa A')
  await expect(groupRow).toContainText('1 carteira')

  const investmentRow = page.getByRole('row').filter({ hasText: 'CDB Banco Alfa 2028' })
  await investmentRow.getByRole('button').click()
  await page.getByRole('menuitem', { name: 'Ver Detalhes' }).click()
  await expect(page.getByText('Consolidado das carteiras acessíveis')).toBeVisible()
  await expect(page.getByText('R$ 15.500,00').first()).toBeVisible()
  await expect(page.getByRole('row').filter({ hasText: 'Carteira Pessoa A' })).toBeVisible()
  await expect(page.getByRole('cell', { name: '20/11/2026', exact: true })).toBeVisible()

  await page.locator('#consolidado-titular').click()
  await page.locator('#consolidado-titular').selectOption('')
  await expect(page.getByText('R$ 31.000,00').first()).toBeVisible()
  await expect(page.getByRole('row').filter({ hasText: 'Carteira Ana' })).toBeVisible()

  await page.locator('#consolidado-carteira').selectOption(carteiraPessoaA)
  await expect(page.getByText('Visão por carteira')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'CDB Banco Alfa 2028' })).toBeVisible()
  await page.getByRole('button', { name: 'Ver consolidado do grupo' }).click()
  await expect(page.getByText('Consolidado das carteiras acessíveis')).toBeVisible()
  await expect(page.getByText('R$ 31.000,00').first()).toBeVisible()

  for (const width of [360, 320]) {
    await page.setViewportSize({ width, height: 800 })
    await page.goto('/renda-fixa?groupId=grupo-familia')
    await expect(page.getByRole('heading', { name: 'Renda Fixa' })).toBeVisible()
    await expectNoHorizontalPageOverflow(page)

    const mobileInvestmentCard = page.locator('div.cursor-pointer').filter({ hasText: 'CDB Banco Alfa 2028' }).first()
    await expect(mobileInvestmentCard).toBeVisible()
    await mobileInvestmentCard.click()
    await expect(page.getByText('Consolidado das carteiras acessíveis')).toBeVisible()
    await expect(page.getByText('R$ 31.000,00').first()).toBeVisible()
    await expectNoHorizontalPageOverflow(page)
  }

  await page.setViewportSize({ width: 1920, height: 1080 })
  await expect(page.getByText('Consolidado das carteiras acessíveis')).toBeVisible()
  await expectNoHorizontalPageOverflow(page)

  await page.goto('/renda-fixa?groupId=grupo-familia')
  await page.locator('#investment-situation-filter').click()
  await page.getByRole('option', { name: 'Encerradas' }).click()
  await expect(page.getByRole('row').filter({ hasText: 'Ativo encerrado' })).toBeVisible()
  await expect(page.getByRole('row').filter({ hasText: 'CDB Banco Alfa 2028' })).toHaveCount(0)

  await page.locator('#investment-situation-filter').click()
  await page.getByRole('option', { name: 'Todas' }).click()
  await expect(page.getByRole('row').filter({ hasText: 'Ativo encerrado' })).toBeVisible()
  await expect(page.getByRole('row').filter({ hasText: 'CDB Banco Alfa 2028' })).toBeVisible()
})

async function expectNoHorizontalPageOverflow(page: import('@playwright/test').Page) {
  const widths = await page.evaluate(() => ({
    viewport: window.innerWidth,
    document: document.documentElement.scrollWidth,
    body: document.body.scrollWidth,
  }))
  expect(widths.document, `document width ${widths.document}px exceeds viewport ${widths.viewport}px`).toBeLessThanOrEqual(widths.viewport)
  expect(widths.body, `body width ${widths.body}px exceeds viewport ${widths.viewport}px`).toBeLessThanOrEqual(widths.viewport)
}
