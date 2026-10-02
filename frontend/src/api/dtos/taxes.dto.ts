export interface TaxaEconomicaDto {
  id: string
  nome: string
  simbolo: string
  valorAtual: number
  valorAnterior: number
  variacao: number
  descricao: string
  origem: string
  atualizadoEm: string
  grupoId: string
  unidade: 'Percentual' | 'R$/US$' | 'BRL' | 'Pontos'
  periodicidade: 'Diaria' | 'Mensal' | 'Anual' | 'Pontual'
  dataReferencia: string
  atualizadoPorUserId: string
}

export interface CriarTaxaEconomicaRequest {
  nome: string
  simbolo: string
  valorAtual: number
  valorAnterior: number
  descricao: string
  origem: string
  unidade: TaxaEconomicaDto['unidade']
  periodicidade: TaxaEconomicaDto['periodicidade']
  dataReferencia: string
}

export interface AtualizarTaxaEconomicaRequest {
  nome: string
  simbolo: string
  valorAtual: number
  valorAnterior: number
  descricao: string
  origem: string
  unidade: TaxaEconomicaDto['unidade']
  periodicidade: TaxaEconomicaDto['periodicidade']
  dataReferencia: string
}

export interface TaxaEconomicaHistoricoDto {
  valorAnterior: number
  valorNovo: number
  unidadeAnterior?: TaxaEconomicaDto['unidade']
  unidadeNova?: TaxaEconomicaDto['unidade']
  periodicidadeAnterior?: TaxaEconomicaDto['periodicidade']
  periodicidadeNova?: TaxaEconomicaDto['periodicidade']
  origem: string
  dataReferencia: string
  responsavelUserId: string
  atualizadoEmUtc: string
}

export interface EstimativaImpostoMensalDto {
  ano: number; mes: number; versaoRegras: string; vendasAcoesComuns: number
  operacoesSemModalidade: number; operacoesForaEscopo: number; estimativaIncompleta: boolean
  estimativaIr: number; categorias: CategoriaImpostoEstimadoDto[]; limitacoes: string[]; fontes: string[]
}

export interface CategoriaImpostoEstimadoDto {
  categoria: string; ganhoLiquido: number; prejuizoCompensado: number; prejuizoAcumulado: number
  baseTributavel: number; aliquota: number; isento: boolean; irEstimado: number
}
