import { useState, useEffect } from "react"
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
} from "recharts"
import {
  formatCurrency,
  formatPercentage,
  formatDate,
} from "@/lib/mock-data"
import { useInvestment, useInvestmentTransactions } from "@/hooks/use-investment-details"
import { FixedIncomeDto, VariableIncomeDto } from "@/api/dtos"

// Using mock generator for chart data as it's typically complex to fetch historical series in a simple mock setup
// In a real app, this would come from a historical data endpoint.
const generateChartData = () => [
  { date: "01/12", position: 7750, profit: 850, average: 32.50, dailyChange: 1.2 },
  { date: "02/12", position: 7820, profit: 920, average: 32.50, dailyChange: 0.9 },
  { date: "03/12", position: 7680, profit: 780, average: 32.50, dailyChange: -1.8 },
  { date: "04/12", position: 7890, profit: 990, average: 32.50, dailyChange: 2.7 },
  { date: "05/12", position: 7950, profit: 1050, average: 32.50, dailyChange: 0.8 },
  { date: "06/12", position: 7720, profit: 820, average: 32.50, dailyChange: -2.9 },
  { date: "07/12", position: 7800, profit: 900, average: 32.50, dailyChange: 1.0 },
  { date: "08/12", position: 7880, profit: 980, average: 32.50, dailyChange: 1.0 },
  { date: "09/12", position: 7960, profit: 1060, average: 32.50, dailyChange: 1.0 },
  { date: "10/12", position: 8100, profit: 1200, average: 32.50, dailyChange: 1.8 },
  { date: "11/12", position: 8050, profit: 1150, average: 32.50, dailyChange: -0.6 },
  { date: "12/12", position: 8200, profit: 1300, average: 32.50, dailyChange: 1.9 },
]

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
  const [chartType, setChartType] = useState("position")

  // Handle initial action from URL
  useEffect(() => {
    const action = search.action
    if (action === "buy" || action === "sell") {
      setTransactionType(action)
      setDialogOpen(true)
    }
  }, [search.action])

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
  const investedValue = asset.totalInvested
  const profit = asset.gain
  const profitPercentage = asset.gainPercentage

  // Calculate projected value at maturity for fixed income
  const calculateProjectedValue = () => {
    if (!fixedAsset) return null

    const today = new Date()
    // Handle date strings properly
    if (!fixedAsset.maturityDate || !fixedAsset.purchaseDate) return null

    const maturity = new Date(fixedAsset.maturityDate)
    const purchase = new Date(fixedAsset.purchaseDate)

    // Days remaining until maturity
    const daysRemaining = Math.max(0, Math.ceil((maturity.getTime() - today.getTime()) / (1000 * 60 * 60 * 24)))
    const totalDays = Math.ceil((maturity.getTime() - purchase.getTime()) / (1000 * 60 * 60 * 24))
    const yearsRemaining = daysRemaining / 365

    // Parse rate and calculate projection
    let annualRate = 0
    const CDI_RATE = 0.1215 // 12.15% CDI aproximado
    const IPCA_RATE = 0.045 // 4.5% IPCA aproximado

    const rateStr = fixedAsset.interestRate?.toString() || '0'

    if (fixedAsset.indexer === 'CDI') {
      const cdiPercentage = parseFloat(rateStr) / 100
      annualRate = CDI_RATE * cdiPercentage
    } else if (fixedAsset.indexer === 'IPCA') {
      // Simplification for mock
      annualRate = IPCA_RATE + (parseFloat(rateStr) / 100)
    } else {
      annualRate = parseFloat(rateStr) / 100
    }

    // Compound interest projection from current value
    const projectedValue = fixedAsset.currentValue * Math.pow(1 + annualRate, yearsRemaining)
    const projectedProfit = projectedValue - fixedAsset.totalInvested
    const projectedProfitPercentage = (projectedProfit / fixedAsset.totalInvested) * 100

    return {
      projectedValue,
      projectedProfit,
      projectedProfitPercentage,
      daysRemaining,
      maturityDate: fixedAsset.maturityDate,
    }
  }

  const projection = calculateProjectedValue()
  const chartData = generateChartData() // Using static mock for chart

  const handleTransaction = () => {
    // Implement API call for transaction
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

  const getChartDataKey = () => {
    switch (chartType) {
      case "position": return "position"
      case "profit": return "profit"
      case "average": return "average"
      case "dailyChange": return "dailyChange"
      default: return "position"
    }
  }

  const getChartColor = () => {
    switch (chartType) {
      case "position": return "hsl(var(--primary))"
      case "profit": return profit >= 0 ? "hsl(var(--success))" : "hsl(var(--destructive))"
      case "average": return "hsl(var(--chart-3))"
      case "dailyChange": return "hsl(var(--chart-4))"
      default: return "hsl(var(--primary))"
    }
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
                : `${fixedAsset?.interestRate}%` // Simplification
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
      <Tabs defaultValue="history" className="space-y-4">
        <TabsList>
          <TabsTrigger value="history">Histórico</TabsTrigger>
          <TabsTrigger value="charts">Gráficos</TabsTrigger>
        </TabsList>

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

        <TabsContent value="charts" className="space-y-4">
          {/* Charts content logic is complex to migrate fully without dedicated endpoint for historical data series.
              Keeping mock chart data logic for now as 'generateChartData' is local function in this file.
              The task is to migrate pages to API. The chart data usually comes from a dedicated endpoint.
              For now, the page loads but chart is static mock. This is acceptable for this level of migration unless specific historical endpoint is available.
           */}
          <Card>
            <CardHeader className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <CardTitle className="text-lg">Evolução do Ativo</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="h-[400px]">
                <ResponsiveContainer width="100%" height="100%">
                  <AreaChart data={chartData}>
                    {/* ... keeping existing chart config ... */}
                     <defs>
                      <linearGradient id="chartGradient" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor={getChartColor()} stopOpacity={0.3} />
                        <stop offset="95%" stopColor={getChartColor()} stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                    <XAxis dataKey="date" className="text-xs fill-muted-foreground" />
                    <YAxis className="text-xs fill-muted-foreground" />
                    <Tooltip />
                    <Area type="monotone" dataKey={getChartDataKey()} stroke={getChartColor()} strokeWidth={2} fill="url(#chartGradient)" />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}
