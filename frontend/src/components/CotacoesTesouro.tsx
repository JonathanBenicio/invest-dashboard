import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { tesouroDiretoService, type TituloTesouroPublico } from '@/api/services/tesouro-direto.service'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

const dataBr = (data: string) => data.split('-').reverse().join('/')
const dinheiro = (valor: number) => valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const chave = (titulo: TituloTesouroPublico) => JSON.stringify([titulo.tipoTitulo, titulo.dataVencimento])

export function CotacoesTesouro() {
  const [aberto, setAberto] = useState(false)
  const [selecionado, setSelecionado] = useState('')
  const [inicio, setInicio] = useState('')
  const [fim, setFim] = useState('')
  const [periodo, setPeriodo] = useState<{ inicio: string; fim: string } | null>(null)
  const [erro, setErro] = useState('')
  const titulos = useQuery({ queryKey: ['tesouro-publico-titulos'], queryFn: tesouroDiretoService.titulos, enabled: aberto, retry: false })
  const titulo = titulos.data?.dados.titulos.find(item => chave(item) === selecionado)
  const cotacoes = useQuery({
    queryKey: ['tesouro-publico-cotacoes', selecionado, periodo?.inicio, periodo?.fim],
    queryFn: () => tesouroDiretoService.cotacoes(titulo!, periodo!.inicio, periodo!.fim),
    enabled: !!titulo && !!periodo, retry: false,
  })

  function selecionar(valor: string) {
    setSelecionado(valor)
    setPeriodo(null)
    setErro('')
    const escolhido = titulos.data?.dados.titulos.find(item => chave(item) === valor)
    if (!escolhido) return
    const ultimo = escolhido.ultimaDataDisponivel
    const anoAnterior = new Date(`${ultimo}T00:00:00Z`)
    anoAnterior.setUTCDate(anoAnterior.getUTCDate() - 365)
    const primeiro = anoAnterior.toISOString().slice(0, 10)
    setInicio(primeiro)
    setFim(ultimo)
    setPeriodo({ inicio: primeiro, fim: ultimo })
  }

  return <Card>
    <CardHeader><CardTitle>Cotações públicas do Tesouro Direto</CardTitle><CardDescription>Histórico diário de taxas e preços divulgado pelo Tesouro Transparente; não representa sua posição pessoal.</CardDescription></CardHeader>
    <CardContent className="space-y-4">
      {!aberto && <Button variant="outline" onClick={() => setAberto(true)}>Consultar cotações do Tesouro</Button>}
      {aberto && <>
        {titulos.isLoading && <p role="status" className="text-sm">Consultando dados públicos do Tesouro...</p>}
        {titulos.isError && <div role="alert" className="text-sm text-destructive">Não foi possível consultar os títulos públicos. <Button variant="link" onClick={() => void titulos.refetch()}>Tentar novamente</Button></div>}
        {titulos.data && <>
          <p className="text-xs text-muted-foreground"><a className="underline" href={titulos.data.dados.urlFonte} target="_blank" rel="noreferrer">{titulos.data.dados.origem}</a> · última data disponível {dataBr(titulos.data.dados.ultimaDataDisponivel)} · coleta {new Date(titulos.data.dados.coletadoEmUtc).toLocaleString('pt-BR')}</p>
          {titulos.data.dados.titulos.length === 0 ? <p role="status">Nenhum título disponível.</p> : <div className="grid gap-2">
            <label htmlFor="tesouro-publico-titulo" className="text-sm font-medium">Título e vencimento</label>
            <Select value={selecionado} onValueChange={selecionar}><SelectTrigger id="tesouro-publico-titulo"><SelectValue placeholder="Selecione um título" /></SelectTrigger><SelectContent>{titulos.data.dados.titulos.map(item => <SelectItem key={chave(item)} value={chave(item)}>{item.tipoTitulo} · {dataBr(item.dataVencimento)}</SelectItem>)}</SelectContent></Select>
          </div>}
          {titulo && <form className="flex flex-wrap items-end gap-3" onSubmit={event => { event.preventDefault(); if (!inicio || !fim || inicio > fim) { setErro('Informe um período válido.'); return } setErro(''); setPeriodo({ inicio, fim }) }}>
            <div className="grid gap-2"><label htmlFor="tesouro-publico-inicio" className="text-sm font-medium">Data inicial</label><Input id="tesouro-publico-inicio" type="date" value={inicio} onChange={event => setInicio(event.target.value)} required /></div>
            <div className="grid gap-2"><label htmlFor="tesouro-publico-fim" className="text-sm font-medium">Data final</label><Input id="tesouro-publico-fim" type="date" value={fim} onChange={event => setFim(event.target.value)} required /></div>
            <Button type="submit" variant="outline">Consultar histórico</Button>
          </form>}
          {titulo && <p className="text-xs text-muted-foreground">Última cotação deste título: {dataBr(titulo.ultimaDataDisponivel)}.</p>}
          {erro && <p role="alert" className="text-sm text-destructive">{erro}</p>}
          {cotacoes.isLoading && titulo && periodo && <p role="status" className="text-sm">Carregando histórico...</p>}
          {cotacoes.isError && <div role="alert" className="text-sm text-destructive">Não foi possível consultar o histórico. <Button variant="link" onClick={() => void cotacoes.refetch()}>Tentar novamente</Button></div>}
          {cotacoes.data && <>
            <p className="text-xs text-muted-foreground">PU base: referência para marcação a mercado em D0. Os valores são públicos e podem estar defasados.</p>
            {cotacoes.data.dados.cotacoes.length === 0 ? <p role="status">Nenhuma cotação no período.</p> : <>
              <div className="h-64 min-w-0" aria-label="Gráfico histórico do preço unitário base"><ResponsiveContainer width="100%" height="100%"><LineChart data={cotacoes.data.dados.cotacoes}><XAxis dataKey="dataBase" tickFormatter={dataBr} minTickGap={30} /><YAxis domain={['auto', 'auto']} /><Tooltip labelFormatter={label => dataBr(String(label))} formatter={valor => dinheiro(Number(valor))} /><Line type="monotone" dataKey="puBaseManha" name="PU base" dot={false} stroke="var(--color-primary, #0f766e)" /></LineChart></ResponsiveContainer></div>
              <p className="text-xs text-muted-foreground">A tabela mostra até as 30 cotações mais recentes do período.</p>
              <div className="overflow-x-auto rounded-md border"><Table><TableHeader><TableRow><TableHead>Data</TableHead><TableHead>PU base</TableHead><TableHead>PU venda</TableHead><TableHead>Taxa venda (% a.a.)</TableHead></TableRow></TableHeader><TableBody>{cotacoes.data.dados.cotacoes.slice(-30).reverse().map(item => <TableRow key={item.dataBase}><TableCell>{dataBr(item.dataBase)}</TableCell><TableCell>{dinheiro(item.puBaseManha)}</TableCell><TableCell>{dinheiro(item.puVendaManha)}</TableCell><TableCell>{item.taxaVendaManha.toLocaleString('pt-BR')}%</TableCell></TableRow>)}</TableBody></Table></div>
            </>}
          </>}
        </>}
      </>}
    </CardContent>
  </Card>
}
