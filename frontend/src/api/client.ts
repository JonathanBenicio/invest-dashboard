import { API_CONFIG, getApiUrl } from './env'
import { ApiError, UnauthorizedError, ValidationError, NotFoundError } from './errors'
import { useAuthStore } from '@/store/authStore'
import type { ApiResponse } from './dtos'

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'

interface RequestOptions extends Omit<RequestInit, 'method' | 'body'> {
  params?: Record<string, string | number | boolean | undefined>
}

/**
 * Build URL with query parameters
 */
const buildUrl = (endpoint: string, params?: RequestOptions['params']): string => {
  const apiPath = getApiUrl(endpoint)

  // When BASE_URL is empty (MSW mode), apiPath is relative like "/api/v1/..."
  // We need to provide a base for the URL constructor
  const url = apiPath.startsWith('http')
    ? new URL(apiPath)
    : new URL(apiPath, window.location.origin)

  if (params) {
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined) {
        url.searchParams.append(key, String(value))
      }
    })
  }

  return url.toString()
}

/**
 * Get default headers for requests
 */
const getDefaultHeaders = (): HeadersInit => {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  }

  return headers
}

/**
 * Handle API response and errors
 */
const handleResponse = async <T>(response: Response): Promise<T> => {
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}))
    const message = errorData.message || response.statusText

    switch (response.status) {
      case 400:
        throw new ValidationError(message, errorData.errors)
      case 401:
        useAuthStore.getState().logout()
        throw new UnauthorizedError(message)
      case 403:
        throw new ApiError(message || 'Access Forbidden', 403, 'FORBIDDEN')
      case 404:
        throw new NotFoundError(message)
      default:
        throw new ApiError(message, response.status, errorData.code)
    }
  }

  // Handle empty responses (204 No Content)
  if (response.status === 204) {
    return {} as T
  }

  return response.json()
}

/**
 * Make HTTP request
 */
const request = async <T>(
  method: HttpMethod,
  endpoint: string,
  data?: unknown,
  options: RequestOptions = {}
): Promise<T> => {
  const { params, ...fetchOptions } = options
  const url = buildUrl(endpoint, params)

  const controller = new AbortController()
  const timeoutId = setTimeout(() => controller.abort(), API_CONFIG.TIMEOUT)

  try {
    const response = await fetch(url, {
      method,
      headers: {
        ...getDefaultHeaders(),
        ...fetchOptions.headers,
      },
      credentials: 'include',
      body: data ? JSON.stringify(data) : undefined,
      signal: controller.signal,
      ...fetchOptions,
    })

    return handleResponse<T>(response)
  } catch (error) {
    if (error instanceof Error && error.name === 'AbortError') {
      throw new ApiError('Request timeout', 408, 'TIMEOUT')
    }
    throw error
  } finally {
    clearTimeout(timeoutId)
  }
}

/**
 * API Client with typed methods
 */
export const api = {
  /**
   * GET request
   */
  get: <T>(endpoint: string, options?: RequestOptions): Promise<T> =>
    request<T>('GET', endpoint, undefined, options),

  /**
   * POST request
   */
  post: <T>(endpoint: string, data?: unknown, options?: RequestOptions): Promise<T> =>
    request<T>('POST', endpoint, data, options),

  /**
   * PUT request
   */
  put: <T>(endpoint: string, data?: unknown, options?: RequestOptions): Promise<T> =>
    request<T>('PUT', endpoint, data, options),

  /**
   * PATCH request
   */
  patch: <T>(endpoint: string, data?: unknown, options?: RequestOptions): Promise<T> =>
    request<T>('PATCH', endpoint, data, options),

  /**
   * DELETE request
   */
  delete: <T>(endpoint: string, options?: RequestOptions): Promise<T> =>
    request<T>('DELETE', endpoint, undefined, options),
}

export type { RequestOptions }
