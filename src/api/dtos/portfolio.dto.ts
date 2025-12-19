/**
 * Portfolio DTOs
 * Types for portfolio operations
 */

import type { BaseEntity, PaginationParams } from './base.dto'

/**
 * Portfolio entity
 */
export interface PortfolioDto extends BaseEntity {
  name: string
  description?: string
  totalValue: number
  totalInvested: number
  totalGain: number
  gainPercentage: number
  currency: string
  isActive: boolean
}

/**
 * Portfolio summary with asset allocation
 */
export interface PortfolioSummaryDto extends PortfolioDto {
  assetAllocation: AssetAllocationDto[]
  performanceHistory: PerformancePointDto[]
}

/**
 * Asset allocation breakdown
 */
export interface AssetAllocationDto {
  category: string
  value: number
  percentage: number
  color?: string
}

/**
 * Performance data point for charts
 */
export interface PerformancePointDto {
  date: string
  value: number
  percentageChange: number
}

/**
 * Create portfolio request
 */
export interface CreatePortfolioRequest {
  name: string
  description?: string
  currency?: string
}

/**
 * Update portfolio request
 */
export interface UpdatePortfolioRequest {
  name?: string
  description?: string
  isActive?: boolean
}

/**
 * Portfolio list filters
 */
export interface PortfolioFilters extends PaginationParams {
  isActive?: boolean
  search?: string
}
