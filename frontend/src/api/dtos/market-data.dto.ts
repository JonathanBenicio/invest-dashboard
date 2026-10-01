export interface CotacaoMercadoDto {
  simbolo: string
  nome: string
  preco: number
  observadoEmUtc: string
  moeda: string
  setor?: string
  subtipo?: string
  origem: string
}

export interface ResultadoBuscaMercadoDto {
  simbolo: string
  nome: string
  moeda: string
  setor?: string
  subtipo: string
}

export interface PontoHistoricoMercadoDto {
  simbolo: string
  dataUtc: string
  preco: number
  origem: string
  ajustado: boolean
}
