/**
 * API Environment Configuration
 * Centralizes all API-related environment variables
 */

export const API_CONFIG = {
  /**
   * Base URL for API requests
   * Uses VITE_API_URL from environment or defaults to localhost
   */
  BASE_URL: import.meta.env.VITE_API_URL || 'http://localhost:5000',

  /**
   * Request timeout in milliseconds
   */
  TIMEOUT: 10000,

  /**
   * API version prefix
   */
  VERSION: '/api/v1',
} as const

/**
 * Get the full API URL with version prefix
 */
export const getApiUrl = (endpoint: string): string => {
  const baseUrl = API_CONFIG.BASE_URL.replace(/\/$/, '')
  const path = endpoint.startsWith('/') ? endpoint : `/${endpoint}`
  return `${baseUrl}${API_CONFIG.VERSION}${path}`
}
