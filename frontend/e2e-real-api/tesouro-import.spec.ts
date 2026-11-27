import { expect, test, type Page } from '@playwright/test'
import { apiUrl, createAccessToken, createPortfolio, jwtSecret, mockRefreshSession } from './auth'

function operacao(data: string, natureza: string, tipo: string, quantidade: string, preco: string, valor: string) {
  const campos = [
    ['Data', data], ['Natureza', natureza], ['Tipo', tipo], ['Quantidade', quantidade],
    ['Preço (R$)', preco], ['Taxa (% a.a.)', '-'], ['Valor (R$)', valor],
  ]
  return `<div class="td-posicao-detalhada__info">${campos.map(([rotulo, valorCampo]) =>
    `<p class="td-posicao-detalhada__info__valor">${rotulo}: <span class="td-posicao-detalhada__info__data">${valorCampo}</span></p>`,
  ).join('')}</div>`
}

type Cenario = 'abertura' | 'posicao-ausente' | 'venda-integral'

function extrato(cenario: Cenario) {
  const compra = operacao('02/03/2024', 'Compra', 'Crédito', '1,00', '20,00', '20,00')
  const venda = operacao('04/03/2024', 'Venda', 'Débito', '1,00', '22,00', '22,00')
  const taxa = operacao('04/03/2024', 'Taxas', 'Débito', '-', '-', '0,25')
  const posicao = `<tr class="saldo-table-data-values"><td></td>${[
    '02/03/2024', '1,00', '20,00', '20,00', '10,50%', '10,50%', '10,50%', '22,00', '1',
    '22,50%', '0,00', '0,00', '0,00', '0,00', '22,00',
  ].map(valor => `<td><span>${valor}</span></td>`).join('')}</tr>`
  return `<!doctype html><html lang="pt-BR"><body>
    <div class="td-custom-meus-investimentos-titulo">
      <h1 class="td-meus-investimentos-detalhe__titulo_card">Tesouro Prefixado 2035</h1>
      <div class="td-meu-investimento-titulo">Posição em Março/2024
        <div class="td-meu-investimento-datalhe-posicao"><span class="td-meu-investimento-titulo--valor">R$ 22,00</span></div>
      </div>
      <table><tbody><tr class="saldo-table-vencimento"><th><strong>VENCIMENTO: 01/01/2035</strong></th></tr></tbody></table>
      ${compra}${cenario === 'venda-integral' ? venda + taxa : ''}
      <table><tbody>
        <tr class="saldo-table-data-names"><td></td><td>DATA DA APLICAÇÃO</td><td>QUANTIDADE DE TÍTULOS</td><td>PREÇO DO TÍTULO NA APLICAÇÃO</td><td>VALOR INVESTIDO</td><td>RENTABILIDADE CONTRATADA</td></tr>
        <tr class="saldo-table-data-names"><td colspan="2">RENTABILIDADE CONTRATADA</td><td>VALOR BRUTO</td><td>TEMPO DA APLICAÇÃO</td><td>ALÍQUOTA</td><td colspan="2">IMPOSTO PREVISTO</td><td colspan="2">TAXA DEVIDA</td><td>VALOR LÍQUIDO</td></tr>
        <tr class="saldo-table-data-names"><td>ANUALIZADA</td><td>ACUMULADA</td><td>I.R.</td><td>IOF</td><td>B3</td><td>INSTITUIÇÃO FINANCEIRA</td></tr>
        ${cenario === 'abertura' ? posicao : ''}
      </tbody></table>
    </div>
  </body></html>`
}

async function enviarExtrato(page: Page, carteira: string, referencia: string, cenario: Cenario) {
  await page.goto('/importar')
  await page.getByRole('tab', { name: 'Tesouro Direto' }).click()
  await page.getByLabel('Carteira de destino').click()
  await page.getByRole('option', { name: carteira, exact: true }).click()
  await page.getByLabel('Arquivo do extrato').setInputFiles({
    name: 'extrato-sintetico.html',
    mimeType: 'text/html',
    buffer: Buffer.from(extrato(cenario)),
  })
  await page.getByRole('button', { name: 'Analisar extrato' }).click()
  await expect(page.getByText(/Posição em março de 2024/)).toBeVisible()
  await page.getByLabel('Data de referência do saldo').fill(referencia)
  await page.getByRole('button', { name: 'Salvar e continuar' }).click()
  await page.getByRole('button', { name: 'Continuar para escolha da posição' }).click()
  await page.getByLabel('Posição correspondente ao título e vencimento do extrato').click()
  const opcao = cenario === 'abertura' ? /^Criar posição: Tesouro Prefixado 2035/ : /^Tesouro Prefixado 2035.*01\/01\/2035/
  await page.getByRole('option', { name: opcao }).click()
  await page.getByRole('button', { name: 'Continuar para revisão' }).click()
}

async function confirmar(page: Page) {
  await page.getByRole('button', { name: 'Confirmar importação' }).click()
  await page.getByRole('alertdialog').getByRole('button', { name: 'Confirmar lote' }).click()
  await expect(page.getByText(/Importação confirmada em/)).toBeVisible()
}

test('importa, encerra por venda integral e consulta o extrato oficial pela API real', async ({ page, request }) => {
  expect(jwtSecret, 'E2E_JWT_SECRET deve corresponder à API de teste').toBeTruthy()
  const token = createAccessToken(jwtSecret!)
  await mockRefreshSession(page, token)
  const carteira = await createPortfolio(request, token, `Tesouro E2E ${Date.now()}`)

  await enviarExtrato(page, carteira.nome, '2024-03-03', 'abertura')
  await expect(page.getByText(/Este título ainda não tem uma posição correspondente/)).toBeVisible()
  await confirmar(page)

  const authorization = { Authorization: `Bearer ${token}` }
  const investimentos = await request.get(`${apiUrl}/api/v1/investments?carteiraId=${carteira.id}`, { headers: authorization })
  expect(investimentos.ok()).toBe(true)
  const posicoes = (await investimentos.json()).dados as { id: string; ativoId: string; quantidade: number }[]
  expect(posicoes).toHaveLength(1)
  const [posicao] = posicoes
  expect(posicao.quantidade).toBe(1)

  await enviarExtrato(page, carteira.nome, '2024-03-05', 'posicao-ausente')
  await expect(page.getByText(/Não há venda posterior à base ativa/)).toBeVisible()
  await expect(page.getByRole('button', { name: 'Confirmar importação' })).toBeDisabled()
  const posicaoPreservada = await request.get(`${apiUrl}/api/v1/investments/${posicao.id}`, { headers: authorization })
  expect(posicaoPreservada.ok()).toBe(true)
  expect((await posicaoPreservada.json()).dados).toMatchObject({ quantidade: 1, situacao: 'open' })

  await enviarExtrato(page, carteira.nome, '2024-03-05', 'venda-integral')
  await expect(page.getByText(/vendas registradas desde a base ativa zeram o saldo/)).toBeVisible()
  for (const indice of [1, 2, 3]) {
    const linha = page.getByRole('listitem').filter({ has: page.getByText(new RegExp(`^Movimentação do extrato ${indice} ·`)) })
    await linha.getByRole('button', { name: 'Revisar esta movimentação' }).click()
    await linha.getByLabel(`Decisão para movimentação ${indice}`).click()
    await page.getByRole('option', { name: 'Sem vínculo com lançamento manual' }).click()
    await linha.getByRole('button', { name: 'Salvar revisão' }).click()
    await expect(linha.getByText(/Revisão salva/)).toBeVisible()
  }
  await page.getByLabel(/Revisei as divergências e aceito o saldo proposto como base/).check()
  await confirmar(page)

  await page.goto(`/investimento/${posicao.id}`)
  const extratoOficial = page.getByRole('table', { name: 'Movimentações oficiais do Tesouro Direto' })
  await expect(extratoOficial.getByRole('row')).toHaveCount(4)
  await expect(extratoOficial.getByText('Valor bruto do resgate')).toBeVisible()
  await expect(extratoOficial.getByText('Cobrança de taxa')).toBeVisible()
  await page.getByText('O que mudou na última importação').click()
  await expect(page.getByText(/Quantidade anterior: 1 · Atual: 0 · Variação: -1/)).toBeVisible()

  const posicaoAtual = await request.get(`${apiUrl}/api/v1/investments/${posicao.id}`, { headers: authorization })
  expect(posicaoAtual.ok()).toBe(true)
  expect((await posicaoAtual.json()).dados).toMatchObject({ quantidade: 0, situacao: 'closed' })
  const transacoes = await request.get(`${apiUrl}/api/v1/transactions/portfolio/${carteira.id}`, { headers: authorization })
  expect(transacoes.ok()).toBe(true)
  expect((await transacoes.json()).dados).toEqual([])
})
