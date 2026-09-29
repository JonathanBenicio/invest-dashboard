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
import type { RegistrarTransacaoRequest } from "@/api/dtos/transacao.dto"
import { formatCurrency } from "@/lib/utils"
import { ArrowLeft, BarChart3, CalendarDays, MinusCircle, PlusCircle, TrendingDown, TrendingUp, Wallet } from "lucide-react"
import { toast } from "sonner"
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"

type TransactionAction = "Buy" | "Sell"

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
  const [transactionDate, setTransactionDate] = useState(new Date().toISOString().slice(0, 10))

  const { data: investmentResponse, isLoading, isError } = useInvestment(id)
  const { data: transactionsResponse } = useInvestmentTransactions(id)
  const storedAsset = investmentResponse?.data
  const quoteTickers = useMemo(
    () => storedAsset?.type === "variable_income" ? [storedAsset.ticker] : [],
    [storedAsset?.ticker, storedAsset?.type],
  )
  const { quotesBySymbol, isLoading: isLoadingQuote } = useMarketQuotes(quoteTickers)
  const quote = storedAsset ? quotesBySymbol.get(storedAsset.ticker.toUpperCase()) : undefined
  const asset = useMemo(() => {
    if (!storedAsset || !quote) return storedAsset

    const currentValue = storedAsset.quantity * quote.price
    const gain = currentValue - storedAsset.totalInvested
    return {
      ...storedAsset,
      currentPrice: quote.price,
      currentValue,
      gain,
      gainPercentage: storedAsset.totalInvested > 0 ? (gain / storedAsset.totalInvested) * 100 : 0,
    }
  }, [quote, storedAsset])
  const transactions = transactionsResponse?.data ?? []

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
    queryFn: async () => {
      if (asset?.type === "fixed_income") {
        const response = await investmentService.getPriceHistory(id, `${fromDate}T00:00:00.000Z`)
        return response.data
      }

      const response = await marketDataService.getDailyHistory(
        [asset!.ticker],
        fromDate,
        new Date().toISOString().slice(0, 10),
      )
      return response.data.map(point => ({
        date: point.dateUtc,
        price: point.price,
        source: point.source,
        isAdjusted: point.isAdjusted,
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
    if (asset && !unitPrice) setUnitPrice(String(asset.currentPrice))
  }, [asset, unitPrice])

  const priceHistory = historyQuery.data ?? []
  const chartData = priceHistory.map((point) => ({
    date: new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "short" }).format(new Date(point.date)),
    price: point.price,
  }))

  const submitTransaction = () => {
    if (!asset) return
    const request: RegistrarTransacaoRequest = {
      portfolioId: asset.portfolioId,
      assetId: asset.assetId,
      ticker: asset.ticker,
      type: action,
      quantity: Number(quantity),
      unitPrice: Number(unitPrice),
      fees: Number(fees || 0),
      transactionDate: new Date(`${transactionDate}T12:00:00.000Z`).toISOString(),
      idempotencyKey: crypto.randomUUID(),
      assetClass: asset.type === "fixed_income" ? "RENDA_FIXA" : asset.subtype,
    }
    createTransaction.mutate(request)
  }

  const openTransactionDialog = (nextAction: TransactionAction) => {
    setAction(nextAction)
    setQuantity("")
    setFees("0")
    setUnitPrice(String(asset?.currentPrice ?? ""))
    setDialogOpen(true)
  }

  if (isLoading) return <div className="flex h-96 items-center justify-center text-muted-foreground">Carregando posição...</div>
  if (isError || !asset) {
    return <div className="space-y-4"><p className="text-muted-foreground">Posição não encontrada ou indisponível.</p><Button variant="outline" onClick={() => window.history.back()}><ArrowLeft className="mr-2 h-4 w-4" />Voltar</Button></div>
  }

  const isFixedIncome = asset.type === "fixed_income"
  const gainClass = asset.gain >= 0 ? "text-success" : "text-destructive"

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-start gap-3">
          <Button variant="ghost" size="icon" onClick={() => window.history.back()} aria-label="Voltar"><ArrowLeft className="h-5 w-5" /></Button>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2"><h1 className="text-2xl font-bold">{asset.name || asset.ticker}</h1><Badge variant="secondary">{asset.subtype}</Badge><Badge variant={asset.status === "open" ? "default" : "outline"}>{asset.status === "open" ? "Em carteira" : "Encerrada"}</Badge></div>
            <p className="text-sm text-muted-foreground">{asset.ticker}{asset.issuer ? ` · ${asset.issuer}` : ""}</p>
          </div>
        </div>
        {asset.status === "open" && <div className="flex gap-2 sm:ml-auto">
          <Button variant="success" size="sm" onClick={() => openTransactionDialog("Buy")}><PlusCircle className="mr-2 h-4 w-4" />Comprar</Button>
          <Button variant="destructive" size="sm" onClick={() => openTransactionDialog("Sell")}><MinusCircle className="mr-2 h-4 w-4" />Vender</Button>
        </div>}
      </div>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Metric title="Valor da posição" value={money(asset.currentValue)} icon={<Wallet className="h-4 w-4" />} detail={`${asset.quantity} unidades`} />
        <Metric title="Custo da posição" value={money(asset.totalInvested)} icon={<BarChart3 className="h-4 w-4" />} detail={`Preço médio ${money(asset.averagePrice)}`} />
        <Metric title={isFixedIncome ? "Valor por unidade" : "Cotação atual"} value={money(asset.currentPrice)} icon={<CalendarDays className="h-4 w-4" />} detail={isFixedIncome ? "Atualizado por extrato" : quote ? formatQuoteObservation(quote.source.toUpperCase(), quote.observedAtUtc) : isLoadingQuote ? "Buscando cotação Brapi..." : "Cotação Brapi indisponível; último preço salvo"} />
        <Metric title="Resultado não realizado" value={money(asset.gain)} icon={asset.gain >= 0 ? <TrendingUp className="h-4 w-4" /> : <TrendingDown className="h-4 w-4" />} detail={`${asset.gainPercentage.toFixed(2)}%`} valueClass={gainClass} />
      </div>

      <Card>
        <CardHeader className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><div><CardTitle>Histórico de preço</CardTitle><p className="text-sm text-muted-foreground">Série armazenada para este ativo; valores em BRL por unidade.</p></div><ChartPeriodFilter value={period} onChange={setPeriod} /></CardHeader>
        <CardContent>
          {historyQuery.isLoading ? <div className="flex h-64 items-center justify-center text-muted-foreground">Carregando histórico...</div> : chartData.length < 2 ? <div className="flex h-48 items-center justify-center text-center text-sm text-muted-foreground">A série de preços não está disponível para este período. Verifique o acesso a cotações ou registre valores de extrato para renda fixa.</div> :
            <div className="h-64 sm:h-80"><ResponsiveContainer width="100%" height="100%"><AreaChart data={chartData}><defs><linearGradient id="positionPrice" x1="0" y1="0" x2="0" y2="1"><stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.3} /><stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} /></linearGradient></defs><CartesianGrid strokeDasharray="3 3" className="stroke-muted" /><XAxis dataKey="date" tick={{ fontSize: 11 }} /><YAxis domain={["auto", "auto"]} tickFormatter={(value) => money(Number(value))} width={90} tick={{ fontSize: 11 }} /><Tooltip formatter={(value: number) => [money(value), "Preço unitário"]} /><Area type="monotone" dataKey="price" name="Preço unitário" stroke="hsl(var(--primary))" fill="url(#positionPrice)" connectNulls={false} /></AreaChart></ResponsiveContainer></div>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Movimentações da posição</CardTitle></CardHeader>
        <CardContent>
          {transactions.length === 0 ? <p className="py-8 text-center text-sm text-muted-foreground">Nenhuma movimentação encontrada.</p> : <div className="overflow-x-auto"><Table><TableHeader><TableRow><TableHead>Data</TableHead><TableHead>Tipo</TableHead><TableHead className="text-right">Quantidade</TableHead><TableHead className="text-right">Preço unitário</TableHead><TableHead className="text-right">Total</TableHead></TableRow></TableHeader><TableBody>{transactions.map((item) => <TableRow key={item.id}><TableCell>{dateLabel(item.transactionDate)}</TableCell><TableCell><Badge variant={item.type === "Buy" ? "default" : "destructive"}>{item.type === "Buy" ? "Compra" : "Venda"}</Badge></TableCell><TableCell className="text-right">{item.quantity}</TableCell><TableCell className="text-right">{money(item.unitPrice)}</TableCell><TableCell className="text-right">{money(item.totalAmount)}</TableCell></TableRow>)}</TableBody></Table></div>}
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
            {action === "Sell" && <p className="text-xs text-muted-foreground">Disponível para venda: {asset.quantity} unidades.</p>}
            <Button className="w-full" variant={action === "Buy" ? "success" : "destructive"} onClick={submitTransaction} disabled={createTransaction.isPending || !Number(quantity) || !Number(unitPrice) || Number(fees) < 0 || !transactionDate || (action === "Sell" && Number(quantity) > asset.quantity)}>{createTransaction.isPending ? "Salvando..." : action === "Buy" ? "Confirmar compra" : "Confirmar venda"}</Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}

function Metric({ title, value, icon, detail, valueClass }: { title: string; value: string; icon: React.ReactNode; detail: string; valueClass?: string }) {
  return <Card><CardHeader className="flex flex-row items-center justify-between pb-2"><CardTitle className="text-xs font-medium text-muted-foreground sm:text-sm">{title}</CardTitle><span className="text-muted-foreground">{icon}</span></CardHeader><CardContent><p className={`text-lg font-bold sm:text-2xl ${valueClass ?? ""}`}>{value}</p><p className="mt-1 truncate text-[10px] text-muted-foreground sm:text-xs">{detail}</p></CardContent></Card>
}
