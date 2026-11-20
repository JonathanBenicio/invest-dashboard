import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { indicadorBcbService } from '@/api/services/indicador-bcb.service'
import type { SerieIndicadorBcbDto } from '@/api/dtos/indicador-bcb.dto'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

type Periodo = { dataDe: string; dataAte: string }

function hojeEmSaoPaulo(): string {
  const partes = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'America/Sao_Paulo', year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(new Date())
  const valor = (tipo: Intl.DateTimeFormatPartTypes) => partes.find(parte => parte.type === tipo)?.value
  return `${valor('year')}-${valor('month')}-${valor('day')}`
}

function periodoInicial(): Periodo {
  const fim = hojeEmSaoPaulo()
  const inicio = new Date(`${fim}T00:00:00Z`)
  inicio.setUTCDate(inicio.getUTCDate() - 90)
  return { dataDe: inicio.toISOString().slice(0, 10), dataAte: fim }
}

// Datas do SGS são dias de referência, sem fuso horário.
function formatarData(data: string): string {
  const [ano, mes, dia] = data.split('-')
  return ano && mes && dia ? `${dia}/${mes}/${ano}` : data
}

function DadosSerie({ serie }: { serie: SerieIndicadorBcbDto }) {
  const pontos = [...serie.pontos].sort((a, b) => b.data.localeCompare(a.data))
  const visiveis = pontos.slice(0, 10)

  return (
    <div className="space-y-3">
      <p className="text-xs text-muted-foreground">
        {serie.origem} · {serie.unidade} · {serie.periodicidade} · consultado em{' '}
        {new Date(serie.atualizadoEmUtc).toLocaleString('pt-BR')}
      </p>
      {pontos.length === 0 ? (
        <p role="status" className="text-sm text-muted-foreground">Nenhum dado publicado no período selecionado.</p>
      ) : (
        <>
          {pontos.length > visiveis.length && (
            <p className="text-xs text-muted-foreground">Exibindo os 10 registros mais recentes de {pontos.length} no período.</p>
          )}
          <div className="overflow-x-auto rounded-md border">
            <Table>
              <TableHeader><TableRow><TableHead>Data de referência</TableHead><TableHead className="text-right">Valor (%)</TableHead></TableRow></TableHeader>
              <TableBody>{visiveis.map(ponto => (
                <TableRow key={ponto.data}>
                  <TableCell className="whitespace-nowrap">{formatarData(ponto.data)}</TableCell>
                  <TableCell className="text-right">{ponto.valor.toLocaleString('pt-BR', { maximumFractionDigits: 8 })}%</TableCell>
                </TableRow>
              ))}</TableBody>
            </Table>
          </div>
        </>
      )}
    </div>
  )
}

function ConsultaIndicador({ indicador, periodo }: { indicador: 'selic' | 'ipca'; periodo: Periodo }) {
  const query = useQuery({
    queryKey: ['indicador-bcb', indicador, periodo.dataDe, periodo.dataAte],
    queryFn: () => indicadorBcbService.consultar(indicador, periodo.dataDe, periodo.dataAte),
  })
  const titulo = indicador === 'selic' ? 'Selic diária' : 'IPCA mensal'

  return (
    <section aria-label={titulo} className="min-w-0 space-y-3">
      <h3 className="font-semibold">{titulo}</h3>
      {query.isLoading ? <p role="status" className="text-sm text-muted-foreground">Consultando o Banco Central…</p> :
        query.isError ? <div role="alert" className="space-y-2 text-sm text-destructive"><p>Não foi possível consultar {titulo}.</p><Button variant="outline" size="sm" onClick={() => void query.refetch()}>Tentar novamente</Button></div> :
          query.data?.dados ? <DadosSerie serie={query.data.dados} /> : <p role="status" className="text-sm text-muted-foreground">Dados indisponíveis.</p>}
    </section>
  )
}

export function IndicadoresBcb() {
  const [periodo, setPeriodo] = useState(periodoInicial)
  const [inicio, setInicio] = useState(periodo.dataDe)
  const [fim, setFim] = useState(periodo.dataAte)
  const [erro, setErro] = useState('')
  const hoje = hojeEmSaoPaulo()

  function consultar(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const dias = (Date.parse(`${fim}T00:00:00Z`) - Date.parse(`${inicio}T00:00:00Z`)) / 86_400_000
    if (!inicio || !fim || !Number.isFinite(dias) || dias < 0 || dias > 3650 || fim > hoje) {
      setErro('Informe um período válido, sem datas futuras e de até 3.650 dias.')
      return
    }
    setErro('')
    setPeriodo({ dataDe: inicio, dataAte: fim })
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Indicadores oficiais do Banco Central</CardTitle>
        <CardDescription>Consulta informativa às séries do SGS. Estes dados não alteram as taxas manuais nem as projeções.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-5">
        <form onSubmit={consultar} className="flex flex-wrap items-end gap-3">
          <div className="grid gap-2"><Label htmlFor="bcb-data-inicial">Data inicial</Label><Input id="bcb-data-inicial" type="date" value={inicio} max={hoje} onChange={event => setInicio(event.target.value)} required /></div>
          <div className="grid gap-2"><Label htmlFor="bcb-data-final">Data final</Label><Input id="bcb-data-final" type="date" value={fim} max={hoje} onChange={event => setFim(event.target.value)} required /></div>
          <Button type="submit" variant="outline">Consultar período</Button>
        </form>
        {erro && <p role="alert" className="text-sm text-destructive">{erro}</p>}
        <div className="grid gap-6 lg:grid-cols-2">
          <ConsultaIndicador indicador="selic" periodo={periodo} />
          <ConsultaIndicador indicador="ipca" periodo={periodo} />
        </div>
      </CardContent>
    </Card>
  )
}
