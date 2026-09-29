import { useMemo, useState } from "react"
import { useQuery } from "@tanstack/react-query"
import { Award, PieChart as PieChartIcon, TrendingDown, TrendingUp, Wallet } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useMarketQuotes } from "@/hooks/use-market-quotes"
import { usePortfolio, usePortfolios } from "@/hooks/use-portfolios"
import { portfolioService } from "@/api/services/portfolio.service"
import { formatCurrency } from "@/lib/utils"
import {
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts"

type HistoryPeriod = "1m" | "3m" | "6m" | "1y" | "all"

const sectorColors = [
  "hsl(220, 70%, 50%)",
  "hsl(145, 65%, 42%)",
  "hsl(38, 92%, 50%)",
  "hsl(200, 85%, 50%)",
  "hsl(0, 72%, 51%)",
  "hsl(165, 55%, 38%)",
]

function getHistoryStart(period: HistoryPeriod): string {
  const date = new Date()
  if (period === "1m") date.setMonth(date.getMonth() - 1)
  else if (period === "3m") date.setMonth(date.getMonth() - 3)
  else if (period === "6m") date.setMonth(date.getMonth() - 6)
  else if (period === "1y") date.setFullYear(date.getFullYear() - 1)
  else date.setDate(date.getDate() - 3650)
  return date.toISOString().slice(0, 10)
}

export default function Analysis() {
  const [period, setPeriod] = useState<HistoryPeriod>("6m")
  const [selectedPortfolioId, setSelectedPortfolioId] = useState("")
  const { data: portfoliosResponse, isLoading: isLoadingPortfolios, isError: portfoliosError } = usePortfolios({ page: 1, pageSize: 100 })
  const portfolios = portfoliosResponse?.data ?? []
  const portfolioId = selectedPortfolioId || portfolios[0]?.id || ""
  const { data: portfolioResponse, isLoading: isLoadingPortfolio, isError: portfolioError } = usePortfolio(portfolioId)
  const portfolio = portfolioResponse?.data

  const quoteSymbols = useMemo(
    () => portfolio?.positions.filter(position => position.status === "open" && position.type === "variable_income").map(position => position.ticker) ?? [],
    [portfolio?.positions],
  )
  const { quotesBySymbol, unavailableSymbols, isLoading: isLoadingQuotes } = useMarketQuotes(quoteSymbols)

  const historyRange = useMemo(() => ({
    fromDate: getHistoryStart(period),
    toDate: new Date().toISOString().slice(0, 10),
  }), [period])
  const historyQuery = useQuery({
    queryKey: ["portfolio-history", portfolioId, historyRange],
    queryFn: () => portfolioService.getHistory(portfolioId, historyRange.fromDate, historyRange.toDate),
    enabled: Boolean(portfolioId),
  })

  const positions = useMemo(() => (portfolio?.positions ?? [])
    .filter(position => position.status === "open")
    .map(position => {
      const quote = position.type === "variable_income"
        ? quotesBySymbol.get(position.ticker.toUpperCase())
        : undefined
      if (!quote) return position

      const currentValue = position.quantity * quote.price
      const gain = currentValue - position.totalInvested
      return {
        ...position,
        currentPrice: quote.price,
        currentValue,
        gain,
        gainPercentage: position.totalInvested > 0 ? gain / position.totalInvested * 100 : 0,
      }
    }), [portfolio?.positions, quotesBySymbol])

  const rankedPositions = [...positions].sort((left, right) => right.gainPercentage - left.gainPercentage)
  const bestPosition = rankedPositions[0]
  const worstPosition = rankedPositions[rankedPositions.length - 1]
  const totalInvested = positions.reduce((total, position) => total + position.totalInvested, 0)
  const currentValue = positions.reduce((total, position) => total + position.currentValue, 0)
  const totalGain = positions.reduce((total, position) => total + position.gain, 0)
  const gainPercentage = totalInvested > 0 ? totalGain / totalInvested * 100 : 0

  const history = historyQuery.data?.data ?? []
  const completeHistory = history
    .filter(point => point.isComplete && point.totalValue !== null)
    .map(point => ({ date: point.date, value: point.totalValue as number }))
  const hasIncompleteHistory = history.some(point => !point.isComplete)

  const allocation = positions.reduce<Array<{ name: string; value: number }>>((groups, position) => {
    const name = position.type === "fixed_income" ? "Renda fixa" : position.sector || "Sem setor"
    const existing = groups.find(group => group.name === name)
    if (existing) existing.value += position.currentValue
    else groups.push({ name, value: position.currentValue })
    return groups
  }, []).sort((left, right) => right.value - left.value)

  const isLoading = isLoadingPortfolios || (Boolean(portfolioId) && isLoadingPortfolio)
  const hasError = portfoliosError || portfolioError

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Análise da carteira</h1>
          <p className="text-muted-foreground">Valores calculados a partir das posições e cotações carregadas pela API.</p>
        </div>
        <Select value={portfolioId} onValueChange={setSelectedPortfolioId} disabled={portfolios.length === 0}>
          <SelectTrigger className="w-full sm:w-[240px]" aria-label="Selecionar carteira">
            <SelectValue placeholder="Selecione a carteira" />
          </SelectTrigger>
          <SelectContent>
            {portfolios.map(item => <SelectItem key={item.id} value={item.id}>{item.name}</SelectItem>)}
          </SelectContent>
        </Select>
      </div>

      {isLoading ? <p className="py-12 text-center text-muted-foreground">Carregando dados da carteira...</p> : null}
      {hasError ? <p role="alert" className="rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">Não foi possível carregar os dados da carteira. Tente novamente.</p> : null}
      {!isLoading && !hasError && portfolios.length === 0 ? <p className="py-12 text-center text-muted-foreground">Nenhuma carteira foi encontrada.</p> : null}

      {portfolio && !isLoading && !hasError && (
        <>
          {unavailableSymbols.length > 0 && !isLoadingQuotes && (
            <p role="status" className="rounded-md border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm text-amber-800 dark:text-amber-200">
              Cotação indisponível para {unavailableSymbols.join(", ")}. A análise mantém os últimos valores salvos.
            </p>
          )}

          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Metric title="Patrimônio em posições abertas" value={formatCurrency(currentValue)} icon={<Wallet className="h-4 w-4" />} detail={`${positions.length} posições`} />
            <Metric title="Resultado não realizado" value={formatCurrency(totalGain)} icon={totalGain >= 0 ? <TrendingUp className="h-4 w-4" /> : <TrendingDown className="h-4 w-4" />} detail={`${gainPercentage >= 0 ? "+" : ""}${gainPercentage.toFixed(2)}% sobre o custo`} valueClass={totalGain >= 0 ? "text-success" : "text-destructive"} />
            <Metric title="Maior alta" value={bestPosition?.ticker ?? "—"} icon={<Award className="h-4 w-4" />} detail={bestPosition ? `${bestPosition.gainPercentage >= 0 ? "+" : ""}${bestPosition.gainPercentage.toFixed(2)}%` : "Sem posições abertas"} />
            <Metric title="Maior baixa" value={worstPosition?.ticker ?? "—"} icon={<TrendingDown className="h-4 w-4" />} detail={worstPosition ? `${worstPosition.gainPercentage >= 0 ? "+" : ""}${worstPosition.gainPercentage.toFixed(2)}%` : "Sem posições abertas"} />
          </div>

          <Tabs defaultValue="performance" className="space-y-4">
            <TabsList>
              <TabsTrigger value="performance">Histórico</TabsTrigger>
              <TabsTrigger value="sectors">Alocação</TabsTrigger>
              <TabsTrigger value="assets">Por posição</TabsTrigger>
            </TabsList>

            <TabsContent value="performance">
              <Card>
                <CardHeader className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <CardTitle>Evolução patrimonial</CardTitle>
                    <CardDescription>Somente dias em que o backend confirmou valor para todas as posições.</CardDescription>
                  </div>
                  <Select value={period} onValueChange={value => setPeriod(value as HistoryPeriod)}>
                    <SelectTrigger className="w-[150px]" aria-label="Período do histórico"><SelectValue /></SelectTrigger>
                    <SelectContent>
                      <SelectItem value="1m">1 mês</SelectItem>
                      <SelectItem value="3m">3 meses</SelectItem>
                      <SelectItem value="6m">6 meses</SelectItem>
                      <SelectItem value="1y">1 ano</SelectItem>
                      <SelectItem value="all">Até 10 anos</SelectItem>
                    </SelectContent>
                  </Select>
                </CardHeader>
                <CardContent>
                  {historyQuery.isLoading ? <p className="py-16 text-center text-sm text-muted-foreground">Carregando histórico...</p>
                    : historyQuery.isError ? <p role="alert" className="py-16 text-center text-sm text-destructive">Não foi possível carregar o histórico.</p>
                      : completeHistory.length < 2 ? <p className="py-16 text-center text-sm text-muted-foreground">Ainda não há pontos completos suficientes para traçar o histórico.</p>
                        : <div className="h-[320px]">
                          {hasIncompleteHistory && <p role="status" className="mb-3 text-xs text-amber-700 dark:text-amber-300">Dias incompletos foram omitidos porque faltam preços para algumas posições.</p>}
                          <ResponsiveContainer width="100%" height="100%">
                            <LineChart data={completeHistory}>
                              <XAxis dataKey="date" tickFormatter={value => new Date(value).toLocaleDateString("pt-BR")} minTickGap={32} />
                              <YAxis tickFormatter={value => formatCurrency(Number(value))} width={105} />
                              <Tooltip labelFormatter={value => new Date(String(value)).toLocaleDateString("pt-BR")} formatter={(value: number) => formatCurrency(value)} />
                              <Line type="monotone" dataKey="value" name="Patrimônio" stroke="hsl(var(--primary))" strokeWidth={2} dot={false} connectNulls={false} />
                            </LineChart>
                          </ResponsiveContainer>
                        </div>}
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="sectors">
              <Card>
                <CardHeader><CardTitle>Distribuição das posições</CardTitle><CardDescription>Participação por setor e classe de ativo, com base nos valores atuais.</CardDescription></CardHeader>
                <CardContent>
                  {allocation.length === 0 ? <p className="py-12 text-center text-sm text-muted-foreground">Não há posições abertas para distribuir.</p> : <div className="h-[320px]">
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie data={allocation} dataKey="value" nameKey="name" innerRadius={55} outerRadius={105} paddingAngle={2}>
                          {allocation.map((item, index) => <Cell key={item.name} fill={sectorColors[index % sectorColors.length]} />)}
                        </Pie>
                        <Tooltip formatter={(value: number) => formatCurrency(value)} />
                        <Legend />
                      </PieChart>
                    </ResponsiveContainer>
                  </div>}
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="assets">
              <Card>
                <CardHeader><CardTitle>Resultado por posição</CardTitle><CardDescription>Ordenado pelo resultado percentual atual.</CardDescription></CardHeader>
                <CardContent>
                  {rankedPositions.length === 0 ? <p className="py-12 text-center text-sm text-muted-foreground">Não há posições abertas nesta carteira.</p> : <div className="space-y-3">
                    {rankedPositions.map(position => <div key={position.id} className="flex items-center gap-4 rounded-lg border p-3">
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-2"><span className="font-medium">{position.ticker}</span><Badge variant="outline">{position.subtype}</Badge></div>
                        <p className="truncate text-xs text-muted-foreground">{position.name} · {formatCurrency(position.currentValue)}</p>
                      </div>
                      <span className={`text-sm font-semibold ${position.gain >= 0 ? "text-success" : "text-destructive"}`}>
                        {position.gainPercentage >= 0 ? "+" : ""}{position.gainPercentage.toFixed(2)}%
                      </span>
                    </div>)}
                  </div>}
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        </>
      )}
    </div>
  )
}

function Metric({ title, value, icon, detail, valueClass }: { title: string; value: string; icon: React.ReactNode; detail: string; valueClass?: string }) {
  return <Card>
    <CardHeader className="flex flex-row items-center justify-between pb-2"><CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>{icon}</CardHeader>
    <CardContent><p className={`text-xl font-bold ${valueClass ?? ""}`}>{value}</p><p className="mt-1 text-xs text-muted-foreground">{detail}</p></CardContent>
  </Card>
}
