import { Calendar, Target, TrendingUp } from "lucide-react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from "recharts"
import { FixedIncomeAsset, formatCurrency } from "@/lib/mock-data"

interface FixedIncomeProjectionProps {
  assets: FixedIncomeAsset[]
}

export function FixedIncomeProjection({ assets }: FixedIncomeProjectionProps) {
  const calculateProjection = () => {
    if (assets.length === 0) return []

    const today = new Date()
    const projectionData: Array<{ month: string; projection: number; invested: number }> = []

    // Create data points for every 6 months until the furthest maturity date
    const maxMaturityDate = new Date(Math.max(...assets.map(a => new Date(a.maturityDate).getTime())))
    const monthsBetween = Math.ceil((maxMaturityDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24 * 30))

    for (let month = 0; month <= monthsBetween; month += 6) {
      const projectionDate = new Date(today)
      projectionDate.setMonth(projectionDate.getMonth() + month)

      let totalProjection = 0
      let totalInvested = 0

      assets.forEach(asset => {
        const maturityDate = new Date(asset.maturityDate)
        totalInvested += asset.investedValue

        if (projectionDate <= maturityDate) {
          const monthsToMaturity = Math.max(0, (maturityDate.getTime() - new Date().getTime()) / (1000 * 60 * 60 * 24 * 30))
          const totalMonths = Math.max(0, (maturityDate.getTime() - new Date(asset.purchaseDate).getTime()) / (1000 * 60 * 60 * 24 * 30))
          const elapsedMonths = totalMonths - monthsToMaturity

          let projectedValue = asset.investedValue

          if (asset.rateType === 'Prefixado') {
            // For fixed rate, calculate compound interest
            const rateValue = parseFloat(asset.rate.replace('%', '').replace(',', '.'))
            const rate = rateValue / 100
            const years = elapsedMonths / 12
            projectedValue = asset.investedValue * Math.pow(1 + rate, years)
          } else if (asset.rateType === 'CDI') {
            // For CDI rates, use approximate CDI rate of 10.65% annually
            const cdiRate = 0.1065
            const rateMultiplier = parseFloat(asset.rate.replace('%', '').replace(',', '.')) / 100
            const effectiveRate = cdiRate * rateMultiplier
            const years = elapsedMonths / 12
            projectedValue = asset.investedValue * Math.pow(1 + effectiveRate, years)
          } else if (asset.rateType === 'IPCA') {
            // For IPCA, use approximate IPCA of 4% + spread
            const spreadMatch = asset.rate.match(/[\d.,]+/)
            const spread = spreadMatch ? parseFloat(spreadMatch[0].replace(',', '.')) / 100 : 0.05
            const ipcaRate = 0.04
            const effectiveRate = (1 + ipcaRate) * (1 + spread) - 1
            const years = elapsedMonths / 12
            projectedValue = asset.investedValue * Math.pow(1 + effectiveRate, years)
          }

          totalProjection += projectedValue
        }
      })

      if (totalProjection > 0) {
        projectionData.push({
          month: projectionDate.toLocaleDateString('pt-BR', { month: 'short', year: '2-digit' }),
          projection: Math.round(totalProjection),
          invested: totalInvested,
        })
      }
    }

    return projectionData
  }

  const projectionData = calculateProjection()

  if (projectionData.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Target className="h-5 w-5" />
            Projeção de Renda Fixa
          </CardTitle>
          <CardDescription>Visualize o crescimento estimado de seus investimentos</CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">Nenhum ativo com data de vencimento disponível</p>
        </CardContent>
      </Card>
    )
  }

  const totalCurrentValue = assets.reduce((acc, asset) => acc + asset.currentValue, 0)
  const finalProjectedValue = projectionData[projectionData.length - 1]?.projection || 0
  const projectedGain = finalProjectedValue - totalCurrentValue
  const projectionPercentage = ((projectedGain / totalCurrentValue) * 100).toFixed(2)

  return (
    <Card className="border-primary/20 bg-gradient-to-r from-primary/5 to-transparent">
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <div>
          <CardTitle className="flex items-center gap-2 text-lg">
            <Target className="h-5 w-5 text-primary" />
            Projeção Consolidada Renda Fixa
          </CardTitle>
          <CardDescription>Estimativa até o vencimento dos ativos</CardDescription>
        </div>
        <Badge variant="outline" className="flex items-center gap-1">
          <Calendar className="h-3 w-3" />
          No Vencimento
        </Badge>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Summary Cards */}
        <div className="grid gap-4 sm:grid-cols-3">
          <div>
            <p className="text-sm text-muted-foreground">Valor Atual Total</p>
            <p className="text-2xl font-bold text-foreground">{formatCurrency(totalCurrentValue)}</p>
          </div>
          <div>
            <p className="text-sm text-muted-foreground">Valor Projetado Total</p>
            <p className="text-2xl font-bold text-primary">{formatCurrency(finalProjectedValue)}</p>
          </div>
          <div>
            <p className="text-sm text-muted-foreground">Lucro Projetado</p>
            <div className="flex items-center gap-2">
              <p className={`text-2xl font-bold ${projectedGain >= 0 ? 'text-success' : 'text-destructive'}`}>
                {formatCurrency(projectedGain)}
              </p>
              <TrendingUp className={`h-5 w-5 ${projectedGain >= 0 ? 'text-success' : 'text-destructive'}`} />
            </div>
            <p className={`text-xs ${projectedGain >= 0 ? 'text-success' : 'text-destructive'}`}>
              {projectedGain >= 0 ? '+' : ''}{projectionPercentage}%
            </p>
          </div>
        </div>

        {/* Chart */}
        <div className="rounded-lg border bg-card p-4">
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={projectionData}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--muted))" />
              <XAxis 
                dataKey="month" 
                stroke="hsl(var(--muted-foreground))"
                style={{ fontSize: '0.875rem' }}
              />
              <YAxis 
                stroke="hsl(var(--muted-foreground))"
                style={{ fontSize: '0.875rem' }}
                tickFormatter={(value) => `R$ ${(value / 1000).toFixed(0)}k`}
              />
              <Tooltip 
                formatter={(value) => formatCurrency(value as number)}
                contentStyle={{
                  backgroundColor: 'hsl(var(--card))',
                  border: '1px solid hsl(var(--border))',
                  borderRadius: '0.5rem'
                }}
              />
              <Legend />
              <Line
                type="monotone"
                dataKey="invested"
                stroke="hsl(var(--muted-foreground))"
                strokeWidth={2}
                dot={false}
                name="Valor Investido"
              />
              <Line
                type="monotone"
                dataKey="projection"
                stroke="hsl(var(--success))"
                strokeWidth={2}
                dot={false}
                name="Projeção"
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </CardContent>
    </Card>
  )
}