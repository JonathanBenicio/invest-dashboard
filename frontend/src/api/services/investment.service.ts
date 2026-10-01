/**
 * Investment Service
 * Handles all investment-related API calls
 */

import { api } from '../client'
import type {
  PosicaoInvestimentoDto,
  PrecoHistoricoDto,
  RendaFixaDto,
  RendaVariavelDto,
  CriarRendaFixaRequest,
  CriarRendaVariavelRequest,
  AtualizarInvestimentoRequest,
  InvestimentoFiltros,
  ResumoInvestimentoDto,
  ProventoDto,
  ProjecaoRendaFixaDto,
  RespostaApi,
  RespostaPaginada,
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
  getAll: (filters?: InvestimentoFiltros): Promise<RespostaPaginada<PosicaoInvestimentoDto>> =>
    api.get(INVESTMENT_ENDPOINTS.BASE, { params: filters as Record<string, string | number | boolean> }),

  /**
   * Get dividends history
   */
  getDividends: (): Promise<RespostaPaginada<ProventoDto>> =>
    api.get(INVESTMENT_ENDPOINTS.DIVIDENDS),

  /**
   * Get investment transactions
   */
  getTransactions: (id: string): Promise<RespostaPaginada<import('@/api/dtos').TransacaoDto>> =>
    api.get(INVESTMENT_ENDPOINTS.TRANSACTIONS(id)),

  getPriceHistory: (id: string, fromDate?: string): Promise<RespostaApi<PrecoHistoricoDto[]>> =>
    api.get(`/investments/${id}/history`, { params: fromDate ? { dataDe: fromDate } : undefined }),

  /**
   * Get investments by portfolio
   */
  getByPortfolio: (portfolioId: string, filters?: InvestimentoFiltros): Promise<RespostaPaginada<PosicaoInvestimentoDto>> =>
    api.get(INVESTMENT_ENDPOINTS.BY_PORTFOLIO(portfolioId), { params: filters as Record<string, string | number | boolean> }),

  /**
   * Get investment by ID
   */
  getById: (id: string): Promise<RespostaApi<PosicaoInvestimentoDto>> =>
    api.get(INVESTMENT_ENDPOINTS.DETAIL(id)),

  /**
   * Get investment summary for dashboard
   */
  getSummary: (): Promise<RespostaApi<ResumoInvestimentoDto>> =>
    api.get(INVESTMENT_ENDPOINTS.SUMMARY),

  getFixedIncomeProjection: (id: string): Promise<RespostaApi<ProjecaoRendaFixaDto>> =>
    api.get('/investments/' + id + '/projecao-renda-fixa'),

  /**
   * Create a fixed income investment
   */
  createFixedIncome: (data: CriarRendaFixaRequest): Promise<RespostaApi<RendaFixaDto>> =>
    api.post(INVESTMENT_ENDPOINTS.FIXED_INCOME, data),

  /**
   * Create a variable income investment
   */
  createVariableIncome: (data: CriarRendaVariavelRequest): Promise<RespostaApi<RendaVariavelDto>> =>
    api.post(INVESTMENT_ENDPOINTS.VARIABLE_INCOME, data),

  /**
   * Update an existing investment
   */
  update: (id: string, data: AtualizarInvestimentoRequest): Promise<RespostaApi<PosicaoInvestimentoDto>> =>
    api.post(`/investments/${id}/valuations`, data),

  /**
   * Delete an investment
   */
  delete: (id: string): Promise<RespostaApi<void>> =>
    api.delete(INVESTMENT_ENDPOINTS.DETAIL(id)),
}
