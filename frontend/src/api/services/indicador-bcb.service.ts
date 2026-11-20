import { api } from '@/api/client'
import type { RespostaApi } from '@/api/dtos/base.dto'
import type { SerieIndicadorBcbDto } from '@/api/dtos/indicador-bcb.dto'

export const indicadorBcbService = {
  consultar: (indicador: 'selic' | 'ipca', dataDe: string, dataAte: string) =>
    api.get<RespostaApi<SerieIndicadorBcbDto>>(`/indicadores/${indicador}`, {
      params: { dataDe, dataAte },
    }),
}
