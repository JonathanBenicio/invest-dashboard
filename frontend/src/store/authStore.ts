import { create } from 'zustand'
import { authService } from '@/api/services/auth.service'
import type { AuthenticatedUserDto, AuthSessionDto, LoginRequest, RegisterRequest } from '@/api/dtos'
import { queryClient } from '@/lib/query-client'

interface AuthState {
  user: AuthenticatedUserDto | null
  accessToken: string | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (credentials: LoginRequest) => Promise<AuthSessionDto>
  register: (request: RegisterRequest) => Promise<AuthSessionDto>
  logout: () => Promise<void>
  checkAuth: () => Promise<void>
  clearSession: () => void
  setAccessToken: (token: string | null) => void
  hasPermission: (permission: 'view' | 'edit' | 'admin') => boolean
}

function storeSession(
  session: AuthSessionDto,
  set: (partial: Partial<AuthState>) => void,
  currentUser: AuthenticatedUserDto | null,
) {
  if (currentUser?.id !== session.user.id) queryClient.clear()
  set({
    user: session.user,
    accessToken: session.accessToken,
    isAuthenticated: true,
    isLoading: false,
  })
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  accessToken: null,
  isAuthenticated: false,
  isLoading: true,

  setAccessToken: (accessToken) => set({ accessToken }),

  clearSession: () => {
    queryClient.clear()
    set({ user: null, accessToken: null, isAuthenticated: false, isLoading: false })
  },

  login: async (credentials) => {
    set({ isLoading: true })
    try {
      const session = (await authService.login(credentials)).data
      if (!session?.accessToken) throw new Error('Login response did not include an access token.')
      storeSession(session, set, get().user)
      return session
    } catch (error) {
      get().clearSession()
      throw error
    }
  },

  register: async (request) => {
    set({ isLoading: true })
    try {
      const session = (await authService.register(request)).data
      if (!session) throw new Error('Registration response is invalid.')
      if (session.requiresEmailConfirmation || !session.accessToken) {
        get().clearSession()
        return session
      }
      storeSession(session, set, get().user)
      return session
    } catch (error) {
      get().clearSession()
      throw error
    }
  },

  logout: async () => {
    try {
      await authService.logout()
    } catch (error) {
      console.error('Logout request failed.', error)
    } finally {
      get().clearSession()
    }
  },

  checkAuth: async () => {
    set({ isLoading: true })
    try {
      const session = (await authService.refresh()).data
      if (!session?.accessToken) throw new Error('Refresh response did not include an access token.')
      storeSession(session, set, get().user)
    } catch {
      get().clearSession()
    }
  },

  hasPermission: (permission) => {
    const role = get().user?.role
    if (!role) return false
    if (permission === 'admin') return role === 'admin'
    return permission === 'view' || role === 'admin' || role === 'user'
  },
}))
