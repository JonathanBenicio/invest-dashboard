import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

export type InvestmentSituationFilterValue = 'balance' | 'closed' | 'all'

interface InvestmentSituationFilterProps {
  value: InvestmentSituationFilterValue
  onChange: (value: InvestmentSituationFilterValue) => void
}

export function InvestmentSituationFilter({ value, onChange }: InvestmentSituationFilterProps) {
  return <div className="grid w-full max-w-sm min-w-0 gap-1.5">
    <Label htmlFor="investment-situation-filter">Posições</Label>
    <Select value={value} onValueChange={next => onChange(next as InvestmentSituationFilterValue)}>
      <SelectTrigger id="investment-situation-filter"><SelectValue /></SelectTrigger>
      <SelectContent>
        <SelectItem value="balance">Com saldo</SelectItem>
        <SelectItem value="closed">Encerradas</SelectItem>
        <SelectItem value="all">Todas</SelectItem>
      </SelectContent>
    </Select>
  </div>
}
