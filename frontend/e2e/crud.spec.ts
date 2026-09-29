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
})
