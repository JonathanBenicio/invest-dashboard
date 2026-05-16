import { create } from 'zustand'
import { api } from '@/api/client'
import { supabase } from '@/lib/supabase'
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
      const { data, error } = await supabase.auth.signInWithPassword({
        email: credentials.email,
        password: credentials.password,
      })

      if (error) throw error

      const user = data.user
      if (!user) throw new Error('User not found')

      const userDto: UserDto = {
        id: user.id,
        email: user.email || '',
        name: user.user_metadata.full_name || '',
        role: 'user', // Default role
      }

      set({ user: userDto, isAuthenticated: true, isLoading: false })
    } catch (error) {
      set({ isLoading: false })
      throw error
    }
  },

  logout: async () => {
    try {
      await supabase.auth.signOut()
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
      const { data, error } = await supabase.auth.getUser()
      if (error) throw error

      const user = data.user
      if (!user) throw new Error('User not found')

      const userDto: UserDto = {
        id: user.id,
        email: user.email || '',
        name: user.user_metadata.full_name || '',
        role: 'user', // Default role
      }

      set({ user: userDto, isAuthenticated: true, isLoading: false })
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
