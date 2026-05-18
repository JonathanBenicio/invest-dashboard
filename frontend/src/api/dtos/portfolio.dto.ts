/**
 * DTOs de Carteira
 * Tipos para operações de carteira
 */

import type { BaseEntity, PaginationParams } from './base.dto'

/**
 * Entidade de carteira
 */
export interface CarteiraDto extends BaseEntity {
  name: string
  description?: string
  totalValue: number
  totalInvested: number
  totalGain: number
  gainPercentage: number
  currency: string
  isActive: boolean
  // Extended fields for UI
  bankId?: string
  bankName?: string
  bankLogo?: string
  userId?: string
  userName?: string
  userEmail?: string
  assetsCount?: number
  profitability?: number
}

/**
 * Resumo da carteira com alocação de ativos
 */
export interface ResumoCarteiraDto extends CarteiraDto {
  assetAllocation: AlocacaoAtivoDto[]
  performanceHistory: PontoPerformanceDto[]
}

/**
 * Detalhamento de alocação de ativos
 */
export interface AlocacaoAtivoDto {
  category: string
  value: number
  percentage: number
  color?: string
}

/**
 * Ponto de dados de performance para gráficos
 */
export interface PontoPerformanceDto {
  date: string
  value: number
  percentageChange: number
}

/**
 * Requisição para criar carteira
 */
export interface CriarCarteiraRequest {
  name: string
  description?: string
  currency?: string
}

/**
 * Requisição para atualizar carteira
 */
export interface AtualizarCarteiraRequest {
  name?: string
  description?: string
  isActive?: boolean
}

/**
 * Filtros para lista de carteiras
 */
export interface CarteiraFiltros extends PaginationParams {
  isActive?: boolean
  search?: string
}
