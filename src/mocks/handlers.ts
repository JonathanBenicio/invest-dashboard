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

    let investments = mockAllInvestments

    if (type === 'fixed_income') {
      investments = mockFixedIncomeInvestments
    } else if (type === 'variable_income') {
      investments = mockVariableIncomeInvestments
    }

    return HttpResponse.json(createPaginatedResponse(investments, page, pageSize))
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
    }

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
    }

    return HttpResponse.json(createResponse(newInvestment, 'Investimento criado com sucesso'))
  }),

  http.patch(`${BASE_URL}/investments/:id`, async ({ params, request }) => {
    await delay(400)
    const { id } = params
    const body = await request.json() as Record<string, unknown>
    const investment = mockAllInvestments.find(inv => inv.id === id)

    if (!investment) {
      return HttpResponse.json(
        { success: false, message: 'Investimento não encontrado' },
        { status: 404 }
      )
    }

    const updated = {
      ...investment,
      ...body,
      updatedAt: new Date().toISOString(),
    }

    return HttpResponse.json(createResponse(updated, 'Investimento atualizado com sucesso'))
  }),

  http.delete(`${BASE_URL}/investments/:id`, async ({ params }) => {
    await delay(400)
    const { id } = params
    const investment = mockAllInvestments.find(inv => inv.id === id)

    if (!investment) {
      return HttpResponse.json(
        { success: false, message: 'Investimento não encontrado' },
        { status: 404 }
      )
    }

    return HttpResponse.json(createResponse(null, 'Investimento deletado com sucesso'))
  }),
]
