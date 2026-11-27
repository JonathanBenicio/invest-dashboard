import { queryKeys } from '@/api/query-keys'
import { useMemo, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import type { ColumnDef } from '@tanstack/react-table'
import { flexRender, getCoreRowModel, useReactTable } from '@tanstack/react-table'
import { Button } from '@/components/ui/button'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Table, TableBody, TableCaption, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { ApiError } from '@/api/errors/api-error'
import { financialInstitutionService } from '@/api/services/financial-institution.service'
import { tesouroDiretoService, type LoteTesouro, type PreviaTesouro } from '@/api/services/tesouro-direto.service'
import { ReferenciaPublicaTesouro } from './ReferenciaPublicaTesouro'
import { ConciliacaoTesouro } from './ConciliacaoTesouro'

type Carteira = { id: string; nome: string; grupoId?: string; instituicaoFinanceira?: string; instituicaoFinanceiraId?: string }

function normalizarInstituicao(nome: string) {
  const normalizado = nome.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase()
    .replace(/[^a-z0-9]+/g, ' ').trim()
  return normalizado === 'xp investimentos cctvm s a' || normalizado === 'xp investimentos cctvm sa'
    ? 'xp investimentos'
    : normalizado
}

const nomesCampos: Record<string, string> = {
  data: 'data', natureza: 'natureza', tipo: 'tipo', quantidade: 'quantidade', preco: 'preço', taxaAnual: 'taxa do título (% a.a.)', valor: 'valor',
  data_aplicacao: 'data de aplicação', quantidade_titulos: 'quantidade de títulos', preco_aplicacao: 'preço de aplicação', valor_investido: 'valor investido',
  rentabilidade_contratada: 'rentabilidade contratada', rentabilidade_bruta_anualizada: 'rentabilidade anualizada', rentabilidade_bruta_acumulada: 'rentabilidade acumulada',
  valor_bruto: 'valor bruto', dias_corridos: 'dias corridos', aliquota_ir: 'alíquota de IR', imposto_previsto_ir: 'IR previsto', imposto_previsto_iof: 'IOF previsto',
  taxa_devida_b3: 'taxa B3 devida (R$)', taxa_devida_instituicao: 'taxa da instituição devida (R$)', valor_liquido: 'valor líquido',
  dataAplicacao: 'data de aplicação', quantidadeTitulos: 'quantidade de títulos', precoAplicacao: 'preço de aplicação', valorInvestido: 'valor investido',
  rentabilidadeContratada: 'rentabilidade contratada', rentabilidadeBrutaAnualizada: 'rentabilidade anualizada', rentabilidadeBrutaAcumulada: 'rentabilidade acumulada',
  valorBruto: 'valor bruto', diasCorridos: 'dias corridos', aliquotaIr: 'alíquota de IR', impostoPrevistoIr: 'IR previsto', impostoPrevistoIof: 'IOF previsto',
  taxaDevidaB3: 'taxa B3 devida (R$)', taxaDevidaInstituicao: 'taxa da instituição devida (R$)', valorLiquido: 'valor líquido',
}
const rotuloCampo = (campo: string) => {
  const normalizado = campo.trim().replace(/:$/, '')
  return normalizado.toLocaleLowerCase('pt-BR') === 'taxa (% a.a.)'
    ? 'Taxa do título (% a.a.)'
    : nomesCampos[normalizado] ?? normalizado.replace(/_/g, ' ')
}

type LinhaPreviaTabela = {
  id: string
  numero: string
  campos: Record<string, string>
  leitura: string
  requerRevisao: boolean
}

function TabelaPreviaTesouro({ caption, cabecalhoNumero, linhas }: {
  caption: string
  cabecalhoNumero: string
  linhas: LinhaPreviaTabela[]
}) {
  const campos = useMemo(() => [...new Set(linhas.flatMap(linha => Object.keys(linha.campos)))], [linhas])
  const colunas = useMemo<ColumnDef<LinhaPreviaTabela>[]>(() => [
    {
      accessorKey: 'numero',
      header: cabecalhoNumero,
      cell: ({ getValue }) => <span className="whitespace-nowrap">{getValue<string>()}</span>,
    },
    ...campos.map((campo): ColumnDef<LinhaPreviaTabela> => ({
      id: campo,
      accessorFn: (linha: LinhaPreviaTabela) => linha.campos[campo] ?? '',
      header: rotuloCampo(campo),
      cell: ({ getValue }) => <span className="whitespace-nowrap">{getValue<string>() || '—'}</span>,
    })),
    {
      accessorKey: 'leitura',
      header: 'Leitura',
      cell: ({ row }) => <span className={row.original.requerRevisao ? 'text-amber-700 dark:text-amber-300' : 'whitespace-nowrap'}>{row.original.leitura}</span>,
    },
  ], [cabecalhoNumero, campos])
  const tabela = useReactTable({ data: linhas, columns: colunas, getCoreRowModel: getCoreRowModel() })

  return <div className="min-w-0 max-w-full overflow-hidden rounded-md border">
    <Table className="min-w-max text-sm" aria-label={caption} scrollLabel={`Rolagem: ${caption}`}>
      <TableCaption className="sr-only">{caption}</TableCaption>
      <TableHeader>{tabela.getHeaderGroups().map(grupo => <TableRow key={grupo.id}>
        {grupo.headers.map(cabecalho => <TableHead key={cabecalho.id} scope="col" className="whitespace-nowrap bg-muted/50 px-3">
          {cabecalho.isPlaceholder ? null : flexRender(cabecalho.column.columnDef.header, cabecalho.getContext())}
        </TableHead>)}
      </TableRow>)}</TableHeader>
      <TableBody>{tabela.getRowModel().rows.map(linha => <TableRow key={linha.original.id}>
        {linha.getVisibleCells().map(celula => <TableCell key={celula.id} className="px-3 py-3">
          {flexRender(celula.column.columnDef.cell, celula.getContext())}
        </TableCell>)}
      </TableRow>)}</TableBody>
    </Table>
  </div>
}

export function PreviaTesouroDireto({ carteiras, carregando, erroCarteiras, tentarNovamente }: { carteiras: Carteira[]; carregando: boolean; erroCarteiras: boolean; tentarNovamente: () => void }) {
  const [carteira, setCarteira] = useState('')
  const [arquivo, setArquivo] = useState<File | null>(null)
  const [leitura, setLeitura] = useState<PreviaTesouro | null>(null)
  const [dataReferencia, setDataReferencia] = useState('')
  const [pagina, setPagina] = useState(1)
  const [lote, setLote] = useState<LoteTesouro | null>(null)
  const [erro, setErro] = useState('')
  const [processando, setProcessando] = useState(false)
  const queryClient = useQueryClient()
  const historico = useQuery({ queryKey: ['tesouro-lotes', carteira, pagina], queryFn: () => tesouroDiretoService.lotes(carteira, pagina), enabled: !!carteira, retry: false })
  const previa = lote?.previa
  const carteiraDestino = carteiras.find(item => item.id === (lote?.carteiraId ?? carteira))
  const previaExibida = previa ?? leitura
  const mes = previaExibida?.mesReferencia
  const ano = previaExibida?.anoReferencia
  const mesAno = ano && mes ? `${ano}-${String(mes).padStart(2, '0')}` : null
  const ultimoDia = ano && mes ? new Date(Date.UTC(ano, mes, 0)).getUTCDate() : 31
  const dataMinima = mesAno ? `${mesAno}-01` : undefined
  const dataMaxima = mesAno ? `${mesAno}-${String(ultimoDia).padStart(2, '0')}` : undefined
  const referenciaDoArquivo = mesAno && ano && mes
    ? new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(new Date(Date.UTC(ano, mes - 1, 1)))
    : null
  const referenciaForaDoMes = !!(mesAno && dataReferencia &&
    (dataReferencia.slice(0, 7) !== mesAno || Number(dataReferencia.slice(-2)) < 1 || Number(dataReferencia.slice(-2)) > ultimoDia))
  const referencia = mesAno && referenciaForaDoMes ? '' : dataReferencia
  const instituicaoExtrato = previaExibida?.instituicaoHtml?.trim()
  const instituicaoCarteira = carteiraDestino?.instituicaoFinanceira?.trim()
  const instituicoes = useQuery({
    queryKey: ['financial-institutions', carteiraDestino?.grupoId],
    queryFn: () => financialInstitutionService.list(carteiraDestino?.grupoId),
    enabled: !!instituicaoExtrato && !!carteiraDestino?.instituicaoFinanceiraId,
  })
  const nomeInstituicaoExtrato = instituicaoExtrato ? normalizarInstituicao(instituicaoExtrato) : ''
  const instituicaoIdentificada = instituicoes.data?.dados.find(item => normalizarInstituicao(item.nome) === nomeInstituicaoExtrato)
  const comparacaoInstituicao = nomeInstituicaoExtrato && instituicaoCarteira && carteiraDestino?.instituicaoFinanceiraId
    ? nomeInstituicaoExtrato === normalizarInstituicao(instituicaoCarteira) || instituicaoIdentificada?.id === carteiraDestino.instituicaoFinanceiraId
      ? 'corresponde'
      : instituicaoIdentificada ? 'diverge' : 'inconclusiva'
    : 'inconclusiva'
  const instituicaoDivergente = comparacaoInstituicao === 'diverge'
  const identificacaoInstituicoes = previaExibida && <div className="min-w-0 space-y-1 break-words text-sm">
    <p>Instituição do extrato: <strong>{instituicaoExtrato || 'Não identificada'}</strong></p>
    <p>Instituição da carteira: <strong>{instituicaoCarteira || 'Não cadastrada'}</strong></p>
    {comparacaoInstituicao === 'corresponde' && <p className="text-muted-foreground">A instituição corresponde à carteira.</p>}
    {comparacaoInstituicao === 'diverge' && <p role="status" className="text-amber-700 dark:text-amber-300">A instituição do extrato difere da carteira. Confira o destino selecionado.</p>}
    {instituicaoDivergente && <p role="alert" className="font-medium text-destructive">Importação bloqueada. Escolha uma carteira vinculada à instituição identificada no extrato.</p>}
    {instituicaoExtrato && instituicaoCarteira && comparacaoInstituicao === 'inconclusiva' && <p className="text-muted-foreground">Não foi possível confirmar a correspondência entre as instituições.</p>}
  </div>
  const linhasPosicao = useMemo<LinhaPreviaTabela[]>(() => previa?.posicoes.map(posicao => {
    const camposNaoInterpretados = posicao.dadosInterpretados?.camposNaoInterpretados ?? []
    return {
      id: `posicao-${posicao.numeroLinha}`,
      numero: String(posicao.numeroLinha),
      campos: posicao.camposBrutos,
      leitura: camposNaoInterpretados.length
        ? `Revisar: ${camposNaoInterpretados.map(rotuloCampo).join(', ')}`
        : posicao.dadosInterpretados ? 'Interpretada' : 'Não interpretada',
      requerRevisao: camposNaoInterpretados.length > 0 || !posicao.dadosInterpretados,
    }
  }) ?? [], [previa])
  const extratoConfirmado = lote?.estado === 'Confirmado'
  const linhasMovimentacao = useMemo<LinhaPreviaTabela[]>(() => previa?.movimentacoes.map((movimentacao, indice) => {
    const dados = movimentacao.dadosInterpretados
    const camposNaoInterpretados = dados?.camposNaoInterpretados ?? []
    const camposNaoAplicaveis = dados?.camposNaoAplicaveis ?? []
    const camposNaoInformados = dados?.camposNaoInformados ?? []
    let significadoValor: string | null = null
    if (dados?.valor != null && dados.tipo === 'Débito') {
      if (dados.natureza === 'Venda') significadoValor = 'Valor bruto do resgate'
      if (dados.natureza === 'Taxas') significadoValor = 'Cobrança de taxa'
    }
    const camposEssenciaisNaoInterpretados = camposNaoInterpretados
    const leitura = [
      significadoValor,
      camposEssenciaisNaoInterpretados.length ? `Revisar: ${camposEssenciaisNaoInterpretados.map(rotuloCampo).join(', ')}` : null,
      camposNaoAplicaveis.length ? `Não aplicável: ${camposNaoAplicaveis.map(rotuloCampo).join(', ')}` : null,
      camposNaoInformados.length ? `Não informado: ${camposNaoInformados.map(rotuloCampo).join(', ')}` : null,
    ].filter((estado): estado is string => estado !== null)
    return {
      id: `movimento-${indice}`,
      numero: String(indice + 1),
      campos: {
        ...Object.fromEntries(movimentacao.campos.map(campo => [campo.rotulo, campo.valor])),
        ...(extratoConfirmado && { Origem: 'Oficial · Tesouro Direto' }),
      },
      leitura: leitura.length ? leitura.join(' · ') : movimentacao.dadosInterpretados ? 'Interpretada' : 'Não interpretada',
      requerRevisao: camposEssenciaisNaoInterpretados.length > 0 || !movimentacao.dadosInterpretados,
    }
  }) ?? [], [previa, extratoConfirmado])

  function limparEnvio() {
    setLeitura(null)
    setDataReferencia('')
    setLote(null)
    setErro('')
  }

  async function analisar() {
    if (!carteira || !arquivo || processando) return
    setProcessando(true)
    setErro('')
    try {
      const resposta = await tesouroDiretoService.previa(carteira, arquivo)
      setLeitura(resposta.dados)
    } catch {
      setErro('Não foi possível analisar o arquivo. Verifique o formato e tente novamente.')
    } finally {
      setProcessando(false)
    }
  }

  async function salvarPrevia() {
    if (!carteira || !arquivo || !leitura || !referencia || instituicaoDivergente || processando) return
    setProcessando(true)
    setLote(null)
    setErro('')
    try {
      const resposta = await tesouroDiretoService.criarLote(carteira, referencia, arquivo)
      setLote(resposta.dados)
      setPagina(1)
      void queryClient.invalidateQueries({ queryKey: ['tesouro-lotes', carteira] })
    } catch (error) {
      setErro(error instanceof ApiError && error.status < 500 && error.message
        ? error.message
        : 'Não foi possível salvar a prévia. Confira o arquivo e a data de referência.')
    } finally {
      setProcessando(false)
    }
  }

  async function abrirLote(id: string) {
    setProcessando(true)
    setLote(null)
    setErro('')
    try {
      const resposta = await tesouroDiretoService.lote(carteira, id)
      setLote(resposta.dados)
    } catch {
      setErro('Não foi possível abrir esta prévia. Tente novamente.')
    } finally {
      setProcessando(false)
    }
  }

  async function excluirPrevia(id: string) {
    setProcessando(true)
    setErro('')
    try {
      await tesouroDiretoService.excluirPrevia(carteira, id)
      if (lote?.id === id) setLote(null)
      await queryClient.invalidateQueries({ queryKey: ['tesouro-lotes', carteira] })
    } catch {
      setErro('Não foi possível excluir a prévia. Atualize a página e tente novamente.')
    } finally {
      setProcessando(false)
    }
  }

  function botaoExcluir(id: string) {
    return <AlertDialog>
      <AlertDialogTrigger asChild><Button type="button" variant="outline" disabled={processando}>Excluir prévia</Button></AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader><AlertDialogTitle>Excluir esta prévia?</AlertDialogTitle><AlertDialogDescription>Os dados e as revisões desta prévia pendente serão excluídos.</AlertDialogDescription></AlertDialogHeader>
        <AlertDialogFooter><AlertDialogCancel>Cancelar</AlertDialogCancel><AlertDialogAction onClick={() => void excluirPrevia(id)}>Excluir prévia</AlertDialogAction></AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  }

  return <div className="min-w-0 space-y-4">
    {!lote ? <Card>
      <CardHeader>
        <CardTitle>Etapa 1 · Arquivo e destino</CardTitle>
        <CardDescription>Escolha a carteira e o HTML do Tesouro Direto para identificar a referência do extrato.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid gap-2">
          <label htmlFor="tesouro-carteira" className="text-sm font-medium">Carteira de destino</label>
          <Select value={carteira} onValueChange={value => { setCarteira(value); setPagina(1); limparEnvio() }} disabled={carregando || processando || carteiras.length === 0}>
            <SelectTrigger id="tesouro-carteira"><SelectValue placeholder={carregando ? 'Carregando carteiras...' : 'Selecione a carteira'} /></SelectTrigger>
            <SelectContent>{carteiras.map(item => <SelectItem key={item.id} value={item.id}>{item.nome}</SelectItem>)}</SelectContent>
          </Select>
        </div>
        {erroCarteiras && <p role="alert" className="text-sm text-destructive">Não foi possível carregar as carteiras. <Button type="button" variant="link" onClick={tentarNovamente}>Tentar novamente</Button></p>}
        {!carregando && !erroCarteiras && carteiras.length === 0 && <p role="status" className="text-sm">Crie uma carteira antes de analisar o extrato.</p>}
        <div className="grid gap-2">
          <label htmlFor="tesouro-arquivo" className="text-sm font-medium">Arquivo do extrato</label>
          <input id="tesouro-arquivo" type="file" accept=".html,.htm" disabled={processando} className="max-w-full text-sm"
            onChange={event => { setArquivo(event.target.files?.[0] ?? null); limparEnvio() }} />
        </div>
        {!leitura ? <Button type="button" onClick={() => void analisar()} disabled={!carteira || !arquivo || processando}>
          Analisar extrato
        </Button> : <div className="space-y-4">
          {identificacaoInstituicoes}
          <div className="grid gap-2">
            <label htmlFor="tesouro-data-referencia" className="text-sm font-medium">{mesAno ? 'Data de referência do saldo' : 'Data de referência do extrato'}</label>
            {mesAno ? <>
              <p id="tesouro-ajuda-referencia" className="text-sm text-muted-foreground">Posição em {referenciaDoArquivo}, conforme o HTML. Selecione o dia no calendário.</p>
              <Input id="tesouro-data-referencia" type="date" min={dataMinima} max={dataMaxima} aria-invalid={referenciaForaDoMes} aria-describedby={referenciaForaDoMes ? 'tesouro-ajuda-referencia tesouro-data-invalida' : 'tesouro-ajuda-referencia'} value={dataReferencia} disabled={processando} onChange={event => setDataReferencia(event.target.value)} />
              {referenciaForaDoMes && <p id="tesouro-data-invalida" className="text-sm text-destructive">Escolha uma data de {referenciaDoArquivo}.</p>}
            </> : <>
              <p id="tesouro-ajuda-referencia" className="text-sm text-muted-foreground">O mês e o ano não foram identificados no HTML. Informe a data de referência do saldo.</p>
              <Input id="tesouro-data-referencia" type="date" aria-describedby="tesouro-ajuda-referencia" value={dataReferencia} disabled={processando} onChange={event => setDataReferencia(event.target.value)} />
            </>}
          </div>
          <p className="text-sm text-muted-foreground">Só os dados extraídos serão salvos para revisão.</p>
          <Button type="button" onClick={() => void salvarPrevia()} disabled={!referencia || instituicaoDivergente || processando}>Salvar e continuar</Button>
          {instituicaoDivergente && <Button type="button" variant="outline" onClick={() => document.getElementById('tesouro-carteira')?.focus()}>Escolher outra carteira</Button>}
        </div>}
        {processando && <p role="status" className="text-sm">{leitura ? 'Salvando prévia…' : 'Aguarde a leitura do extrato…'}</p>}
        {erro && <p role="alert" className="text-sm text-destructive">{erro}</p>}
      </CardContent>
    </Card> : <Card>
      <CardHeader>
        <CardTitle>Etapa 1 concluída · Extrato analisado</CardTitle>
        <CardDescription>Confira o título e o destino antes de escolher a posição.</CardDescription>
      </CardHeader>
      <CardContent className="flex flex-wrap items-center justify-between gap-3 text-sm">
        <div className="min-w-0">
          <p className="font-medium">{previa?.titulo ?? 'Título não identificado'}{previa?.vencimento ? ` · vence em ${previa.vencimento.split('-').reverse().join('/')}` : ''}</p>
          <p className="text-muted-foreground">Carteira: {carteiraDestino?.nome ?? 'carteira selecionada'} · Referência {lote.dataReferencia.split('-').reverse().join('/')}</p>
          <p className="text-muted-foreground">{mesAno ? 'Mês e ano lidos do HTML. Dia informado por você.' : 'Referência informada por você.'}</p>
        </div>
        <Button type="button" variant="outline" onClick={() => { setArquivo(null); limparEnvio() }}>Trocar arquivo ou carteira</Button>
        {lote.estado === 'Pendente' && botaoExcluir(lote.id)}
        {erro && <p role="alert" className="w-full text-sm text-destructive">{erro}</p>}
        <div className="w-full">{identificacaoInstituicoes}</div>
      </CardContent>
    </Card>}
    {carteira && !lote && <Card>
      <CardHeader><CardTitle>Prévias salvas</CardTitle><CardDescription>Retome a revisão de uma importação desta carteira.</CardDescription></CardHeader>
      <CardContent className="space-y-2">
        {historico.isLoading && <p role="status">Carregando prévias...</p>}
        {historico.isError && <p role="alert">Não foi possível carregar as prévias. <Button type="button" variant="link" onClick={() => void historico.refetch()}>Tentar novamente</Button></p>}
        {historico.data && historico.data.dados.length === 0 && <p>{pagina === 1 ? 'Nenhuma prévia salva para esta carteira.' : 'Nenhuma prévia nesta página.'}</p>}
        {historico.data && historico.data.dados.length > 0 && <ul className="space-y-2">{historico.data.dados.map(item => <li key={item.id} className="flex flex-wrap items-center gap-2 rounded-md border p-2 text-sm">
          <span>Referência {item.dataReferencia.split('-').reverse().join('/')} · {item.formato.toUpperCase()} · {new Date(item.criadoEmUtc).toLocaleString('pt-BR')}{item.estado === 'Confirmado' ? ' · Confirmado' : ''}</span>
          <Button type="button" variant="outline" disabled={processando} onClick={() => void abrirLote(item.id)}>{item.estado === 'Confirmado' ? 'Ver extrato confirmado' : 'Revisar prévia'}</Button>
          {item.estado === 'Pendente' && botaoExcluir(item.id)}
        </li>)}</ul>}
        {historico.data && <div className="flex flex-wrap items-center gap-2" aria-label="Páginas das prévias">
          <Button type="button" variant="outline" disabled={pagina === 1 || historico.isFetching} onClick={() => setPagina(atual => atual - 1)}>Página anterior</Button>
          <span>Página {pagina}</span>
          <Button type="button" variant="outline" disabled={historico.data.dados.length < 50 || pagina >= 10000 || historico.isFetching} onClick={() => setPagina(atual => atual + 1)}>Próxima página</Button>
        </div>}
      </CardContent>
    </Card>}
    {previa && <Card>
      <CardHeader>
        <CardTitle>{lote.estado === 'Confirmado' ? 'Importação confirmada' : 'Etapas 2 e 3 · Escolha e revisão'}</CardTitle>
        <CardDescription>{lote.estado === 'Confirmado' ? 'As movimentações oficiais foram registradas.' : 'Escolha uma posição compatível e confira o saldo antes de confirmar.'}</CardDescription>
      </CardHeader>
      <CardContent className="min-w-0 space-y-5">
        {lote.estado === 'Confirmado'
          ? <p role="status" className="font-medium">Importação confirmada{lote.confirmadoEmUtc ? ` em ${new Date(lote.confirmadoEmUtc).toLocaleString('pt-BR')}` : ''}.</p>
          : <ConciliacaoTesouro key={lote.id} lote={lote} onConfirmado={confirmado => {
            setLote(atual => atual?.id === confirmado.id ? confirmado : atual)
            void queryClient.invalidateQueries({ queryKey: ['tesouro-lotes', confirmado.carteiraId] })
            void queryClient.invalidateQueries({ queryKey: queryKeys.portfolios.all })
            void queryClient.invalidateQueries({ queryKey: queryKeys.investments.all })
            void queryClient.invalidateQueries({ queryKey: ['extrato-tesouro', confirmado.carteiraId] })
          }} />}
        <details className="space-y-3 rounded-md border p-3">
          <summary className="cursor-pointer font-medium">Ver dados completos e avisos do arquivo</summary>
          <div className="space-y-4 pt-3">
            <p className="text-sm">Referência do saldo informada: {lote.dataReferencia.split('-').reverse().join('/')}{previa.vencimentoOriginal ? ` · Vencimento do arquivo: ${previa.vencimentoOriginal}` : ''}</p>
            <p className="text-sm text-muted-foreground">“Taxa (% a.a.)” é a rentabilidade do título ou da operação. Custos aparecem em “Taxa devida” (B3 e instituição), em reais. IR/IOF previstos e valor líquido são observações do extrato na data de referência. <a className="underline underline-offset-2" href="https://www.tesourodireto.com.br/b/impostos-e-taxas-no-tesouro-direto" target="_blank" rel="noreferrer">Entenda impostos e taxas</a>.</p>
            {previa.avisos.map((aviso, index) => <p key={index} className="text-sm text-muted-foreground">{aviso}</p>)}
            {previa.linhasNaoInterpretadas.length > 0 && <div role="status" className="text-sm text-amber-700 dark:text-amber-300"><p>{previa.linhasNaoInterpretadas.length} linhas não foram interpretadas e precisam de revisão no arquivo original.</p><p>Linhas: {previa.linhasNaoInterpretadas.join(', ')}.</p></div>}
            <section className="min-w-0 space-y-3"><h3 className="font-semibold">Posições identificadas ({previa.posicoes.length})</h3>
              {previa.posicoes.length > 0 && <p className="text-xs text-muted-foreground">Deslize horizontalmente para ver todas as colunas.</p>}
              {previa.posicoes.length === 0 ? <p className="text-sm text-muted-foreground">Nenhuma posição identificada neste arquivo.</p> : <TabelaPreviaTesouro caption="Posições identificadas no extrato" cabecalhoNumero="Linha" linhas={linhasPosicao} />}
            </section>
            <section className="min-w-0 space-y-3"><h3 className="font-semibold">Movimentações identificadas ({previa.movimentacoes.length})</h3>
              {previa.movimentacoes.length > 0 && <p className="text-xs text-muted-foreground">Deslize horizontalmente para ver todas as colunas.</p>}
              {previa.movimentacoes.length === 0 ? <p className="text-sm text-muted-foreground">Nenhuma movimentação identificada neste arquivo.</p> : <TabelaPreviaTesouro caption="Movimentações identificadas no extrato" cabecalhoNumero="Nº" linhas={linhasMovimentacao} />}
            </section>
          </div>
        </details>
        {previa.titulo && previa.vencimento && <details className="rounded-md border p-3"><summary className="cursor-pointer font-medium">Consultar cotação pública de referência</summary><div className="pt-3"><ReferenciaPublicaTesouro key={`${previa.titulo}|${previa.vencimento}`} titulo={previa.titulo} vencimento={previa.vencimento} /></div></details>}
      </CardContent>
    </Card>}
  </div>
}
