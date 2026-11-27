import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '@/api/errors/api-error'
import { tesouroDiretoService, type ExtratoOficialTesouro as Extrato } from '@/api/services/tesouro-direto.service'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { formatCurrency } from '@/lib/utils'

const dataCivil = (data: string) => data.split('-').reverse().join('/')
const numero = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 8 })

function campoAusente(dados: Extrato['eventos'][number]['dados'] | null, campo: string, feminino = false) {
  if (dados?.dadosInterpretados?.camposNaoAplicaveis?.includes(campo)) return 'Não aplicável'
  if (dados?.dadosInterpretados?.camposNaoInterpretados?.includes(campo)) return feminino ? 'Não interpretada' : 'Não interpretado'
  return feminino ? 'Não informada' : 'Não informado'
}

function EventoOficial({ evento }: { evento: Extrato['eventos'][number] }) {
  const dados = evento.dados?.dadosInterpretados
  const venda = dados?.natureza === 'Venda' && dados.tipo === 'Débito'
  const taxa = dados?.natureza === 'Taxas'
  const ausente = (campo: string, feminino = false) => campoAusente(evento.dados, campo, feminino)
  const taxaAnual = dados?.taxaAnual != null ? `${numero.format(dados.taxaAnual)}% a.a.` : ausente('taxaAnual', true)
  return <TableRow>
    <TableCell className="whitespace-nowrap">{dados?.data ? dataCivil(dados.data) : ausente('data', true)}</TableCell>
    <TableCell>
      <p className="font-medium">{taxa ? 'Cobrança de taxa' : dados?.natureza ?? 'Tipo não informado'}</p>
      {dados?.tipo && <p className="text-xs text-muted-foreground">{dados.tipo}</p>}
    </TableCell>
    <TableCell className="whitespace-nowrap text-right">
      {dados?.valor != null ? formatCurrency(dados.valor) : ausente('valor')}
      {venda && <p className="text-xs text-muted-foreground">Valor bruto do resgate</p>}
    </TableCell>
    <TableCell>
      <details className="min-w-32">
        <summary className="cursor-pointer">Ver dados</summary>
        <dl className="mt-2 space-y-1 text-xs">
          <div><dt className="font-medium">Quantidade</dt><dd>{dados?.quantidade != null ? numero.format(dados.quantidade) : ausente('quantidade', true)}</dd></div>
          <div><dt className="font-medium">Preço unitário</dt><dd>{dados?.preco != null ? formatCurrency(dados.preco) : ausente('preco')}</dd></div>
          <div><dt className="font-medium">Taxa do título</dt><dd>{taxaAnual}</dd></div>
          {evento.transacaoId && <div><dt className="font-medium">Conciliação</dt><dd>Vinculada a uma movimentação da carteira</dd></div>}
        </dl>
      </details>
    </TableCell>
  </TableRow>
}

function ResumoImportacao({ resumo }: { resumo: NonNullable<Extrato['resumoUltimaImportacao']> }) {
  const anteriorDisponivel = !!resumo.loteAnteriorId && !!resumo.dataReferenciaAnterior
  return <details className="rounded-md border p-3 text-sm">
    <summary className="cursor-pointer font-medium">O que mudou na última importação</summary>
    <div className="mt-3 space-y-3 break-words">
      {anteriorDisponivel && resumo.dataReferenciaAnterior
        ? <p>Comparação de {dataCivil(resumo.dataReferenciaAnterior)} a {dataCivil(resumo.dataReferenciaAtual)}.</p>
        : <p>O extrato anterior está indisponível para comparação.{resumo.dataReferenciaAnterior && ` Referência anterior: ${dataCivil(resumo.dataReferenciaAnterior)}.`}</p>}
      {resumo.quantidadeAnterior != null && resumo.variacaoQuantidade != null && <p>
        Quantidade anterior: <strong>{numero.format(resumo.quantidadeAnterior)}</strong> ·
        Atual: <strong>{numero.format(resumo.quantidadeAtual)}</strong> ·
        Variação: <strong>{resumo.variacaoQuantidade > 0 ? '+' : ''}{numero.format(resumo.variacaoQuantidade)}</strong>
      </p>}
      {resumo.divergencias.length > 0 ? <ul className="list-disc space-y-2 pl-5">
        {resumo.divergencias.map((item, indice) => <li key={indice}>
          <p>{item.descricao}</p>
          {item.anterior && <p className="text-muted-foreground">Anterior: {item.anterior}</p>}
          {item.atual && <p className="text-muted-foreground">Atual: {item.atual}</p>}
        </li>)}
      </ul> : anteriorDisponivel && <p>Nenhuma diferença foi identificada nas linhas comparáveis.</p>}
    </div>
  </details>
}

export function ExtratoOficialTesouro({ carteiraId, ativoId }: { carteiraId: string; ativoId: string }) {
  const [paginacao, setPaginacao] = useState<{ loteId: string; cursores: number[] } | null>(null)
  const queryClient = useQueryClient()
  const cursor = paginacao?.cursores.at(-1) ?? -1
  const loteId = paginacao?.loteId
  const chave = ['extrato-tesouro', carteiraId, ativoId] as const
  const extrato = useQuery({
    queryKey: [...chave, loteId, cursor],
    queryFn: () => tesouroDiretoService.extratoOficial(carteiraId, ativoId, cursor, loteId),
    retry: false,
  })
  const dados = extrato.isError ? undefined : extrato.data?.dados
  const base = dados?.baseAtiva
  const proximo = dados?.proximoIndice
  const baseMudou = extrato.error instanceof ApiError && extrato.error.status === 409
  const semAcesso = extrato.error instanceof ApiError && [403, 404].includes(extrato.error.status)

  function voltarAoExtratoAtual() {
    queryClient.removeQueries({ queryKey: [...chave, undefined, -1], exact: true })
    setPaginacao(null)
  }

  return <Card className="min-w-0">
    <CardHeader><CardTitle>Extrato oficial do Tesouro Direto</CardTitle></CardHeader>
    <CardContent className="min-w-0 space-y-4">
      {extrato.isPending && <p role="status" className="text-sm text-muted-foreground">Carregando extrato…</p>}
      {extrato.isError && <div role="alert" className="space-y-3 text-sm">
        <p>{baseMudou ? 'Uma nova importação atualizou o extrato. Abra a primeira página para continuar.'
          : semAcesso ? 'O extrato está indisponível para esta posição.' : 'Não foi possível carregar o extrato.'}</p>
        {baseMudou ? <Button variant="outline" onClick={voltarAoExtratoAtual}>Voltar à primeira página</Button>
          : !semAcesso && <Button variant="outline" onClick={() => void extrato.refetch()}>Tentar novamente</Button>}
      </div>}
      {dados && !base && <p role="status" className="text-sm text-muted-foreground">Esta posição ainda não tem um extrato confirmado.</p>}
      {dados && base && <>
        <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm">
          <p>Referência do saldo: <strong>{dataCivil(base.dataReferencia)}</strong></p>
          <p>Quantidade no extrato: <strong>{numero.format(base.quantidade)}</strong></p>
        </div>
        {dados.resumoUltimaImportacao && <ResumoImportacao resumo={dados.resumoUltimaImportacao} />}
        {dados.eventos.length === 0 ? <p className="text-sm text-muted-foreground">O extrato ativo não contém movimentações.</p> : <>
          <p className="text-sm text-muted-foreground">Origem: Portal do Tesouro Direto. Deslize a tabela para ver os detalhes quando necessário.</p>
          <Table className="min-w-max" aria-label="Movimentações oficiais do Tesouro Direto" scrollLabel="Rolagem das movimentações oficiais">
            <TableHeader><TableRow>
              <TableHead scope="col">Data</TableHead>
              <TableHead scope="col">Movimentação</TableHead>
              <TableHead scope="col" className="text-right">Valor</TableHead>
              <TableHead scope="col">Detalhes</TableHead>
            </TableRow></TableHeader>
            <TableBody>{dados.eventos.map(evento => <EventoOficial key={evento.indiceEvento} evento={evento} />)}</TableBody>
          </Table>
        </>}
        {(paginacao || proximo != null) && <nav aria-label="Páginas do extrato" className="flex flex-wrap items-center gap-2">
          <Button size="sm" variant="outline" disabled={!paginacao || extrato.isFetching} onClick={() => setPaginacao(atual =>
            atual && atual.cursores.length > 1 ? { ...atual, cursores: atual.cursores.slice(0, -1) } : null)}>
            Página anterior
          </Button>
          <span className="text-sm">Página {(paginacao?.cursores.length ?? 0) + 1}</span>
          <Button size="sm" variant="outline" disabled={proximo == null || extrato.isFetching} onClick={() => {
            if (proximo != null) setPaginacao(atual => ({ loteId: base.loteId, cursores: [...atual?.cursores ?? [], proximo] }))
          }}>
            Próxima página
          </Button>
        </nav>}
      </>}
    </CardContent>
  </Card>
}
