import { create } from 'zustand'
import { api } from '@/api/client'
import type { UserDto } from '@/api/dtos'

interface AuthState {
  user: UserDto | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (credentials: any) => Promise<void>
  logout: () => Promise<void>
  checkAuth: () => Promise<void>
  hasPermission: (permission: 'view' | 'edit' | 'admin') => boolean
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  isAuthenticated: false,
  isLoading: true,

  login: async (credentials) => {
    set({ isLoading: true })
    try {
      const response = await api.post<{ data: UserDto }>('/auth/login', credentials)
      set({ user: response.data, isAuthenticated: true, isLoading: false })
    } catch (error) {
      set({ isLoading: false })
      throw error
    }
  },

  logout: async () => {
    try {
      await api.post('/auth/logout')
    } catch (error) {
      console.error('Logout failed', error)
    } finally {
      // Always clear local state
      set({ user: null, isAuthenticated: false })
    }
  },

  checkAuth: async () => {
    set({ isLoading: true })
    try {
      const response = await api.get<{ data: UserDto }>('/auth/me')
      set({ user: response.data, isAuthenticated: true, isLoading: false })
    } catch (error) {
      set({ user: null, isAuthenticated: false, isLoading: false })
    }
  },

  hasPermission: (requiredRole) => {
    const { user } = get()
    if (!user) return false

    const role = user.role as string

    // Admin has all permissions
    if (role === 'admin') return true

    // Edit has edit and view permissions
    if (requiredRole === 'edit' && role === 'edit') return true
    if (requiredRole === 'view' && (role === 'edit' || role === 'view' || role === 'user')) return true

    return false
  },
}))
