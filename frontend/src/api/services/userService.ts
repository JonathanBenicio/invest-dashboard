import { api } from '@/api/client'
import type { PaginatedResponse, ApiResponse, UserDto } from '@/api/dtos'

export interface UserFilters {
  page?: number
  pageSize?: number
  search?: string
}

export type CreateUsuarioDto = Partial<UserDto> & { email: string; name: string }
export type UpdateUsuarioDto = Partial<UserDto>

export const userService = {
  getUsers: async (filters?: UserFilters) => {
    const params = new URLSearchParams()
    if (filters?.page) params.append('page', filters.page.toString())
    if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString())
    if (filters?.search) params.append('search', filters.search)

    return api.get<PaginatedResponse<UserDto>>(`/users?${params.toString()}`)
  },

  createUser: async (data: CreateUsuarioDto) => {
    return api.post<ApiResponse<UserDto>>('/users', data)
  },

  updateUser: async (id: string, data: UpdateUsuarioDto) => {
    return api.patch<ApiResponse<UserDto>>(`/users/${id}`, data)
  },

  deleteUser: async (id: string) => {
    return api.delete<ApiResponse<null>>(`/users/${id}`)
  },
}
