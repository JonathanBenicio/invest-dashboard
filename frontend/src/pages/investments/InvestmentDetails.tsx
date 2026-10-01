import { useEffect, useMemo, useState } from "react"
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { investmentDetailsRoute } from "../../router"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { ChartPeriodFilter, type ChartPeriod } from "@/components/ChartPeriodFilter"
import { useInvestment, useInvestmentTransactions } from "@/hooks/use-investment-details"
import { investmentService } from "@/api/services/investment.service"
import { marketDataService } from "@/api/services/market-data.service"
import { formatQuoteObservation, useMarketQuotes } from "@/hooks/use-market-quotes"
import { transactionService } from "@/api/services/transaction.service"
import { groupService } from "@/api/services/group.service"
import type { RegistrarTransacaoRequest } from "@/api/dtos/transacao.dto"
import { formatCurrency, localDateInputToISOString, toLocalDateInputValue } from "@/lib/utils"
import { ArrowLeft, BarChart3, CalendarDays, MinusCircle, PlusCircle, TrendingDown, TrendingUp, Wallet } from "lucide-react"
import { toast } from "sonner"
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"

type TransactionAction = "Buy" | "Sell"
type HistoricalPricePoint = { date: string; price: number }

const money = (value: number) => formatCurrency(value)
const dateLabel = (value: string) => new Intl.DateTimeFormat("pt-BR").format(new Date(value))

export default function InvestmentDetails() {
  const { id } = investmentDetailsRoute.useParams()
  const search = investmentDetailsRoute.useSearch()
  const queryClient = useQueryClient()
  const [period, setPeriod] = useState<ChartPeriod>("30d")
  const [action, setAction] = useState<TransactionAction>("Buy")
  const [dialogOpen, setDialogOpen] = useState(false)
  const [quantity, setQuantity] = useState("")
  const [unitPrice, setUnitPrice] = useState("")
  const [fees, setFees] = useState("0")
  const [taxModality, setTaxModality] = useState<"" | "Comum" | "DayTrade">("")
  const [transactionDate, setTransactionDate] = useState(toLocalDateInputValue())

  const { data: investmentResponse, isLoading, isError } = useInvestment(id)
  const { data: transactionsResponse, isLoading: isLoadingTransactions, isError: isTransactionsError, refetch: refetchTransactions } = useInvestmentTransactions(id)
  const storedAsset = investmentResponse?.dados
  const groupsQuery = useQuery({
    queryKey: ['portfolio-groups'],
    queryFn: groupService.list,
    enabled: !!storedAsset?.grupoId,
  })
  const groupName = groupsQuery.data?.dados.find(group => group.id === storedAsset?.grupoId)?.nome
  const fixedIncomeProjectionQuery = useQuery({
    queryKey: ['investment', id, 'fixed-income-projection'],
    queryFn: () => investmentService.getFixedIncomeProjection(id),
    enabled: storedAsset?.tipo === 'fixed_income',
  })
  const quoteTickers = useMemo(
    () => storedAsset?.tipo === "variable_income" ? [storedAsset.ticker] : [],
    [storedAsset?.ticker, storedAsset?.tipo],
  )
  const { quotesBySymbol, isLoading: isLoadingQuote } = useMarketQuotes(quoteTickers)
  const quote = storedAsset ? quotesBySymbol.get(storedAsset.ticker.toUpperCase()) : undefined
  const asset = useMemo(() => {
    if (!storedAsset || !quote) return storedAsset

    const currentValue = storedAsset.quantidade * quote.preco
    const gain = currentValue - storedAsset.totalInvestido
    return {
      ...storedAsset,
      precoAtual: quote.preco,
      currentValue,
      gain,
      percentualGanho: storedAsset.totalInvestido > 0 ? (gain / storedAsset.totalInvestido) * 100 : 0,
    }
  }, [quote, storedAsset])
  const transactions = transactionsResponse?.dados ?? []
  const requiresTaxModality = Boolean(storedAsset && storedAsset.tipo !== "fixed_income" &&
    ["ACAO", "STOCK", "FII"].includes(storedAsset.subtipo.toUpperCase()))

  const fromDate = useMemo(() => {
    const date = new Date()
    if (period === "7d") date.setDate(date.getDate() - 7)
    else if (period === "30d") date.setDate(date.getDate() - 30)
    else if (period === "1y") date.setFullYear(date.getFullYear() - 1)
    else date.setFullYear(date.getFullYear() - 10)
    return date.toISOString().slice(0, 10)
  }, [period])

  const historyQuery = useQuery({
    queryKey: ["investment", id, "history", period],
    queryFn: async (): Promise<HistoricalPricePoint[]> => {
      if (asset?.tipo === "fixed_income") {
        const response = await investmentService.getPriceHistory(id, `${fromDate}T00:00:00.000Z`)
        return response.dados.map(point => ({ date: point.data, price: point.preco }))
      }

      const response = await marketDataService.getDailyHistory(
        [asset!.ticker],
        fromDate,
        new Date().toISOString().slice(0, 10),
      )
      return response.dados.map(point => ({
        date: point.dataUtc,
        price: point.preco,
        source: point.origem,
        isAdjusted: point.ajustado,
      }))
    },
    enabled: !!asset,
  })

  const createTransaction = useMutation({
    mutationFn: (request: RegistrarTransacaoRequest) => transactionService.create(request),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["investment", id] }),
        queryClient.invalidateQueries({ queryKey: ["investments"] }),
        queryClient.invalidateQueries({ queryKey: ["portfolios"] }),
      ])
      setDialogOpen(false)
      setQuantity("")
      setFees("0")
      toast.success(action === "Buy" ? "Compra registrada." : "Venda registrada.")
    },
    onError: () => toast.error("Não foi possível registrar a movimentação. Confira a quantidade disponível e tente novamente."),
  })

  useEffect(() => {
    if (search.action === "buy" || search.action === "sell") {
      setAction(search.action === "buy" ? "Buy" : "Sell")
      setDialogOpen(true)
    }
  }, [search.action])

  useEffect(() => {
    if (asset && !unitPrice) setUnitPrice(String(asset.precoAtual))
  }, [asset, unitPrice])

  const priceHistory = historyQuery.data ?? []
  const chartData = priceHistory.map((point) => ({
    date: new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "short" }).format(new Date(point.date)),
    price: point.price,
  }))

  const submitTransaction = () => {
    if (!asset) return
    const request: RegistrarTransacaoRequest = {
      carteiraId: asset.carteiraId,
      ativoId: asset.ativoId,
      ticker: asset.ticker,
      tipo: action,
      quantidade: Number(quantity),
      precoUnitario: Number(unitPrice),
      taxas: Number(fees || 0),
      ...(action === "Sell" && taxModality ? { modalidadeFiscal: taxModality } : {}),
      dataTransacao: localDateInputToISOString(transactionDate),
      chaveIdempotencia: crypto.randomUUID(),
      classeAtivo: asset.tipo === "fixed_income" ? "RENDA_FIXA" : asset.subtipo,
    }
    createTransaction.mutate(request)
  }

  const openTransactionDialog = (nextAction: TransactionAction) => {
    setAction(nextAction)
    setQuantity("")
    setFees("0")
    setTaxModality("")
    setUnitPrice(String(asset?.precoAtual ?? ""))
    setDialogOpen(true)
  }

  if (isLoading) return <div className="flex h-96 items-center justify-center text-muted-foreground">Carregando posição...</div>
  if (isError || !asset) {
    return <div className="space-y-4"><p className="text-muted-foreground">Posição não encontrada ou indisponível.</p><Button variant="outline" onClick={() => window.history.back()}><ArrowLeft className="mr-2 h-4 w-4" />Voltar</Button></div>
  }

  const isFixedIncome = asset.tipo === "fixed_income"
  const gainClass = asset.ganho >= 0 ? "text-success" : "text-destructive"

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-start gap-3">
          <Button variant="ghost" size="icon" onClick={() => window.history.back()} aria-label="Voltar"><ArrowLeft className="h-5 w-5" /></Button>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2"><h1 className="text-2xl font-bold">{asset.nome || asset.ticker}</h1><Badge variant="secondary">{asset.subtipo}</Badge><Badge variant={asset.situacao === "open" ? "default" : "outline"} className={asset.situacao === "matured" ? "border-warning/50 text-warning" : undefined}>{asset.situacao === "open" ? "Em carteira" : asset.situacao === "matured" ? "Vencido" : "Encerrada"}</Badge></div>
            <p className="text-sm text-muted-foreground">{asset.ticker}{asset.emissor ? ` · ${asset.emissor}` : ""}</p>
            {(asset.carteiraNome || asset.titular || asset.instituicaoFinanceira || groupName) && <p className="mt-1 text-xs text-muted-foreground">{[
              asset.carteiraNome,
              groupName,
              asset.titular ? `Titular: ${asset.titular}` : undefined,
              asset.instituicaoFinanceira,
            ].filter(Boolean).join(' · ')}</p>}
          </div>
        </div>
        {asset.situacao !== "closed" && <div className="flex gap-2 sm:ml-auto">
          {asset.situacao === "open" && <Button variant="success" size="sm" onClick={() => openTransactionDialog("Buy")}><PlusCircle className="mr-2 h-4 w-4" />Comprar</Button>}
          <Button variant="destructive" size="sm" onClick={() => openTransactionDialog("Sell")}><MinusCircle className="mr-2 h-4 w-4" />{asset.situacao === "matured" ? "Registrar resgate" : "Vender"}</Button>
        </div>}
      </div>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Metric title="Valor da posição" value={money(asset.valorAtual)} icon={<Wallet className="h-4 w-4" />} detail={`${asset.quantidade} unidades`} />
        <Metric title="Custo da posição" value={money(asset.totalInvestido)} icon={<BarChart3 className="h-4 w-4" />} detail={`Preço médio ${money(asset.precoMedio)}`} />
        <Metric title={isFixedIncome ? "Valor por unidade" : "Cotação atual"} value={money(asset.precoAtual)} icon={<CalendarDays className="h-4 w-4" />} detail={isFixedIncome ? "Atualizado por extrato" : quote ? formatQuoteObservation(quote.origem.toUpperCase(), quote.observadoEmUtc) : isLoadingQuote ? "Buscando cotação Brapi..." : "Cotação Brapi indisponível; último preço salvo"} />
        <Metric title="Resultado não realizado" value={money(asset.ganho)} icon={asset.ganho >= 0 ? <TrendingUp className="h-4 w-4" /> : <TrendingDown className="h-4 w-4" />} detail={`${asset.percentualGanho.toFixed(2)}%`} valueClass={gainClass} />
      </div>

      {isFixedIncome && <Card>
        <CardHeader><CardTitle>Estimativa até o vencimento</CardTitle></CardHeader>
        <CardContent className="space-y-2">
          {fixedIncomeProjectionQuery.isLoading ? <p className="text-sm text-muted-foreground">Calculando…</p> :
            fixedIncomeProjectionQuery.isError ? <p className="text-sm text-muted-foreground">A estimativa não está disponível.</p> :
            fixedIncomeProjectionQuery.data ? <><div className="grid gap-2 sm:grid-cols-2"><p>Último valor de extrato: <strong>{money(fixedIncomeProjectionQuery.data.dados.valorObservado)}</strong></p><p>Vencimento: <strong>{dateLabel(fixedIncomeProjectionQuery.data.dados.dataVencimento)}</strong></p></div>
              {fixedIncomeProjectionQuery.data.dados.valorProjetadoBruto === null ?
                <p className="text-sm text-muted-foreground">{fixedIncomeProjectionQuery.data.dados.motivo}</p> :
                <p>Estimativa bruta: <strong>{money(fixedIncomeProjectionQuery.data.dados.valorProjetadoBruto)}</strong></p>}
              {fixedIncomeProjectionQuery.data.dados.taxaSimbolo && fixedIncomeProjectionQuery.data.dados.taxaValorObservado !== undefined && <p className="text-sm">Premissa observada: <strong>{fixedIncomeProjectionQuery.data.dados.taxaSimbolo} {fixedIncomeProjectionQuery.data.dados.taxaValorObservado.toLocaleString('pt-BR')} {fixedIncomeProjectionQuery.data.dados.taxaUnidade}</strong>{fixedIncomeProjectionQuery.data.dados.taxaDataReferencia ? ` · referência ${dateLabel(fixedIncomeProjectionQuery.data.dados.taxaDataReferencia)}` : ''}{fixedIncomeProjectionQuery.data.dados.taxaPeriodicidade ? ` · ${fixedIncomeProjectionQuery.data.dados.taxaPeriodicidade}` : ''}</p>}
              {fixedIncomeProjectionQuery.data.dados.taxaIdade !== undefined && fixedIncomeProjectionQuery.data.dados.taxaValidade !== undefined && <p className="text-xs text-muted-foreground">Taxa observada há {fixedIncomeProjectionQuery.data.dados.taxaIdade} {fixedIncomeProjectionQuery.data.dados.taxaUnidadeValidade}; validade máxima {fixedIncomeProjectionQuery.data.dados.taxaValidade} {fixedIncomeProjectionQuery.data.dados.taxaUnidadeValidade}.</p>}
              {fixedIncomeProjectionQuery.data.dados.taxaOrigem && <p className="text-xs text-muted-foreground">Fonte da taxa: {fixedIncomeProjectionQuery.data.dados.taxaOrigem}.</p>}
              {asset.liquidez && <p className="text-sm">Liquidez: {asset.liquidez}</p>}
              {asset.convencao && <p className="text-sm">Convenção: {asset.convencao}</p>}
              {fixedIncomeProjectionQuery.data.dados.observadoEmUtc && <p className="text-xs text-muted-foreground">Extrato observado em {dateLabel(fixedIncomeProjectionQuery.data.dados.observadoEmUtc)}.</p>}
            </> : <p>Estimativa indisponível.</p>}
          <p className="text-xs text-muted-foreground">Estimativa bruta; não lança rendimento nem resgate no caixa.</p>
        </CardContent>
      </Card>}

      <Card>
        <CardHeader className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><div><CardTitle>Histórico de preço</CardTitle><p className="text-sm text-muted-foreground">Série armazenada para este ativo; valores em BRL por unidade.</p></div><ChartPeriodFilter value={period} onChange={setPeriod} /></CardHeader>
        <CardContent>
          {historyQuery.isLoading ? <div className="flex h-64 items-center justify-center text-muted-foreground">Carregando histórico...</div> : historyQuery.isError ? <div role="alert" className="flex h-48 flex-col items-center justify-center gap-3 text-center text-sm text-destructive">Não foi possível carregar o histórico de preços.<Button variant="outline" size="sm" onClick={() => void historyQuery.refetch()}>Tentar novamente</Button></div> : chartData.length < 2 ? <div role="status" className="flex h-48 items-center justify-center text-center text-sm text-muted-foreground">A série de preços não está disponível para este período. Verifique o acesso a cotações ou registre valores de extrato para renda fixa.</div> :
            <div className="h-64 sm:h-80"><ResponsiveContainer width="100%" height="100%"><AreaChart data={chartData}><defs><linearGradient id="positionPrice" x1="0" y1="0" x2="0" y2="1"><stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.3} /><stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} /></linearGradient></defs><CartesianGrid strokeDasharray="3 3" className="stroke-muted" /><XAxis dataKey="date" tick={{ fontSize: 11 }} /><YAxis domain={["auto", "auto"]} tickFormatter={(value) => money(Number(value))} width={90} tick={{ fontSize: 11 }} /><Tooltip formatter={(value: number) => [money(value), "Preço unitário"]} /><Area type="monotone" dataKey="price" name="Preço unitário" stroke="hsl(var(--primary))" fill="url(#positionPrice)" connectNulls={false} /></AreaChart></ResponsiveContainer></div>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Movimentações da posição</CardTitle></CardHeader>
        <CardContent>
          {isLoadingTransactions ? <p className="py-8 text-center text-sm text-muted-foreground">Carregando movimentações...</p> : isTransactionsError ? <div role="alert" className="flex flex-col items-center gap-3 py-8 text-center text-sm text-destructive">Não foi possível carregar as movimentações.<Button variant="outline" size="sm" onClick={() => void refetchTransactions()}>Tentar novamente</Button></div> : transactions.length === 0 ? <p role="status" className="py-8 text-center text-sm text-muted-foreground">Nenhuma movimentação encontrada.</p> : <div className="overflow-x-auto"><Table><TableHeader><TableRow><TableHead>Data</TableHead><TableHead>Tipo</TableHead><TableHead>Modalidade fiscal</TableHead><TableHead className="text-right">Quantidade</TableHead><TableHead className="text-right">Preço unitário</TableHead><TableHead className="text-right">Total</TableHead></TableRow></TableHeader><TableBody>{transactions.map((item) => <TableRow key={item.id}><TableCell>{dateLabel(item.dataTransacao)}</TableCell><TableCell><Badge variant={item.tipo === "Buy" ? "default" : "destructive"}>{item.tipo === "Buy" ? "Compra" : "Venda"}</Badge></TableCell><TableCell>{item.tipo === "Buy" ? "—" : item.modalidadeFiscal === "Comum" ? "Comum" : item.modalidadeFiscal === "DayTrade" ? "Day trade" : "Não informada"}</TableCell><TableCell className="text-right">{item.quantidade}</TableCell><TableCell className="text-right">{money(item.precoUnitario)}</TableCell><TableCell className="text-right">{money(item.valorTotal)}</TableCell></TableRow>)}</TableBody></Table></div>}
        </CardContent>
      </Card>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent><DialogHeader><DialogTitle>{action === "Buy" ? "Registrar compra" : "Registrar venda"} · {asset.ticker}</DialogTitle></DialogHeader>
          <div className="space-y-4 pt-2">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2"><Label htmlFor="trade-quantity">Quantidade</Label><Input id="trade-quantity" type="number" min="0.00000001" step="any" value={quantity} onChange={(event) => setQuantity(event.target.value)} /></div>
              <div className="space-y-2"><Label htmlFor="trade-price">Preço por unidade</Label><Input id="trade-price" type="number" min="0.00000001" step="any" value={unitPrice} onChange={(event) => setUnitPrice(event.target.value)} /></div>
              <div className="space-y-2"><Label htmlFor="trade-fees">Taxas</Label><Input id="trade-fees" type="number" min="0" step="0.01" value={fees} onChange={(event) => setFees(event.target.value)} /></div>
              <div className="space-y-2"><Label htmlFor="trade-date">Data da operação</Label><Input id="trade-date" type="date" value={transactionDate} onChange={(event) => setTransactionDate(event.target.value)} /></div>
            </div>
            {quantity && unitPrice && <div className="rounded-lg bg-muted p-3 text-sm">Total estimado: <strong>{money(Number(quantity) * Number(unitPrice) + (action === "Buy" ? Number(fees || 0) : -Number(fees || 0)))}</strong></div>}
            {action === "Sell" && <p className="text-xs text-muted-foreground">Disponível para venda: {asset.quantidade} unidades.</p>}
            {action === "Sell" && requiresTaxModality && <div className="space-y-2"><Label htmlFor="trade-tax-modality">Modalidade fiscal (obrigatória)</Label><select id="trade-tax-modality" required aria-required="true" className="h-10 w-full rounded-md border bg-background px-3 text-sm" value={taxModality} onChange={event => setTaxModality(event.target.value as "" | "Comum" | "DayTrade")}><option value="">Selecione para incluir na estimativa</option><option value="Comum">Operação comum</option><option value="DayTrade">Day trade</option></select><p className="text-xs text-muted-foreground">Esse dado alimenta a estimativa mensal de IR; a classificação é sua.</p></div>}
            <Button className="w-full" variant={action === "Buy" ? "success" : "destructive"} onClick={submitTransaction} disabled={createTransaction.isPending || !Number(quantity) || !Number(unitPrice) || Number(fees) < 0 || !transactionDate || (action === "Sell" && Number(quantity) > asset.quantidade) || (action === "Sell" && requiresTaxModality && !taxModality)}>{createTransaction.isPending ? "Salvando..." : action === "Buy" ? "Confirmar compra" : action === "Sell" ? (isFixedIncome ? "Registrar resgate" : "Confirmar venda") : "Confirmar venda"}</Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}

function Metric({ title, value, icon, detail, valueClass }: { title: string; value: string; icon: React.ReactNode; detail: string; valueClass?: string }) {
  return <Card><CardHeader className="flex flex-row items-center justify-between pb-2"><CardTitle className="text-xs font-medium text-muted-foreground sm:text-sm">{title}</CardTitle><span className="text-muted-foreground">{icon}</span></CardHeader><CardContent><p className={`text-lg font-bold sm:text-2xl ${valueClass ?? ""}`}>{value}</p><p className="mt-1 truncate text-[10px] text-muted-foreground sm:text-xs">{detail}</p></CardContent></Card>
}
