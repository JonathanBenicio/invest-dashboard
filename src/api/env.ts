/**
 * API Environment Configuration
 * Centralizes all API-related environment variables
 */

export const API_CONFIG = {
  /**
   * Base URL for API requests
   * Uses VITE_API_URL from environment or defaults to localhost
   */
  /**
   * When MSW is active, use empty string so requests are relative.
   * MSW intercepts relative requests on the same origin automatically.
   */
  BASE_URL: (import.meta.env.VITE_USE_MSW === 'true')
    ? ''
    : (import.meta.env.VITE_API_URL || 'http://localhost:5000'),

  /**
   * Request timeout in milliseconds
   */
  TIMEOUT: 10000,

  /**
   * API version prefix
   */
  VERSION: '/api/v1',
} as const

export const BRAPI_CONFIG = {
  /**
   * Base URL for Brapi API
   */
  BASE_URL: 'https://brapi.dev/api',

  /**
   * API Token for Brapi
   * Get yours at https://brapi.dev
   */
  TOKEN: import.meta.env.VITE_BRAPI_TOKEN || '',
} as const

/**
 * Get the full API URL with version prefix
 */
export const getApiUrl = (endpoint: string): string => {
  const baseUrl = API_CONFIG.BASE_URL.replace(/\/$/, '')
  const path = endpoint.startsWith('/') ? endpoint : `/${endpoint}`
  return `${baseUrl}${API_CONFIG.VERSION}${path}`
}
