import { api } from '@/api/client'
import type { PaginatedResponse, ApiResponse, UserDto } from '@/api/dtos'

export interface UserFilters {
  page?: number
  pageSize?: number
  search?: string
}

export type CreateUserDto = Partial<UserDto> & { email: string; name: string }
export type UpdateUserDto = Partial<UserDto>

export const userService = {
  getUsers: async (filters?: UserFilters) => {
    const params = new URLSearchParams()
    if (filters?.page) params.append('page', filters.page.toString())
    if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString())
    if (filters?.search) params.append('search', filters.search)

    const response = await api.get<PaginatedResponse<UserDto>>(`/users?${params.toString()}`)
    return response.data
  },

  createUser: async (data: CreateUserDto) => {
    const response = await api.post<ApiResponse<UserDto>>('/users', data)
    return response.data
  },

  updateUser: async (id: string, data: UpdateUserDto) => {
    const response = await api.patch<ApiResponse<UserDto>>(`/users/${id}`, data)
    return response.data
  },

  deleteUser: async (id: string) => {
    const response = await api.delete<ApiResponse<null>>(`/users/${id}`)
    return response.data
  },
}
