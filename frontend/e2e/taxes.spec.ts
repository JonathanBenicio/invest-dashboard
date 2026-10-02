import { expect, test } from '@playwright/test'

test('admin updates a group rate and the history retains each unit and periodicity', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('E-mail').fill('admin@investpro.com')
  await page.getByLabel('Senha').fill('password')
  await page.getByRole('button', { name: 'Entrar' }).click()
  await expect(page.getByText(/Patrim.*Total/).first()).toBeVisible({ timeout: 10_000 })

  await page.getByRole('link', { name: 'Taxas e Indicadores' }).click()
  await expect(page.getByRole('heading', { name: 'Taxas e Indicadores' })).toBeVisible()
  await page.getByRole('button', { name: 'Editar SELIC' }).click()
  const editDialog = page.getByRole('dialog')
  await editDialog.locator('#edit-currentValue').fill('5.2')
  await editDialog.locator('#edit-previousValue').fill('12.75')
  await editDialog.locator('#edit-unit').selectOption('R$/US$')
  await editDialog.locator('#edit-periodicity').selectOption('Pontual')
  await editDialog.getByRole('button', { name: 'Atualizar' }).click()
  await expect(page.getByText('Taxa atualizada com sucesso.', { exact: true })).toBeVisible()
  await expect(page.getByText('5,2 R$/US$').first()).toBeVisible()

  await page.getByRole('button', { name: 'Histórico de SELIC' }).click()
  const historyDialog = page.getByRole('dialog')
  await expect(historyDialog.getByText('12,75%')).toBeVisible()
  await expect(historyDialog.getByText('5,2 R$/US$')).toBeVisible()
  await expect(historyDialog.getByText('Anual')).toBeVisible()
  await expect(historyDialog.getByText('Pontual')).toBeVisible()
  await expect(historyDialog.getByText('Usuário user-1')).toBeVisible()
})