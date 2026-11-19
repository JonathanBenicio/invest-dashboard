import { queryKeys } from '@/api/query-keys'
import { useMemo, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import type { ColumnDef } from '@tanstack/react-table'
import { flexRender, getCoreRowModel, useReactTable } from '@tanstack/react-table'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Table, TableBody, TableCaption, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { tesouroDiretoService, type LoteTesouro } from '@/api/services/tesouro-direto.service'
import { ReferenciaPublicaTesouro } from './ReferenciaPublicaTesouro'
import { ConciliacaoTesouro } from './ConciliacaoTesouro'

type Carteira = { id: string; nome: string }

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
    ...campos.map(campo => ({
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

  return <div className="rounded-md border">
    <Table className="min-w-max text-sm" aria-label={caption}>
      <TableCaption className="sr-only">{caption}</TableCaption>
      <TableHeader>{tabela.getHeaderGroups().map(grupo => <TableRow key={grupo.id}>
        {grupo.headers.map(cabecalho => <TableHead key={cabecalho.id} scope="col">
          {cabecalho.isPlaceholder ? null : flexRender(cabecalho.column.columnDef.header, cabecalho.getContext())}
        </TableHead>)}
      </TableRow>)}</TableHeader>
      <TableBody>{tabela.getRowModel().rows.map(linha => <TableRow key={linha.original.id}>
        {linha.getVisibleCells().map(celula => <TableCell key={celula.id}>
          {flexRender(celula.column.columnDef.cell, celula.getContext())}
        </TableCell>)}
      </TableRow>)}</TableBody>
    </Table>
  </div>
}

export function PreviaTesouroDireto({ carteiras, carregando, erroCarteiras, tentarNovamente }: { carteiras: Carteira[]; carregando: boolean; erroCarteiras: boolean; tentarNovamente: () => void }) {
  const [carteira, setCarteira] = useState('')
  const [arquivo, setArquivo] = useState<File | null>(null)
  const [dataReferencia, setDataReferencia] = useState('')
  const [pagina, setPagina] = useState(1)
  const [lote, setLote] = useState<LoteTesouro | null>(null)
  const [erro, setErro] = useState('')
  const [processando, setProcessando] = useState(false)
  const queryClient = useQueryClient()
  const historico = useQuery({ queryKey: ['tesouro-lotes', carteira, pagina], queryFn: () => tesouroDiretoService.lotes(carteira, pagina), enabled: !!carteira, retry: false })
  const previa = lote?.previa
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
  const linhasMovimentacao = useMemo<LinhaPreviaTabela[]>(() => previa?.movimentacoes.map((movimentacao, indice) => {
    const camposNaoInterpretados = movimentacao.dadosInterpretados?.camposNaoInterpretados ?? []
    const camposNaoAplicaveis = movimentacao.dadosInterpretados?.camposNaoAplicaveis ?? []
    const camposEssenciaisNaoInterpretados = camposNaoInterpretados
    const leitura = [
      camposEssenciaisNaoInterpretados.length ? `Revisar: ${camposEssenciaisNaoInterpretados.map(rotuloCampo).join(', ')}` : null,
      camposNaoAplicaveis.length ? `Não aplicável: ${camposNaoAplicaveis.map(rotuloCampo).join(', ')}` : null,
    ].filter((estado): estado is string => estado !== null)
    return {
      id: `movimento-${indice}`,
      numero: String(indice + 1),
      campos: Object.fromEntries(movimentacao.campos.map(campo => [campo.rotulo, campo.valor])),
      leitura: leitura.length ? leitura.join(' · ') : movimentacao.dadosInterpretados ? 'Interpretada' : 'Não interpretada',
      requerRevisao: camposEssenciaisNaoInterpretados.length > 0 || !movimentacao.dadosInterpretados,
    }
  }) ?? [], [previa])

  async function analisar() {
    if (!carteira || !arquivo || !dataReferencia) return
    setProcessando(true)
    setLote(null)
    setErro('')
    try {
      const resposta = await tesouroDiretoService.criarLote(carteira, dataReferencia, arquivo)
      setLote(resposta.dados)
      setPagina(1)
      void queryClient.invalidateQueries({ queryKey: ['tesouro-lotes', carteira] })
    } catch {
      setErro('Não foi possível analisar o arquivo. Verifique o formato e tente novamente.')
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

  return <div className="space-y-4">
    <Card>
      <CardHeader>
        <CardTitle>Extrato do Tesouro Direto</CardTitle>
        <CardDescription>Selecione uma carteira e um arquivo HTML salvo do portal Tesouro Direto para revisar posições e movimentações.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid gap-2">
          <label htmlFor="tesouro-carteira" className="text-sm font-medium">Carteira de destino</label>
          <Select value={carteira} onValueChange={value => { setCarteira(value); setPagina(1); setLote(null); setErro('') }} disabled={carregando || processando || carteiras.length === 0}>
            <SelectTrigger id="tesouro-carteira"><SelectValue placeholder={carregando ? 'Carregando carteiras...' : 'Selecione a carteira'} /></SelectTrigger>
            <SelectContent>{carteiras.map(item => <SelectItem key={item.id} value={item.id}>{item.nome}</SelectItem>)}</SelectContent>
          </Select>
        </div>
        {erroCarteiras && <p role="alert" className="text-sm text-destructive">Não foi possível carregar as carteiras. <Button type="button" variant="link" onClick={tentarNovamente}>Tentar novamente</Button></p>}
        {!carregando && !erroCarteiras && carteiras.length === 0 && <p role="status" className="text-sm">Crie uma carteira antes de analisar o extrato.</p>}
        <div className="grid gap-2">
          <label htmlFor="tesouro-arquivo" className="text-sm font-medium">Arquivo do extrato</label>
          <input id="tesouro-arquivo" type="file" accept=".html,.htm" disabled={processando} className="max-w-full text-sm"
            onChange={event => { setArquivo(event.target.files?.[0] ?? null); setLote(null); setErro('') }} />
        </div>
        <div className="grid gap-2">
          <label htmlFor="tesouro-data-referencia" className="text-sm font-medium">Data de referência do extrato</label>
          <Input id="tesouro-data-referencia" type="date" value={dataReferencia} disabled={processando} onChange={event => { setDataReferencia(event.target.value); setLote(null); setErro('') }} />
          <p className="text-sm text-muted-foreground">Informe a data à qual os valores do extrato se referem.</p>
        </div>
        <p className="text-sm text-muted-foreground">A análise acontece durante a solicitação. Os dados financeiros extraídos serão salvos para revisão; o arquivo original não é armazenado.</p>
        <Button type="button" onClick={() => void analisar()} disabled={!carteira || !arquivo || !dataReferencia || processando}>
          {processando ? 'Processando...' : 'Analisar e salvar prévia'}
        </Button>
        {processando && <p role="status" className="text-sm">Aguarde a análise do arquivo.</p>}
        {erro && <p role="alert" className="text-sm text-destructive">{erro}</p>}
      </CardContent>
    </Card>
    {carteira && <Card>
      <CardHeader><CardTitle>Prévias salvas</CardTitle><CardDescription>Retome a revisão de uma importação desta carteira.</CardDescription></CardHeader>
      <CardContent className="space-y-2">
        {historico.isLoading && <p role="status">Carregando prévias...</p>}
        {historico.isError && <p role="alert">Não foi possível carregar as prévias. <Button type="button" variant="link" onClick={() => void historico.refetch()}>Tentar novamente</Button></p>}
        {historico.data && historico.data.dados.length === 0 && <p>{pagina === 1 ? 'Nenhuma prévia salva para esta carteira.' : 'Nenhuma prévia nesta página.'}</p>}
        {historico.data && historico.data.dados.length > 0 && <ul className="space-y-2">{historico.data.dados.map(item => <li key={item.id} className="flex flex-wrap items-center gap-2 rounded-md border p-2 text-sm">
          <span>Referência {item.dataReferencia.split('-').reverse().join('/')} · {item.formato.toUpperCase()} · {new Date(item.criadoEmUtc).toLocaleString('pt-BR')}{item.estado === 'Confirmado' ? ' · Confirmado' : ''}</span>
          <Button type="button" variant="outline" disabled={processando} onClick={() => void abrirLote(item.id)}>{item.estado === 'Confirmado' ? 'Ver extrato confirmado' : 'Revisar prévia'}</Button>
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
        <CardTitle>{lote.estado === 'Confirmado' ? 'Extrato confirmado' : 'Prévia do extrato'}</CardTitle>
        <CardDescription>Os valores abaixo vêm do arquivo enviado.{lote.estado !== 'Confirmado' && ' Esta etapa não registra operações nem altera a carteira.'}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-5">
        <p className="text-sm">Data de referência informada: {lote.dataReferencia.split('-').reverse().join('/')}</p>
        <p className="text-xs text-muted-foreground">Nos movimentos, “Taxa (% a.a.)” é a taxa do título/operação; não é uma cobrança em reais. Os custos aparecem em “Taxa devida” (B3 e instituição), em R$. IR/IOF previstos e valor líquido são os valores do extrato para a data de referência deste lote e podem mudar até um resgate ou evento de custódia. <a className="underline underline-offset-2" href="https://www.tesourodireto.com.br/b/impostos-e-taxas-no-tesouro-direto" target="_blank" rel="noreferrer">Entenda impostos e taxas</a>.</p>
        {previa.titulo && <p className="font-medium">{previa.titulo}{previa.vencimento ? ` · vencimento ${previa.vencimento.split('-').reverse().join('/')}` : ''}</p>}
        {previa.vencimentoOriginal && <p className="text-sm">Vencimento informado no arquivo: {previa.vencimentoOriginal}</p>}
        {previa.titulo && previa.vencimento && <ReferenciaPublicaTesouro key={`${previa.titulo}|${previa.vencimento}`} titulo={previa.titulo} vencimento={previa.vencimento} />}
        {lote.estado === 'Confirmado'
          ? <p role="status" className="font-medium">Importação confirmada{lote.confirmadoEmUtc ? ` em ${new Date(lote.confirmadoEmUtc).toLocaleString('pt-BR')}` : ''}.</p>
          : <ConciliacaoTesouro key={lote.id} lote={lote} onConfirmado={confirmado => {
            setLote(atual => atual?.id === confirmado.id ? confirmado : atual)
            void queryClient.invalidateQueries({ queryKey: ['tesouro-lotes', confirmado.carteiraId] })
            void queryClient.invalidateQueries({ queryKey: queryKeys.portfolios.all })
            void queryClient.invalidateQueries({ queryKey: queryKeys.investments.all })
          }} />}
        <p role="status" className="text-sm text-amber-700 dark:text-amber-300">A leitura pode não identificar todos os dados do extrato.</p>
        {previa.avisos.map((aviso, index) => <p key={index} className="text-sm text-muted-foreground">{aviso}</p>)}
        {previa.linhasNaoInterpretadas.length > 0 && <div role="status" className="text-sm text-amber-700 dark:text-amber-300">
          <p>{previa.linhasNaoInterpretadas.length} linhas não foram interpretadas e precisam de revisão no arquivo original.</p>
          <p>Linhas: {previa.linhasNaoInterpretadas.join(', ')}.</p>
        </div>}
        <section className="space-y-3"><h3 className="font-semibold">Posições identificadas ({previa.posicoes.length})</h3>
          {previa.posicoes.length === 0 ? <p className="text-sm text-muted-foreground">Nenhuma posição identificada neste arquivo.</p> :
            <TabelaPreviaTesouro caption="Posições identificadas no extrato" cabecalhoNumero="Linha" linhas={linhasPosicao} />
          }
        </section>
        <section className="space-y-3"><h3 className="font-semibold">Movimentações identificadas ({previa.movimentacoes.length})</h3>
          {previa.movimentacoes.length === 0 ? <p className="text-sm text-muted-foreground">Nenhuma movimentação identificada neste arquivo.</p> :
            <TabelaPreviaTesouro caption="Movimentações identificadas no extrato" cabecalhoNumero="Nº" linhas={linhasMovimentacao} />
          }
        </section>
      </CardContent>
    </Card>}
  </div>
}
