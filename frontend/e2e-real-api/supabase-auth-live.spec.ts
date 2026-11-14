import { expect, test, type APIRequestContext } from '@playwright/test'

const apiUrl = process.env.E2E_API_URL ?? 'http://127.0.0.1:5000'

type AccountCredentials = {
  email?: string
  password?: string
}

type TestResources = {
  groupId?: string
  portfolioId?: string
  positionId?: string
}

const primaryAccount: AccountCredentials = {
  email: process.env.E2E_SUPABASE_EMAIL,
  password: process.env.E2E_SUPABASE_PASSWORD,
}

const secondaryAccount: AccountCredentials = {
  email: process.env.E2E_SUPABASE_SECONDARY_EMAIL,
  password: process.env.E2E_SUPABASE_SECONDARY_PASSWORD,
}

const unconfirmedAccount: AccountCredentials = {
  email: process.env.E2E_SUPABASE_UNCONFIRMED_EMAIL,
  password: process.env.E2E_SUPABASE_UNCONFIRMED_PASSWORD,
}

const primaryResources: TestResources = {
  groupId: process.env.E2E_SUPABASE_PRIMARY_GROUP_ID,
  portfolioId: process.env.E2E_SUPABASE_PRIMARY_PORTFOLIO_ID,
  positionId: process.env.E2E_SUPABASE_PRIMARY_POSITION_ID,
}

const secondaryResources: TestResources = {
  groupId: process.env.E2E_SUPABASE_SECONDARY_GROUP_ID,
  portfolioId: process.env.E2E_SUPABASE_SECONDARY_PORTFOLIO_ID,
  positionId: process.env.E2E_SUPABASE_SECONDARY_POSITION_ID,
}

function isConfigured(account: AccountCredentials): boolean {
  return Boolean(account.email && account.password)
}

function isGuid(value?: string): boolean {
  return Boolean(value && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value))
}

function authHeaders(token: string) {
  return { Authorization: `Bearer ${token}` }
}

function refreshTokenFrom(header?: string): string | undefined {
  return header?.match(/(?:^|,\s*)refresh_token=([^;]+)/i)?.[1]
}

function expectRefreshCookie(header: string | undefined, deleting = false): void {
  const cookie = header?.toLowerCase() ?? ''
  expect(cookie.includes('refresh_token=')).toBe(true)
  expect(cookie.includes('httponly')).toBe(true)
  expect(cookie.includes('secure')).toBe(true)
  expect(cookie.includes('samesite=lax')).toBe(true)
  expect(cookie.includes('path=/api/v1/auth')).toBe(true)
  if (deleting) {
    expect(cookie.includes('expires=thu, 01 jan 1970')).toBe(true)
  } else {
    expect(cookie.includes('max-age=604800')).toBe(true)
    expect(cookie.includes('expires=')).toBe(true)
  }
}

async function signIn(request: APIRequestContext, account: AccountCredentials) {
  const response = await request.post(`${apiUrl}/api/v1/auth/login`, {
    data: { email: account.email, senha: account.password },
  })
  expect(response.status()).toBe(200)
  return {
    response,
    session: (await response.json()).dados,
    cookieHeader: response.headers()['set-cookie'],
  }
}

async function assertGroupListsOnlyOwnGroup(
  request: APIRequestContext,
  token: string,
  ownGroupId: string,
  otherGroupId: string,
): Promise<void> {
  const response = await request.get(`${apiUrl}/api/v1/grupos-carteiras`, {
    headers: authHeaders(token),
  })
  expect(response.status()).toBe(200)
  const groups = (await response.json()).dados as Array<{ id: string }>
  expect(groups.some(group => group.id === ownGroupId)).toBe(true)
  expect(groups.some(group => group.id === otherGroupId)).toBe(false)
}

async function assertCrossAccountMutationsAreDenied(
  request: APIRequestContext,
  attackerToken: string,
  targetResources: Required<TestResources>,
  targetMembershipId: string,
): Promise<void> {
  const headers = authHeaders(attackerToken)
  const memberRoleUpdate = await request.put(
    `${apiUrl}/api/v1/grupos-carteiras/${targetResources.groupId}/membros/${targetMembershipId}/papel`,
    {
      headers,
      data: { papel: 'Consulta' },
    },
  )
  expect(memberRoleUpdate.status()).toBe(403)

  const memberDelete = await request.delete(
    `${apiUrl}/api/v1/grupos-carteiras/${targetResources.groupId}/membros/${targetMembershipId}`,
    { headers },
  )
  expect(memberDelete.status()).toBe(403)

  const portfolioUpdate = await request.patch(`${apiUrl}/api/v1/portfolios/${targetResources.portfolioId}`, {
    headers,
    data: { nome: 'Tentativa de alteração entre contas' },
  })
  expect(portfolioUpdate.status()).toBe(404)

  const valuation = await request.post(`${apiUrl}/api/v1/investments/${targetResources.positionId}/valuations`, {
    headers,
    data: { valorTotal: 1, data: new Date().toISOString() },
  })
  expect(valuation.status()).toBe(404)

  const positionDelete = await request.delete(`${apiUrl}/api/v1/investments/${targetResources.positionId}`, { headers })
  expect(positionDelete.status()).toBe(404)
}

test.describe('Supabase Auth live smoke', () => {
  test('signs in, verifies claims, rotates the API session and logs out', async ({ request }) => {
    test.skip(!isConfigured(primaryAccount), 'Configure a confirmed primary test account in frontend/.env.e2e.local.')

    const login = await signIn(request, primaryAccount)
    const session = login.session
    expect((session.usuario.email as string).toLowerCase() === primaryAccount.email!.toLowerCase()).toBe(true)
    expect(Boolean(session.usuario.id)).toBe(true)
    expect(Boolean(session.usuario.perfil)).toBe(true)
    expect(session.expiraEmSegundos).toBe(900)
    expect(Boolean(session.tokenAcesso)).toBe(true)
    expectRefreshCookie(login.cookieHeader)

    const initialRefreshToken = refreshTokenFrom(login.cookieHeader)
    expect(Boolean(initialRefreshToken)).toBe(true)

    const refresh = await request.post(`${apiUrl}/api/v1/auth/refresh`, {
      headers: { Cookie: `refresh_token=${initialRefreshToken}` },
    })
    expect(refresh.status()).toBe(200)
    const refreshedSession = (await refresh.json()).dados
    expect((refreshedSession.usuario.email as string).toLowerCase() === primaryAccount.email!.toLowerCase()).toBe(true)
    expect(refreshedSession.usuario.id === session.usuario.id).toBe(true)
    expect(refreshedSession.usuario.perfil === session.usuario.perfil).toBe(true)
    expect(Boolean(refreshedSession.tokenAcesso)).toBe(true)

    const rotatedCookie = refresh.headers()['set-cookie']
    expectRefreshCookie(rotatedCookie)
    const rotatedRefreshToken = refreshTokenFrom(rotatedCookie)
    expect(Boolean(rotatedRefreshToken)).toBe(true)
    expect(rotatedRefreshToken !== initialRefreshToken).toBe(true)

    const me = await request.get(`${apiUrl}/api/v1/auth/me`, {
      headers: authHeaders(refreshedSession.tokenAcesso),
    })
    expect(me.status()).toBe(200)
    const authenticatedUser = (await me.json()).dados
    expect(authenticatedUser.id === session.usuario.id).toBe(true)
    expect((authenticatedUser.email as string).toLowerCase() === primaryAccount.email!.toLowerCase()).toBe(true)
    expect(authenticatedUser.nome === session.usuario.nome).toBe(true)
    expect(authenticatedUser.perfil === session.usuario.perfil).toBe(true)

    const logout = await request.post(`${apiUrl}/api/v1/auth/logout`, {
      headers: {
        ...authHeaders(refreshedSession.tokenAcesso),
        Cookie: `refresh_token=${rotatedRefreshToken}`,
      },
    })
    expect(logout.status()).toBe(200)
    expectRefreshCookie(logout.headers()['set-cookie'], true)

    const refreshAfterLogout = await request.post(`${apiUrl}/api/v1/auth/refresh`, {
      headers: { Cookie: `refresh_token=${rotatedRefreshToken}` },
    })
    expect(refreshAfterLogout.status()).toBe(401)
  })

  test('rejects invalid credentials without issuing a session', async ({ request }) => {
    test.skip(!isConfigured(primaryAccount), 'Configure a confirmed primary test account in frontend/.env.e2e.local.')

    const response = await request.post(`${apiUrl}/api/v1/auth/login`, {
      data: {
        email: primaryAccount.email,
        senha: `${(primaryAccount.password ?? '').slice(0, 80)}.invalid-${randomUUID()}`,
      },
    })

    expect(response.status()).toBe(401)
    expect((response.headers()['set-cookie'] ?? '').toLowerCase().includes('refresh_token=')).toBe(false)
  })

  test('rejects an unconfirmed account without issuing a session', async ({ request }) => {
    test.skip(!isConfigured(unconfirmedAccount), 'Configure a dedicated unconfirmed account to run this scenario.')

    const response = await request.post(`${apiUrl}/api/v1/auth/login`, {
      data: { email: unconfirmedAccount.email, senha: unconfirmedAccount.password },
    })

    expect(response.status()).toBe(401)
    expect((response.headers()['set-cookie'] ?? '').toLowerCase().includes('refresh_token=')).toBe(false)
  })

  test('rejects a replayed refresh token and revokes the API session', async ({ request }) => {
    test.skip(!isConfigured(primaryAccount), 'Configure a confirmed primary test account in frontend/.env.e2e.local.')

    const login = await signIn(request, primaryAccount)
    const initialRefreshToken = refreshTokenFrom(login.cookieHeader)
    expect(Boolean(initialRefreshToken)).toBe(true)

    const refresh = await request.post(`${apiUrl}/api/v1/auth/refresh`, {
      headers: { Cookie: `refresh_token=${initialRefreshToken}` },
    })
    expect(refresh.status()).toBe(200)
    const rotatedRefreshToken = refreshTokenFrom(refresh.headers()['set-cookie'])
    expect(Boolean(rotatedRefreshToken)).toBe(true)

    const replay = await request.post(`${apiUrl}/api/v1/auth/refresh`, {
      headers: { Cookie: `refresh_token=${initialRefreshToken}` },
    })
    expect(replay.status()).toBe(401)

    const rotatedTokenAfterReplay = await request.post(`${apiUrl}/api/v1/auth/refresh`, {
      headers: { Cookie: `refresh_token=${rotatedRefreshToken}` },
    })
    expect(rotatedTokenAfterReplay.status()).toBe(401)

    const meAfterReplay = await request.get(`${apiUrl}/api/v1/auth/me`, {
      headers: authHeaders(login.session.tokenAcesso),
    })
    expect(meAfterReplay.status()).toBe(401)
  })

  test('isolates groups, portfolios and positions between two real accounts', async ({ request }) => {
    const hasAccounts = isConfigured(primaryAccount) && isConfigured(secondaryAccount)
    const hasResources = [
      ...Object.values(primaryResources),
      ...Object.values(secondaryResources),
    ].every(Boolean)
    test.skip(!hasAccounts || !hasResources, 'Configure two confirmed accounts and their separate test resources locally.')

    for (const id of [...Object.values(primaryResources), ...Object.values(secondaryResources)])
      expect(isGuid(id)).toBe(true)

    const [primary, secondary] = await Promise.all([
      signIn(request, primaryAccount),
      signIn(request, secondaryAccount),
    ])
    const primaryToken = primary.session.tokenAcesso as string
    const secondaryToken = secondary.session.tokenAcesso as string
    const primaryData = primaryResources as Required<TestResources>
    const secondaryData = secondaryResources as Required<TestResources>

    await assertGroupListsOnlyOwnGroup(request, primaryToken, primaryData.groupId, secondaryData.groupId)
    await assertGroupListsOnlyOwnGroup(request, secondaryToken, secondaryData.groupId, primaryData.groupId)

    const primaryOwnMembers = await request.get(`${apiUrl}/api/v1/grupos-carteiras/${primaryData.groupId}/membros`, {
      headers: authHeaders(primaryToken),
    })
    expect(primaryOwnMembers.status()).toBe(200)
    const secondaryOwnMembers = await request.get(`${apiUrl}/api/v1/grupos-carteiras/${secondaryData.groupId}/membros`, {
      headers: authHeaders(secondaryToken),
    })
    expect(secondaryOwnMembers.status()).toBe(200)
    const primaryOwner = ((await primaryOwnMembers.json()).dados as Array<{ id: string; usuarioId: string; papel: string }>)
      .find(member => member.usuarioId === primary.session.usuario.id)
    const secondaryOwner = ((await secondaryOwnMembers.json()).dados as Array<{ id: string; usuarioId: string; papel: string }>)
      .find(member => member.usuarioId === secondary.session.usuario.id)
    expect(Boolean(primaryOwner?.id)).toBe(true)
    expect(Boolean(secondaryOwner?.id)).toBe(true)

    const primaryForeignMembers = await request.get(`${apiUrl}/api/v1/grupos-carteiras/${secondaryData.groupId}/membros`, {
      headers: authHeaders(primaryToken),
    })
    expect(primaryForeignMembers.status()).toBe(403)
    const secondaryForeignMembers = await request.get(`${apiUrl}/api/v1/grupos-carteiras/${primaryData.groupId}/membros`, {
      headers: authHeaders(secondaryToken),
    })
    expect(secondaryForeignMembers.status()).toBe(403)

    const primaryPortfolio = await request.get(`${apiUrl}/api/v1/portfolios/${primaryData.portfolioId}`, {
      headers: authHeaders(primaryToken),
    })
    const secondaryPortfolio = await request.get(`${apiUrl}/api/v1/portfolios/${secondaryData.portfolioId}`, {
      headers: authHeaders(secondaryToken),
    })
    expect(primaryPortfolio.status()).toBe(200)
    expect(secondaryPortfolio.status()).toBe(200)
    const primaryPortfolioBefore = (await primaryPortfolio.json()).dados
    const secondaryPortfolioBefore = (await secondaryPortfolio.json()).dados

    const primaryPosition = await request.get(`${apiUrl}/api/v1/investments/${primaryData.positionId}`, {
      headers: authHeaders(primaryToken),
    })
    const secondaryPosition = await request.get(`${apiUrl}/api/v1/investments/${secondaryData.positionId}`, {
      headers: authHeaders(secondaryToken),
    })
    expect(primaryPosition.status()).toBe(200)
    expect(secondaryPosition.status()).toBe(200)
    const primaryPositionBefore = (await primaryPosition.json()).dados
    const secondaryPositionBefore = (await secondaryPosition.json()).dados

    for (const [attacker, target] of [
      { token: primaryToken, resources: secondaryData, membershipId: secondaryOwner!.id },
      { token: secondaryToken, resources: primaryData, membershipId: primaryOwner!.id },
    ]) {
      const portfolioRead = await request.get(`${apiUrl}/api/v1/portfolios/${target.resources.portfolioId}`, {
        headers: authHeaders(attacker.token),
      })
      expect(portfolioRead.status()).toBe(404)

      const positionRead = await request.get(`${apiUrl}/api/v1/investments/${target.resources.positionId}`, {
        headers: authHeaders(attacker.token),
      })
      expect(positionRead.status()).toBe(404)

      await assertCrossAccountMutationsAreDenied(
        request,
        attacker.token,
        target.resources,
        target.membershipId,
      )
    }

    const primaryPositionAfter = await request.get(`${apiUrl}/api/v1/investments/${primaryData.positionId}`, {
      headers: authHeaders(primaryToken),
    })
    const secondaryPositionAfter = await request.get(`${apiUrl}/api/v1/investments/${secondaryData.positionId}`, {
      headers: authHeaders(secondaryToken),
    })
    expect(primaryPositionAfter.status()).toBe(200)
    expect(secondaryPositionAfter.status()).toBe(200)
    expect((await primaryPositionAfter.json()).dados.valorAtual === primaryPositionBefore.valorAtual).toBe(true)
    expect((await secondaryPositionAfter.json()).dados.valorAtual === secondaryPositionBefore.valorAtual).toBe(true)

    const primaryMembersAfter = await request.get(`${apiUrl}/api/v1/grupos-carteiras/${primaryData.groupId}/membros`, {
      headers: authHeaders(primaryToken),
    })
    const secondaryMembersAfter = await request.get(`${apiUrl}/api/v1/grupos-carteiras/${secondaryData.groupId}/membros`, {
      headers: authHeaders(secondaryToken),
    })
    expect(primaryMembersAfter.status()).toBe(200)
    expect(secondaryMembersAfter.status()).toBe(200)
    const primaryRoleUnchanged = ((await primaryMembersAfter.json()).dados as Array<{ id: string; papel: string }>)
      .some(member => member.id === primaryOwner!.id && member.papel === primaryOwner.papel)
    const secondaryRoleUnchanged = ((await secondaryMembersAfter.json()).dados as Array<{ id: string; papel: string }>)
      .some(member => member.id === secondaryOwner!.id && member.papel === secondaryOwner.papel)
    expect(primaryRoleUnchanged).toBe(true)
    expect(secondaryRoleUnchanged).toBe(true)

    const primaryPortfolioAfter = await request.get(`${apiUrl}/api/v1/portfolios/${primaryData.portfolioId}`, {
      headers: authHeaders(primaryToken),
    })
    const secondaryPortfolioAfter = await request.get(`${apiUrl}/api/v1/portfolios/${secondaryData.portfolioId}`, {
      headers: authHeaders(secondaryToken),
    })
    expect(primaryPortfolioAfter.status()).toBe(200)
    expect(secondaryPortfolioAfter.status()).toBe(200)
    expect((await primaryPortfolioAfter.json()).dados.nome === primaryPortfolioBefore.nome).toBe(true)
    expect((await secondaryPortfolioAfter.json()).dados.nome === secondaryPortfolioBefore.nome).toBe(true)
  })
})
