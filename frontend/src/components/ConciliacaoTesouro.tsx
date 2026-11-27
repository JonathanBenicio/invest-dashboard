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
  const [revisaoAberta, setRevisaoAberta] = useState(false)
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
  const referenciaNaoPosterior = !!resultado?.dataBaseAtiva && resultado.dataReferencia <= resultado.dataBaseAtiva
  const posicaoPropostaDisponivel = resultado?.posicaoPropostaDisponivel === true
  const encerramentoProposto = resultado?.encerramentoPosicao?.podeEncerrar === true
  const criarPosicao = selecionado === NOVA_POSICAO && resultado?.criarPosicao === true && resultado.ativoId === null
  const criacaoSemRevisao = criarPosicao && resultado?.criacaoPosicaoSemRevisao === true
  const revisaoManualAnterior = resultado?.motivosCriacaoPosicao?.some(motivo =>
    motivo.includes('Uma revisão anterior vinculou um movimento a uma operação manual')) ?? false
  const posicaoEmRevisao = resultado?.ativos.find(ativo => ativo.ativoId === resultado.ativoId)
  const ativoSelecionado = criarPosicao ? resultado?.podeCriarPosicao === true : !!resultado?.ativoId && resultado.ativoId === selecionado
  const acaoSemPosicaoProposta = resultado?.dataBaseAtiva ? 'A base anterior será preservada.' : 'Nenhuma base será criada.'
  const podeConfirmar = apto && ativoSelecionado && revisaoAberta &&
    (criacaoSemRevisao || (revisoesValidas && vinculosUnicos && (todosLancamentosVinculados || aceiteSnapshotVigente))) &&
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
        ...(criacaoSemRevisao && {
          criacaoPosicaoSemRevisao: true,
          hashPrevia: lote.hashPrevia,
          ...(resultado.importacaoInicialSemRevisao && { importacaoInicialSemRevisao: true }),
        }),
        revisoes: criacaoSemRevisao ? [] : uniaoRevisoes.flatMap(revisao => revisao ? [{ indiceEvento: revisao.indiceEvento, revisao: revisao.revisao }] : []),
        hashDivergencias: resultado.comparacaoSnapshot?.hash,
        aceitarDivergencias,
      })
      onConfirmado(resposta.dados)
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setRevisoes({})
        setResultado(null)
        setSelecionado('')
        setRevisaoAberta(false)
        setAceitarDivergencias(false)
        setConsulta(atual => atual + 1)
        setErro(error.message && error.message !== 'Conflict'
          ? error.message
          : 'O lote, a carteira ou uma revisão mudou. Reabra o lote e compare novamente antes de confirmar.')
      } else {
        setErro('Não foi possível confirmar a importação. Confira os dados revisados e tente novamente.')
      }
    } finally {
      setConfirmando(false)
    }
  }

  async function analisar(ativoId: string | null, reavaliar = false) {
    setOcupado(true)
    setErro('')
    try {
      const resposta = await tesouroDiretoService.conciliacao(carteiraId, loteId, ativoId)
      const preservarRevisoes = reavaliar && resultado?.carteiraVersao === resposta.dados.carteiraVersao
      if (!preservarRevisoes) {
        setRevisoes({})
        setConsulta(atual => atual + 1)
      }
      setAceitarDivergencias(false)
      if (ativoId) setSelecionado(ativoId)
      else setSelecionado('')
      setResultado(resposta.dados)
      setRevisaoAberta(ativoId !== null)
    } catch {
      setResultado(null)
      setSelecionado('')
      setRevisaoAberta(false)
      setRevisoes({})
      setAceitarDivergencias(false)
      setErro('Não foi possível consultar a conciliação. Tente novamente.')
    } finally {
      setOcupado(false)
    }
  }

  function continuarParaRevisao() {
    if (!resultado || !selecionado || ocupado || confirmando) return
    if (selecionado === NOVA_POSICAO) {
      if (!resultado.podeCriarPosicao || !resultado.criarPosicao) return
      setRevisoes({})
      setAceitarDivergencias(false)
      setRevisaoAberta(true)
      return
    }
    void analisar(selecionado)
  }

  const mostrarRevisao = revisaoAberta && !!selecionado &&
    (resultado?.ativoId === selecionado || criarPosicao)

  return <section className="space-y-3 text-sm">
    <h3 className="font-semibold">{mostrarRevisao
      ? criacaoSemRevisao ? resultado?.importacaoInicialSemRevisao ? 'Etapa 3 · Confirme a posição inicial' : 'Etapa 3 · Confirme a nova posição'
        : criarPosicao ? 'Etapa 3 · Revise a nova posição'
          : 'Etapa 3 · Revise as movimentações'
      : 'Etapa 2 · Escolha a posição'}</h3>
    {!resultado && <p className="text-muted-foreground">Vamos procurar uma posição com o mesmo título e vencimento na carteira escolhida.</p>}
    {!resultado && <Button type="button" disabled={ocupado} onClick={() => void analisar(null)}>{ocupado ? 'Buscando posição…' : 'Continuar para escolha da posição'}</Button>}
    {ocupado && <p role="status">Consultando movimentações...</p>}
    {erro && <p role="alert" className="text-destructive">{erro}</p>}
    {resultado && <>
      {!mostrarRevisao && (resultado.ativos.length === 0 && !resultado.podeCriarPosicao ? <div role="status" className="space-y-2 rounded-md border p-3">
        <h4 className="font-medium">Não encontramos uma posição do Tesouro compatível com este título e vencimento, e esta prévia ainda não pode criar uma nova.</h4>
        {resultado.motivosCriacaoPosicao?.length ? <ul className="list-disc space-y-1 pl-5">{resultado.motivosCriacaoPosicao.map((motivo, indice) => <li key={`${indice}-${motivo}`}>{motivo}</li>)}</ul> : <p>Revise o título, vencimento, linhas de posição e movimentações na tabela da prévia.</p>}
        {revisaoManualAnterior
          ? <p><strong>Como seguir:</strong> confira a operação manual e siga a orientação acima. Se o vínculo estiver incorreto, use “Excluir prévia” no topo e envie o HTML novamente. Se estiver correto, concilie a operação antes de criar a posição.</p>
          : <p><strong>Como seguir:</strong> confira se a carteira escolhida é a correta. Se o título já existir, envie o HTML mais recente para essa carteira; se for uma posição nova, corrija os campos indicados e envie outra prévia.</p>}
      </div> : <div className="grid gap-2">
        <label htmlFor="tesouro-conciliacao-ativo" className="font-medium">Posição correspondente ao título e vencimento do extrato</label>
        <Select value={selecionado} onValueChange={valor => { setSelecionado(valor); setRevisaoAberta(false); setRevisoes({}); setAceitarDivergencias(false) }} disabled={ocupado || confirmando}>
          <SelectTrigger id="tesouro-conciliacao-ativo" className="h-auto min-h-10 gap-2 text-left [&>span]:line-clamp-none [&>span]:break-words [&>svg]:shrink-0">
            <SelectValue placeholder="Selecione uma opção" />
          </SelectTrigger>
          <SelectContent className="max-w-[var(--radix-select-content-available-width)]">
            {resultado.ativos.map(ativo => <SelectItem key={ativo.ativoId} className="break-words" value={ativo.ativoId}>
              {ativo.nome} · vence em {ativo.vencimento ? ativo.vencimento.split('-').reverse().join('/') : 'data não informada'} · {ativo.ticker}
            </SelectItem>)}
            {resultado.podeCriarPosicao && <SelectItem value={NOVA_POSICAO} className="break-words">
              Criar posição: {lote.previa.titulo} · {lote.previa.vencimento?.split('-').reverse().join('/')}
            </SelectItem>}
          </SelectContent>
        </Select>
        {resultado.ativos.length === 1 && <p className="text-xs text-muted-foreground">Encontramos uma posição compatível. Selecione-a para continuar.</p>}
        {!resultado.podeCriarPosicao && resultado.motivosCriacaoPosicao?.length ? <div role="status" className="space-y-1 rounded-md border p-3 text-sm">
          <p className="font-medium">A criação de uma nova posição está bloqueada por estes motivos:</p>
          <ul className="list-disc space-y-1 pl-5">{resultado.motivosCriacaoPosicao.map((motivo, indice) => <li key={`${indice}-${motivo}`}>{motivo}</li>)}</ul>
          <p>Se o título já estiver na carteira, selecione a posição existente. Para uma posição nova, corrija os campos marcados na tabela e envie outra prévia.</p>
        </div> : null}
        <Button type="button" disabled={!selecionado || ocupado || confirmando} onClick={continuarParaRevisao}>{ocupado ? 'Comparando…' : 'Continuar para revisão'}</Button>
      </div>)}
      {mostrarRevisao && <>
        <Button type="button" variant="outline" disabled={ocupado || confirmando} onClick={() => void analisar(null)}>Voltar à escolha da posição</Button>
        {!criarPosicao && posicaoEmRevisao && <p role="status" className="rounded-md border p-3">
          Posição escolhida para conciliação: <strong>{posicaoEmRevisao.nome}</strong>
          {posicaoEmRevisao.vencimento && <> · vence em {posicaoEmRevisao.vencimento.split('-').reverse().join('/')}</>}
          {' '}· {posicaoEmRevisao.ticker}
        </p>}
        {criacaoSemRevisao ? <div role="status" className="space-y-2 rounded-md border p-3">
          <p className="font-medium">Este título ainda não tem uma posição correspondente na carteira. A importação vai criar uma nova posição.</p>
          <p>Não é necessária uma revisão individual de cada movimento. Ao confirmar, os {resultado.eventos.length} movimentos do HTML serão registrados como eventos oficiais vinculados à posição, sem criar compras ou vendas duplicadas no saldo.</p>
        </div> : criarPosicao ? <div role="status" className="space-y-2 rounded-md border p-3">
          <p className="font-medium">Você escolheu criar uma nova posição para {lote.previa.titulo ?? 'este título'}.</p>
          <p>O título não será atualizado a partir de uma posição existente nesta carteira. Como a carteira já tem histórico, revise os movimentos do arquivo antes de confirmar. A posição nova será criada ao concluir a importação.</p>
        </div> : <>
          <p>Compare os dados antes de decidir. As sugestões não confirmam que os registros sejam a mesma operação.</p>
          {resultado.comparacaoSnapshot && <div className="space-y-2 rounded-md border p-3">
            <h4 className="font-medium">Comparação com o saldo anterior</h4>
            <p>Quantidade da base anterior: {resultado.comparacaoSnapshot.dataBaseAnterior ? resultado.comparacaoSnapshot.quantidadeAnterior.toLocaleString('pt-BR') : 'sem base de extrato'} · Proposta: {posicaoPropostaDisponivel && resultado.comparacaoSnapshot.quantidadeProposta != null ? resultado.comparacaoSnapshot.quantidadeProposta.toLocaleString('pt-BR') : lote.previa.posicoes.length === 0 ? 'posição não observada neste arquivo' : 'posição não interpretável'}</p>
            <ul className="list-disc space-y-1 pl-5">{resultado.comparacaoSnapshot.divergencias.map((item, indice) => <li key={`${item.tipo}-${item.chave}-${indice}`}>{item.descricao}{item.tipo === 'movimento_manual_na_janela' ? ' · confira esta operação nas movimentações abaixo' : item.anterior ? ` · ${item.anterior}` : ''}{item.atual && item.tipo !== 'movimento_manual_na_janela' ? ` · ${item.atual}` : ''}</li>)}</ul>
            {resultado.comparacaoSnapshot.requerAceite && posicaoPropostaDisponivel && <label className="flex items-start gap-2"><input type="checkbox" checked={aceitarDivergencias} onChange={event => setAceitarDivergencias(event.target.checked)} /><span>Revisei as divergências e aceito o saldo proposto como base. Movimentos manuais anteriores sem vínculo não serão reaplicados ao saldo, mas seus registros serão mantidos na auditoria.</span></label>}
          </div>}
          {encerramentoProposto && <p role="status" className="rounded-md border p-3">
            As vendas registradas desde a base ativa zeram o saldo na data do extrato. Ao confirmar, o histórico será preservado.
          </p>}
          {!posicaoPropostaDisponivel && <p role="status">{resultado.encerramentoPosicao?.motivo ??
            'O extrato não comprova uma posição válida ou saldo zero por venda integral. A confirmação está bloqueada.'} {acaoSemPosicaoProposta}</p>}
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
          {referenciaNaoPosterior && <p role="status">A data do extrato precisa ser posterior à base ativa de {resultado.dataBaseAtiva?.split('-').reverse().join('/')}.</p>}
          {!apto && posicaoPropostaDisponivel && !referenciaNaoPosterior && <p>O extrato ou a posição escolhida não atende aos critérios para confirmar. Confira o título, o vencimento, a data de referência e os campos não interpretados.</p>}
          {apto && !revisoesValidas && <p>Carregue ou salve uma revisão vigente para cada movimentação. Os vínculos precisam corresponder à data, ao tipo, à quantidade e ao preço.</p>}
          {apto && revisoesValidas && !todosLancamentosVinculados && !aceitarDivergencias && <p>Vincule os lançamentos manuais que o saldo do extrato vai substituir ou revise e aceite explicitamente as divergências do snapshot.</p>}
          {!vinculosUnicos && <p role="alert">O mesmo lançamento manual aparece em mais de uma revisão. Corrija os vínculos antes de confirmar.</p>}
          {!posicaoPropostaDisponivel && resultado.ativoId && resultado.dataBaseAtiva && !referenciaNaoPosterior && vinculos.length > 0 &&
            <Button type="button" variant="outline" disabled={ocupado || confirmando} onClick={() => void analisar(resultado.ativoId, true)}>
              Reavaliar saldo após os vínculos
            </Button>}
          {criarPosicao && <p role="status">A posição será criada com o título, vencimento, indexador e taxa reconhecidos no HTML, além do saldo na data do extrato.</p>}
        </>}
        <AlertDialog>
          <AlertDialogTrigger asChild><Button type="button" disabled={!podeConfirmar}>Confirmar importação</Button></AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Confirmar importação do Tesouro?</AlertDialogTitle>
              <AlertDialogDescription>{encerramentoProposto
                ? 'Os eventos oficiais serão registrados e o saldo do título na data do extrato ficará zerado. O histórico será preservado.'
                : criacaoSemRevisao
                  ? 'Os eventos oficiais serão registrados e uma nova posição será criada com o saldo do extrato. Os movimentos não serão somados novamente como operações de compra e venda.'
                  : criarPosicao
                  ? 'Os eventos oficiais serão registrados e uma posição será criada com o saldo do extrato na data de referência.'
                  : 'Os eventos oficiais serão registrados e a posição selecionada será atualizada com o saldo do extrato na data de referência.'}</AlertDialogDescription>
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
