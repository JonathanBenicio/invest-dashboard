/**
 * Base API Response Types
 * Common types used across all API responses
 */

/**
 * Standard API Response wrapper
 */
export interface ApiResponse<T> {
  data: T
  success: boolean
  message?: string
}

/**
 * Paginated API Response
 */
export interface PaginatedResponse<T> {
  data: T[]
  success: boolean
  message?: string
  pagination: {
    page: number
    pageSize: number
    totalCount: number
    totalPages: number
    hasNextPage: boolean
    hasPreviousPage: boolean
  }
}

/**
 * Pagination request parameters
 */
export interface PaginationParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

/**
 * Base entity with common fields
 */
export interface BaseEntity {
  id: string
  createdAt: string
  updatedAt: string
}

/**
 * Error response from API
 */
export interface ErrorResponse {
  success: false
  message: string
  code?: string
  errors?: Record<string, string[]>
}
