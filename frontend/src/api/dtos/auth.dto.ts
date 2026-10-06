/**
 * Authentication DTOs
 * Types for login, register, and token operations
 */

import type { EntidadeBase } from './base.dto'

/**
 * Login request payload
 */
export interface SolicitacaoLogin {
  email: string
  senha: string
  lembrarMe?: boolean
}

/**
 * Register request payload
 */
export interface SolicitacaoCadastro {
  nome: string
  email: string
  senha: string
}

export interface UsuarioAutenticadoDto {
  id: string
  nome: string
  email: string
  perfil: 'user' | 'admin'
  avatar?: string
}

export interface SessaoAutenticacaoDto {
  tokenAcesso: string
  expiraEmSegundos: number
  usuario: UsuarioAutenticadoDto
  requerConfirmacaoEmail: boolean
}

/**
 * User profile data
 */
export interface UsuarioDto extends EntidadeBase {
  nome: string
  email: string
  avatar?: string
  perfil: 'user' | 'admin' | 'edit' | 'view'
  emailVerificado: boolean
  ativo: boolean
  parentesco?: string
}

/**
 * Authentication response with token and user
 */
export interface RespostaAutenticacao {
  usuario: UsuarioDto
  tokenAcesso: string
  tokenAtualizacao: string
  expiraEmSegundos: number
}

/**
 * Token refresh response
 */
export interface RespostaToken {
  tokenAcesso: string
  tokenAtualizacao: string
  expiraEmSegundos: number
}

/**
 * Password reset request
 */
export interface SolicitacaoRedefinicaoSenha {
  email: string
}

/**
 * Password change request
 */
export interface SolicitacaoAlteracaoSenha {
  senhaAtual: string
  novaSenha: string
  confirmarSenha: string
}
