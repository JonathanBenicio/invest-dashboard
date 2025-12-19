import { useState, useEffect } from "react"
import { useParams, useNavigate, useSearchParams } from "react-router-dom"
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
  DialogTrigger,
} from "@/components/ui/dialog"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
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
  Percent,
  LineChart,
  Calendar,
  Target,
} from "lucide-react"
import {
  LineChart as RechartsLineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Area,
  AreaChart,
  Legend,
} from "recharts"
import {
  fixedIncomeAssets,
  variableIncomeAssets,
  formatCurrency,
  formatPercentage,
  formatDate,
} from "@/lib/mock-data"

// Mock history data for assets
const generateHistoryData = (assetId: string) => [
  { id: "1", date: "2024-12-01", type: "Compra", quantity: 50, price: 35.20, total: 1760 },
  { id: "2", date: "2024-11-15", type: "Compra", quantity: 100, price: 33.50, total: 3350 },
  { id: "3", date: "2024-10-20", type: "Venda", quantity: 30, price: 36.80, total: 1104 },
  { id: "4", date: "2024-09-10", type: "Compra", quantity: 80, price: 31.00, total: 2480 },
]

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
  const { id } = useParams()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const type = searchParams.get("type") || "variable"

  const [transactionType, setTransactionType] = useState<"buy" | "sell">("buy")
  const [quantity, setQuantity] = useState("")
  const [price, setPrice] = useState("")
  const [dialogOpen, setDialogOpen] = useState(false)
  const [chartType, setChartType] = useState("position")

  // Handle initial action from URL
  useEffect(() => {
    const action = searchParams.get("action")
    if (action === "buy" || action === "sell") {
      setTransactionType(action)
      setDialogOpen(true)
    }
  }, [searchParams])

  // Find asset based on type
  const asset = type === "fixed"
    ? fixedIncomeAssets.find(a => a.id === id)
    : variableIncomeAssets.find(a => a.id === id)

  if (!asset) {
    return (
      <div className="flex flex-col items-center justify-center h-96">
        <p className="text-muted-foreground">Ativo não encontrado</p>
        <Button variant="outline" onClick={() => navigate(-1)} className="mt-4">
          <ArrowLeft className="mr-2 h-4 w-4" /> Voltar
        </Button>
      </div>
    )
  }

  const isVariable = type === "variable"
  const variableAsset = isVariable ? asset as typeof variableIncomeAssets[0] : null
  const fixedAsset = !isVariable ? asset as typeof fixedIncomeAssets[0] : null

  // Calculate values for variable income
  const totalValue = variableAsset
    ? variableAsset.currentPrice * variableAsset.quantity
    : fixedAsset?.currentValue || 0

  const investedValue = variableAsset
    ? variableAsset.averagePrice * variableAsset.quantity
    : fixedAsset?.investedValue || 0

  const profit = totalValue - investedValue
  const profitPercentage = (profit / investedValue) * 100

  // Calculate projected value at maturity for fixed income
  const calculateProjectedValue = () => {
    if (!fixedAsset) return null

    const today = new Date()
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

    if (fixedAsset.rateType === 'CDI') {
      const cdiPercentage = parseFloat(fixedAsset.rate.replace('%', '')) / 100
      annualRate = CDI_RATE * cdiPercentage
    } else if (fixedAsset.rateType === 'IPCA') {
      const spreadMatch = fixedAsset.rate.match(/[\d,]+/g)
      const spread = spreadMatch ? parseFloat(spreadMatch[spreadMatch.length - 1].replace(',', '.')) / 100 : 0.06
      annualRate = IPCA_RATE + spread
    } else {
      annualRate = parseFloat(fixedAsset.rate.replace('%', '').replace(',', '.')) / 100
    }

    // Compound interest projection from current value
    const projectedValue = fixedAsset.currentValue * Math.pow(1 + annualRate, yearsRemaining)
    const projectedProfit = projectedValue - fixedAsset.investedValue
    const projectedProfitPercentage = (projectedProfit / fixedAsset.investedValue) * 100

    return {
      projectedValue,
      projectedProfit,
      projectedProfitPercentage,
      daysRemaining,
      maturityDate: fixedAsset.maturityDate,
    }
  }

  const projection = calculateProjectedValue()

  const historyData = generateHistoryData(id!)
  const chartData = generateChartData()

  const handleTransaction = () => {
    // Mock transaction handling
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
          <Button variant="ghost" size="icon" onClick={() => navigate(-1)}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-2xl font-bold">
                {isVariable ? variableAsset?.ticker : fixedAsset?.name}
              </h1>
              <Badge variant={isVariable ? "default" : "secondary"}>
                {isVariable ? variableAsset?.type : fixedAsset?.type}
              </Badge>
            </div>
            <p className="text-muted-foreground">
              {isVariable ? variableAsset?.name : fixedAsset?.institution}
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
                {variableAsset?.quantity} unidades
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
                ? formatCurrency(variableAsset?.averagePrice || 0)
                : formatCurrency(fixedAsset?.investedValue || 0)
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
                ? formatCurrency(variableAsset?.currentPrice || 0)
                : fixedAsset?.rate
              }
            </p>
            {!isVariable && (
              <p className="text-xs text-muted-foreground">
                {fixedAsset?.rateType}
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

      {/* Quick Buy/Aporte Button */}
      <Card className="border-success/20 bg-gradient-to-r from-success/5 to-transparent">
        <CardContent className="flex flex-col sm:flex-row items-center justify-between gap-4 py-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-full bg-success/10">
              <PlusCircle className="h-6 w-6 text-success" />
            </div>
            <div>
              <p className="font-medium">
                {isVariable ? "Aumentar Posição" : "Realizar Aporte"}
              </p>
              <p className="text-sm text-muted-foreground">
                {isVariable
                  ? `Cotação atual: ${formatCurrency(variableAsset?.currentPrice || 0)}`
                  : `Taxa: ${fixedAsset?.rate} (${fixedAsset?.rateType})`
                }
              </p>
            </div>
          </div>
          <Button
            size="lg"
            variant="success"
            className="w-full sm:w-auto"
            onClick={() => {
              setTransactionType("buy")
              setDialogOpen(true)
            }}
          >
            <PlusCircle className="mr-2 h-4 w-4" />
            {isVariable ? "Comprar Agora" : "Aportar Agora"}
          </Button>
        </CardContent>
      </Card>

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
                  {historyData.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>{formatDate(item.date)}</TableCell>
                      <TableCell>
                        <Badge
                          variant={item.type === "Compra" ? "default" : "destructive"}
                          className={item.type === "Compra" ? "bg-success" : ""}
                        >
                          {item.type}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">{item.quantity}</TableCell>
                      <TableCell className="text-right">{formatCurrency(item.price)}</TableCell>
                      <TableCell className="text-right font-medium">
                        {formatCurrency(item.total)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="charts" className="space-y-4">
          <Card>
            <CardHeader className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <CardTitle className="text-lg">Evolução do Ativo</CardTitle>
              <Select value={chartType} onValueChange={setChartType}>
                <SelectTrigger className="w-[180px]">
                  <SelectValue placeholder="Selecione o gráfico" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="position">Posição</SelectItem>
                  <SelectItem value="profit">Lucro/Prejuízo</SelectItem>
                  <SelectItem value="average">Preço Médio</SelectItem>
                  <SelectItem value="dailyChange">Variação Diária (%)</SelectItem>
                </SelectContent>
              </Select>
            </CardHeader>
            <CardContent>
              <div className="h-[400px]">
                <ResponsiveContainer width="100%" height="100%">
                  <AreaChart data={chartData}>
                    <defs>
                      <linearGradient id="chartGradient" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor={getChartColor()} stopOpacity={0.3} />
                        <stop offset="95%" stopColor={getChartColor()} stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                    <XAxis
                      dataKey="date"
                      className="text-xs fill-muted-foreground"
                    />
                    <YAxis
                      className="text-xs fill-muted-foreground"
                      tickFormatter={(value) =>
                        chartType === "dailyChange"
                          ? `${value}%`
                          : chartType === "average"
                            ? formatCurrency(value)
                            : formatCurrency(value)
                      }
                    />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: "hsl(var(--card))",
                        border: "1px solid hsl(var(--border))",
                        borderRadius: "8px",
                      }}
                      formatter={(value: number) => [
                        chartType === "dailyChange"
                          ? `${value.toFixed(2)}%`
                          : formatCurrency(value),
                        chartType === "position" ? "Posição"
                          : chartType === "profit" ? "Lucro"
                            : chartType === "average" ? "Preço Médio"
                              : "Variação"
                      ]}
                    />
                    <Area
                      type="monotone"
                      dataKey={getChartDataKey()}
                      stroke={getChartColor()}
                      strokeWidth={2}
                      fill="url(#chartGradient)"
                    />
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
