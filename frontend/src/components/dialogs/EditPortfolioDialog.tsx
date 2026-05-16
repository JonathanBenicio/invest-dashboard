import { useState, useEffect } from "react"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { mockBanks, mockUsers } from "@/lib/mock-data"
import type { PortfolioDto } from "@/api/dtos"

interface EditPortfolioDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  portfolio: PortfolioDto | null
  onSave: (portfolio: PortfolioDto) => void
}

export function EditPortfolioDialog({
  open,
  onOpenChange,
  portfolio,
  onSave,
}: EditPortfolioDialogProps) {
  const [formData, setFormData] = useState({
    name: "",
    bankId: "",
    userId: "",
    description: "",
  })

  useEffect(() => {
    if (portfolio) {
      setFormData({
        name: portfolio.name,
        bankId: portfolio.bankId || "",
        userId: portfolio.userId || "",
        description: portfolio.description || "",
      })
    }
  }, [portfolio])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!portfolio) return

    const bank = mockBanks.find((b) => b.id === formData.bankId)
    const user = mockUsers.find((u) => u.id === formData.userId)

    const updatedPortfolio: PortfolioDto = {
      ...portfolio,
      name: formData.name,
      bankId: formData.bankId,
      bankName: bank?.name || portfolio.bankName,
      bankLogo: bank?.logo || portfolio.bankLogo,
      userId: formData.userId,
      userName: user?.name || portfolio.userName,
      userEmail: user?.email || portfolio.userEmail,
      description: formData.description,
    }

    onSave(updatedPortfolio)
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Editar Carteira</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="edit-name">Nome da Carteira</Label>
            <Input
              id="edit-name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="Ex: Carteira Principal"
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-bank">Banco/Corretora</Label>
            <Select
              value={formData.bankId}
              onValueChange={(value) => setFormData({ ...formData, bankId: value })}
            >
              <SelectTrigger>
                <SelectValue placeholder="Selecione o banco" />
              </SelectTrigger>
              <SelectContent>
                {mockBanks.map((bank) => (
                  <SelectItem key={bank.id} value={bank.id}>
                    <div className="flex items-center gap-2">
                      <span>{bank.logo}</span>
                      <span>{bank.name}</span>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-user">Titular</Label>
            <Select
              value={formData.userId}
              onValueChange={(value) => setFormData({ ...formData, userId: value })}
            >
              <SelectTrigger>
                <SelectValue placeholder="Selecione o titular" />
              </SelectTrigger>
              <SelectContent>
                {mockUsers.map((user) => (
                  <SelectItem key={user.id} value={user.id}>
                    <div className="flex flex-col">
                      <span>{user.name}</span>
                      <span className="text-xs text-muted-foreground">{user.email}</span>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="edit-description">Descrição (opcional)</Label>
            <Input
              id="edit-description"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              placeholder="Descrição da carteira"
            />
          </div>

          <div className="flex gap-2 justify-end">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancelar
            </Button>
            <Button type="submit">Salvar Alterações</Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
