export interface PontoIndicadorBcbDto {
  data: string
  valor: number
}

export interface SerieIndicadorBcbDto {
  indicador: string
  codigoSgs: number
  unidade: string
  periodicidade: string
  dataDe: string
  dataAte: string
  origem: string
  atualizadoEmUtc: string
  pontos: PontoIndicadorBcbDto[]
}
