import { useEffect, useState } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { PosicaoInvestimentoDto, RendaFixaDto, RendaVariavelDto } from '@/api/dtos'

type Investment = PosicaoInvestimentoDto | RendaFixaDto | RendaVariavelDto

interface EditInvestmentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  investment: Investment | null
  type: 'fixed' | 'variable'
  onSave: (updatedInvestment: Investment, valuationDate: string) => void
}

export function EditInvestmentDialog({
  open,
  onOpenChange,
  investment,
  type,
  onSave,
}: EditInvestmentDialogProps) {
  const [totalValue, setTotalValue] = useState('')
  const [valuationDate, setValuationDate] = useState('')

  useEffect(() => {
    setTotalValue(investment?.currentValue.toString() ?? '')
    setValuationDate(new Date().toISOString().slice(0, 10))
  }, [investment])

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!investment) return
    const value = Number(totalValue)
    const currentPrice = investment.quantity > 0 ? value / investment.quantity : 0
    const gain = value - investment.totalInvested

    onSave({
      ...investment,
      currentValue: value,
      currentPrice,
      gain,
      gainPercentage: investment.totalInvested > 0 ? gain / investment.totalInvested * 100 : 0,
    }, valuationDate)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Registrar avaliação do extrato</DialogTitle>
          <DialogDescription>
            {type === 'fixed'
              ? 'Informe o valor total exibido no extrato do contrato.'
              : 'Informe o valor total da posição. A cotação de mercado pode atualizá-lo novamente.'}
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="valuation-total">Valor total atual</Label>
            <Input
              id="valuation-total"
              type="number"
              min="0"
              step="0.01"
              value={totalValue}
              onChange={event => setTotalValue(event.target.value)}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="valuation-date">Data do extrato</Label>
            <Input
              id="valuation-date"
              type="date"
              value={valuationDate}
              onChange={event => setValuationDate(event.target.value)}
              required
            />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit" disabled={!investment || Number(totalValue) < 0}>Salvar avaliação</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
