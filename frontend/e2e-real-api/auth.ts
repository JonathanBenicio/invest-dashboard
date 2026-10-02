import { createHmac } from 'node:crypto'
import type { APIRequestContext, Page } from '@playwright/test'

export const apiUrl = process.env.E2E_API_URL ?? 'http://127.0.0.1:5051'
export const jwtSecret = process.env.E2E_JWT_SECRET
export const testUser = {
  id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001',
  sessionId: 'bbbbbbbb-cccc-dddd-eeee-ffffffff0001',
  email: 'test@investdashboard.com',
  name: 'Test User',
  issuer: process.env.E2E_JWT_ISSUER ?? 'test-issuer',
  audience: process.env.E2E_JWT_AUDIENCE ?? 'test-audience',
}

export async function createGroup(request: APIRequestContext, token: string, name: string) {
  const response = await request.post(`${apiUrl}/api/v1/grupos-carteiras`, {
    headers: { Authorization: `Bearer ${token}` },
    data: { nome: name },
  })
  const payload = await response.json()
  if (response.status() !== 200) {
    throw new Error(`Could not create E2E group: ${JSON.stringify(payload)}`)
  }
  return payload.dados as { id: string; nome: string }
}

export async function createPortfolio(request: APIRequestContext, token: string, name: string) {
  const response = await request.post(`${apiUrl}/api/v1/portfolios`, {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      nome: name,
      instituicaoFinanceiraId: '10000000-0000-4000-8000-000000000001',
    },
  })
  const payload = await response.json()
  if (response.status() !== 201) {
    throw new Error(`Could not create E2E portfolio: ${JSON.stringify(payload)}`)
  }
  return payload.dados as { id: string; grupoId: string; nome: string }
}

export function createAccessToken(secret: string) {
  const nowSeconds = Math.floor(Date.now() / 1000)
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url')
  const claims = Buffer.from(JSON.stringify({
    sub: testUser.id,
    email: testUser.email,
    name: testUser.name,
    role: 'user',
    sid: testUser.sessionId.replaceAll('-', ''),
    iss: testUser.issuer,
    aud: testUser.audience,
    iat: nowSeconds,
    exp: nowSeconds + 600,
  })).toString('base64url')
  const unsignedToken = `${header}.${claims}`
  const signature = createHmac('sha256', secret).update(unsignedToken).digest('base64url')
  return `${unsignedToken}.${signature}`
}

export function dateOnly(offsetDays: number) {
  return new Date(Date.now() + offsetDays * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)
}

export function mockRefreshSession(page: Page, token: string) {
  return page.route(`${apiUrl}/api/v1/auth/refresh`, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      dados: {
        tokenAcesso: token,
        expiraEmSegundos: 600,
        usuario: { id: testUser.id, nome: testUser.name, email: testUser.email, perfil: 'user' },
        requerConfirmacaoEmail: false,
      },
      sucesso: true,
      mensagem: null,
    }),
  }))
}
