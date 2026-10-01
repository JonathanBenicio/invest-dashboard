export interface PontoBenchmarkCdiDto {
  data: string
  taxaDiariaPercentual: number
  indiceBase100: number
}

export interface SerieBenchmarkCdiDto {
  dataDe: string
  dataAte: string
  origem: string
  atualizadoEmUtc: string
  pontos: PontoBenchmarkCdiDto[]
}
