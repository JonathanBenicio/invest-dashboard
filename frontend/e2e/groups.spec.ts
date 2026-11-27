import { expect, test } from '@playwright/test'

test('admin can invite an existing account to the selected group and see the acceptance steps', async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 800 })
  await page.goto('/login')
  await page.getByLabel('E-mail').fill('admin@investpro.com')
  await page.getByLabel('Senha').fill('password')
  await page.getByRole('button', { name: 'Entrar' }).click()
  await expect(page.getByText(/Patrim.*Total/).first()).toBeVisible({ timeout: 10_000 })

  await page.getByRole('button', { name: 'Toggle Sidebar' }).click()
  await page.getByRole('link', { name: 'Grupos e usuários' }).click()
  await page.keyboard.press('Escape')
  await expect(page.getByRole('heading', { name: 'Grupos e usuários' })).toBeVisible()
  const pageWidths = await page.evaluate(() => ({ viewport: innerWidth, document: document.documentElement.scrollWidth }))
  expect(pageWidths.document).toBeLessThanOrEqual(pageWidths.viewport)
  await page.locator('#invite-email').fill('investidor@example.com')
  await page.locator('#invite-role').selectOption('Investidor')
  await page.getByRole('button', { name: 'Criar convite' }).click()
  await expect(page.getByText('Convite registrado', { exact: true })).toBeVisible()
  await expect(page.getByRole('status').filter({ hasText: 'investidor@example.com' })).toContainText('aceitar o convite')
  await expect(page.getByRole('status').filter({ hasText: 'investidor@example.com' })).toContainText('Convite válido até')
  await page.locator('#invite-email').fill('admin@investpro.com')
  await expect(page.getByRole('button', { name: 'Criar convite' })).toBeDisabled()
  await expect(page.getByRole('status').filter({ hasText: 'já é membro ativo' })).toBeVisible()

  await page.getByRole('button', { name: 'Toggle Sidebar' }).click()
  await page.getByRole('link', { name: 'Carteiras' }).click()
  await page.keyboard.press('Escape')
  await page.getByRole('button', { name: 'Nova carteira' }).click()
  await page.locator('#portfolio-name').fill(`Carteira pública ${Date.now()}`)
  await page.locator('#portfolio-holder').fill('Titular sem conta')
  await page.locator('#portfolio-visibility').selectOption('PublicaDoGrupo')
  await expect(page.locator('#portfolio-visibility')).toHaveValue('PublicaDoGrupo')
})

test('requires selecting the destination group when the admin has several groups', async ({ page }) => {
  await page.addInitScript(() => {
    const original = window.fetch.bind(window)
    window.fetch = async (input, init) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url, window.location.origin)
      if (url.pathname === '/api/v1/grupos-carteiras' && (!init?.method || init.method === 'GET')) {
        return new Response(JSON.stringify({ dados: [
          { id: 'group-1', nome: 'Grupo principal', papel: 'Admin' },
          { id: 'group-2', nome: 'Grupo Tesouro', papel: 'Admin' },
        ] }), { status: 200, headers: { 'Content-Type': 'application/json' } })
      }
      return original(input, init)
    }
  })
  await page.goto('/login')
  await page.getByLabel('E-mail').fill('admin@investpro.com')
  await page.getByLabel('Senha').fill('password')
  await page.getByRole('button', { name: 'Entrar' }).click()
  await expect(page.getByText(/Patrim.*Total/).first()).toBeVisible({ timeout: 10_000 })
  await page.getByRole('link', { name: 'Grupos e usuários' }).click()
  const group = page.getByLabel('1. Grupo de destino')
  await expect(group).toHaveValue('')
  await expect(page.getByLabel('E-mail da conta')).toHaveCount(0)
  await group.selectOption('group-2')
  await expect(page.getByRole('heading', { name: '2. Adicionar pessoa a Grupo Tesouro' })).toBeVisible()
  await expect(page.getByLabel('E-mail da conta')).toBeVisible()
})


test('cadastra perfil de titular e reutiliza na carteira com instituição estável', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('E-mail').fill('admin@investpro.com')
  await page.getByLabel('Senha').fill('password')
  await page.getByRole('button', { name: 'Entrar' }).click()
  await page.getByRole('link', { name: 'Grupos e usuários' }).click()
  await page.locator('details > summary').click()
  await page.getByRole('button', { name: 'Novo titular', exact: true }).click()
  const holderName = `Titular família ${Date.now()}`
  await page.locator('#holder-profile-name').fill(holderName)
  await page.locator('#holder-profile-relationship').fill('Mãe')
  await page.getByRole('button', { name: 'Salvar titular' }).click()
  await expect(page.getByText('Perfil de titular salvo', { exact: true })).toBeVisible()
  await page.getByRole('link', { name: 'Carteiras', exact: true }).click()
  await page.getByRole('button', { name: 'Nova carteira', exact: true }).click()
  await page.locator('#portfolio-name').fill(`Carteira ${Date.now()}`)
  await page.locator('#portfolio-holder-profile').selectOption({ label: `${holderName} · Mãe` })
  await expect(page.locator('#portfolio-holder')).toHaveValue(holderName)
  await page.locator('#portfolio-institution').selectOption('10000000-0000-4000-8000-000000000001')
  const request = page.waitForRequest(request => request.method() === 'POST' && request.url().endsWith('/portfolios'))
  await page.getByRole('button', { name: 'Criar carteira', exact: true }).click()
  const payload = (await request).postDataJSON()
  expect(payload.titularId).toBeTruthy()
  expect(payload.instituicaoFinanceiraId).toBe('10000000-0000-4000-8000-000000000001')
  await expect(page.getByText('A carteira foi salva.', { exact: true })).toBeVisible()
})
