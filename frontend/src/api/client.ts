import { API_CONFIG, getApiUrl } from './env'
import { ApiError, NotFoundError, UnauthorizedError, ValidationError } from './errors'
import type { ApiResponse, AuthSessionDto } from './dtos'
import { useAuthStore } from '@/store/authStore'

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'

interface RequestOptions extends Omit<RequestInit, 'method' | 'body'> {
  params?: Record<string, string | number | boolean | undefined>
}

interface ApiProblem {
  message?: string
  title?: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}

let refreshPromise: Promise<string | null> | null = null

const buildUrl = (endpoint: string, params?: RequestOptions['params']): string => {
  const apiPath = getApiUrl(endpoint)
  const url = apiPath.startsWith('http')
    ? new URL(apiPath)
    : new URL(apiPath, window.location.origin)

  Object.entries(params ?? {}).forEach(([key, value]) => {
    if (value !== undefined) url.searchParams.append(key, String(value))
  })

  return url.toString()
}

const getDefaultHeaders = (): HeadersInit => ({
  'Content-Type': 'application/json',
  Accept: 'application/json',
})

const readProblem = async (response: Response): Promise<ApiProblem> =>
  response.json().catch(() => ({} as ApiProblem))

const errorMessage = (error: ApiProblem, statusText: string) =>
  error.detail || error.message || error.title || statusText

const handleResponse = async <T>(response: Response): Promise<T> => {
  if (!response.ok) {
    const errorData = await readProblem(response)
    const message = errorMessage(errorData, response.statusText)

    switch (response.status) {
      case 400:
        throw new ValidationError(message, errorData.errors)
      case 401:
        useAuthStore.getState().clearSession()
        throw new UnauthorizedError(message)
      case 403:
        throw new ApiError(message, 403, 'FORBIDDEN')
      case 404:
        throw new NotFoundError(message)
      default:
        throw new ApiError(message, response.status, errorData.code)
    }
  }

  if (response.status === 204) return {} as T
  return response.json()
}

const refreshAccessToken = (): Promise<string | null> => {
  if (!refreshPromise) {
    const refreshUrl = buildUrl('/auth/refresh')
    refreshPromise = fetch(refreshUrl, {
      method: 'POST',
      headers: getDefaultHeaders(),
      credentials: 'include',
      signal: AbortSignal.timeout(API_CONFIG.TIMEOUT),
    })
      .then(async (response) => {
        if (!response.ok) return null

        const result = await response.json() as ApiResponse<AuthSessionDto>
        const token = result.data?.accessToken
        if (token) useAuthStore.getState().setAccessToken(token)
        return token ?? null
      })
      .catch(() => null)
      .finally(() => {
        refreshPromise = null
      })
  }

  return refreshPromise
}

const request = async <T>(
  method: HttpMethod,
  endpoint: string,
  data?: unknown,
  options: RequestOptions = {},
): Promise<T> => {
  const { params, ...fetchOptions } = options
  const url = buildUrl(endpoint, params)
  const controller = new AbortController()
  const timeoutId = setTimeout(() => controller.abort(), API_CONFIG.TIMEOUT)

  const send = (token: string | null) => fetch(url, {
    method,
    headers: {
      ...getDefaultHeaders(),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...fetchOptions.headers,
    },
    credentials: 'include',
    body: data === undefined ? undefined : JSON.stringify(data),
    ...fetchOptions,
    signal: controller.signal,
  })

  try {
    const token = useAuthStore.getState().accessToken
    let response = await send(token)

    if (response.status === 401 && !endpoint.startsWith('/auth/')) {
      const refreshedToken = await refreshAccessToken()
      if (refreshedToken) response = await send(refreshedToken)
    }

    return await handleResponse<T>(response)
  } catch (error) {
    if (error instanceof Error && error.name === 'AbortError') {
      throw new ApiError('Request timeout', 408, 'TIMEOUT')
    }
    if (error instanceof Error && error.name === 'TimeoutError') {
      throw new ApiError('Request timeout', 408, 'TIMEOUT')
    }
    throw error
  } finally {
    clearTimeout(timeoutId)
  }
}

export const api = {
  get: <T>(endpoint: string, options?: RequestOptions): Promise<T> =>
    request<T>('GET', endpoint, undefined, options),
  post: <T>(endpoint: string, data?: unknown, options?: RequestOptions): Promise<T> =>
    request<T>('POST', endpoint, data, options),
  put: <T>(endpoint: string, data?: unknown, options?: RequestOptions): Promise<T> =>
    request<T>('PUT', endpoint, data, options),
  patch: <T>(endpoint: string, data?: unknown, options?: RequestOptions): Promise<T> =>
    request<T>('PATCH', endpoint, data, options),
  delete: <T>(endpoint: string, options?: RequestOptions): Promise<T> =>
    request<T>('DELETE', endpoint, undefined, options),
}

export type { RequestOptions }
