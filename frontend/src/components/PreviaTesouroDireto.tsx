import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { tesouroDiretoService, type PreviaTesouro } from '@/api/services/tesouro-direto.service'

type Carteira = { id: string; nome: string }

export function PreviaTesouroDireto({ carteiras, carregando, erroCarteiras, tentarNovamente }: { carteiras: Carteira[]; carregando: boolean; erroCarteiras: boolean; tentarNovamente: () => void }) {
  const [carteira, setCarteira] = useState('')
  const [arquivo, setArquivo] = useState<File | null>(null)
  const [previa, setPrevia] = useState<PreviaTesouro | null>(null)
  const [erro, setErro] = useState('')
  const [processando, setProcessando] = useState(false)

  async function analisar() {
    if (!carteira || !arquivo) return
    setProcessando(true)
    setPrevia(null)
    setErro('')
    try {
      const resposta = await tesouroDiretoService.previa(carteira, arquivo)
      setPrevia(resposta.dados)
    } catch {
      setErro('Não foi possível analisar o arquivo. Verifique o formato e tente novamente.')
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
          <Select value={carteira} onValueChange={value => { setCarteira(value); setPrevia(null) }} disabled={carregando || processando || carteiras.length === 0}>
            <SelectTrigger id="tesouro-carteira"><SelectValue placeholder={carregando ? 'Carregando carteiras...' : 'Selecione a carteira'} /></SelectTrigger>
            <SelectContent>{carteiras.map(item => <SelectItem key={item.id} value={item.id}>{item.nome}</SelectItem>)}</SelectContent>
          </Select>
        </div>
        {erroCarteiras && <p role="alert" className="text-sm text-destructive">Não foi possível carregar as carteiras. <Button type="button" variant="link" onClick={tentarNovamente}>Tentar novamente</Button></p>}
        {!carregando && !erroCarteiras && carteiras.length === 0 && <p role="status" className="text-sm">Crie uma carteira antes de analisar o extrato.</p>}
        <div className="grid gap-2">
          <label htmlFor="tesouro-arquivo" className="text-sm font-medium">Arquivo do extrato</label>
          <input id="tesouro-arquivo" type="file" accept=".html,.htm,.xlsx" disabled={processando} className="max-w-full text-sm"
            onChange={event => { setArquivo(event.target.files?.[0] ?? null); setPrevia(null); setErro('') }} />
        </div>
        <p className="text-sm text-muted-foreground">A análise acontece durante a solicitação. Mantenha esta tela aberta até concluir; a prévia não é salva.</p>
        <Button type="button" onClick={() => void analisar()} disabled={!carteira || !arquivo || processando}>
          {processando ? 'Analisando arquivo...' : 'Analisar arquivo'}
        </Button>
        {processando && <p role="status" className="text-sm">Aguarde a análise do arquivo.</p>}
        {erro && <p role="alert" className="text-sm text-destructive">{erro}</p>}
      </CardContent>
    </Card>
    {previa && <Card>
      <CardHeader>
        <CardTitle>Prévia do extrato</CardTitle>
        <CardDescription>Revise os dados reconhecidos. Esta etapa não registra operações nem altera a carteira.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-5">
        {previa.titulo && <p className="font-medium">{previa.titulo}{previa.vencimento ? ` · vencimento ${previa.vencimento.split('-').reverse().join('/')}` : ''}</p>}
        {previa.vencimentoOriginal && <p className="text-sm">Vencimento informado no arquivo: {previa.vencimentoOriginal}</p>}
        <p role="status" className="text-sm text-amber-700 dark:text-amber-300">O histórico pode estar incompleto. A confirmação e a conciliação ainda não estão disponíveis.</p>
        {previa.avisos.map((aviso, index) => <p key={index} className="text-sm text-muted-foreground">{aviso}</p>)}
        {previa.linhasNaoInterpretadas.length > 0 && <p className="text-sm text-amber-700 dark:text-amber-300">{previa.linhasNaoInterpretadas.length} linhas não foram interpretadas e precisam de revisão.</p>}
        <section className="space-y-3"><h3 className="font-semibold">Posições identificadas ({previa.posicoes.length})</h3>
          {previa.posicoes.length === 0 ? <p className="text-sm text-muted-foreground">Nenhuma posição identificada neste arquivo.</p> : previa.posicoes.map((posicao, index) => <div key={posicao.numeroLinha} className="rounded-md border p-3 text-sm">
            <p className="font-medium">Posição {index + 1}</p>
            <dl className="mt-2 grid gap-2 sm:grid-cols-2">{Object.entries(posicao.camposBrutos).map(([rotulo, valor]) => <div key={rotulo}><dt className="text-muted-foreground">{rotulo.replace(/_/g, ' ')}</dt><dd className="break-words">{valor || '—'}</dd></div>)}</dl>
          </div>)}
        </section>
        <section className="space-y-3"><h3 className="font-semibold">Movimentações identificadas ({previa.movimentacoes.length})</h3>
          {previa.movimentacoes.length === 0 ? <p className="text-sm text-muted-foreground">Nenhuma movimentação identificada neste arquivo.</p> : previa.movimentacoes.map((movimentacao, index) => <div key={index} className="rounded-md border p-3 text-sm">
            <dl className="grid gap-2 sm:grid-cols-2">{movimentacao.campos.map((campo, campoIndex) => <div key={campoIndex}><dt className="text-muted-foreground">{campo.rotulo}</dt><dd className="break-words">{campo.valor}</dd></div>)}</dl>
          </div>)}
        </section>
      </CardContent>
    </Card>}
  </div>
}
