import { useState, useMemo } from "react"
import { ArrowUpRight, ArrowDownRight, TrendingUp, Calendar, Wallet, PiggyBank } from "lucide-react"
import { useQuery } from "@tanstack/react-query"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { investmentService, queryKeys } from "@/api"
import { formatCurrency } from "@/lib/utils"
import { ChartPeriodFilter, type ChartPeriod, generatePeriodData } from "@/components/ChartPeriodFilter"
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  Tooltip,
  PieChart,
  Pie,
  Cell,
  Legend
} from "recharts"

export default function Dashboard() {
  const [chartPeriod, setChartPeriod] = useState<ChartPeriod>('30d')

  // Fetch investment summary
  const { data: summaryData, isLoading } = useQuery({
    queryKey: queryKeys.investments.summary(),
    queryFn: () => investmentService.getSummary(),
  })

  const summary = summaryData?.data

  // Generate chart data based on selected period
  const portfolioHistory = useMemo(() => {
    if (!summary) return []
    return generatePeriodData(chartPeriod, summary.currentValue)
  }, [chartPeriod, summary])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-muted-foreground">Carregando...</div>
      </div>
    )
  }

  if (!summary) return null

  const totalPortfolio = summary.currentValue
  const totalProfit = summary.totalGain
  const profitPercentage = summary.gainPercentage

  // Find best and worst performers
  const bestPerformer = summary.topPerformers[0]
  const worstPerformer = summary.worstPerformers[0]

  const allocationData = [
    { name: 'Renda Fixa', value: summary.fixedIncomeTotal, color: 'hsl(220, 70%, 50%)' },
    { name: 'Renda Variável', value: summary.variableIncomeTotal, color: 'hsl(145, 65%, 42%)' },
  ]

  // Mock upcoming data (will be replaced with real data later)
  const upcomingMaturities: any[] = []
  const upcomingDividends: any[] = []

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Dashboard</h1>
        <p className="text-muted-foreground">Visão geral da sua carteira de investimentos</p>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Patrimônio Total
            </CardTitle>
            <Wallet className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(totalPortfolio)}</div>
            <p className="text-xs text-muted-foreground mt-1">
              Atualizado em tempo real
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Rentabilidade Total
            </CardTitle>
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${totalProfit >= 0 ? 'text-success' : 'text-destructive'}`}>
              {formatCurrency(totalProfit)}
            </div>
            <div className={`flex items-center text-xs mt-1 ${profitPercentage >= 0 ? 'text-success' : 'text-destructive'}`}>
              {profitPercentage >= 0 ? <ArrowUpRight className="h-3 w-3 mr-1" /> : <ArrowDownRight className="h-3 w-3 mr-1" />}
              {profitPercentage >= 0 ? '+' : ''}{profitPercentage.toFixed(2)}%
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Maior Alta
            </CardTitle>
            <ArrowUpRight className="h-4 w-4 text-success" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{bestPerformer?.ticker || bestPerformer?.name || '-'}</div>
            <div className="flex items-center text-xs text-success mt-1">
              <ArrowUpRight className="h-3 w-3 mr-1" />
              +{bestPerformer?.gainPercentage.toFixed(2)}%
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Maior Baixa
            </CardTitle>
            <ArrowDownRight className="h-4 w-4 text-destructive" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{worstPerformer?.ticker || worstPerformer?.name || '-'}</div>
            <div className="flex items-center text-xs text-destructive mt-1">
              <ArrowDownRight className="h-3 w-3 mr-1" />
              {worstPerformer?.gainPercentage.toFixed(2)}%
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Charts */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Portfolio Evolution */}
        <Card className="lg:col-span-1">
          <CardHeader className="flex flex-row items-start justify-between space-y-0">
            <div>
              <CardTitle>Evolução Patrimonial</CardTitle>
              <CardDescription>Sua Carteira vs Benchmark (CDI)</CardDescription>
            </div>
            <ChartPeriodFilter value={chartPeriod} onChange={setChartPeriod} />
          </CardHeader>
          <CardContent>
            <div className="h-[300px]">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={portfolioHistory}>
                  <defs>
                    <linearGradient id="colorValue" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="hsl(220, 70%, 50%)" stopOpacity={0.3} />
                      <stop offset="95%" stopColor="hsl(220, 70%, 50%)" stopOpacity={0} />
                    </linearGradient>
                    <linearGradient id="colorBenchmark" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="hsl(145, 65%, 42%)" stopOpacity={0.3} />
                      <stop offset="95%" stopColor="hsl(145, 65%, 42%)" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <XAxis dataKey="date" axisLine={false} tickLine={false} className="text-xs" />
                  <YAxis
                    axisLine={false}
                    tickLine={false}
                    tickFormatter={(value) => `${(value / 1000).toFixed(0)}k`}
                    className="text-xs"
                  />
                  <Tooltip
                    formatter={(value: number) => formatCurrency(value)}
                    labelStyle={{ color: 'hsl(220, 20%, 15%)' }}
                    contentStyle={{
                      backgroundColor: 'hsl(0, 0%, 100%)',
                      border: '1px solid hsl(220, 15%, 90%)',
                      borderRadius: '8px'
                    }}
                  />
                  <Area
                    type="monotone"
                    dataKey="value"
                    stroke="hsl(220, 70%, 50%)"
                    fillOpacity={1}
                    fill="url(#colorValue)"
                    name="Sua Carteira"
                  />
                  <Area
                    type="monotone"
                    dataKey="benchmark"
                    stroke="hsl(145, 65%, 42%)"
                    fillOpacity={1}
                    fill="url(#colorBenchmark)"
                    name="CDI"
                  />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        {/* Allocation */}
        <Card>
          <CardHeader>
            <CardTitle>Alocação por Tipo</CardTitle>
            <CardDescription>Distribuição atual dos ativos</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="h-[300px]">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={allocationData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={100}
                    paddingAngle={2}
                    dataKey="value"
                  >
                    {allocationData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip
                    formatter={(value: number) => formatCurrency(value)}
                    contentStyle={{
                      backgroundColor: 'hsl(0, 0%, 100%)',
                      border: '1px solid hsl(220, 15%, 90%)',
                      borderRadius: '8px'
                    }}
                  />
                  <Legend
                    layout="vertical"
                    align="right"
                    verticalAlign="middle"
                    formatter={(value) => <span className="text-sm text-foreground">{value}</span>}
                  />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Bottom Cards */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Upcoming Maturities */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Calendar className="h-5 w-5" />
                Próximos Vencimentos
              </CardTitle>
              <CardDescription>Renda fixa com vencimento próximo</CardDescription>
            </div>
          </CardHeader>
          <CardContent>
            {upcomingMaturities.length === 0 ? (
              <p className="text-sm text-muted-foreground">Nenhum vencimento próximo</p>
            ) : (
              <div className="space-y-4">
                {upcomingMaturities.map((asset) => (
                  <div key={asset.id} className="flex items-center justify-between p-3 rounded-lg bg-muted/50">
                    <div>
                      <p className="font-medium text-sm">{asset.name}</p>
                      <p className="text-xs text-muted-foreground">{asset.type} • {asset.rate} {asset.rateType}</p>
                    </div>
                    <div className="text-right">
                      <p className="font-medium text-sm">{formatCurrency(asset.currentValue)}</p>
                      <p className="text-xs text-muted-foreground">
                        {new Date(asset.maturityDate).toLocaleDateString('pt-BR')}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Upcoming Dividends */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <PiggyBank className="h-5 w-5" />
                Próximos Proventos
              </CardTitle>
              <CardDescription>Dividendos e rendimentos previstos</CardDescription>
            </div>
          </CardHeader>
          <CardContent>
            {upcomingDividends.length === 0 ? (
              <p className="text-sm text-muted-foreground">Nenhum provento próximo</p>
            ) : (
              <div className="space-y-4">
                {upcomingDividends.map((dividend) => (
                  <div key={dividend.id} className="flex items-center justify-between p-3 rounded-lg bg-muted/50">
                    <div>
                      <p className="font-medium text-sm">{dividend.ticker}</p>
                      <p className="text-xs text-muted-foreground">{dividend.type}</p>
                    </div>
                    <div className="text-right">
                      <p className="font-medium text-sm text-success">{formatCurrency(dividend.value)}</p>
                      <p className="text-xs text-muted-foreground">
                        {new Date(dividend.paymentDate).toLocaleDateString('pt-BR')}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
