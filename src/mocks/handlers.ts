/**
 * MSW Request Handlers
 * Mock API endpoints for development
 */

import { http, HttpResponse, delay } from 'msw'
import { API_CONFIG } from '@/api/env'
import type { ApiResponse, PaginatedResponse } from '@/api/dtos'
import {
  mockCurrentUser,
  mockPortfolios,
  mockAllInvestments,
  mockFixedIncomeInvestments,
  mockVariableIncomeInvestments,
  mockInvestmentSummary,
  mockPortfolioSummary,
} from './data'

const BASE_URL = API_CONFIG.BASE_URL+API_CONFIG.VERSION

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

export const handlers = [
  // Auth endpoints
  http.get(`${BASE_URL}/auth/me`, async () => {
    await delay(300)
    return HttpResponse.json(createResponse(mockCurrentUser))
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
    }

    return HttpResponse.json(createResponse(newPortfolio, 'Portfolio criado com sucesso'))
  }),

  http.patch(`${BASE_URL}/portfolios/:id`, async ({ params, request }) => {
    await delay(400)
    const { id } = params
    const body = await request.json() as Record<string, unknown>
    const portfolio = mockPortfolios.find(p => p.id === id)

    if (!portfolio) {
      return HttpResponse.json(
        { success: false, message: 'Portfolio não encontrado' },
        { status: 404 }
      )
    }

    const updated = {
      ...portfolio,
      ...body,
      updatedAt: new Date().toISOString(),
    }

    return HttpResponse.json(createResponse(updated, 'Portfolio atualizado com sucesso'))
  }),

  http.delete(`${BASE_URL}/portfolios/:id`, async ({ params }) => {
    await delay(400)
    const { id } = params
    const portfolio = mockPortfolios.find(p => p.id === id)

    if (!portfolio) {
      return HttpResponse.json(
        { success: false, message: 'Portfolio não encontrado' },
        { status: 404 }
      )
    }

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
      const isFixed = inv.type === 'CDB' || inv.type === 'LCI' || inv.type === 'LCA' ||
                      inv.type === 'Tesouro Direto' || inv.type === 'Debênture' ||
                      inv.type === 'CRI' || inv.type === 'CRA' || inv.type === 'fixed_income';

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
      };

      const gain = baseDto.currentValue - baseDto.totalInvested;
      const gainPercentage = baseDto.totalInvested > 0 ? (gain / baseDto.totalInvested) * 100 : 0;

      if (isFixed) {
        return {
          ...baseDto,
          type: 'fixed_income', // Ensure correct high-level type
          issuer: legacy.issuer || legacy.institution || 'Unknown',
          interestRate: legacy.interestRate || parseFloat(legacy.rate?.replace('%', '') || '0'),
          indexer: legacy.indexer || legacy.rateType,
          gain,
          gainPercentage
        };
      } else {
        // Variable Income
        return {
          ...baseDto,
          type: 'variable_income',
          ticker: legacy.ticker || legacy.name, // Use name as ticker if missing for generic
          gain,
          gainPercentage
        };
      }
    });

    return HttpResponse.json(createPaginatedResponse(mappedInvestments, page, pageSize))
  }),

  http.get(`${BASE_URL}/portfolios/:portfolioId/investments`, async ({ params, request }) => {
    await delay(400)
    const { portfolioId } = params
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '10')

    const investments = mockAllInvestments.filter(inv => inv.portfolioId === portfolioId)

    return HttpResponse.json(createPaginatedResponse(investments, page, pageSize))
  }),

  // Summary MUST come before :id to avoid path collision
  http.get(`${BASE_URL}/investments/summary`, async () => {
    await delay(400)
    return HttpResponse.json(createResponse(mockInvestmentSummary))
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
    await delay(500)
    const body = await request.json() as { averagePrice?: number; quantity?: number; [key: string]: unknown }

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
    await delay(500)
    const body = await request.json() as { ticker?: string; averagePrice?: number; quantity?: number; [key: string]: unknown }

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

  http.delete(`${BASE_URL}/investments/:id`, async ({ params }) => {
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
]
