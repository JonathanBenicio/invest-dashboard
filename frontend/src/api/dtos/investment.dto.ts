/**
 * DTOs de Investimento
 * Tipos para investimentos de renda fixa e variável
 */

import type { BaseEntity, PaginationParams } from './base.dto'

/**
 * Tipo de investimento
 */
export type TipoInvestimento = 'fixed_income' | 'variable_income'

/**
 * Subtipos de renda fixa
 */
export type TipoRendaFixa = 'CDB' | 'LCI' | 'LCA' | 'TESOURO_DIRETO' | 'DEBENTURE' | 'CRI' | 'CRA'

/**
 * Subtipos de renda variável
 */
export type TipoRendaVariavel = 'ACAO' | 'FII' | 'ETF' | 'BDR' | 'CRYPTO'

/**
 * Entidade base de investimento
 */
export interface PosicaoInvestimentoDto extends BaseEntity {
  portfolioId: string
  name: string
  ticker?: string
  type: TipoInvestimento
  subtype: TipoRendaFixa | TipoRendaVariavel
  quantity: number
  averagePrice: number
  currentPrice: number
  totalInvested: number
  currentValue: number
  gain: number
  gainPercentage: number
  currency: string
}

/**
 * Campos específicos de renda fixa
 */
export interface RendaFixaDto extends PosicaoInvestimentoDto {
  type: 'fixed_income'
  subtype: TipoRendaFixa
  issuer: string
  interestRate: number
  indexer?: 'CDI' | 'IPCA' | 'SELIC' | 'PREFIXADO'
  maturityDate: string
  purchaseDate: string
}

/**
 * Campos específicos de renda variável
 */
export interface RendaVariavelDto extends PosicaoInvestimentoDto {
  type: 'variable_income'
  subtype: TipoRendaVariavel
  sector?: string
  dividendYield?: number
  lastDividend?: number
}

/**
 * Requisição para criar renda fixa
 */
export interface CriarRendaFixaRequest {
  portfolioId: string
  name: string
  subtype: TipoRendaFixa
  issuer: string
  quantity: number
  averagePrice: number
  interestRate: number
  indexer?: 'CDI' | 'IPCA' | 'SELIC' | 'PREFIXADO'
  maturityDate: string
  purchaseDate: string
}

/**
 * Requisição para criar renda variável
 */
export interface CriarRendaVariavelRequest {
  portfolioId: string
  ticker: string
  subtype: TipoRendaVariavel
  quantity: number
  averagePrice: number
  purchaseDate: string
}

/**
 * Requisição para atualizar investimento
 */
export interface AtualizarInvestimentoRequest {
  quantity?: number
  averagePrice?: number
  currentPrice?: number
}

/**
 * Filtros para lista de investimentos
 */
export interface InvestimentoFiltros extends PaginationParams {
  portfolioId?: string
  type?: TipoInvestimento
  subtype?: TipoRendaFixa | TipoRendaVariavel
  search?: string
  issuer?: string
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

/**
 * Resumo de investimento para dashboard
 */
export interface ResumoInvestimentoDto {
  totalInvested: number
  currentValue: number
  totalGain: number
  gainPercentage: number
  fixedIncomeTotal: number
  variableIncomeTotal: number
  topPerformers: PosicaoInvestimentoDto[]
  worstPerformers: PosicaoInvestimentoDto[]
}
