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
  RespostaApi,
  RespostaPaginada,
  PontoHistoricoCarteiraDto,
  ResumoCarteirasDto,
  ProjecaoRendaFixaConsolidadaDto,
} from '../dtos'

const PORTFOLIO_ENDPOINTS = {
  BASE: '/portfolios',
  DETAIL: (id: string) => `/portfolios/${id}`,
  SUMMARY: (id: string) => `/portfolios/${id}/summary`,
  HISTORY: (id: string) => `/portfolios/${id}/history`,
} as const

/**
 * Portfolio service with CRUD operations
 */
export const portfolioService = {
  /**
   * Get all portfolios with optional filters
   */
  getAll: (filters?: CarteiraFiltros): Promise<RespostaPaginada<CarteiraDto>> =>
    api.get(PORTFOLIO_ENDPOINTS.BASE, { params: filters as Record<string, string | number | boolean> }),
  getFixedIncomeProjection: (grupoId?: string): Promise<RespostaApi<ProjecaoRendaFixaConsolidadaDto>> =>
    api.get('/portfolios/projecao-renda-fixa', { params: grupoId ? { grupoId } : undefined }),

  getSummaryAll: (grupoId?: string): Promise<RespostaApi<ResumoCarteirasDto>> =>
    api.get(`${PORTFOLIO_ENDPOINTS.BASE}/resumo-geral`, { params: grupoId ? { grupoId } : undefined }),

  /**
   * Get portfolio by ID
   */
  getById: (id: string): Promise<RespostaApi<CarteiraDto>> =>
    api.get(PORTFOLIO_ENDPOINTS.DETAIL(id)),

  /**
   * Get portfolio summary with allocation and performance
   */
  getSummary: (id: string): Promise<RespostaApi<ResumoCarteiraDto>> =>
    api.get(PORTFOLIO_ENDPOINTS.SUMMARY(id)),

  getHistory: (id: string, fromDate: string, toDate: string): Promise<RespostaApi<PontoHistoricoCarteiraDto[]>> =>
    api.get(PORTFOLIO_ENDPOINTS.HISTORY(id), { params: { dataDe: fromDate, dataAte: toDate } }),

  /**
   * Create a new portfolio
   */
  create: (data: CriarCarteiraRequest): Promise<RespostaApi<CarteiraDto>> =>
    api.post(PORTFOLIO_ENDPOINTS.BASE, data),

  /**
   * Update an existing portfolio
   */
  update: (id: string, data: AtualizarCarteiraRequest): Promise<RespostaApi<CarteiraDto>> =>
    api.patch(PORTFOLIO_ENDPOINTS.DETAIL(id), data),

  /**
   * Delete a portfolio
   */
  delete: (id: string): Promise<RespostaApi<void>> =>
    api.delete(PORTFOLIO_ENDPOINTS.DETAIL(id)),
}
