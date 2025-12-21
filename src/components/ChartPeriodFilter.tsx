import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"

export type ChartPeriod = '7d' | '30d' | '1y' | 'max'

interface ChartPeriodFilterProps {
  value: ChartPeriod
  onChange: (period: ChartPeriod) => void
}

const periodLabels: Record<ChartPeriod, string> = {
  '7d': '7 dias',
  '30d': '30 dias',
  '1y': '1 ano',
  'max': 'Máximo',
}

export function ChartPeriodFilter({ value, onChange }: ChartPeriodFilterProps) {
  return (
    <ToggleGroup 
      type="single" 
      value={value} 
      onValueChange={(v) => v && onChange(v as ChartPeriod)}
      className="justify-start"
    >
      {(Object.keys(periodLabels) as ChartPeriod[]).map((period) => (
        <ToggleGroupItem 
          key={period} 
          value={period} 
          size="sm"
          className="text-xs px-3"
        >
          {periodLabels[period]}
        </ToggleGroupItem>
      ))}
    </ToggleGroup>
  )
}

// Helper to generate mock data based on period
export function generatePeriodData(period: ChartPeriod, baseValue: number) {
  const now = new Date()
  const data: { date: string; value: number; benchmark: number }[] = []
  
  let days: number
  switch (period) {
    case '7d':
      days = 7
      break
    case '30d':
      days = 30
      break
    case '1y':
      days = 365
      break
    case 'max':
      days = 730 // 2 years
      break
  }

  const volatility = 0.02
  let currentValue = baseValue * (1 - (days / 365) * 0.15)
  let benchmarkValue = currentValue * 0.95

  for (let i = days; i >= 0; i--) {
    const date = new Date(now)
    date.setDate(date.getDate() - i)
    
    // Add some randomness
    const dailyChange = 1 + (Math.random() - 0.45) * volatility
    const benchmarkChange = 1 + (Math.random() - 0.48) * volatility * 0.5
    
    currentValue *= dailyChange
    benchmarkValue *= benchmarkChange

    // Format date based on period
    let dateLabel: string
    if (period === '7d') {
      dateLabel = date.toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit' })
    } else if (period === '30d') {
      dateLabel = date.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })
    } else {
      dateLabel = date.toLocaleDateString('pt-BR', { month: 'short', year: '2-digit' })
    }

    data.push({
      date: dateLabel,
      value: Math.round(currentValue),
      benchmark: Math.round(benchmarkValue),
    })
  }

  // Sample data to reduce points for readability
  if (period === '1y') {
    return data.filter((_, i) => i % 7 === 0 || i === data.length - 1)
  }
  if (period === 'max') {
    return data.filter((_, i) => i % 14 === 0 || i === data.length - 1)
  }

  return data
}
