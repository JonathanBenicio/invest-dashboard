import type { ParametrosPaginacao } from './base.dto'

export interface CarteiraDto {
  id: string
  criadoEm?: string
  atualizadoEm?: string
  nome: string
  descricao?: string
  grupoId?: string
  titularId?: string
  titular: string
  titularVinculado: boolean
  parentesco?: string
  instituicaoFinanceiraId?: string
  instituicaoFinanceira?: string
  tipoInstituicao?: 'Banco' | 'Corretora' | 'DTVM' | 'Outra'
  visibilidade: 'Particular' | 'PublicaDoGrupo'
  posicoes: import('./investment.dto').PosicaoInvestimentoDto[]
  valorTotal: number
  totalInvestido: number
  ganhoTotal: number
  percentualGanho: number
  moeda: string
  quantidadeAtivos: number
}

export interface ResumoCarteiraDto extends CarteiraDto {
  alocacaoAtivos: AlocacaoAtivoDto[]
}

export interface AlocacaoAtivoDto {
  categoria: string
  valor: number
  percentual: number
  cor?: string
}

export interface PontoHistoricoCarteiraDto {
  data: string
  valorTotal: number | null
  estaCompleto: boolean
  tickersAusentes: string[]
}

export interface CriarCarteiraRequest {
  nome: string
  descricao?: string
  grupoId: string
  titular?: string
  titularId?: string
  titularUsuarioId?: string
  parentesco?: string
  instituicaoFinanceiraId?: string
  instituicaoFinanceira?: string
  tipoInstituicao?: 'Banco' | 'Corretora' | 'DTVM' | 'Outra'
  visibilidade: 'Particular' | 'PublicaDoGrupo'
}

export interface AtualizarCarteiraRequest {
  nome?: string
  descricao?: string
  titular?: string
  titularId?: string
  titularUsuarioId?: string
  desvincularTitular?: boolean
  parentesco?: string
  instituicaoFinanceiraId?: string
  instituicaoFinanceira?: string
  tipoInstituicao?: 'Banco' | 'Corretora' | 'DTVM' | 'Outra'
  visibilidade?: 'Particular' | 'PublicaDoGrupo'
}

export interface GrupoCarteirasDto {
  id: string
  nome: string
  papel: 'Admin' | 'Investidor' | 'Consulta'
}

export interface ResumoCarteirasDto {
  valorTotal: number
  totalInvestido: number
  ganhoTotal: number
  percentualGanho: number
  quantidadeCarteiras: number
}

export interface ProjecaoRendaFixaDto {
  posicaoId: string
  ticker: string
  valorObservado: number
  observadoEmUtc?: string
  valorProjetadoBruto: number | null
  dataVencimento: string
  estadoProjecao: string
  motivo?: string
  taxaSimbolo?: string
  taxaValorObservado?: number
  taxaUnidade?: string
  taxaPeriodicidade?: string
  taxaDataReferencia?: string
  taxaOrigem?: string
  taxaIdade?: number
  taxaValidade?: number
  taxaUnidadeValidade?: string
}

export interface ProjecaoRendaFixaConsolidadaDto {
  valorObservado: number
  valorProjetadoBruto: number | null
  quantidadePosicoes: number
  estaCompleta: boolean
  posicoesSemProjecao: string[]
}

export interface MembroGrupoDto {
  id: string
  usuarioId: string
  email: string
  nome: string
  papel: 'Admin' | 'Investidor' | 'Consulta'
  ativo: boolean
}

export interface ConvitePendenteGrupoDto {
  id: string
  grupoId: string
  grupoNome: string
  papel: 'Admin' | 'Investidor' | 'Consulta'
  expiraEmUtc: string
}

export interface ResultadoConviteGrupoDto {
  emailEnviado: boolean
  mensagem: string
}

export interface InstituicaoFinanceiraDto {
  id: string
  nome: string
  categoria: 'Banco' | 'Corretora' | 'DTVM' | 'Outra'
  grupoId?: string
  personalizada: boolean
}

export interface CarteiraFiltros extends ParametrosPaginacao {
  busca?: string
  grupoId?: string
}
