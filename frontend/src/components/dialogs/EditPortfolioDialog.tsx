import { useEffect, useState } from 'react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { AtualizarCarteiraRequest, CarteiraDto } from '@/api/dtos'

interface EditPortfolioDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  portfolio: CarteiraDto | null
  onSave: (portfolioId: string, update: AtualizarCarteiraRequest) => void
}

export function EditPortfolioDialog({ open, onOpenChange, portfolio, onSave }: EditPortfolioDialogProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')

  useEffect(() => {
    setName(portfolio?.name ?? '')
    setDescription(portfolio?.description ?? '')
  }, [portfolio])

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!portfolio) return
    onSave(portfolio.id, { name: name.trim(), description: description.trim() || undefined })
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Editar carteira</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="edit-name">Nome</Label>
            <Input
              id="edit-name"
              value={name}
              onChange={event => setName(event.target.value)}
              maxLength={100}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="edit-description">Descrição (opcional)</Label>
            <Input
              id="edit-description"
              value={description}
              onChange={event => setDescription(event.target.value)}
              maxLength={500}
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit">Salvar</Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
