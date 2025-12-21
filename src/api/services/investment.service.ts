/**
 * Investment Service
 * Handles all investment-related API calls
 */

import { api } from '../client'
import type {
  InvestmentDto,
  FixedIncomeDto,
  VariableIncomeDto,
  CreateFixedIncomeRequest,
  CreateVariableIncomeRequest,
  UpdateInvestmentRequest,
  InvestmentFilters,
  InvestmentSummaryDto,
  ApiResponse,
  PaginatedResponse,
} from '../dtos'

const INVESTMENT_ENDPOINTS = {
  BASE: '/investments',
  DETAIL: (id: string) => `/investments/${id}`,
  FIXED_INCOME: '/investments/fixed-income',
  VARIABLE_INCOME: '/investments/variable-income',
  SUMMARY: '/investments/summary',
  BY_PORTFOLIO: (portfolioId: string) => `/portfolios/${portfolioId}/investments`,
} as const

/**
 * Investment service with CRUD operations
 */
export const investmentService = {
  /**
   * Get all investments with optional filters
   */
  getAll: (filters?: InvestmentFilters): Promise<PaginatedResponse<InvestmentDto>> =>
    api.get(INVESTMENT_ENDPOINTS.BASE, { params: filters as Record<string, string | number | boolean> }),

  /**
   * Get investments by portfolio
   */
  getByPortfolio: (portfolioId: string, filters?: InvestmentFilters): Promise<PaginatedResponse<InvestmentDto>> =>
    api.get(INVESTMENT_ENDPOINTS.BY_PORTFOLIO(portfolioId), { params: filters as Record<string, string | number | boolean> }),

  /**
   * Get investment by ID
   */
  getById: (id: string): Promise<ApiResponse<InvestmentDto>> =>
    api.get(INVESTMENT_ENDPOINTS.DETAIL(id)),

  /**
   * Get investment summary for dashboard
   */
  getSummary: (): Promise<ApiResponse<InvestmentSummaryDto>> =>
    api.get(INVESTMENT_ENDPOINTS.SUMMARY),

  /**
   * Create a fixed income investment
   */
  createFixedIncome: (data: CreateFixedIncomeRequest): Promise<ApiResponse<FixedIncomeDto>> =>
    api.post(INVESTMENT_ENDPOINTS.FIXED_INCOME, data),

  /**
   * Create a variable income investment
   */
  createVariableIncome: (data: CreateVariableIncomeRequest): Promise<ApiResponse<VariableIncomeDto>> =>
    api.post(INVESTMENT_ENDPOINTS.VARIABLE_INCOME, data),

  /**
   * Update an existing investment
   */
  update: (id: string, data: UpdateInvestmentRequest): Promise<ApiResponse<InvestmentDto>> =>
    api.patch(INVESTMENT_ENDPOINTS.DETAIL(id), data),

  /**
   * Delete an investment
   */
  delete: (id: string): Promise<ApiResponse<void>> =>
    api.delete(INVESTMENT_ENDPOINTS.DETAIL(id)),
}
