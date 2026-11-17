import { expect, test } from '@playwright/test'

test('revisa candidatos da carteira sem confirmar ou gravar associação', async ({ page }) => {
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    let revisoesEnviadas = 0
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      const json = (dados: unknown) => new Response(JSON.stringify({ dados }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      if (url.pathname.endsWith('/tesouro-direto/lotes')) {
        if (init?.method === 'GET') return json([])
        return json({ id: 'lote-1', carteiraId: 'portfolio-1', dataReferencia: '2026-11-14', formato: 'html', estado: 'Pendente', criadoEmUtc: '2026-11-14T12:00:00Z', confirmadoEmUtc: null,
          previa: { carteiraId: 'portfolio-1', formato: 'html', titulo: 'Título sintético', vencimento: '2035-01-01', posicoes: [], movimentacoes: [], linhasNaoInterpretadas: [], avisos: [], exigeRevisao: true, coberturaCompleta: false, podeConfirmar: false } })
      }
      if (url.pathname.endsWith('/lotes/lote-1/conciliacao')) {
        const request = JSON.parse(String(init?.body)) as { ativoId: string | null }
        return json({ carteiraVersao: 1, dataReferencia: '2026-11-14', ativoId: request.ativoId,
          ativos: [{ ativoId: 'ativo-1', ticker: 'TESOURO35' }],
          eventos: [{ indice: 0, dadosInterpretados: { natureza: 'Compra', tipo: 'Crédito', data: '2026-10-01', quantidade: 2, preco: 100 }, candidatos: request.ativoId ? ['transacao-1'] : [] },
            { indice: 1, dadosInterpretados: { natureza: 'Venda', tipo: 'Débito', data: null, quantidade: 1, preco: 100 }, candidatos: [] }],
          transacoes: request.ativoId ? [{ id: 'transacao-1', tipo: 'Buy', data: '2026-10-01T12:00:00Z', quantidade: 2, preco: 100, posteriorBase: false }] : [], podeConfirmar: false })
      }
      if (url.pathname.endsWith('/lotes/lote-1/revisoes/0')) return json([])
      if (url.pathname.endsWith('/lotes/lote-1/revisoes/1')) return json([{ id: 'revisao-antiga', indiceEvento: 1, revisao: 1, decisao: 'sem_vinculo', ativoId: 'ativo-1', transacaoId: null, carteiraVersao: 0, criadoEmUtc: '2026-11-14T12:00:00Z', divergencias: ['carteiraAlterada'] }])
      if (url.pathname.endsWith('/lotes/lote-1/revisoes')) {
        const pedido = JSON.parse(String(init?.body)) as { indiceEvento: number; decisao: string; ativoId: string; transacaoId: string | null; carteiraVersao: number; revisaoAnterior: number }
        revisoesEnviadas++
        if (revisoesEnviadas > 1) return new Response(null, { status: 409 })
        if (pedido.indiceEvento !== 0 || pedido.decisao !== 'sem_vinculo' || pedido.ativoId !== 'ativo-1' || pedido.transacaoId !== null || pedido.carteiraVersao !== 1 || pedido.revisaoAnterior !== 0) return new Response(null, { status: 400 })
        return json({ id: 'revisao-1', ...pedido, revisao: 1, criadoEmUtc: '2026-11-14T12:00:00Z' })
      }
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
  await page.getByRole('button', { name: 'Revisar conciliação' }).click()
  await page.getByLabel('Posição da carteira para comparar').click()
  await page.getByRole('option', { name: 'TESOURO35' }).click()
  await page.getByRole('button', { name: 'Comparar movimentações' }).click()
  await expect(page.getByText('Movimentação do extrato 1 · Compra · 01/10/2026')).toBeVisible()
  await expect(page.getByText(/Compra · 01\/10\/2026 · quantidade 2 · preço/).first()).toBeVisible()
  await expect(page.getByText(/sugerida para comparação/)).toBeVisible()
  await expect(page.getByText(/sugestões não confirmam/)).toBeVisible()
  await page.getByRole('button', { name: 'Revisar esta movimentação' }).first().click()
  await expect(page.getByText('Nenhuma revisão anterior.')).toBeVisible()
  await page.getByLabel('Decisão para movimentação 1').click()
  await page.getByRole('option', { name: 'Sem vínculo com lançamento manual' }).click()
  await page.getByRole('button', { name: 'Salvar revisão' }).click()
  await expect(page.getByText('Revisão salva. A importação ainda não foi confirmada.')).toBeVisible()
  await expect(page.getByText('Revisão 1: sem vínculo com lançamento manual')).toBeVisible()
  await page.getByLabel('Decisão para movimentação 1').click()
  await page.getByRole('option', { name: /sugestão/ }).click()
  await page.getByRole('button', { name: 'Salvar revisão' }).click()
  await expect(page.getByRole('alert')).toContainText('Compare novamente as movimentações')
  await expect(page.getByRole('button', { name: 'Salvar revisão' })).toBeDisabled()
  await page.getByRole('button', { name: 'Revisar esta movimentação' }).last().click()
  await expect(page.getByText(/Não é possível propor vínculo: data não interpretada/)).toBeVisible()
  await expect(page.getByText(/Divergências: carteira alterada desde a revisão/)).toBeVisible()
  await expect(page.getByRole('button', { name: /confirmar/i })).toHaveCount(0)
})
