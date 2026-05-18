/**
 * Investment Service
 * Handles all investment-related API calls
 */

import { api } from '../client'
import type {
  PosicaoInvestimentoDto,
  RendaFixaDto,
  RendaVariavelDto,
  CriarRendaFixaRequest,
  CriarRendaVariavelRequest,
  AtualizarInvestimentoRequest,
  InvestimentoFiltros,
  ResumoInvestimentoDto,
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
  DIVIDENDS: '/investments/dividends',
  TRANSACTIONS: (id: string) => `/investments/${id}/transactions`,
} as const

/**
 * Investment service with CRUD operations
 */
export const investmentService = {
  /**
   * Get all investments with optional filters
   */
  getAll: (filters?: InvestimentoFiltros): Promise<PaginatedResponse<PosicaoInvestimentoDto>> =>
    api.get(INVESTMENT_ENDPOINTS.BASE, { params: filters as Record<string, string | number | boolean> }),

  /**
   * Get dividends history
   */
  getDividends: (): Promise<PaginatedResponse<any>> =>
    api.get(INVESTMENT_ENDPOINTS.DIVIDENDS),

  /**
   * Get investment transactions
   */
  getTransactions: (id: string): Promise<PaginatedResponse<any>> =>
    api.get(INVESTMENT_ENDPOINTS.TRANSACTIONS(id)),

  /**
   * Get investments by portfolio
   */
  getByPortfolio: (portfolioId: string, filters?: InvestimentoFiltros): Promise<PaginatedResponse<PosicaoInvestimentoDto>> =>
    api.get(INVESTMENT_ENDPOINTS.BY_PORTFOLIO(portfolioId), { params: filters as Record<string, string | number | boolean> }),

  /**
   * Get investment by ID
   */
  getById: (id: string): Promise<ApiResponse<PosicaoInvestimentoDto>> =>
    api.get(INVESTMENT_ENDPOINTS.DETAIL(id)),

  /**
   * Get investment summary for dashboard
   */
  getSummary: (): Promise<ApiResponse<ResumoInvestimentoDto>> =>
    api.get(INVESTMENT_ENDPOINTS.SUMMARY),

  /**
   * Create a fixed income investment
   */
  createFixedIncome: (data: CriarRendaFixaRequest): Promise<ApiResponse<RendaFixaDto>> =>
    api.post(INVESTMENT_ENDPOINTS.FIXED_INCOME, data),

  /**
   * Create a variable income investment
   */
  createVariableIncome: (data: CriarRendaVariavelRequest): Promise<ApiResponse<RendaVariavelDto>> =>
    api.post(INVESTMENT_ENDPOINTS.VARIABLE_INCOME, data),

  /**
   * Update an existing investment
   */
  update: (id: string, data: AtualizarInvestimentoRequest): Promise<ApiResponse<PosicaoInvestimentoDto>> =>
    api.patch(INVESTMENT_ENDPOINTS.DETAIL(id), data),

  /**
   * Delete an investment
   */
  delete: (id: string): Promise<ApiResponse<void>> =>
    api.delete(INVESTMENT_ENDPOINTS.DETAIL(id)),
}
