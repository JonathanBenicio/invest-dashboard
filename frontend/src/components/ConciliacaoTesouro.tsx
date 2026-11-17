import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { tesouroDiretoService, type ConciliacaoTesouro as Resultado } from '@/api/services/tesouro-direto.service'
import { RevisaoEventoTesouro } from './RevisaoEventoTesouro'

const data = (valor: string) => new Date(valor).toLocaleDateString('pt-BR', { timeZone: 'America/Sao_Paulo' })

export function ConciliacaoTesouro({ carteiraId, loteId }: { carteiraId: string; loteId: string }) {
  const [resultado, setResultado] = useState<Resultado | null>(null)
  const [consulta, setConsulta] = useState(0)
  const [selecionado, setSelecionado] = useState('')
  const [ocupado, setOcupado] = useState(false)
  const [erro, setErro] = useState('')

  async function analisar(ativoId: string | null) {
    setOcupado(true)
    setErro('')
    try {
      const resposta = await tesouroDiretoService.conciliacao(carteiraId, loteId, ativoId)
      setResultado(resposta.dados)
      setConsulta(atual => atual + 1)
    } catch {
      setResultado(null)
      setErro('Não foi possível consultar a conciliação. Tente novamente.')
    } finally {
      setOcupado(false)
    }
  }

  return <section className="space-y-3 rounded-md border p-3 text-sm">
    <h3 className="font-semibold">Revisão da conciliação</h3>
    <p>Consulte possíveis correspondências com lançamentos existentes. Nenhuma associação será aplicada nesta etapa.</p>
    {!resultado && <Button type="button" variant="outline" disabled={ocupado} onClick={() => void analisar(null)}>Revisar conciliação</Button>}
    {ocupado && <p role="status">Consultando movimentações...</p>}
    {erro && <p role="alert" className="text-destructive">{erro}</p>}
    {resultado && <>
      {resultado.ativos.length === 0 ? <p>Não há posições de renda fixa nesta carteira para comparar.</p> : <div className="grid gap-2">
        <label htmlFor="tesouro-conciliacao-ativo" className="font-medium">Posição da carteira para comparar</label>
        <Select value={selecionado} onValueChange={setSelecionado} disabled={ocupado}>
          <SelectTrigger id="tesouro-conciliacao-ativo"><SelectValue placeholder="Selecione uma posição" /></SelectTrigger>
          <SelectContent>{resultado.ativos.map(ativo => <SelectItem key={ativo.ativoId} value={ativo.ativoId}>{ativo.ticker}</SelectItem>)}</SelectContent>
        </Select>
        <Button type="button" variant="outline" disabled={!selecionado || ocupado} onClick={() => void analisar(selecionado)}>Comparar movimentações</Button>
      </div>}
      {resultado.ativoId && resultado.ativoId !== selecionado && <p>Selecione “Comparar movimentações” para consultar a posição escolhida.</p>}
      {resultado.ativoId && resultado.ativoId === selecionado && <>
        <p>Compare os dados antes de decidir. As sugestões não confirmam que os registros sejam a mesma operação.</p>
        <ul className="space-y-3">{resultado.eventos.map(evento => {
          const movimento = evento.dadosInterpretados
          const candidatos = resultado.transacoes.filter(transacao => evento.candidatos.includes(transacao.id))
          const tipoEvento = movimento?.natureza === 'Compra' && movimento.tipo === 'Crédito' ? 'Buy' : movimento?.natureza === 'Venda' && movimento.tipo === 'Débito' ? 'Sell' : null
          const motivosSemVinculo = [
            !tipoEvento ? 'natureza ou tipo não reconhecido para compra ou venda' : null,
            !movimento?.data ? 'data não interpretada' : movimento.data > resultado.dataReferencia ? 'data posterior à referência do extrato' : null,
            movimento?.quantidade == null || movimento.quantidade <= 0 ? 'quantidade não interpretada ou inválida' : null,
            movimento?.preco == null || movimento.preco <= 0 ? 'preço não interpretado ou inválido' : null,
          ].filter((motivo): motivo is string => motivo !== null)
          return <li key={evento.indice} className="rounded-md border p-2">
            <p className="font-medium">Movimentação do extrato {evento.indice + 1}{movimento?.natureza ? ` · ${movimento.natureza}` : ''}{movimento?.data ? ` · ${movimento.data.split('-').reverse().join('/')}` : ''}</p>
            {candidatos.length === 0 ? <p>Nenhuma correspondência sugerida.</p> : <ul className="mt-1 space-y-1">{candidatos.map(item => <li key={item.id}>
              {item.tipo === 'Buy' ? 'Compra' : item.tipo === 'Sell' ? 'Venda' : item.tipo} · {data(item.data)} · quantidade {item.quantidade.toLocaleString('pt-BR')} · preço {item.preco.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}{item.posteriorBase ? ' · posterior à data de referência' : ''}
            </li>)}</ul>}
            <RevisaoEventoTesouro key={`${consulta}-${evento.indice}`} carteiraId={carteiraId} loteId={loteId} ativoId={resultado.ativoId!} carteiraVersao={resultado.carteiraVersao}
              indiceEvento={evento.indice} tipoEvento={motivosSemVinculo.length === 0 ? tipoEvento : null} motivosSemVinculo={motivosSemVinculo}
              candidatos={evento.candidatos} transacoes={resultado.transacoes} />
          </li>
        })}</ul>
        <h4 className="font-medium">Movimentações existentes na carteira</h4>
        {resultado.transacoes.length === 0 ? <p>Nenhuma movimentação encontrada para esta posição.</p> : <ul className="space-y-1">{resultado.transacoes.map(item => <li key={item.id}>
          {item.tipo === 'Buy' ? 'Compra' : item.tipo === 'Sell' ? 'Venda' : item.tipo} · {data(item.data)} · quantidade {item.quantidade.toLocaleString('pt-BR')} · preço {item.preco.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}{item.posteriorBase ? ' · posterior à data de referência' : resultado.eventos.some(evento => evento.candidatos.includes(item.id)) ? ' · sugerida para comparação' : ' · sem correspondência sugerida'}
        </li>)}</ul>}
        <p>A confirmação da importação ainda não está disponível.</p>
      </>}
    </>}
  </section>
}
