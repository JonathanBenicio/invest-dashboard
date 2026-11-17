import { useState } from 'react'
import { ApiError } from '@/api/errors/api-error'
import { tesouroDiretoService, type ConciliacaoTesouro, type RevisaoTesouro } from '@/api/services/tesouro-direto.service'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

type Transacao = ConciliacaoTesouro['transacoes'][number]
const nomesDivergencias: Record<string, string> = {
  data: 'data', quantidade: 'quantidade', preco: 'preço', transacaoIndisponivel: 'lançamento não disponível', carteiraAlterada: 'carteira alterada desde a revisão',
}
const descrever = (item: Transacao) => {
  const data = new Date(item.data).toLocaleDateString('pt-BR', { timeZone: 'America/Sao_Paulo' })
  const tipo = item.tipo === 'Buy' ? 'Compra' : item.tipo === 'Sell' ? 'Venda' : item.tipo
  return `${tipo} · ${data} · quantidade ${item.quantidade.toLocaleString('pt-BR')} · preço ${item.preco.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}`
}

export function RevisaoEventoTesouro({ carteiraId, loteId, ativoId, carteiraVersao, indiceEvento, tipoEvento, motivosSemVinculo, candidatos, transacoes }: {
  carteiraId: string; loteId: string; ativoId: string; carteiraVersao: number; indiceEvento: number
  tipoEvento: 'Buy' | 'Sell' | null; motivosSemVinculo: string[]; candidatos: string[]; transacoes: Transacao[]
}) {
  const [historico, setHistorico] = useState<RevisaoTesouro[] | null>(null)
  const [opcao, setOpcao] = useState('')
  const [ocupado, setOcupado] = useState(false)
  const [erro, setErro] = useState('')
  const [conflito, setConflito] = useState(false)
  const [salvo, setSalvo] = useState(false)
  const candidatosDisponiveis = transacoes.filter(item => tipoEvento !== null && item.tipo === tipoEvento && !item.posteriorBase)

  async function carregar() {
    setOcupado(true); setErro(''); setSalvo(false)
    try {
      const resposta = await tesouroDiretoService.revisoes(carteiraId, loteId, indiceEvento)
      setHistorico(resposta.dados)
    } catch {
      setErro('Não foi possível carregar as revisões. Tente novamente.')
    } finally { setOcupado(false) }
  }

  async function salvar() {
    if (!historico || !opcao || conflito) return
    const revisaoAnterior = historico.at(-1)?.revisao ?? 0
    setOcupado(true); setErro(''); setSalvo(false)
    try {
      const resposta = await tesouroDiretoService.registrarRevisao(carteiraId, loteId, {
        indiceEvento, decisao: opcao === 'sem_vinculo' ? 'sem_vinculo' : 'vincular', ativoId,
        transacaoId: opcao === 'sem_vinculo' ? null : opcao, carteiraVersao, revisaoAnterior,
      })
      setHistorico([...historico, resposta.dados]); setOpcao(''); setSalvo(true)
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setConflito(true)
        setErro('A carteira ou uma revisão mudou. Compare novamente as movimentações antes de registrar sua decisão.')
      } else {
        setErro('Não foi possível salvar a revisão. Tente novamente.')
      }
    } finally { setOcupado(false) }
  }

  return <div className="mt-2 space-y-2">
    {!historico && <Button type="button" variant="outline" size="sm" disabled={ocupado} onClick={() => void carregar()}>Revisar esta movimentação</Button>}
    {ocupado && <p role="status">Aguarde...</p>}
    {erro && <p role="alert" className="text-destructive">{erro}</p>}
    {salvo && <p role="status">Revisão salva. A importação ainda não foi confirmada.</p>}
    {historico && <>
      <p>Esta decisão registra sua revisão; ela não altera a carteira nem confirma a importação.</p>
      {motivosSemVinculo.length > 0 && <p className="text-amber-700 dark:text-amber-300">Não é possível propor vínculo: {motivosSemVinculo.join('; ')}. Você ainda pode registrar uma revisão sem vínculo.</p>}
      {historico.length === 0 ? <p>Nenhuma revisão anterior.</p> : <ol className="space-y-1" aria-label={`Histórico de revisões da movimentação ${indiceEvento + 1}`}>
        {historico.map(item => <li key={item.id}>Revisão {item.revisao}: {item.decisao === 'sem_vinculo' ? 'sem vínculo com lançamento manual' : (() => {
          const transacao = transacoes.find(t => t.id === item.transacaoId)
          return transacao ? `vínculo proposto com ${descrever(transacao)}` : 'vínculo proposto com lançamento indisponível para comparação'
        })()}{(item.divergencias?.length ?? 0) > 0 && <span className="text-amber-700 dark:text-amber-300"> · Divergências: {item.divergencias!.map(campo => nomesDivergencias[campo] ?? campo).join(', ')}</span>}</li>)}
      </ol>}
      {historico.length >= 100 ? <p>Limite de revisões atingido para esta movimentação.</p> : <>
        <label htmlFor={`tesouro-revisao-${indiceEvento}`} className="block font-medium">Decisão para movimentação {indiceEvento + 1}</label>
        <Select value={opcao} onValueChange={setOpcao} disabled={ocupado || conflito}>
          <SelectTrigger id={`tesouro-revisao-${indiceEvento}`}><SelectValue placeholder="Selecione uma decisão" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="sem_vinculo">Sem vínculo com lançamento manual</SelectItem>
            {candidatosDisponiveis.map(item => <SelectItem key={item.id} value={item.id}>{descrever(item)} · {candidatos.includes(item.id) ? 'sugestão' : 'outro lançamento'}</SelectItem>)}
          </SelectContent>
        </Select>
        <Button type="button" variant="outline" size="sm" disabled={!opcao || ocupado || conflito} onClick={() => void salvar()}>Salvar revisão</Button>
      </>}
    </>}
  </div>
}
