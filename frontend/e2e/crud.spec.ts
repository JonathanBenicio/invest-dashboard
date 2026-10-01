import { test, expect } from '@playwright/test'

test.describe('Acompanhamento de posições', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel('E-mail').fill('admin@investpro.com')
    await page.getByLabel('Senha').fill('password')
    await page.getByRole('button', { name: 'Entrar' }).click()
    await expect(page.getByText(/Patrim.*Total/).first()).toBeVisible({ timeout: 10_000 })
  })

  test('configura titular, grupo e visibilidade na nova carteira', async ({ page }) => {
    await page.getByRole('link', { name: 'Carteiras' }).click()
    await page.getByRole('button', { name: 'Nova carteira' }).click()
    await page.locator('#portfolio-name').fill(`Carteira E2E ${Date.now()}`)
    await page.locator('#portfolio-holder').fill('Titular E2E')
    await page.locator('#portfolio-visibility').selectOption('PublicaDoGrupo')
    await expect(page.locator('#portfolio-group')).toHaveValue('group-1')
    await expect(page.locator('#portfolio-visibility')).toHaveValue('PublicaDoGrupo')
    await expect(page.locator('#portfolio-holder')).toHaveValue('Titular E2E')
  })

  test('exige modalidade fiscal antes de confirmar a venda de ação', async ({ page }) => {
    await page.getByRole('link', { name: /Renda Vari/ }).click()
    const stockRow = page.getByRole('row').filter({ hasText: 'PETR4' })
    await stockRow.getByRole('button').click()
    await page.getByRole('menuitem', { name: 'Vender' }).click()

    const modality = page.locator('#trade-tax-modality')
    await expect(modality).toBeVisible()
    await expect(modality).toHaveAttribute('required', '')
    await page.locator('#trade-quantity').fill('1')
    await page.locator('#trade-price').fill('40')
    const confirm = page.getByRole('button', { name: 'Confirmar venda' })
    await expect(confirm).toBeDisabled()
    await modality.selectOption('Comum')
    await expect(confirm).toBeEnabled()
  })

  test('exige modalidade fiscal para venda tributável importada por CSV', async ({ page }) => {
    await page.getByRole('link', { name: /Importar opera/ }).click()
    await page.locator('input[type="file"]').setInputFiles({
      name: 'venda-sem-modalidade.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from(
        'ticker;type;quantity;unitPrice;fees;transactionDate;assetClass;modalidadeFiscal\nPETR4;Sell;10;40;0;2026-09-29;ACAO;\n',
      ),
    })

    await expect(page.getByText(/Informe modalidadeFiscal/)).toBeVisible()
    await expect(page.getByRole('table').getByText(/Obrigat/)).toBeVisible()
  })

  test('registra renda fixa com o valor atual do extrato', async ({ page }) => {
    const assetName = `CDB E2E ${Date.now()}`
    const now = new Date()
    const today = new Date(now.getTime() - now.getTimezoneOffset() * 60_000).toISOString().slice(0, 10)

    await page.getByRole('link', { name: 'Renda Fixa' }).click()
    await page.getByRole('button', { name: 'Adicionar contrato' }).click()
    await page.getByText('Selecione a carteira').click()
    await page.getByRole('option', { name: 'Carteira Principal' }).click()
    await page.locator('#name').fill(assetName)
    await page.locator('#institution').fill('Banco de teste')
    await page.locator('#investedValue').fill('5000')
    await page.locator('#rate').fill('110')
    await page.locator('#statementValue').fill('5075')
    await page.locator('#purchaseDate').fill(today)
    await page.locator('#maturityDate').fill('2027-01-15')
    await page.getByRole('button', { name: 'Adicionar', exact: true }).click()

    await expect(page.getByRole('table').getByText(assetName, { exact: true })).toBeVisible()
  })

  test('exibe a origem e o horário da cotação carregada pela API', async ({ page }) => {
    await page.getByRole('link', { name: /Renda Vari/ }).click()

    await expect(page.getByText(/Cotação DEMO ·/).first()).toBeVisible()
    await expect(page.getByRole('status')).toHaveCount(0)
  })

  test('mostra a projeção consolidada e identifica posições sem premissa', async ({ page }) => {
    await page.getByRole('link', { name: 'Renda Fixa' }).click()
    await expect(page.getByText('Projeção bruta até o vencimento')).toBeVisible()
    await expect(page.getByText('Sem projeção consolidada porque faltam dados ou premissas para todas as posições.')).toBeVisible()
    await expect(page.getByText(/112\.500,00/)).toBeVisible()
    await expect(page.getByText('Posições pendentes: TESOURO-IPCA-2029')).toBeVisible()
  })
  test('analisa posições da carteira usando os contratos da API', async ({ page }) => {
    await page.getByRole('link', { name: 'Análise' }).click()

    await expect(page.getByRole('heading', { name: 'Análise da carteira' })).toBeVisible()
    await expect(page.getByText(/Valores calculados a partir das posições/)).toBeVisible()
  })

  test('abre detalhes de renda fixa com os campos portugueses do histórico', async ({ page }) => {
    await page.getByRole('link', { name: 'Renda Fixa' }).click()
    const investmentRow = page.getByRole('row').filter({ hasText: 'CDB Banco Inter' })
    await investmentRow.getByRole('button').click()
    await page.getByRole('menuitem', { name: 'Ver Detalhes' }).click()

    await expect(page.getByRole('heading', { name: 'CDB Banco Inter' })).toBeVisible()
    await expect(page.getByText('Estimativa até o vencimento')).toBeVisible()
    await expect(page.getByText(/Último valor de extrato:/)).toBeVisible()
    await expect(page.getByText(/Título vencido/)).toBeVisible()
    await expect(page.getByText(/não movimenta o caixa/)).toBeVisible()
    await expect(page.getByText('Histórico de preço')).toBeVisible()
    await expect(page.locator('.recharts-area')).toHaveCount(1)
    await expect(page.getByText('Something went wrong!')).toHaveCount(0)
  })

  test('lê CSV, valida linhas e envia operações para a carteira selecionada', async ({ page }) => {
    await page.getByRole('link', { name: 'Importar operações' }).click()
    await page.getByRole('combobox', { name: 'Carteira de destino' }).click()
    await page.getByRole('option', { name: 'Carteira Principal' }).click()
    await page.locator('input[type="file"]').setInputFiles({
      name: 'operacoes.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from(
        'ticker;type;quantity;unitPrice;fees;transactionDate;assetClass;name;sector;issuer;subtype;indexer;interestRate;maturityDate;initialStatementValue\nPETR4;Buy;10;35,50;1;2026-09-29;ACAO;Petrobras;Petróleo;;;;;;\nRFABC1;Buy;1000;1;0;2026-09-29;RENDA_FIXA;CDB;;Banco de teste;CDB;CDI;110;2028-01-15;1010\nINVLD;Buy;0;10;0;2026-09-29;ACAO;Linha inválida;;;;;;;;\n',
      ),
    })

    await expect(page.getByRole('table').getByText('PETR4')).toBeVisible()
    await expect(page.getByRole('table').getByText('RFABC1')).toBeVisible()
    await expect(page.getByText('Quantidade deve ser maior que zero.')).toBeVisible()
    await page.getByRole('button', { name: 'Importar 2 operações' }).click()
    await expect(page.getByRole('cell', { name: 'Operação registrada.' })).toHaveCount(2)
    await expect(page.getByText('2 operações registradas; 1 linha precisa de correção.', { exact: true })).toBeVisible()
  })
})
