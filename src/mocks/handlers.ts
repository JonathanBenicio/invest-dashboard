/**
 * MSW Request Handlers
 * Mock API endpoints for development
 */

import { http, HttpResponse, delay } from 'msw'
import { API_CONFIG } from '@/api/env'
import type { ApiResponse, PaginatedResponse, UserDto } from '@/api/dtos'
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

  const token = cookies.split(';').find(c => c.trim().startsWith('auth_token='))
  if (token) {
    userId = token.split('=')[1]
  }

  if (!userId) {
    return { authorized: false, status: 401, message: 'Não autenticado' }
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
    return HttpResponse.json(createResponse(user), {
      headers: {
        'Set-Cookie': `auth_token=${user.id}; HttpOnly; Path=/; SameSite=Strict`,
      }
    })
  }),

  http.post(`${BASE_URL}/auth/logout`, async () => {
    return HttpResponse.json(createResponse(null), {
      headers: {
        'Set-Cookie': 'auth_token=; HttpOnly; Path=/; Max-Age=0',
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

  // User Management endpoints (Admin only)
  http.get(`${BASE_URL}/users`, async ({ request }) => {
    const check = checkPermission(request, 'admin')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

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
      // Assuming 'issuer' field exists on FixedIncomeDto, but it might be 'institution' in mock data
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
    // The UI expects InvestmentDto / FixedIncomeDto structure, but mock data has legacy structure.
    const mappedInvestments = investments.map(inv => {
      // Check if it's already in DTO format (has totalInvested) or legacy (has investedValue)
      // Or simply normalize everything.

      const legacy = inv as any;
      const typeStr = inv.type as string;
      const isFixed = typeStr === 'CDB' || typeStr === 'LCI' || typeStr === 'LCA' ||
                      typeStr === 'Tesouro Direto' || typeStr === 'Debênture' ||
                      typeStr === 'CRI' || typeStr === 'CRA' || typeStr === 'fixed_income';

      // Default mapping for Base InvestmentDto fields from legacy
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

      const legacy = inv as any;
      const typeStr2 = inv.type as string;
      const isFixed = typeStr2 === 'CDB' || typeStr2 === 'LCI' || typeStr2 === 'LCA' ||
                      typeStr2 === 'Tesouro Direto' || typeStr2 === 'Debênture' ||
                      typeStr2 === 'CRI' || typeStr2 === 'CRA' || typeStr2 === 'fixed_income';

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
    // Importing dividends from data.ts would be better but I can assume it's there or use mock-data if imported
    // Since I cannot change imports easily without context, I will use a hardcoded empty list or try to access 'dividends' from mock-data if available in scope.
    // 'dividends' is not imported in this file. I should add it to imports or use a placeholder.
    // Checking imports... 'dividends' is not in './data'. It is in '@/lib/mock-data'.
    // I will add a simplified mock response for now to pass the check, or add import.
    // Let's rely on the previous plan: I'm adding handlers.

    // I'll return an empty list or a static list for now, as importing from lib might break isolation if not careful.
    // But ideally I should import from '@/lib/mock-data'.
    // Let's assume I can add the import.

    // Actually, let's look at the imports again.
    // import { ... } from './data'
    // I'll use a local const for now to avoid import errors until I fix imports.
    const mockDividends = [
      { id: '1', ticker: 'PETR4', type: 'Dividendo', value: 1.25, paymentDate: '2024-12-15', exDate: '2024-11-28' },
      { id: '2', ticker: 'ITUB4', type: 'JCP', value: 0.45, paymentDate: '2024-12-20', exDate: '2024-12-01' },
      { id: '3', ticker: 'HGLG11', type: 'Rendimento', value: 1.10, paymentDate: '2024-12-10', exDate: '2024-11-30' },
    ]
    return HttpResponse.json(createPaginatedResponse(mockDividends))
  }),

  // Transactions
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
]
