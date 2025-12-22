import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { api } from '@/api/client'
import type { UserDto } from '@/api/dtos'
import { useNavigate } from '@tanstack/react-router'
import { useToast } from '@/hooks/use-toast'

interface AuthContextType {
  user: UserDto | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (credentials: any) => Promise<void>
  logout: () => Promise<void>
  hasPermission: (permission: 'view' | 'edit' | 'admin') => boolean
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const navigate = useNavigate()
  const { toast } = useToast()

  const checkAuth = async () => {
    try {
      const response = await api.get<{ data: UserDto }>('/auth/me')
      setUser(response.data)
    } catch (error) {
      setUser(null)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    checkAuth()
  }, [])

  const login = async (credentials: any) => {
    try {
      setIsLoading(true)
      const response = await api.post<{ data: UserDto }>('/auth/login', credentials)
      setUser(response.data)
      toast({
        title: "Login realizado com sucesso!",
        description: `Bem-vindo, ${response.data.name}`,
      })
      navigate({ to: '/dashboard' })
    } catch (error) {
      toast({
        title: "Erro no login",
        description: "Credenciais inválidas. Tente novamente.",
        variant: "destructive"
      })
      throw error
    } finally {
      setIsLoading(false)
    }
  }

  const logout = async () => {
    try {
      await api.post('/auth/logout')
      setUser(null)
      navigate({ to: '/login' })
    } catch (error) {
      console.error('Logout failed', error)
    }
  }

  const hasPermission = (requiredRole: 'view' | 'edit' | 'admin') => {
    if (!user) return false

    // Admin has all permissions
    if (user.role === 'admin') return true

    // Edit has edit and view permissions
    if (requiredRole === 'edit' && user.role === 'edit') return true
    if (requiredRole === 'view' && (user.role === 'edit' || user.role === 'view')) return true

    return false
  }

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, isLoading, login, logout, hasPermission }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
