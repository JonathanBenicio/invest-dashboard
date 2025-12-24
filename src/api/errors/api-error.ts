/**
 * Base API Error class
 * All custom API errors extend this class
 */
export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly code?: string
  ) {
    super(message)
    this.name = 'ApiError'
    Object.setPrototypeOf(this, ApiError.prototype)
  }
}

/**
 * Unauthorized Error (401)
 * Thrown when authentication fails or token is invalid
 */
export class UnauthorizedError extends ApiError {
  constructor(message = 'Não autorizado') {
    super(message, 401, 'UNAUTHORIZED')
    this.name = 'UnauthorizedError'
    Object.setPrototypeOf(this, UnauthorizedError.prototype)
  }
}

/**
 * Forbidden Error (403)
 * Thrown when user lacks permission
 */
export class ForbiddenError extends ApiError {
  constructor(message = 'Acesso negado') {
    super(message, 403, 'FORBIDDEN')
    this.name = 'ForbiddenError'
    Object.setPrototypeOf(this, ForbiddenError.prototype)
  }
}

/**
 * Not Found Error (404)
 * Thrown when resource doesn't exist
 */
export class NotFoundError extends ApiError {
  constructor(message = 'Recurso não encontrado') {
    super(message, 404, 'NOT_FOUND')
    this.name = 'NotFoundError'
    Object.setPrototypeOf(this, NotFoundError.prototype)
  }
}

/**
 * Validation Error (400)
 * Thrown when request data is invalid
 */
export class ValidationError extends ApiError {
  constructor(
    message: string,
    public readonly errors?: Record<string, string[]>
  ) {
    super(message, 400, 'VALIDATION_ERROR')
    this.name = 'ValidationError'
    Object.setPrototypeOf(this, ValidationError.prototype)
  }
}

/**
 * Server Error (500)
 * Thrown when server encounters an internal error
 */
export class ServerError extends ApiError {
  constructor(message = 'Erro interno do servidor') {
    super(message, 500, 'SERVER_ERROR')
    this.name = 'ServerError'
    Object.setPrototypeOf(this, ServerError.prototype)
  }
}

/**
 * Network Error
 * Thrown when there's a network connectivity issue
 */
export class NetworkError extends ApiError {
  constructor(message = 'Erro de conexão') {
    super(message, 0, 'NETWORK_ERROR')
    this.name = 'NetworkError'
    Object.setPrototypeOf(this, NetworkError.prototype)
  }
}
