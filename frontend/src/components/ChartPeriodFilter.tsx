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
