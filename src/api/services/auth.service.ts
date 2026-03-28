/**
 * Authentication Service
 * Handles all auth-related API calls
 */

import { api } from '../client'
import type {
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  TokenResponse,
  UserDto,
  PasswordResetRequest,
  PasswordChangeRequest,
  ApiResponse,
} from '../dtos'

const AUTH_ENDPOINTS = {
  LOGIN: '/auth/login',
  REGISTER: '/auth/register',
  LOGOUT: '/auth/logout',
  REFRESH: '/auth/refresh',
  ME: '/auth/me',
  PASSWORD_RESET: '/auth/password-reset',
  PASSWORD_CHANGE: '/auth/password-change',
} as const

/**
 * Authentication service with all auth operations
 */
export const authService = {
  /**
   * Login with email and password
   */
  login: (credentials: LoginRequest): Promise<ApiResponse<AuthResponse>> =>
    api.post(AUTH_ENDPOINTS.LOGIN, credentials),

  /**
   * Register a new user
   */
  register: (data: RegisterRequest): Promise<ApiResponse<AuthResponse>> =>
    api.post(AUTH_ENDPOINTS.REGISTER, data),

  /**
   * Logout current user
   */
  logout: (): Promise<ApiResponse<void>> =>
    api.post(AUTH_ENDPOINTS.LOGOUT),

  /**
   * Get current user profile
   */
  getMe: (): Promise<ApiResponse<UserDto>> =>
    api.get(AUTH_ENDPOINTS.ME),

  /**
   * Refresh access token
   */
  refreshToken: (refreshToken: string): Promise<ApiResponse<TokenResponse>> =>
    api.post(AUTH_ENDPOINTS.REFRESH, { refreshToken }),

  /**
   * Request password reset email
   */
  requestPasswordReset: (data: PasswordResetRequest): Promise<ApiResponse<void>> =>
    api.post(AUTH_ENDPOINTS.PASSWORD_RESET, data),

  /**
   * Change password
   */
  changePassword: (data: PasswordChangeRequest): Promise<ApiResponse<void>> =>
    api.post(AUTH_ENDPOINTS.PASSWORD_CHANGE, data),

  /**
   * Update user profile
   */
  updateProfile: (data: Partial<UserDto>): Promise<ApiResponse<UserDto>> =>
    api.patch(AUTH_ENDPOINTS.ME, data),
}
