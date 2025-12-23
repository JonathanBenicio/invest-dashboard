/**
 * Authentication DTOs
 * Types for login, register, and token operations
 */

import type { BaseEntity } from './base.dto'

/**
 * Login request payload
 */
export interface LoginRequest {
  email: string
  password: string
  rememberMe?: boolean
}

/**
 * Register request payload
 */
export interface RegisterRequest {
  name: string
  email: string
  password: string
  confirmPassword: string
}

/**
 * User profile data
 */
export interface UserDto extends BaseEntity {
  name: string
  email: string
  avatar?: string
  role: 'user' | 'admin' | 'edit' | 'view'
  isEmailVerified: boolean
  isActive: boolean
  parentesco?: string
}

/**
 * Authentication response with token and user
 */
export interface AuthResponse {
  user: UserDto
  accessToken: string
  refreshToken: string
  expiresIn: number
}

/**
 * Token refresh response
 */
export interface TokenResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
}

/**
 * Password reset request
 */
export interface PasswordResetRequest {
  email: string
}

/**
 * Password change request
 */
export interface PasswordChangeRequest {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}
