import { useState, useEffect, useMemo } from "react"
import { useNavigate } from "@tanstack/react-router"
import { investmentDetailsRoute } from "../../router"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  ArrowLeft,
  TrendingUp,
  TrendingDown,
  DollarSign,
  BarChart3,
  PlusCircle,
  MinusCircle,
  Wallet,
  Target,
  Calendar,
} from "lucide-react"
import {
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Area,
  AreaChart,
  BarChart,
  Bar,
  ComposedChart,
  Line,
  Legend,
  ReferenceLine,
} from "recharts"
import {
  formatCurrency,
  formatPercentage,
  formatDate,
} from "@/lib/mock-data"
import { useInvestment, useInvestmentTransactions } from "@/hooks/use-investment-details"
import { FixedIncomeDto, VariableIncomeDto } from "@/api/dtos"
import { ChartPeriodFilter, ChartPeriod, generatePeriodData } from "@/components/ChartPeriodFilter"

// Generate monthly profitability data
const generateMonthlyProfitability = () => {
  const months = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez']
  return months.map((month) => ({
    month,
    profitability: (Math.random() - 0.3) * 8, // -2.4% to 5.6%
  }))
}

// Generate contributions vs value data
const generateContributionsVsValue = (): { month: string; invested: number; value: number }[] => {
  const data: { month: string; invested: number; value: number }[] = []
  let invested = 0
  let value = 0
  const months = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez']
  
  for (let i = 0; i < 12; i++) {
    const contribution = Math.random() > 0.3 ? Math.floor(Math.random() * 5000) + 1000 : 0
    invested += contribution
    const growth = 1 + (Math.random() - 0.3) * 0.05
    value = (value + contribution) * growth
    
    data.push({
      month: months[i],
      invested: Math.round(invested),
      value: Math.round(value),
    })
  }
  return data
}

// Generate dividend history data
const generateDividendHistory = (): { month: string; dividends: number }[] => {
  const months = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez']
  return months.map((month) => ({
    month,
    dividends: Math.floor(Math.random() * 500) + 50,
  }))
}

// Generate projection data for fixed income
const generateProjectionData = (currentValue: number, maturityDate: string, annualRate: number): { date: string; value: number }[] => {
  const data: { date: string; value: number }[] = []
  const now = new Date()
  const maturity = new Date(maturityDate)
  const monthsRemaining = Math.max(1, Math.ceil((maturity.getTime() - now.getTime()) / (1000 * 60 * 60 * 24 * 30)))
  const monthlyRate = Math.pow(1 + annualRate, 1/12) - 1
  
  let projectedValue = currentValue
  for (let i = 0; i <= Math.min(monthsRemaining, 24); i++) {
    const date = new Date(now)
    date.setMonth(date.getMonth() + i)
    
    data.push({
      date: date.toLocaleDateString('pt-BR', { month: 'short', year: '2-digit' }),
      value: Math.round(projectedValue),
    })
    
    projectedValue *= (1 + monthlyRate)
  }
  return data
}

export default function InvestmentDetails() {
  const { id } = investmentDetailsRoute.useParams()
  const search = investmentDetailsRoute.useSearch()
  const navigate = useNavigate()
  const type = search.type || "variable"

  const { data: investmentResponse, isLoading } = useInvestment(id)
  const { data: transactionsResponse } = useInvestmentTransactions(id)

  const asset = investmentResponse?.data
  const transactions = transactionsResponse?.data || []

  const [transactionType, setTransactionType] = useState<"buy" | "sell">("buy")
  const [quantity, setQuantity] = useState("")
  const [price, setPrice] = useState("")
  const [dialogOpen, setDialogOpen] = useState(false)
  const [chartPeriod, setChartPeriod] = useState<ChartPeriod>('30d')

  // Handle initial action from URL
  useEffect(() => {
    const action = search.action
    if (action === "buy" || action === "sell") {
      setTransactionType(action)
      setDialogOpen(true)
    }
  }, [search.action])

  // Memoized chart data
  const evolutionData = useMemo(() => {
    if (!asset) return []
    return generatePeriodData(chartPeriod, asset.currentValue)
  }, [chartPeriod, asset?.currentValue])

  const monthlyProfitability = useMemo(() => generateMonthlyProfitability(), [])
  const contributionsVsValue = useMemo(() => generateContributionsVsValue(), [])
  const dividendHistory = useMemo(() => generateDividendHistory(), [])

  if (isLoading) {
    return <div className="flex items-center justify-center h-96">Carregando detalhes...</div>
  }

  if (!asset) {
    return (
      <div className="flex flex-col items-center justify-center h-96">
        <p className="text-muted-foreground">Ativo não encontrado</p>
        <Button variant="outline" onClick={() => window.history.back()} className="mt-4">
          <ArrowLeft className="mr-2 h-4 w-4" /> Voltar
        </Button>
      </div>
    )
  }

  const isVariable = asset.type === "variable_income"
  const variableAsset = isVariable ? asset as VariableIncomeDto : null
  const fixedAsset = !isVariable ? asset as FixedIncomeDto : null

  // Helper to get display name
  const displayName = isVariable ? variableAsset?.ticker : fixedAsset?.name
  const displayType = asset.subtype
  const displayInstitution = isVariable ? variableAsset?.name : fixedAsset?.issuer

  // Values
  const totalValue = asset.currentValue
  const profit = asset.gain
  const profitPercentage = asset.gainPercentage

  // Calculate projected value at maturity for fixed income
  const calculateProjectedValue = () => {
    if (!fixedAsset) return null

    const today = new Date()
    if (!fixedAsset.maturityDate || !fixedAsset.purchaseDate) return null

    const maturity = new Date(fixedAsset.maturityDate)
    const purchase = new Date(fixedAsset.purchaseDate)

    const daysRemaining = Math.max(0, Math.ceil((maturity.getTime() - today.getTime()) / (1000 * 60 * 60 * 24)))
    const yearsRemaining = daysRemaining / 365

    let annualRate = 0
    const CDI_RATE = 0.1215
    const IPCA_RATE = 0.045

    const rateStr = fixedAsset.interestRate?.toString() || '0'

    if (fixedAsset.indexer === 'CDI') {
      const cdiPercentage = parseFloat(rateStr) / 100
      annualRate = CDI_RATE * cdiPercentage
    } else if (fixedAsset.indexer === 'IPCA') {
      annualRate = IPCA_RATE + (parseFloat(rateStr) / 100)
    } else {
      annualRate = parseFloat(rateStr) / 100
    }

    const projectedValue = fixedAsset.currentValue * Math.pow(1 + annualRate, yearsRemaining)
    const projectedProfit = projectedValue - fixedAsset.totalInvested
    const projectedProfitPercentage = (projectedProfit / fixedAsset.totalInvested) * 100

    return {
      projectedValue,
      projectedProfit,
      projectedProfitPercentage,
      daysRemaining,
      maturityDate: fixedAsset.maturityDate,
      annualRate,
    }
  }

  const projection = calculateProjectedValue()
  const projectionChartData = projection && fixedAsset 
    ? generateProjectionData(fixedAsset.currentValue, fixedAsset.maturityDate, projection.annualRate)
    : []

  const handleTransaction = () => {
    console.log({
      type: transactionType,
      quantity: Number(quantity),
      price: Number(price),
      total: Number(quantity) * Number(price),
    })
    setDialogOpen(false)
    setQuantity("")
    setPrice("")
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" onClick={() => window.history.back()}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-2xl font-bold">
                {displayName}
              </h1>
              <Badge variant={isVariable ? "default" : "secondary"}>
                {displayType}
              </Badge>
            </div>
            <p className="text-muted-foreground">
              {displayInstitution}
            </p>
          </div>
        </div>

        {/* Transaction Buttons */}
        <div className="flex gap-2">
          <Button
            variant="success"
            onClick={() => {
              setTransactionType("buy")
              setDialogOpen(true)
            }}
          >
            <PlusCircle className="mr-2 h-4 w-4" />
            {isVariable ? "Comprar" : "Aportar"}
          </Button>
          <Button
            variant="destructive"
            onClick={() => {
              setTransactionType("sell")
              setDialogOpen(true)
            }}
          >
            <MinusCircle className="mr-2 h-4 w-4" />
            {isVariable ? "Vender" : "Resgatar"}
          </Button>

          <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>
                  {transactionType === "buy"
                    ? (isVariable ? "Registrar Compra" : "Registrar Aporte")
                    : (isVariable ? "Registrar Venda" : "Registrar Resgate")
                  }
                </DialogTitle>
              </DialogHeader>
              <div className="space-y-4 pt-4">
                {isVariable ? (
                  <>
                    <div className="space-y-2">
                      <Label htmlFor="quantity">Quantidade</Label>
                      <Input
                        id="quantity"
                        type="number"
                        placeholder="Ex: 100"
                        value={quantity}
                        onChange={(e) => setQuantity(e.target.value)}
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="price">Preço por unidade</Label>
                      <Input
                        id="price"
                        type="number"
                        step="0.01"
                        placeholder="Ex: 35.50"
                        value={price}
                        onChange={(e) => setPrice(e.target.value)}
                      />
                    </div>
                    {quantity && price && (
                      <div className="p-4 bg-muted rounded-lg">
                        <p className="text-sm text-muted-foreground">Total da operação</p>
                        <p className="text-xl font-bold">
                          {formatCurrency(Number(quantity) * Number(price))}
                        </p>
                      </div>
                    )}
                  </>
                ) : (
                  <>
                    {transactionType === "sell" && (
                      <div className="space-y-2">
                        <Label htmlFor="fixedQuantity">Quantidade (Opcional)</Label>
                        <Input
                          id="fixedQuantity"
                          type="number"
                          placeholder="Ex: 1"
                          value={quantity}
                          onChange={(e) => setQuantity(e.target.value)}
                        />
                      </div>
                    )}
                    <div className="space-y-2">
                      <Label htmlFor="amount">Valor do {transactionType === "buy" ? "Aporte" : "Resgate"}</Label>
                      <Input
                        id="amount"
                        type="number"
                        step="0.01"
                        placeholder="Ex: 5000.00"
                        value={price}
                        onChange={(e) => setPrice(e.target.value)}
                      />
                      {price && (
                        <div className="p-4 bg-muted rounded-lg mt-4">
                          <p className="text-sm text-muted-foreground">
                            {transactionType === "buy" ? "Valor do aporte" : "Valor do resgate"}
                          </p>
                          <p className="text-xl font-bold">
                            {formatCurrency(Number(price))}
                          </p>
                        </div>
                      )}
                    </div>
                  </>
                )}
                <Button
                  className="w-full"
                  variant={transactionType === "buy" ? "success" : (transactionType === "sell" ? "destructive" : "default")}
                  onClick={handleTransaction}
                  disabled={isVariable ? (!quantity || !price) : !price}
                >
                  {transactionType === "buy"
                    ? (isVariable ? "Confirmar Compra" : "Confirmar Aporte")
                    : (isVariable ? "Confirmar Venda" : "Confirmar Resgate")
                  }
                </Button>
              </div>
            </DialogContent>
          </Dialog>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Posição Total
            </CardTitle>
            <Wallet className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{formatCurrency(totalValue)}</p>
            {isVariable && (
              <p className="text-xs text-muted-foreground">
                {asset.quantity} unidades
              </p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              {isVariable ? "Preço Médio" : "Valor Investido"}
            </CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">
              {isVariable
                ? formatCurrency(asset.averagePrice)
                : formatCurrency(asset.totalInvested)
              }
            </p>
            {isVariable && (
              <p className="text-xs text-muted-foreground">por unidade</p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Cotação Atual
            </CardTitle>
            <BarChart3 className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">
              {isVariable
                ? formatCurrency(asset.currentPrice)
                : `${fixedAsset?.interestRate}%`
              }
            </p>
            {!isVariable && (
              <p className="text-xs text-muted-foreground">
                {fixedAsset?.indexer}
              </p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Rentabilidade
            </CardTitle>
            {profit >= 0 ? (
              <TrendingUp className="h-4 w-4 text-success" />
            ) : (
              <TrendingDown className="h-4 w-4 text-destructive" />
            )}
          </CardHeader>
          <CardContent>
            <p className={`text-2xl font-bold ${profit >= 0 ? "text-success" : "text-destructive"}`}>
              {formatPercentage(profitPercentage)}
            </p>
            <p className={`text-xs ${profit >= 0 ? "text-success" : "text-destructive"}`}>
              {formatCurrency(profit)}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Fixed Income Projection Card */}
      {!isVariable && projection && (
        <Card className="border-primary/20 bg-gradient-to-r from-primary/5 to-transparent">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-lg flex items-center gap-2">
              <Target className="h-5 w-5 text-primary" />
              Projeção no Vencimento
            </CardTitle>
            <Badge variant="outline" className="flex items-center gap-1">
              <Calendar className="h-3 w-3" />
              {formatDate(projection.maturityDate)}
            </Badge>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <div>
                <p className="text-sm text-muted-foreground">Valor Projetado</p>
                <p className="text-2xl font-bold text-primary">
                  {formatCurrency(projection.projectedValue)}
                </p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Lucro Projetado</p>
                <p className="text-2xl font-bold text-success">
                  {formatCurrency(projection.projectedProfit)}
                </p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Rentabilidade Total</p>
                <p className="text-2xl font-bold text-success">
                  {formatPercentage(projection.projectedProfitPercentage)}
                </p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Dias até Vencimento</p>
                <p className="text-2xl font-bold">
                  {projection.daysRemaining}
                </p>
                <p className="text-xs text-muted-foreground">
                  ~{Math.round(projection.daysRemaining / 30)} meses
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Tabs: History & Charts */}
      <Tabs defaultValue="charts" className="space-y-4">
        <TabsList>
          <TabsTrigger value="charts">Gráficos</TabsTrigger>
          <TabsTrigger value="history">Histórico</TabsTrigger>
        </TabsList>

        <TabsContent value="charts" className="space-y-4">
          {/* Main Chart - Asset Evolution with Period Filter */}
          <Card>
            <CardHeader className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <CardTitle className="text-lg">Evolução do Ativo</CardTitle>
              <ChartPeriodFilter value={chartPeriod} onChange={setChartPeriod} />
            </CardHeader>
            <CardContent>
              <div className="h-[350px]">
                <ResponsiveContainer width="100%" height="100%">
                  <AreaChart data={evolutionData}>
                    <defs>
                      <linearGradient id="colorValue" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.3} />
                        <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} />
                      </linearGradient>
                      <linearGradient id="colorBenchmark" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="hsl(var(--muted-foreground))" stopOpacity={0.2} />
                        <stop offset="95%" stopColor="hsl(var(--muted-foreground))" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                    <XAxis dataKey="date" className="text-xs fill-muted-foreground" />
                    <YAxis 
                      className="text-xs fill-muted-foreground" 
                      tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`}
                    />
                    <Tooltip 
                      formatter={(value: number, name: string) => [
                        formatCurrency(value), 
                        name === 'value' ? 'Valor' : (isVariable ? 'Ibovespa' : 'CDI')
                      ]}
                    />
                    <Legend />
                    <Area 
                      type="monotone" 
                      dataKey="value" 
                      name="Valor" 
                      stroke="hsl(var(--primary))" 
                      strokeWidth={2} 
                      fill="url(#colorValue)" 
                    />
                    <Area 
                      type="monotone" 
                      dataKey="benchmark" 
                      name={isVariable ? "Ibovespa" : "CDI"}
                      stroke="hsl(var(--muted-foreground))" 
                      strokeWidth={1} 
                      strokeDasharray="5 5"
                      fill="url(#colorBenchmark)" 
                    />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
            </CardContent>
          </Card>

          {/* Secondary Charts Grid */}
          <div className="grid gap-4 lg:grid-cols-2">
            {/* Monthly Profitability Chart */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Rentabilidade Mensal</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="h-[280px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={monthlyProfitability}>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                      <XAxis dataKey="month" className="text-xs fill-muted-foreground" />
                      <YAxis 
                        className="text-xs fill-muted-foreground" 
                        tickFormatter={(v) => `${v.toFixed(1)}%`}
                      />
                      <Tooltip 
                        formatter={(value: number) => [`${value.toFixed(2)}%`, 'Rentabilidade']}
                      />
                      <ReferenceLine y={0} stroke="hsl(var(--muted-foreground))" />
                      <Bar 
                        dataKey="profitability" 
                        name="Rentabilidade"
                        fill="hsl(var(--primary))"
                        radius={[4, 4, 0, 0]}
                      >
                        {monthlyProfitability.map((entry, index) => (
                          <Bar 
                            key={`bar-${index}`}
                            dataKey="profitability"
                            fill={entry.profitability >= 0 ? 'hsl(var(--success))' : 'hsl(var(--destructive))'}
                          />
                        ))}
                      </Bar>
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>

            {/* Contributions vs Value Chart */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Aportes vs Valor Atual</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="h-[280px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <ComposedChart data={contributionsVsValue}>
                      <defs>
                        <linearGradient id="colorValueGradient" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="5%" stopColor="hsl(var(--success))" stopOpacity={0.3} />
                          <stop offset="95%" stopColor="hsl(var(--success))" stopOpacity={0} />
                        </linearGradient>
                      </defs>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                      <XAxis dataKey="month" className="text-xs fill-muted-foreground" />
                      <YAxis 
                        className="text-xs fill-muted-foreground" 
                        tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`}
                      />
                      <Tooltip 
                        formatter={(value: number, name: string) => [
                          formatCurrency(value), 
                          name === 'invested' ? 'Investido' : 'Valor Atual'
                        ]}
                      />
                      <Legend />
                      <Area 
                        type="monotone" 
                        dataKey="value" 
                        name="Valor Atual"
                        stroke="hsl(var(--success))" 
                        fill="url(#colorValueGradient)" 
                      />
                      <Line 
                        type="monotone" 
                        dataKey="invested" 
                        name="Investido"
                        stroke="hsl(var(--primary))" 
                        strokeWidth={2}
                        strokeDasharray="5 5"
                        dot={false}
                      />
                    </ComposedChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Type-specific charts */}
          {isVariable ? (
            /* Dividends Chart for Variable Income */
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Dividendos Recebidos</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="h-[280px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={dividendHistory}>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                      <XAxis dataKey="month" className="text-xs fill-muted-foreground" />
                      <YAxis 
                        className="text-xs fill-muted-foreground" 
                        tickFormatter={(v) => formatCurrency(v)}
                      />
                      <Tooltip formatter={(value: number) => [formatCurrency(value), 'Dividendos']} />
                      <Bar 
                        dataKey="dividends" 
                        name="Dividendos"
                        fill="hsl(var(--chart-4))"
                        radius={[4, 4, 0, 0]}
                      />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
                <div className="mt-4 flex justify-between text-sm">
                  <div>
                    <p className="text-muted-foreground">Total no período</p>
                    <p className="text-lg font-bold text-success">
                      {formatCurrency(dividendHistory.reduce((sum, d) => sum + d.dividends, 0))}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-muted-foreground">Média mensal</p>
                    <p className="text-lg font-bold">
                      {formatCurrency(dividendHistory.reduce((sum, d) => sum + d.dividends, 0) / 12)}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          ) : projection && (
            /* Projection Chart for Fixed Income */
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Projeção até o Vencimento</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="h-[280px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={projectionChartData}>
                      <defs>
                        <linearGradient id="projectionGradient" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.4} />
                          <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} />
                        </linearGradient>
                      </defs>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                      <XAxis dataKey="date" className="text-xs fill-muted-foreground" />
                      <YAxis 
                        className="text-xs fill-muted-foreground" 
                        tickFormatter={(v) => `${(v / 1000).toFixed(0)}k`}
                      />
                      <Tooltip formatter={(value: number) => [formatCurrency(value), 'Valor Projetado']} />
                      <Area 
                        type="monotone" 
                        dataKey="value" 
                        name="Projeção"
                        stroke="hsl(var(--primary))" 
                        strokeWidth={2}
                        fill="url(#projectionGradient)" 
                      />
                    </AreaChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>
          )}
        </TabsContent>

        <TabsContent value="history" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Histórico de Operações</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Data</TableHead>
                    <TableHead>Tipo</TableHead>
                    <TableHead className="text-right">Quantidade</TableHead>
                    <TableHead className="text-right">Preço</TableHead>
                    <TableHead className="text-right">Total</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {transactions.length > 0 ? transactions.map((item: any) => (
                    <TableRow key={item.id}>
                      <TableCell>{formatDate(item.date)}</TableCell>
                      <TableCell>
                        <Badge
                          variant={item.type === "Compra" || item.type === "Aporte" ? "default" : "destructive"}
                          className={item.type === "Compra" || item.type === "Aporte" ? "bg-success" : ""}
                        >
                          {item.type}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">{item.quantity || '-'}</TableCell>
                      <TableCell className="text-right">{item.price ? formatCurrency(item.price) : '-'}</TableCell>
                      <TableCell className="text-right font-medium">
                        {formatCurrency(item.total)}
                      </TableCell>
                    </TableRow>
                  )) : (
                    <TableRow>
                        <TableCell colSpan={5} className="text-center py-4">Nenhuma transação encontrada</TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}
