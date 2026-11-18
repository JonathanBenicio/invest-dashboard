import { useQuery } from '@tanstack/react-query'
import { tesouroDiretoService } from '@/api/services/tesouro-direto.service'
import type { AvaliacaoPosicaoTesouroDto } from '@/api/services/tesouro-direto.service'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

const money = (value: number) => value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const date = (value: string) => value.split('-').reverse().join('/')

export function useAvaliacaoTesouro(positionId: string, enabled: boolean) {
  return useQuery({
    queryKey: ['tesouro-position-valuation', positionId],
    queryFn: () => tesouroDiretoService.avaliacao(positionId),
    enabled,
    retry: false,
  })
}

export function AvaliacaoTesouro({
  value,
  isLoading,
  isError,
  onRetry,
}: {
  value?: AvaliacaoPosicaoTesouroDto
  isLoading: boolean
  isError: boolean
  onRetry: () => void
}) {
  return <Card>
    <CardHeader>
      <CardTitle>Cotação atual e valores do extrato</CardTitle>
      <CardDescription>O valor bruto usa PU base × quantidade. Tributos e taxas do extrato continuam associados à data em que foram informados.</CardDescription>
    </CardHeader>
    <CardContent className="space-y-3">
      {isLoading && <p role="status" className="text-sm text-muted-foreground">Consultando PU e dados do extrato...</p>}
      {isError && <div role="alert" className="text-sm text-destructive">Não foi possível consultar a cotação do Tesouro.<Button className="ml-2" variant="outline" size="sm" onClick={onRetry}>Tentar novamente</Button></div>}
      {value && <>
        <dl className="grid gap-3 sm:grid-cols-2">
          <div><dt className="text-xs text-muted-foreground">Valor bruto pela cotação</dt><dd className="font-semibold">{value.valorBrutoMercado === null ? 'Indisponível' : money(value.valorBrutoMercado)}</dd></div>
          <div><dt className="text-xs text-muted-foreground">PU base</dt><dd>{value.puBase === null ? 'Indisponível' : `${money(value.puBase)}${value.dataCotacao ? ` · ${date(value.dataCotacao)}` : ''}`}</dd></div>
          <div><dt className="text-xs text-muted-foreground">IR informado no extrato</dt><dd>{value.impostoRendaExtrato === null ? 'Não informado' : `${money(value.impostoRendaExtrato)}${value.dataExtrato ? ` · ${date(value.dataExtrato)}` : ''}`}</dd></div>
          <div><dt className="text-xs text-muted-foreground">IOF informado no extrato</dt><dd>{value.iofExtrato === null ? 'Não informado' : `${money(value.iofExtrato)}${value.dataExtrato ? ` · ${date(value.dataExtrato)}` : ''}`}</dd></div>
          <div><dt className="text-xs text-muted-foreground">Custódia B3 informada no extrato</dt><dd>{value.taxaCustodiaExtrato === null ? 'Não informada' : `${money(value.taxaCustodiaExtrato)}${value.dataTaxasExtrato ? ` · ${date(value.dataTaxasExtrato)}` : ''}`}</dd></div>
          <div><dt className="text-xs text-muted-foreground">Taxa da instituição informada no extrato</dt><dd>{value.taxaInstituicaoExtrato === null ? 'Não informada' : `${money(value.taxaInstituicaoExtrato)}${value.dataTaxasExtrato ? ` · ${date(value.dataTaxasExtrato)}` : ''}`}</dd></div>
          <div><dt className="text-xs text-muted-foreground">Valor líquido no extrato</dt><dd>{value.valorLiquidoExtrato === null ? 'Não informado' : `${money(value.valorLiquidoExtrato)}${value.dataExtrato ? ` · ${date(value.dataExtrato)}` : ''}`}</dd></div>
          <div><dt className="text-xs text-muted-foreground">Valor líquido estimado na cotação</dt><dd className="font-semibold">{value.valorLiquidoEstimado === null ? 'Indisponível' : money(value.valorLiquidoEstimado)}</dd></div>
        </dl>
        {value.motivoLiquidoIndisponivel && <p role="status" className="text-sm text-amber-700 dark:text-amber-300">Líquido atual não calculado: {value.motivoLiquidoIndisponivel}</p>}
        {value.avisos.map((warning, index) => <p key={index} className="text-xs text-muted-foreground">{warning}</p>)}
        {value.origemCotacao && <p className="text-xs text-muted-foreground">Fonte: {value.urlFonteCotacao ? <a className="underline underline-offset-2" href={value.urlFonteCotacao} target="_blank" rel="noreferrer">{value.origemCotacao}</a> : value.origemCotacao}{value.coletadoEmUtc ? ` · coletado ${new Date(value.coletadoEmUtc).toLocaleString('pt-BR')}` : ''}</p>}
      </>}
    </CardContent>
  </Card>
}
