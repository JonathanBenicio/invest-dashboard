import { useState } from 'react'
import { ApiError } from '@/api/errors/api-error'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { tesouroDiretoService, type ConciliacaoTesouro as Resultado, type LoteTesouro, type RevisaoTesouro } from '@/api/services/tesouro-direto.service'
import { RevisaoEventoTesouro } from './RevisaoEventoTesouro'

const data = (valor: string) => new Date(valor).toLocaleDateString('pt-BR', { timeZone: 'America/Sao_Paulo' })
const NOVA_POSICAO = '__nova_posicao_html__'

export function ConciliacaoTesouro({ lote, onConfirmado }: { lote: LoteTesouro; onConfirmado: (lote: LoteTesouro) => void }) {
  const { carteiraId, id: loteId } = lote
  const [resultado, setResultado] = useState<Resultado | null>(null)
  const [consulta, setConsulta] = useState(0)
  const [selecionado, setSelecionado] = useState('')
  const [ocupado, setOcupado] = useState(false)
  const [erro, setErro] = useState('')
  const [revisoes, setRevisoes] = useState<Record<number, RevisaoTesouro | null>>({})
  const [confirmando, setConfirmando] = useState(false)
  const [aceitarDivergencias, setAceitarDivergencias] = useState(false)
  const uniaoRevisoes = resultado?.eventos.map(evento => revisoes[evento.indice]) ?? []
  const revisoesValidas = uniaoRevisoes.every((revisao, indice) => revisao != null &&
    revisao.ativoId === resultado?.ativoId && revisao.carteiraVersao === resultado?.carteiraVersao &&
    (revisao.decisao === 'sem_vinculo' ||
      (revisao.transacaoId != null && resultado?.eventos[indice].candidatos.includes(revisao.transacaoId))))
  const vinculos = uniaoRevisoes.flatMap(revisao => revisao?.transacaoId ? [revisao.transacaoId] : [])
  const vinculosUnicos = new Set(vinculos).size === vinculos.length
  const todosLancamentosVinculados = resultado?.transacoes.every(transacao =>
    transacao.posteriorBase || vinculos.includes(transacao.id) ||
    (!!resultado.dataBaseAtiva && data(transacao.data).split('/').reverse().join('-') <= resultado.dataBaseAtiva)) ?? false
  const aceiteSnapshotVigente = resultado?.comparacaoSnapshot?.requerAceite === true && aceitarDivergencias
  const apto = resultado?.podeConfirmar === true
  const posicaoPropostaDisponivel = resultado?.posicaoPropostaDisponivel === true
  const criarPosicao = selecionado === NOVA_POSICAO && resultado?.criarPosicao === true && resultado.ativoId === null
  const importacaoDireta = criarPosicao && resultado?.importacaoInicialSemRevisao === true
  const ativoSelecionado = criarPosicao ? resultado?.podeCriarPosicao === true : !!resultado?.ativoId && resultado.ativoId === selecionado
  const acaoSemPosicaoProposta = resultado?.dataBaseAtiva ? 'A base anterior será preservada.' : 'Nenhuma base será criada.'
  const podeConfirmar = apto && ativoSelecionado &&
    (importacaoDireta || (revisoesValidas && vinculosUnicos && (todosLancamentosVinculados || aceiteSnapshotVigente))) &&
    (!resultado?.comparacaoSnapshot?.requerAceite || aceitarDivergencias) && !ocupado && !confirmando

  async function confirmar() {
    if (!podeConfirmar || !resultado) return
    setConfirmando(true)
    setErro('')
    try {
      const resposta = await tesouroDiretoService.confirmar(carteiraId, loteId, {
        ativoId: criarPosicao ? null : resultado.ativoId,
        criarPosicao,
        carteiraVersao: resultado.carteiraVersao,
        ...(importacaoDireta && { importacaoInicialSemRevisao: true, hashPrevia: lote.hashPrevia }),
        revisoes: importacaoDireta ? [] : uniaoRevisoes.flatMap(revisao => revisao ? [{ indiceEvento: revisao.indiceEvento, revisao: revisao.revisao }] : []),
        hashDivergencias: resultado.comparacaoSnapshot?.hash,
        aceitarDivergencias,
      })
      onConfirmado(resposta.dados)
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setRevisoes({})
        setResultado(null)
        setAceitarDivergencias(false)
        setConsulta(atual => atual + 1)
        setErro('O lote, a carteira ou uma revisão mudou. Reabra o lote e compare novamente antes de confirmar.')
      } else {
        setErro('Não foi possível confirmar a importação. Confira os dados revisados e tente novamente.')
      }
    } finally {
      setConfirmando(false)
    }
  }

  async function analisar(ativoId: string | null) {
    setOcupado(true)
    setErro('')
    try {
      const resposta = await tesouroDiretoService.conciliacao(carteiraId, loteId, ativoId)
      setRevisoes({})
      setAceitarDivergencias(false)
      if (resposta.dados.importacaoInicialSemRevisao === true) setSelecionado(NOVA_POSICAO)
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
    <h3 className="font-semibold">{importacaoDireta ? 'Primeira posição da carteira' : 'Revisão da conciliação'}</h3>
    {!resultado && <p>Compare os eventos do HTML com lançamentos existentes ou prepare uma nova posição.</p>}
    {!resultado && <Button type="button" variant="outline" disabled={ocupado} onClick={() => void analisar(null)}>Revisar conciliação</Button>}
    {ocupado && <p role="status">Consultando movimentações...</p>}
    {erro && <p role="alert" className="text-destructive">{erro}</p>}
    {resultado && <>
      {!importacaoDireta && (resultado.ativos.length === 0 && !resultado.podeCriarPosicao ? <div role="status" className="space-y-2 rounded-md border p-3">
        <h4 className="font-medium">Não há uma posição de renda fixa nesta carteira, e esta prévia ainda não pode criar uma nova.</h4>
        {resultado.motivosCriacaoPosicao?.length ? <ul className="list-disc space-y-1 pl-5">{resultado.motivosCriacaoPosicao.map((motivo, indice) => <li key={`${indice}-${motivo}`}>{motivo}</li>)}</ul> : <p>Revise o título, vencimento, linhas de posição e movimentações na tabela da prévia.</p>}
        <p><strong>Como seguir:</strong> confira a coluna Leitura e corrija os campos indicados no portal. Depois, envie uma nova prévia. Se o título já existir, selecione a carteira que contém essa posição.</p>
      </div> : <div className="grid gap-2">
        <label htmlFor="tesouro-conciliacao-ativo" className="font-medium">Posição da carteira para comparar</label>
        <Select value={selecionado} onValueChange={setSelecionado} disabled={ocupado || confirmando}>
          <SelectTrigger id="tesouro-conciliacao-ativo"><SelectValue placeholder="Selecione uma posição" /></SelectTrigger>
          <SelectContent>{resultado.ativos.map(ativo => <SelectItem key={ativo.ativoId} value={ativo.ativoId}>{ativo.ticker}</SelectItem>)}{resultado.podeCriarPosicao && <SelectItem value={NOVA_POSICAO}>Criar posição a partir do HTML</SelectItem>}</SelectContent>
        </Select>
        {!resultado.podeCriarPosicao && resultado.motivosCriacaoPosicao?.length ? <div role="status" className="space-y-1 rounded-md border p-3 text-sm">
          <p className="font-medium">A criação de uma nova posição está bloqueada por estes motivos:</p>
          <ul className="list-disc space-y-1 pl-5">{resultado.motivosCriacaoPosicao.map((motivo, indice) => <li key={`${indice}-${motivo}`}>{motivo}</li>)}</ul>
          <p>Se o título já estiver na carteira, selecione a posição existente. Para uma posição nova, corrija os campos marcados na tabela e envie outra prévia.</p>
        </div> : null}
        <Button type="button" variant="outline" disabled={!selecionado || ocupado || confirmando} onClick={() => void analisar(selecionado === NOVA_POSICAO ? null : selecionado)}>Comparar movimentações</Button>
      </div>)}
      {selecionado && (resultado.ativoId === selecionado || criarPosicao) && <>
        {importacaoDireta ? <p role="status">
          Como a carteira está vazia, não é necessária uma revisão individual de cada movimento. Ao confirmar, a posição será criada com o saldo do extrato.
          Movimentações do HTML para registrar como oficiais: {resultado.eventos.length}. Elas ficarão vinculadas ao lote e à nova posição; o ledger não receberá compras/vendas duplicadas.
        </p> : <>
          <p>Compare os dados antes de decidir. As sugestões não confirmam que os registros sejam a mesma operação.</p>
          {resultado.comparacaoSnapshot && <div className="space-y-2 rounded-md border p-3">
            <h4 className="font-medium">Comparação com o saldo anterior</h4>
            <p>Quantidade da base anterior: {resultado.comparacaoSnapshot.dataBaseAnterior ? resultado.comparacaoSnapshot.quantidadeAnterior.toLocaleString('pt-BR') : 'sem base de extrato'} · Proposta: {posicaoPropostaDisponivel && resultado.comparacaoSnapshot.quantidadeProposta != null ? resultado.comparacaoSnapshot.quantidadeProposta.toLocaleString('pt-BR') : lote.previa.posicoes.length === 0 ? 'posição não observada neste arquivo' : 'posição não interpretável'}</p>
            <ul className="list-disc space-y-1 pl-5">{resultado.comparacaoSnapshot.divergencias.map((item, indice) => <li key={`${item.tipo}-${item.chave}-${indice}`}>{item.descricao}{item.tipo === 'movimento_manual_na_janela' ? ' · confira esta operação nas movimentações abaixo' : item.anterior ? ` · ${item.anterior}` : ''}{item.atual && item.tipo !== 'movimento_manual_na_janela' ? ` · ${item.atual}` : ''}</li>)}</ul>
            {resultado.comparacaoSnapshot.requerAceite && posicaoPropostaDisponivel && <label className="flex items-start gap-2"><input type="checkbox" checked={aceitarDivergencias} onChange={event => setAceitarDivergencias(event.target.checked)} /><span>Revisei as divergências e aceito o saldo proposto como base. Movimentos manuais anteriores sem vínculo não serão reaplicados ao saldo, mas seus registros serão mantidos na auditoria.</span></label>}
          </div>}
          {!posicaoPropostaDisponivel && <p role="status">O HTML não contém uma posição interpretável para este título. Isso não equivale a saldo zero nem comprova encerramento; a confirmação ficará bloqueada. {acaoSemPosicaoProposta}</p>}
          {resultado.eventos.length === 0 ? posicaoPropostaDisponivel ? <p role="status">Este snapshot não contém movimentações. A confirmação atualizará apenas o saldo da posição na data de referência.</p> : <p role="status">Este snapshot não contém movimentações e não apresenta posição interpretável.</p> : <ul className="space-y-3">{resultado.eventos.map(evento => {
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
              <RevisaoEventoTesouro key={`${consulta}-${evento.indice}`} carteiraId={carteiraId} loteId={loteId} hashPrevia={lote.hashPrevia} ativoId={criarPosicao ? null : resultado.ativoId} carteiraVersao={resultado.carteiraVersao}
                indiceEvento={evento.indice} tipoEvento={motivosSemVinculo.length === 0 ? tipoEvento : null} motivosSemVinculo={motivosSemVinculo}
                candidatos={evento.candidatos} transacoes={resultado.transacoes} bloqueado={ocupado || confirmando}
                onRevisao={revisao => setRevisoes(atuais => ({ ...atuais, [evento.indice]: revisao }))} />
            </li>
          })}</ul>}
          <h4 className="font-medium">Movimentações existentes na carteira</h4>
          {resultado.transacoes.length === 0 ? <p>Nenhuma movimentação encontrada para esta posição.</p> : <ul className="space-y-1">{resultado.transacoes.map(item => <li key={item.id}>
            {item.tipo === 'Buy' ? 'Compra' : item.tipo === 'Sell' ? 'Venda' : item.tipo} · {data(item.data)} · quantidade {item.quantidade.toLocaleString('pt-BR')} · preço {item.preco.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}{item.posteriorBase ? ' · posterior à data de referência' : resultado.eventos.some(evento => evento.candidatos.includes(item.id)) ? ' · sugerida para comparação' : ' · sem correspondência sugerida'}
          </li>)}</ul>}
          {!apto && posicaoPropostaDisponivel && <p>O extrato ou a posição escolhida não atende aos critérios para confirmar. Confira o título, o vencimento, a data de referência e os campos não interpretados.</p>}
          {apto && !revisoesValidas && <p>Carregue ou salve uma revisão vigente para cada movimentação. Os vínculos precisam corresponder à data, ao tipo, à quantidade e ao preço.</p>}
          {apto && revisoesValidas && !todosLancamentosVinculados && !aceitarDivergencias && <p>Vincule os lançamentos manuais que o saldo do extrato vai substituir ou revise e aceite explicitamente as divergências do snapshot.</p>}
          {!vinculosUnicos && <p role="alert">O mesmo lançamento manual aparece em mais de uma revisão. Corrija os vínculos antes de confirmar.</p>}
          {criarPosicao && <p role="status">A posição será criada com o título, vencimento, indexador e taxa reconhecidos no HTML, além do saldo na data do extrato.</p>}
        </>}
        <AlertDialog>
          <AlertDialogTrigger asChild><Button type="button" disabled={!podeConfirmar}>Confirmar importação</Button></AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Confirmar importação do Tesouro?</AlertDialogTitle>
              <AlertDialogDescription>{criarPosicao ? 'Os eventos oficiais serão registrados e uma posição será criada com o saldo do extrato na data de referência.' : 'Os eventos oficiais serão registrados e a posição selecionada será atualizada com o saldo do extrato na data de referência.'}</AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Voltar à revisão</AlertDialogCancel>
              <AlertDialogAction disabled={!podeConfirmar} onClick={() => void confirmar()}>Confirmar lote</AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
        {confirmando && <p role="status">Confirmando importação...</p>}
      </>}
    </>}
  </section>
}
