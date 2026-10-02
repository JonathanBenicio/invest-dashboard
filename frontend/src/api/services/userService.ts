import { api } from '@/api/client'
import type { RespostaPaginada, RespostaApi, UsuarioDto } from '@/api/dtos'

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

    return api.get<RespostaPaginada<UsuarioDto>>(`/users?${params.toString()}`)
  },

  createUser: async (data: CreateUsuarioDto) => {
    return api.post<RespostaApi<UsuarioDto>>('/users', data)
  },

  updateUser: async (id: string, data: UpdateUsuarioDto) => {
    return api.patch<RespostaApi<UsuarioDto>>(`/users/${id}`, data)
  },

  deleteUser: async (id: string) => {
    return api.delete<RespostaApi<null>>(`/users/${id}`)
  },
}
