import { api } from '../client'
import type { ApiResponse, AuthenticatedUserDto, AuthSessionDto, LoginRequest, RegisterRequest } from '../dtos'

const BASE = '/auth'

export const authService = {
  login: (credentials: LoginRequest) =>
    api.post<ApiResponse<AuthSessionDto>>(`${BASE}/login`, credentials),

  register: (data: RegisterRequest) =>
    api.post<ApiResponse<AuthSessionDto>>(`${BASE}/register`, data),

  refresh: () =>
    api.post<ApiResponse<AuthSessionDto>>(`${BASE}/refresh`),

  logout: () =>
    api.post<ApiResponse<boolean>>(`${BASE}/logout`),

  me: () =>
    api.get<ApiResponse<AuthenticatedUserDto>>(`${BASE}/me`),
}
