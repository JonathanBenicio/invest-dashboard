import { api } from '@/api/client'
import type { PaginatedResponse, ApiResponse, UsuarioDto } from '@/api/dtos'

export interface UserFilters {
  page?: number
  pageSize?: number
  search?: string
}

export type CreateUsuarioDto = Partial<UsuarioDto> & { email: string; name: string }
export type UpdateUsuarioDto = Partial<UsuarioDto>

export const userService = {
  getUsers: async (filters?: UserFilters) => {
    const params = new URLSearchParams()
    if (filters?.page) params.append('page', filters.page.toString())
    if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString())
    if (filters?.search) params.append('search', filters.search)

    return api.get<PaginatedResponse<UsuarioDto>>(`/users?${params.toString()}`)
  },

  createUser: async (data: CreateUsuarioDto) => {
    return api.post<ApiResponse<UsuarioDto>>('/users', data)
  },

  updateUser: async (id: string, data: UpdateUsuarioDto) => {
    return api.patch<ApiResponse<UsuarioDto>>(`/users/${id}`, data)
  },

  deleteUser: async (id: string) => {
    return api.delete<ApiResponse<null>>(`/users/${id}`)
  },
}
