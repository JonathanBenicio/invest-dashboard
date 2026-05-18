/**
 * Portfolio Service
 * Handles all portfolio-related API calls
 */

import { api } from '../client'
import type {
  CarteiraDto,
  ResumoCarteiraDto,
  CriarCarteiraRequest,
  AtualizarCarteiraRequest,
  CarteiraFiltros,
  ApiResponse,
  PaginatedResponse,
} from '../dtos'

const PORTFOLIO_ENDPOINTS = {
  BASE: '/portfolios',
  DETAIL: (id: string) => `/portfolios/${id}`,
  SUMMARY: (id: string) => `/portfolios/${id}/summary`,
} as const

/**
 * Portfolio service with CRUD operations
 */
export const portfolioService = {
  /**
   * Get all portfolios with optional filters
   */
  getAll: (filters?: CarteiraFiltros): Promise<PaginatedResponse<CarteiraDto>> =>
    api.get(PORTFOLIO_ENDPOINTS.BASE, { params: filters as Record<string, string | number | boolean> }),

  /**
   * Get portfolio by ID
   */
  getById: (id: string): Promise<ApiResponse<CarteiraDto>> =>
    api.get(PORTFOLIO_ENDPOINTS.DETAIL(id)),

  /**
   * Get portfolio summary with allocation and performance
   */
  getSummary: (id: string): Promise<ApiResponse<ResumoCarteiraDto>> =>
    api.get(PORTFOLIO_ENDPOINTS.SUMMARY(id)),

  /**
   * Create a new portfolio
   */
  create: (data: CriarCarteiraRequest): Promise<ApiResponse<CarteiraDto>> =>
    api.post(PORTFOLIO_ENDPOINTS.BASE, data),

  /**
   * Update an existing portfolio
   */
  update: (id: string, data: AtualizarCarteiraRequest): Promise<ApiResponse<CarteiraDto>> =>
    api.patch(PORTFOLIO_ENDPOINTS.DETAIL(id), data),

  /**
   * Delete a portfolio
   */
  delete: (id: string): Promise<ApiResponse<void>> =>
    api.delete(PORTFOLIO_ENDPOINTS.DETAIL(id)),
}
