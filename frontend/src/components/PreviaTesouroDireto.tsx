import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { tesouroDiretoService, type LoteTesouro } from '@/api/services/tesouro-direto.service'
import { ReferenciaPublicaTesouro } from './ReferenciaPublicaTesouro'
import { ConciliacaoTesouro } from './ConciliacaoTesouro'

type Carteira = { id: string; nome: string }

const nomesCampos: Record<string, string> = {
  data: 'data', natureza: 'natureza', tipo: 'tipo', quantidade: 'quantidade', preco: 'preço', taxaAnual: 'taxa anual', valor: 'valor',
  dataAplicacao: 'data de aplicação', quantidadeTitulos: 'quantidade de títulos', precoAplicacao: 'preço de aplicação', valorInvestido: 'valor investido',
  rentabilidadeContratada: 'rentabilidade contratada', rentabilidadeBrutaAnualizada: 'rentabilidade anualizada', rentabilidadeBrutaAcumulada: 'rentabilidade acumulada',
  valorBruto: 'valor bruto', diasCorridos: 'dias corridos', aliquotaIr: 'alíquota de IR', impostoPrevistoIr: 'IR previsto', impostoPrevistoIof: 'IOF previsto',
  taxaDevidaB3: 'taxa B3', taxaDevidaInstituicao: 'taxa da instituição', valorLiquido: 'valor líquido',
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
        <CardDescription>Selecione uma carteira e um arquivo HTML salvo ou XLSX de posições para examinar os dados disponíveis.</CardDescription>
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
          <input id="tesouro-arquivo" type="file" accept=".html,.htm,.xlsx" disabled={processando} className="max-w-full text-sm"
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
          <span>Referência {item.dataReferencia.split('-').reverse().join('/')} · {item.formato.toUpperCase()} · {new Date(item.criadoEmUtc).toLocaleString('pt-BR')}</span>
          <Button type="button" variant="outline" disabled={processando} onClick={() => void abrirLote(item.id)}>Revisar prévia</Button>
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
        <CardTitle>Prévia do extrato</CardTitle>
        <CardDescription>Os valores abaixo vêm do arquivo enviado. Esta etapa não registra operações nem altera a carteira.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-5">
        <p className="text-sm">Data de referência informada: {lote.dataReferencia.split('-').reverse().join('/')}</p>
        {previa.titulo && <p className="font-medium">{previa.titulo}{previa.vencimento ? ` · vencimento ${previa.vencimento.split('-').reverse().join('/')}` : ''}</p>}
        {previa.vencimentoOriginal && <p className="text-sm">Vencimento informado no arquivo: {previa.vencimentoOriginal}</p>}
        {previa.titulo && previa.vencimento && <ReferenciaPublicaTesouro key={`${previa.titulo}|${previa.vencimento}`} titulo={previa.titulo} vencimento={previa.vencimento} />}
        <ConciliacaoTesouro key={lote.id} carteiraId={carteira} loteId={lote.id} />
        <p role="status" className="text-sm text-amber-700 dark:text-amber-300">A leitura pode não identificar todos os dados do extrato. A confirmação e a conciliação ainda não estão disponíveis.</p>
        {previa.avisos.map((aviso, index) => <p key={index} className="text-sm text-muted-foreground">{aviso}</p>)}
        {previa.linhasNaoInterpretadas.length > 0 && <div role="status" className="text-sm text-amber-700 dark:text-amber-300">
          <p>{previa.linhasNaoInterpretadas.length} linhas não foram interpretadas e precisam de revisão no arquivo original.</p>
          <p>Linhas: {previa.linhasNaoInterpretadas.join(', ')}.</p>
        </div>}
        <section className="space-y-3"><h3 className="font-semibold">Posições identificadas ({previa.posicoes.length})</h3>
          {previa.posicoes.length === 0 ? <p className="text-sm text-muted-foreground">Nenhuma posição identificada neste arquivo.</p> : previa.posicoes.map((posicao, index) => <div key={posicao.numeroLinha} className="rounded-md border p-3 text-sm">
            <p className="font-medium">Posição {index + 1}</p>
            {!!posicao.dadosInterpretados?.camposNaoInterpretados.length && <p role="status" className="mt-2 text-amber-700 dark:text-amber-300">
              Não foi possível interpretar: {posicao.dadosInterpretados.camposNaoInterpretados.map(campo => nomesCampos[campo] ?? campo).join(', ')}. Confira os valores originais abaixo.
            </p>}
            <dl className="mt-2 grid gap-2 sm:grid-cols-2">{Object.entries(posicao.camposBrutos).map(([rotulo, valor]) => <div key={rotulo}><dt className="text-muted-foreground">{rotulo.replace(/_/g, ' ')}</dt><dd className="break-words">{valor || '—'}</dd></div>)}</dl>
          </div>)}
        </section>
        <section className="space-y-3"><h3 className="font-semibold">Movimentações identificadas ({previa.movimentacoes.length})</h3>
          {previa.movimentacoes.length === 0 ? <p className="text-sm text-muted-foreground">Nenhuma movimentação identificada neste arquivo.</p> : previa.movimentacoes.map((movimentacao, index) => <div key={index} className="rounded-md border p-3 text-sm">
            <p className="mb-2 font-medium">Movimentação {index + 1}</p>
            {!!movimentacao.dadosInterpretados?.camposNaoInterpretados.length && <p role="status" className="mb-2 text-amber-700 dark:text-amber-300">
              Não foi possível interpretar: {movimentacao.dadosInterpretados.camposNaoInterpretados.map(campo => nomesCampos[campo] ?? campo).join(', ')}. Confira os valores originais abaixo.
            </p>}
            <dl className="grid gap-2 sm:grid-cols-2">{movimentacao.campos.map((campo, campoIndex) => <div key={campoIndex}><dt className="text-muted-foreground">{campo.rotulo}</dt><dd className="break-words">{campo.valor}</dd></div>)}</dl>
          </div>)}
        </section>
      </CardContent>
    </Card>}
  </div>
}
