/**
 * MSW Request Handlers
 * Mock API endpoints for development
 */

import { http, HttpResponse, delay } from 'msw'
import { API_CONFIG } from '@/api/env'
import type { ApiResponse, PaginatedResponse, RegistrarTransacaoRequest, TransacaoDto, UserDto } from '@/api/dtos'
import {
  mockCurrentUser,
  mockPortfolios,
  mockAllInvestments,
  mockFixedIncomeInvestments,
  mockVariableIncomeInvestments,
  mockInvestmentSummary,
  mockPortfolioSummary,
  mockUsers,
} from './data'

// MSW matches paths relative to the current origin automatically
// '/api/v1/...' will be intercepted on any domain (localhost, GitHub Pages, Capacitor)
const BASE_URL = API_CONFIG.VERSION
let activeMockUserId: string | null = null
const mockTransactionsByIdempotencyKey = new Map<string, TransacaoDto>()

/**
 * Helper to create API response
 */
function createResponse<T>(data: T, message?: string): ApiResponse<T> {
  return {
    data,
    success: true,
    message,
  }
}

/**
 * Helper to create paginated response
 */
function createPaginatedResponse<T>(
  data: T[],
  page = 1,
  pageSize = 10
): PaginatedResponse<T> {
  const totalCount = data.length
  const totalPages = Math.ceil(totalCount / pageSize)
  const start = (page - 1) * pageSize
  const end = start + pageSize
  const paginatedData = data.slice(start, end)

  return {
    data: paginatedData,
    success: true,
    pagination: {
      page,
      pageSize,
      totalCount,
      totalPages,
      hasNextPage: page < totalPages,
      hasPreviousPage: page > 1,
    },
  }
}

/**
 * Middleware to check authentication and permissions
 */
function checkPermission(request: Request, requiredRole?: 'edit' | 'admin') {
  const cookies = request.headers.get('cookie') || ''
  let userId = ''

  const authorization = request.headers.get('authorization') || ''
  const accessToken = authorization.match(/^Bearer\s+mock-access-token-(.+)$/i)?.[1]
  if (accessToken) userId = accessToken

  const refreshCookie = cookies.split(';').find(cookie => cookie.trim().startsWith('refresh_token='))
  if (!userId && refreshCookie) userId = refreshCookie.split('=').slice(1).join('=')
  if (!userId) userId = activeMockUserId ?? ''

  if (!userId) {
    return { authorized: false, status: 401, message: 'Authentication is required.' }
  }

  const user = mockUsers.find(u => u.id === userId)

  if (!user) {
    return { authorized: false, status: 401, message: 'Usuário inválido' }
  }

  // Check role
  if (requiredRole) {
    const role = user.role as string
    if (requiredRole === 'admin' && role !== 'admin') {
      return { authorized: false, status: 403, message: 'Acesso negado: Requer privilégios de Admin' }
    }
    if (requiredRole === 'edit' && role === 'view') {
      return { authorized: false, status: 403, message: 'Acesso negado: Apenas leitura' }
    }
  }

  return { authorized: true, user }
}

export const handlers = [
  // Auth endpoints
  http.post(`${BASE_URL}/auth/login`, async ({ request }) => {
    const body = await request.json() as { email?: string; password?: string }
    await delay(500)

    const user = mockUsers.find(u => u.email === body.email)

    // Simple password check (In real app, hash check)
    // For mock: password is 'password' for all, or match specific rules if needed.
    // We'll just check if user exists for now or simple "password" string.
    if (!user || body.password !== 'password') {
      return HttpResponse.json(
        { success: false, message: 'Credenciais inválidas' },
        { status: 401 }
      )
    }

    // Set cookie
    return HttpResponse.json(createResponse({
      accessToken: `mock-access-token-${user.id}`,
      expiresIn: 900,
      user,
      requiresEmailConfirmation: false,
    }), {
      headers: {
        'Set-Cookie': `refresh_token=${user.id}; HttpOnly; Path=/api/v1/auth; SameSite=Lax`,
      }
    })
  }),

  http.post(`${BASE_URL}/auth/register`, async ({ request }) => {
    const body = await request.json() as { name?: string; email?: string; password?: string }
    await delay(300)

    if (!body.name || !body.email || !body.password || mockUsers.some(user => user.email === body.email)) {
      return HttpResponse.json(
        { success: false, message: 'Não foi possível criar esta conta.' },
        { status: 409 }
      )
    }

    activeMockUserId = user.id

    const now = new Date().toISOString()
    const user: UserDto = {
      id: `user-${Date.now()}`,
      name: body.name,
      email: body.email,
      role: 'user',
      isEmailVerified: true,
      isActive: true,
      createdAt: now,
      updatedAt: now,
    }
    mockUsers.push(user)
    activeMockUserId = user.id

    return HttpResponse.json(createResponse({
      accessToken: `mock-access-token-${user.id}`,
      expiresIn: 900,
      user,
      requiresEmailConfirmation: false,
    }), {
      headers: {
        'Set-Cookie': `refresh_token=${user.id}; HttpOnly; Path=/api/v1/auth; SameSite=Lax`,
      }
    })
  }),

  http.post(`${BASE_URL}/auth/refresh`, async ({ request }) => {
    const check = checkPermission(request)
    if (!check.authorized || !check.user) {
      return HttpResponse.json({ success: false, message: check.message }, { status: check.status })
    }

    return HttpResponse.json(createResponse({
      accessToken: `mock-access-token-${check.user.id}`,
      expiresIn: 900,
      user: check.user,
      requiresEmailConfirmation: false,
    }))
  }),

  http.post(`${BASE_URL}/auth/logout`, async () => {
    activeMockUserId = null
    return HttpResponse.json(createResponse(null), {
      headers: {
        'Set-Cookie': 'refresh_token=; HttpOnly; Path=/api/v1/auth; SameSite=Lax; Max-Age=0',
      }
    })
  }),

  http.get(`${BASE_URL}/auth/me`, async ({ request }) => {
    await delay(300)
    const check = checkPermission(request)
    if (!check.authorized) {
      return HttpResponse.json(
        { success: false, message: check.message },
        { status: check.status }
      )
    }
    return HttpResponse.json(createResponse(check.user))
  }),

  http.get(`${BASE_URL}/market-data/history`, async () =>
    HttpResponse.json(createResponse([]))),

  http.get(`${BASE_URL}/market-data/quotes`, async ({ request }) => {
    const check = checkPermission(request)
    if (!check.authorized) {
      return HttpResponse.json({ success: false, message: check.message }, { status: check.status })
    }

    const requestedSymbols = new URL(request.url).searchParams.get('symbols')
      ?.split(',')
      .map(symbol => symbol.trim().toUpperCase())
      .filter(Boolean) ?? []
    const quotes = mockAllInvestments
      .filter(investment => investment.type === 'variable_income' && requestedSymbols.includes(investment.ticker.toUpperCase()))
      .map(investment => ({
        symbol: investment.ticker,
        name: investment.name,
        price: investment.currentPrice,
        observedAtUtc: '2024-12-18T00:00:00.000Z',
        currency: investment.currency,
        sector: investment.sector,
        subtype: investment.subtype,
        source: 'demo',
      }))

    return HttpResponse.json(createResponse(quotes))
  }),

  http.patch(`${BASE_URL}/auth/me`, async ({ request }) => {
    await delay(500)
    const check = checkPermission(request)
    if (!check.authorized || !check.user) {
      return HttpResponse.json(
        { success: false, message: check.message || 'Não autorizado' },
        { status: check.status || 401 }
      )
    }

    const body = await request.json() as Partial<UserDto>
    const user = check.user

    // Update fields
    if (body.name) user.name = body.name
    if (body.email) user.email = body.email
    if (body.avatar) user.avatar = body.avatar

    // Update in mockUsers array (reference is already there but to be safe)
    const index = mockUsers.findIndex(u => u.id === user.id)
    if (index !== -1) {
      mockUsers[index] = { ...mockUsers[index], ...body }
    }

    return HttpResponse.json(createResponse(user, 'Perfil atualizado com sucesso'))
  }),

  // User Management endpoints (Admin only)
  http.get(`${BASE_URL}/users`, async ({ request }) => {
    // const check = checkPermission(request, 'admin')
    // if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '10')
    const search = url.searchParams.get('search')

    let users = [...mockUsers]

    if (search) {
      const lowerSearch = search.toLowerCase()
      users = users.filter(u =>
        u.name.toLowerCase().includes(lowerSearch) ||
        u.email.toLowerCase().includes(lowerSearch)
      )
    }

    return HttpResponse.json(createPaginatedResponse(users, page, pageSize))
  }),

  http.post(`${BASE_URL}/users`, async ({ request }) => {
    const check = checkPermission(request, 'admin')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(500)
    const body = await request.json() as Partial<UserDto>

    // Check if email already exists
    if (mockUsers.some(u => u.email === body.email)) {
      return HttpResponse.json(
        { success: false, message: 'E-mail já cadastrado' },
        { status: 400 }
      )
    }

    const newUser: UserDto = {
      id: `${Date.now()}`,
      name: body.name || '',
      email: body.email || '',
      role: body.role || 'view',
      isActive: body.isActive ?? true,
      isEmailVerified: true, // Auto-verify for admin created users
      parentesco: body.parentesco,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }

    mockUsers.push(newUser)

    return HttpResponse.json(createResponse(newUser, 'Usuário criado com sucesso'))
  }),

  http.patch(`${BASE_URL}/users/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'admin')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const body = await request.json() as Partial<UserDto>
    const index = mockUsers.findIndex(u => u.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { success: false, message: 'Usuário não encontrado' },
        { status: 404 }
      )
    }

    const updated = {
      ...mockUsers[index],
      ...body,
      updatedAt: new Date().toISOString(),
    }

    mockUsers[index] = updated

    return HttpResponse.json(createResponse(updated, 'Usuário atualizado com sucesso'))
  }),

  http.delete(`${BASE_URL}/users/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'admin')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const index = mockUsers.findIndex(u => u.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { success: false, message: 'Usuário não encontrado' },
        { status: 404 }
      )
    }

    // Prevent deleting yourself
    if (check.user?.id === id) {
      return HttpResponse.json(
        { success: false, message: 'Não é possível excluir o próprio usuário' },
        { status: 400 }
      )
    }

    mockUsers.splice(index, 1)

    return HttpResponse.json(createResponse(null, 'Usuário excluído com sucesso'))
  }),

  // Portfolio endpoints
  http.get(`${BASE_URL}/portfolios`, async ({ request }) => {
    await delay(400)
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '10')

    return HttpResponse.json(createPaginatedResponse(mockPortfolios, page, pageSize))
  }),

  http.get(`${BASE_URL}/portfolios/:id`, async ({ params }) => {
    await delay(300)
    const { id } = params
    const portfolio = mockPortfolios.find(p => p.id === id)

    if (!portfolio) {
      return HttpResponse.json(
        { success: false, message: 'Portfolio não encontrado' },
        { status: 404 }
      )
    }

    return HttpResponse.json(createResponse(portfolio))
  }),

  http.get(`${BASE_URL}/portfolios/:id/summary`, async ({ params }) => {
    await delay(400)
    const { id } = params

    if (id !== 'portfolio-1') {
      return HttpResponse.json(
        { success: false, message: 'Portfolio não encontrado' },
        { status: 404 }
      )
    }

    return HttpResponse.json(createResponse(mockPortfolioSummary))
  }),

  http.get(`${BASE_URL}/portfolios/:id/history`, async () =>
    HttpResponse.json(createResponse([]))),

  http.post(`${BASE_URL}/portfolios`, async ({ request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(500)
    const body = await request.json() as Record<string, unknown>

    const newPortfolio = {
      id: `portfolio-${Date.now()}`,
      ...body,
      totalValue: 0,
      totalInvested: 0,
      totalGain: 0,
      gainPercentage: 0,
      currency: 'BRL',
      isActive: true,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    } as any

    mockPortfolios.push(newPortfolio)

    return HttpResponse.json(createResponse(newPortfolio, 'Portfolio criado com sucesso'))
  }),

  http.patch(`${BASE_URL}/portfolios/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const body = await request.json() as Record<string, unknown>
    const index = mockPortfolios.findIndex(p => p.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { success: false, message: 'Portfolio não encontrado' },
        { status: 404 }
      )
    }

    const updated = {
      ...mockPortfolios[index],
      ...body,
      updatedAt: new Date().toISOString(),
    }

    mockPortfolios[index] = updated as any

    return HttpResponse.json(createResponse(updated, 'Portfolio atualizado com sucesso'))
  }),

  http.delete(`${BASE_URL}/portfolios/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const index = mockPortfolios.findIndex(p => p.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { success: false, message: 'Portfolio não encontrado' },
        { status: 404 }
      )
    }

    mockPortfolios.splice(index, 1)

    return HttpResponse.json(createResponse(null, 'Portfolio deletado com sucesso'))
  }),

  // Investment endpoints
  http.get(`${BASE_URL}/investments`, async ({ request }) => {
    await delay(400)
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '10')
    const type = url.searchParams.get('type')
    const search = url.searchParams.get('search')
    const sortBy = url.searchParams.get('sortBy')
    const sortOrder = url.searchParams.get('sortOrder') || 'asc'
    const subtype = url.searchParams.get('subtype')
    const issuer = url.searchParams.get('issuer') // Using issuer instead of institution as per DTO
    const sector = url.searchParams.get('sector')

    let investments = mockAllInvestments

    // Filter by Type (Fixed/Variable)
    if (type === 'fixed_income') {
      investments = mockFixedIncomeInvestments
    } else if (type === 'variable_income') {
      investments = mockVariableIncomeInvestments
    }

    // Filter by Subtype (CDB, LCI, etc.)
    if (subtype) {
      investments = investments.filter(inv => inv.subtype === subtype)
    }

    // Filter by Issuer (Institution)
    if (issuer) {
      // Assuming 'issuer' field exists on RendaFixaDto, but it might be 'institution' in mock data
      // Let's check the mock data structure or cast it safely
      investments = investments.filter(inv =>
        ('issuer' in inv && (inv as any).issuer.toLowerCase().includes(issuer.toLowerCase())) ||
        ('institution' in inv && (inv as any).institution.toLowerCase().includes(issuer.toLowerCase()))
      )
    }

    // Filter by Sector
    if (sector) {
      investments = investments.filter(inv =>
        'sector' in inv && (inv as any).sector.toLowerCase().includes(sector.toLowerCase())
      )
    }

    // Global Search (Name or Institution/Issuer)
    if (search) {
      const lowerSearch = search.toLowerCase()
      investments = investments.filter(inv => {
        const nameMatch = inv.name.toLowerCase().includes(lowerSearch)
        const institutionMatch = 'institution' in inv ? (inv as any).institution.toLowerCase().includes(lowerSearch) : false
        const issuerMatch = 'issuer' in inv ? (inv as any).issuer.toLowerCase().includes(lowerSearch) : false
        const tickerMatch = 'ticker' in inv ? (inv as any).ticker?.toLowerCase().includes(lowerSearch) : false

        return nameMatch || institutionMatch || issuerMatch || tickerMatch
      })
    }

    // Sorting
    if (sortBy) {
      investments.sort((a, b) => {
        const aValue = (a as any)[sortBy]
        const bValue = (b as any)[sortBy]

        if (aValue === bValue) return 0

        // Handle undefined values
        if (aValue === undefined) return 1
        if (bValue === undefined) return -1

        const comparison = aValue > bValue ? 1 : -1
        return sortOrder === 'desc' ? -comparison : comparison
      })
    }

    // Map Legacy Data to DTOs
    // The UI expects PosicaoInvestimentoDto / RendaFixaDto structure, but mock data has legacy structure.
    const mappedInvestments = investments.map(inv => {
      // Check if it's already in DTO format (has totalInvested) or legacy (has investedValue)
      // Or simply normalize everything.

      const legacy = inv as any
      const typeStr = inv.type as string
      const isFixed = typeStr === 'CDB' || typeStr === 'LCI' || typeStr === 'LCA' ||
        typeStr === 'Tesouro Direto' || typeStr === 'Debênture' ||
        typeStr === 'CRI' || typeStr === 'CRA' || typeStr === 'fixed_income'

      // Default mapping for Base PosicaoInvestimentoDto fields from legacy
      const baseDto = {
        ...inv,
        portfolioId: legacy.portfolioId || 'portfolio-1', // Default if missing
        subtype: legacy.subtype || legacy.type, // Map legacy type to subtype
        quantity: legacy.quantity || 1,
        averagePrice: legacy.averagePrice || legacy.investedValue || 0,
        totalInvested: legacy.totalInvested || (legacy.investedValue || (legacy.quantity * legacy.averagePrice)) || 0,
        currentValue: legacy.currentValue || (legacy.currentPrice ? legacy.currentPrice * legacy.quantity : 0) || 0,
        // Calculate gain/percentage
      }

      const gain = baseDto.currentValue - baseDto.totalInvested
      const gainPercentage = baseDto.totalInvested > 0 ? (gain / baseDto.totalInvested) * 100 : 0

      if (isFixed) {
        return {
          ...baseDto,
          type: 'fixed_income', // Ensure correct high-level type
          issuer: legacy.issuer || legacy.institution || 'Unknown',
          interestRate: legacy.interestRate || parseFloat(legacy.rate?.replace('%', '') || '0'),
          indexer: legacy.indexer || legacy.rateType,
          gain,
          gainPercentage
        }
      } else {
        // Variable Income
        return {
          ...baseDto,
          type: 'variable_income',
          ticker: legacy.ticker || legacy.name, // Use name as ticker if missing for generic
          gain,
          gainPercentage
        }
      }
    })

    return HttpResponse.json(createPaginatedResponse(mappedInvestments, page, pageSize))
  }),

  http.get(`${BASE_URL}/portfolios/:portfolioId/investments`, async ({ params, request }) => {
    await delay(400)
    const { portfolioId } = params
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '10')

    // Using the same DTO mapping logic for consistency
    const investments = mockAllInvestments.filter(inv => inv.portfolioId === portfolioId)

    const mappedInvestments = investments.map(inv => {

      const legacy = inv as any
      const typeStr2 = inv.type as string
      const isFixed = typeStr2 === 'CDB' || typeStr2 === 'LCI' || typeStr2 === 'LCA' ||
        typeStr2 === 'Tesouro Direto' || typeStr2 === 'Debênture' ||
        typeStr2 === 'CRI' || typeStr2 === 'CRA' || typeStr2 === 'fixed_income'

      const baseDto = {
        ...inv,
        portfolioId: legacy.portfolioId || 'portfolio-1',
        subtype: legacy.subtype || legacy.type,
        quantity: legacy.quantity || 1,
        averagePrice: legacy.averagePrice || legacy.investedValue || 0,
        totalInvested: legacy.totalInvested || (legacy.investedValue || (legacy.quantity * legacy.averagePrice)) || 0,
        currentValue: legacy.currentValue || (legacy.currentPrice ? legacy.currentPrice * legacy.quantity : 0) || 0,
      }

      const gain = baseDto.currentValue - baseDto.totalInvested
      const gainPercentage = baseDto.totalInvested > 0 ? (gain / baseDto.totalInvested) * 100 : 0

      if (isFixed) {
        return {
          ...baseDto,
          type: 'fixed_income',
          issuer: legacy.issuer || legacy.institution || 'Unknown',
          interestRate: legacy.interestRate || parseFloat(legacy.rate?.replace('%', '') || '0'),
          indexer: legacy.indexer || legacy.rateType,
          gain,
          gainPercentage
        }
      } else {
        return {
          ...baseDto,
          type: 'variable_income',
          ticker: legacy.ticker || legacy.name,
          gain,
          gainPercentage
        }
      }
    })

    return HttpResponse.json(createPaginatedResponse(mappedInvestments, page, pageSize))
  }),

  // Summary MUST come before :id to avoid path collision
  http.get(`${BASE_URL}/investments/summary`, async () => {
    await delay(400)
    return HttpResponse.json(createResponse(mockInvestmentSummary))
  }),

  // Dividends
  http.get(`${BASE_URL}/investments/dividends`, async () => {
    await delay(400)
    return HttpResponse.json(createPaginatedResponse([]))
  }),

  // Transactions
  http.post(`${BASE_URL}/transactions`, async ({ request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    const body = await request.json() as RegistrarTransacaoRequest
    const previous = mockTransactionsByIdempotencyKey.get(body.idempotencyKey)
    if (previous) return HttpResponse.json(createResponse(previous))
    if (!mockPortfolios.some(portfolio => portfolio.id === body.portfolioId)) {
      return HttpResponse.json({ success: false, message: 'Carteira não encontrada.' }, { status: 404 })
    }

    const investment = mockAllInvestments.find(item => item.portfolioId === body.portfolioId && item.ticker?.toUpperCase() === body.ticker?.toUpperCase())
    if (body.type === 'Sell' && (!investment || investment.quantity < body.quantity)) {
      return HttpResponse.json({ success: false, message: 'Quantidade disponível insuficiente.' }, { status: 409 })
    }

    const transaction: TransacaoDto = {
      id: `transaction-${crypto.randomUUID()}`,
      portfolioId: body.portfolioId,
      assetId: investment?.assetId,
      ticker: body.ticker,
      type: body.type,
      quantity: body.quantity,
      unitPrice: body.unitPrice,
      fees: body.fees,
      totalAmount: body.quantity * body.unitPrice + (body.type === 'Buy' ? body.fees : -body.fees),
      realizedGain: 0,
      realizedCostBasis: 0,
      transactionDate: body.transactionDate,
      notes: body.notes,
    }
    mockTransactionsByIdempotencyKey.set(body.idempotencyKey, transaction)
    await delay(100)
    return HttpResponse.json(createResponse(transaction), { status: 201 })
  }),

  http.get(`${BASE_URL}/investments/:id/transactions`, async ({ params }) => {
    await delay(400)
    const { id } = params
    const mockTransactions = [
      { id: '1', date: '2024-12-10', type: 'Compra', quantity: 50, price: 37.80, total: 1890 },
      { id: '2', date: '2024-11-05', type: 'Venda', quantity: 10, price: 38.50, total: 385 },
    ]
    return HttpResponse.json(createPaginatedResponse(mockTransactions))
  }),

  http.get(`${BASE_URL}/investments/:id`, async ({ params }) => {
    await delay(300)
    const { id } = params
    const investment = mockAllInvestments.find(inv => inv.id === id)

    if (!investment) {
      return HttpResponse.json(
        { success: false, message: 'Investimento não encontrado' },
        { status: 404 }
      )
    }

    return HttpResponse.json(createResponse(investment))
  }),

  http.post(`${BASE_URL}/investments/fixed-income`, async ({ request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(500)
    const body = await request.json() as { averagePrice?: number; quantity?: number;[key: string]: unknown }

    const avgPrice = body.averagePrice ?? 0
    const qty = body.quantity ?? 0

    const newInvestment = {
      id: `fixed-${Date.now()}`,
      type: 'fixed_income' as const,
      ...body,
      currentPrice: avgPrice,
      totalInvested: qty * avgPrice,
      currentValue: qty * avgPrice,
      gain: 0,
      gainPercentage: 0,
      currency: 'BRL',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    } as any

    mockFixedIncomeInvestments.push(newInvestment)
    mockAllInvestments.push(newInvestment)

    return HttpResponse.json(createResponse(newInvestment, 'Investimento criado com sucesso'))
  }),

  http.post(`${BASE_URL}/investments/variable-income`, async ({ request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(500)
    const body = await request.json() as { ticker?: string; averagePrice?: number; quantity?: number;[key: string]: unknown }

    const avgPrice = body.averagePrice ?? 0
    const qty = body.quantity ?? 0

    const newInvestment = {
      id: `var-${Date.now()}`,
      type: 'variable_income' as const,
      name: body.ticker ?? '',
      ...body,
      currentPrice: avgPrice,
      totalInvested: qty * avgPrice,
      currentValue: qty * avgPrice,
      gain: 0,
      gainPercentage: 0,
      currency: 'BRL',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    } as any

    mockVariableIncomeInvestments.push(newInvestment)
    mockAllInvestments.push(newInvestment)

    return HttpResponse.json(createResponse(newInvestment, 'Investimento criado com sucesso'))
  }),

  http.patch(`${BASE_URL}/investments/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const body = await request.json() as Record<string, unknown>
    const index = mockAllInvestments.findIndex(inv => inv.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { success: false, message: 'Investimento não encontrado' },
        { status: 404 }
      )
    }

    const updated = {
      ...mockAllInvestments[index],
      ...body,
      updatedAt: new Date().toISOString(),
    }

    mockAllInvestments[index] = updated as any

    // Also update in specific lists
    const fixedIndex = mockFixedIncomeInvestments.findIndex(inv => inv.id === id)
    if (fixedIndex !== -1) mockFixedIncomeInvestments[fixedIndex] = updated as any

    const variableIndex = mockVariableIncomeInvestments.findIndex(inv => inv.id === id)
    if (variableIndex !== -1) mockVariableIncomeInvestments[variableIndex] = updated as any


    return HttpResponse.json(createResponse(updated, 'Investimento atualizado com sucesso'))
  }),

  http.delete(`${BASE_URL}/investments/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const index = mockAllInvestments.findIndex(inv => inv.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { success: false, message: 'Investimento não encontrado' },
        { status: 404 }
      )
    }

    mockAllInvestments.splice(index, 1)

    // Remove from specific lists
    const fixedIndex = mockFixedIncomeInvestments.findIndex(inv => inv.id === id)
    if (fixedIndex !== -1) mockFixedIncomeInvestments.splice(fixedIndex, 1)

    const variableIndex = mockVariableIncomeInvestments.findIndex(inv => inv.id === id)
    if (variableIndex !== -1) mockVariableIncomeInvestments.splice(variableIndex, 1)

    return HttpResponse.json(createResponse(null, 'Investimento deletado com sucesso'))
  }),

  // Chat Endpoint
  http.post(`${BASE_URL}/chat`, async ({ request }) => {
    await delay(1000)
    const body = await request.json() as { message: string, userId: string }

    return HttpResponse.json({
      message: `Olá! Recebi sua mensagem: "${body.message}". Como sou uma IA simulada, não posso processar solicitações reais ainda, mas estou aqui para ajudar!`
    })
  }),

  // Taxes endpoints
  http.get(`${BASE_URL}/taxes`, async () => {
    await delay(300)
    const mockRates = [
      {
        id: 'rate-1',
        name: 'SELIC',
        symbol: 'SELIC',
        currentValue: 12.75,
        previousValue: 12.75,
        variation: 0,
        description: 'Taxa básica de juros da economia brasileira',
        source: 'Banco Central',
        lastUpdate: '2024-12-18',
      },
      {
        id: 'rate-2',
        name: 'IPCA',
        symbol: 'IPCA',
        currentValue: 4.83,
        previousValue: 4.76,
        variation: 0.07,
        description: 'Índice Nacional de Preços ao Consumidor Amplo',
        source: 'IBGE',
        lastUpdate: '2024-12-10',
      },
      {
        id: 'rate-3',
        name: 'CDI',
        symbol: 'CDI',
        currentValue: 12.65,
        previousValue: 12.65,
        variation: 0,
        description: 'Certificado de Depósito Interbancário',
        source: 'Cetip',
        lastUpdate: '2024-12-20',
      },
      {
        id: 'rate-4',
        name: 'Dólar Comercial',
        symbol: 'USD/BRL',
        currentValue: 6.28,
        previousValue: 6.15,
        variation: 0.13,
        description: 'Cotação do dólar em relação ao real',
        source: 'Banco Central',
        lastUpdate: '2024-12-20',
      },
      {
        id: 'rate-5',
        name: 'Imposto de Renda - PF',
        symbol: 'IR-PF',
        currentValue: 15.0,
        previousValue: 15.0,
        variation: 0,
        description: 'Alíquota máxima de IR para Pessoa Física',
        source: 'Receita Federal',
        lastUpdate: '2024-12-18',
      },
    ]
    return HttpResponse.json(createResponse(mockRates))
  }),

  http.post(`${BASE_URL}/taxes`, async ({ request }) => {
    await delay(400)
    const body = await request.json() as Record<string, unknown>
    const newRate = {
      id: `rate-${Date.now()}`,
      ...body,
      variation: 0,
      lastUpdate: new Date().toISOString().split('T')[0],
    }
    return HttpResponse.json(createResponse(newRate, 'Taxa adicionada com sucesso'))
  }),

  http.put(`${BASE_URL}/taxes/:id`, async ({ params, request }) => {
    await delay(400)
    const { id } = params
    const body = await request.json() as Record<string, unknown>
    const updatedRate = {
      id,
      ...body,
      lastUpdate: new Date().toISOString().split('T')[0],
    }
    return HttpResponse.json(createResponse(updatedRate, 'Taxa atualizada com sucesso'))
  }),

  http.delete(`${BASE_URL}/taxes/:id`, async () => {
    await delay(300)
    return HttpResponse.json(createResponse(null, 'Taxa removida com sucesso'))
  }),

  // Simulation endpoints
  http.post(`${BASE_URL}/simulation`, async ({ request }) => {
    await delay(500)
    const body = await request.json() as {
      initialAmount: number
      monthlyContribution: number
      years: number
      annualInterestRate: number
      strategy?: string
    }

    const totalMonths = body.years * 12
    const monthlyRate = Math.pow(1 + body.annualInterestRate / 100, 1 / 12) - 1
    const points: Array<{ month: number; invested: number; total: number; interest: number }> = []

    let currentAmount = body.initialAmount
    let totalInvested = body.initialAmount

    for (let i = 0; i <= totalMonths; i++) {
      points.push({
        month: i,
        invested: Number(totalInvested.toFixed(2)),
        total: Number(currentAmount.toFixed(2)),
        interest: Number((currentAmount - totalInvested).toFixed(2)),
      })
      if (i < totalMonths) {
        currentAmount = currentAmount * (1 + monthlyRate) + body.monthlyContribution
        totalInvested += body.monthlyContribution
      }
    }

    return HttpResponse.json(createResponse({
      points,
      finalAmount: points[points.length - 1].total,
      totalInvested: points[points.length - 1].invested,
      totalInterest: points[points.length - 1].interest,
      strategyName: body.strategy === 'montecarlo' ? 'Estatístico (Monte Carlo)' : 'Matemático (Determinístico)',
    }))
  }),

  http.get(`${BASE_URL}/simulation/strategies`, async () => {
    await delay(200)
    return HttpResponse.json(createResponse([
      { id: 'deterministic', name: 'Matemático (Determinístico)', description: 'Simulação baseada em juros compostos com taxa fixa' },
      { id: 'montecarlo', name: 'Estatístico (Monte Carlo)', description: 'Simulação probabilística com volatilidade' },
    ]))
  }),
]
