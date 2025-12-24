import { test, expect } from '@playwright/test'

test.describe('Dashboard CRUD Operations', () => {

  test.beforeEach(async ({ page }) => {
    await page.goto('/dashboard')
    // Wait for something stable
    await expect(page.getByText('Patrimônio Total')).toBeVisible({ timeout: 10000 })
  })

  test('should create, update and delete a portfolio', async ({ page }) => {
    const timestamp = Date.now()
    const portfolioName = `Port ${timestamp}`
    const portfolioNameUpdated = `Port ${timestamp} Upd`

    await page.goto('/carteiras')

    // Create
    await page.click('text=Nova Carteira')
    await page.fill('#name', portfolioName)

    // Selects
    await page.click('text=Selecione o banco')
    await page.click('div[role="option"] >> nth=0')
    await page.click('text=Selecione o titular')
    await page.click('div[role="option"] >> nth=0')

    await page.fill('#description', 'Created via E2E test')
    await page.click('button:has-text("Criar Carteira")')

    // Verify in Table specifically (avoid Toast match)
    // Using .last() or specific text match in cell
    // We expect a cell with the exact name
    await expect(page.locator('table').getByText(portfolioName, { exact: true })).toBeVisible()

    // Edit
    // Locate the row. If multiple (unlikely with timestamp), take first/last.
    const portfolioRow = page.locator('tr', { hasText: portfolioName }).first()
    await portfolioRow.locator('button').click()
    await page.click('text=Editar')

    // Edit name
    // Try 'edit-name' as verified by subagent, fallback to 'name' selector logic if needed
    // We'll trust the edit dialog has an input we can fill. 
    // If 'edit-name' fails we might need to be smarter, but let's try standard locator by value if possible
    // await page.locator(`input[value="${portfolioName}"]`).fill(portfolioNameUpdated); // This is brittle
    // Let's use the ID 'edit-name' as we saw in the verification
    await page.locator('#edit-name').fill(portfolioNameUpdated)

    await page.click('button:has-text("Salvar Alterações")')

    await expect(page.locator('table').getByText(portfolioNameUpdated, { exact: true })).toBeVisible()

    // Delete
    const updatedRow = page.locator('tr', { hasText: portfolioNameUpdated }).first()
    await updatedRow.locator('button').click()
    await page.click('text=Excluir')
    await page.click('button:has-text("Excluir")') // Confirm

    await expect(page.locator('table').getByText(portfolioNameUpdated, { exact: true })).not.toBeVisible()
  })

  test('should create a fixed income investment', async ({ page }) => {
    const timestamp = Date.now()
    const assetName = `CDB ${timestamp}`

    await page.goto('/renda-fixa')

    await page.getByRole('button', { name: 'Adicionar Ativo' }).click()

    await page.fill('#name', assetName)
    await page.fill('#institution', 'Banco Robô')
    await page.fill('#investedValue', '5000')
    await page.fill('#rate', '110')
    await page.fill('#purchaseDate', '2024-12-24')
    await page.fill('#maturityDate', '2026-12-31')

    await page.getByRole('button', { name: 'Adicionar', exact: true }).click()

    // Verify in table
    await expect(page.locator('table').getByText(assetName, { exact: true })).toBeVisible()
  })

})
