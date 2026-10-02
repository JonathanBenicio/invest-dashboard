import { create } from 'zustand'
import { authService } from '@/api/services/auth.service'
import type { UsuarioAutenticadoDto, SessaoAutenticacaoDto, SolicitacaoLogin, SolicitacaoCadastro } from '@/api/dtos'
import { queryClient } from '@/lib/query-client'

interface AuthState {
  user: UsuarioAutenticadoDto | null
  accessToken: string | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (credentials: SolicitacaoLogin) => Promise<SessaoAutenticacaoDto>
  register: (request: SolicitacaoCadastro) => Promise<SessaoAutenticacaoDto>
  acceptInvitation: (request: { grupoId: string; conviteId: string; tokenHash?: string; tokenSupabase?: string }) => Promise<SessaoAutenticacaoDto>
  logout: () => Promise<void>
  checkAuth: () => Promise<void>
  clearSession: () => void
  setAccessToken: (token: string | null) => void
  hasPermission: (permission: 'view' | 'edit' | 'admin') => boolean
}

function storeSession(
  session: SessaoAutenticacaoDto,
  set: (partial: Partial<AuthState>) => void,
  currentUser: UsuarioAutenticadoDto | null,
) {
  if (currentUser?.id !== session.usuario.id) queryClient.clear()
  set({
    user: session.usuario,
    accessToken: session.tokenAcesso,
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
      const session = (await authService.login(credentials)).dados
      if (!session?.tokenAcesso) throw new Error('Login response did not include an access token.')
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
      const session = (await authService.register(request)).dados
      if (!session) throw new Error('Registration response is invalid.')
      if (session.requerConfirmacaoEmail || !session.tokenAcesso) {
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

  acceptInvitation: async (request) => {
    set({ isLoading: true })
    try {
      const session = (await authService.acceptInvitation(request)).dados
      if (!session?.tokenAcesso) throw new Error('O convite não gerou uma sessão válida.')
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
      const session = (await authService.refresh()).dados
      if (!session?.tokenAcesso) throw new Error('Refresh response did not include an access token.')
      storeSession(session, set, get().user)
    } catch {
      get().clearSession()
    }
  },

  hasPermission: (permission) => {
    const role = get().user?.perfil
    if (!role) return false
    if (permission === 'admin') return role === 'admin'
    return permission === 'view' || role === 'admin' || role === 'user'
  },
}))
