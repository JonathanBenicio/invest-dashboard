import { expect, test } from '@playwright/test'

const apiUrl = process.env.E2E_API_URL ?? 'http://127.0.0.1:5000'
const email = process.env.E2E_SUPABASE_EMAIL
const password = process.env.E2E_SUPABASE_PASSWORD

test.describe('Supabase Auth live smoke', () => {
  test.skip(!email || !password, 'Configure a confirmed test account in frontend/.env.e2e.local.')

  test('signs in, rotates the API session, reads claims and revokes the session', async ({ request }) => {
    const login = await request.post(`${apiUrl}/api/v1/auth/login`, {
      data: { email, senha: password },
    })
    expect(login.status()).toBe(200)
    const session = (await login.json()).dados
    expect(session.usuario.email.toLowerCase() === email!.toLowerCase()).toBe(true)
    expect(typeof session.tokenAcesso === 'string' && session.tokenAcesso.length > 0).toBe(true)

    const initialCookie = login.headers()['set-cookie']
    expect(initialCookie.toLowerCase().includes('refresh_token=') && initialCookie.toLowerCase().includes('httponly')).toBe(true)
    const initialRefreshToken = initialCookie.match(/refresh_token=([^;]+)/)?.[1]
    expect(initialRefreshToken).toBeTruthy()

    const refresh = await request.post(`${apiUrl}/api/v1/auth/refresh`, {
      headers: { Cookie: `refresh_token=${initialRefreshToken}` },
    })
    expect(refresh.status()).toBe(200)
    const refreshedSession = (await refresh.json()).dados
    expect(refreshedSession.usuario.email.toLowerCase() === email!.toLowerCase()).toBe(true)
    expect(typeof refreshedSession.tokenAcesso === 'string' && refreshedSession.tokenAcesso.length > 0).toBe(true)
    const rotatedCookie = refresh.headers()['set-cookie']
    expect(rotatedCookie.toLowerCase().includes('refresh_token=') && rotatedCookie.toLowerCase().includes('httponly')).toBe(true)
    const rotatedRefreshToken = rotatedCookie.match(/refresh_token=([^;]+)/)?.[1]
    expect(rotatedRefreshToken).toBeTruthy()
    expect(rotatedRefreshToken !== initialRefreshToken).toBe(true)

    const me = await request.get(`${apiUrl}/api/v1/auth/me`, {
      headers: { Authorization: `Bearer ${refreshedSession.tokenAcesso}` },
    })
    expect(me.status()).toBe(200)
    expect((await me.json()).dados.email.toLowerCase() === email!.toLowerCase()).toBe(true)

    const logout = await request.post(`${apiUrl}/api/v1/auth/logout`, {
      headers: {
        Authorization: `Bearer ${refreshedSession.tokenAcesso}`,
        Cookie: `refresh_token=${rotatedRefreshToken}`,
      },
    })
    expect(logout.status()).toBe(200)

    const refreshAfterLogout = await request.post(`${apiUrl}/api/v1/auth/refresh`, {
      headers: { Cookie: `refresh_token=${rotatedRefreshToken}` },
    })
    expect(refreshAfterLogout.status()).toBe(401)
  })
})
