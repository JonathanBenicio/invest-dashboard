import { test, expect } from '@playwright/test'

test.describe('Acompanhamento de posições', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel('E-mail').fill('admin@investpro.com')
    await page.getByLabel('Senha').fill('password')
    await page.getByRole('button', { name: 'Entrar' }).click()
    await expect(page.getByText(/Patrim.*Total/).first()).toBeVisible({ timeout: 10_000 })
  })

  test('cria, altera e exclui uma carteira', async ({ page }) => {
    const name = `Carteira E2E ${Date.now()}`
    const renamed = `${name} atualizada`

    await page.getByRole('link', { name: 'Carteiras' }).click()
    await page.getByRole('button', { name: 'Nova carteira' }).click()
    await page.locator('#portfolio-name').fill(name)
    await page.getByRole('button', { name: 'Criar carteira' }).click()
    await expect(page.getByText(name, { exact: true })).toBeVisible()

    await page.locator(`button[aria-label$="${name}"]`).click()
    await page.getByRole('menuitem', { name: 'Editar' }).click()
    await page.locator('#edit-name').fill(renamed)
    await page.getByRole('button', { name: 'Salvar' }).click()
    await expect(page.getByText(renamed, { exact: true })).toBeVisible()

    await page.locator(`button[aria-label$="${renamed}"]`).click()
    await page.getByRole('menuitem', { name: 'Excluir' }).click()
    await page.getByRole('alertdialog').getByRole('button', { name: 'Excluir' }).click()
    await expect(page.getByText(renamed, { exact: true })).toHaveCount(0)
  })

  test('registra renda fixa com o valor atual do extrato', async ({ page }) => {
    const assetName = `CDB E2E ${Date.now()}`

    await page.getByRole('link', { name: 'Renda Fixa' }).click()
    await page.getByRole('button', { name: 'Adicionar contrato' }).click()
    await page.getByText('Selecione a carteira').click()
    await page.getByRole('option', { name: 'Carteira Principal' }).click()
    await page.locator('#name').fill(assetName)
    await page.locator('#institution').fill('Banco de teste')
    await page.locator('#investedValue').fill('5000')
    await page.locator('#rate').fill('110')
    await page.locator('#statementValue').fill('5075')
    await page.locator('#purchaseDate').fill('2025-01-15')
    await page.locator('#maturityDate').fill('2027-01-15')
    await page.getByRole('button', { name: 'Adicionar', exact: true }).click()

    await expect(page.getByRole('table').getByText(assetName, { exact: true })).toBeVisible()
  })

  test('exibe a origem e o horário da cotação carregada pela API', async ({ page }) => {
    await page.getByRole('link', { name: 'Renda Variável' }).click()

    await expect(page.getByText(/Cotação DEMO ·/).first()).toBeVisible()
    await expect(page.getByRole('status')).toHaveCount(0)
  })

  test('analisa posições da carteira usando os contratos da API', async ({ page }) => {
    await page.getByRole('link', { name: 'Análise' }).click()

    await expect(page.getByRole('heading', { name: 'Análise da carteira' })).toBeVisible()
    await expect(page.getByText(/Valores calculados a partir das posições/)).toBeVisible()
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
