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
import type { FixedIncomeDto, VariableIncomeDto, FixedIncomeType, VariableIncomeType } from "@/api/dtos"

interface EditInvestmentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  investment: FixedIncomeDto | VariableIncomeDto | null
  type: "fixed" | "variable"
  onSave: (data: any) => void
}

export function EditInvestmentDialog({
  open,
  onOpenChange,
  investment,
  type,
  onSave,
}: EditInvestmentDialogProps) {
  const [fixedFormData, setFixedFormData] = useState({
    name: "",
    subtype: "CDB" as FixedIncomeType,
    issuer: "",
    totalInvested: "",
    interestRate: "",
    indexer: "CDI" as 'CDI' | 'IPCA' | 'PREFIXADO',
    maturityDate: "",
  })

  const [variableFormData, setVariableFormData] = useState({
    ticker: "",
    name: "",
    subtype: "ACAO" as VariableIncomeType,
    sector: "",
    quantity: "",
    averagePrice: "",
  })

  useEffect(() => {
    if (!investment) return

    if (type === "fixed") {
      const fixed = investment as FixedIncomeDto
      setFixedFormData({
        name: fixed.name,
        subtype: fixed.subtype,
        issuer: fixed.issuer,
        totalInvested: fixed.totalInvested.toString(),
        interestRate: fixed.interestRate.toString(),
        indexer: fixed.indexer || 'CDI',
        maturityDate: fixed.maturityDate ? fixed.maturityDate.split('T')[0] : '',
      })
    } else {
      const variable = investment as VariableIncomeDto
      setVariableFormData({
        ticker: variable.ticker || "",
        name: variable.name,
        subtype: variable.subtype,
        sector: variable.sector || "",
        quantity: variable.quantity.toString(),
        averagePrice: variable.averagePrice.toString(),
      })
    }
  }, [investment, type])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!investment) return

    if (type === "fixed") {
      // Return updated fields
      const updated = {
        ...investment,
        name: fixedFormData.name,
        subtype: fixedFormData.subtype,
        issuer: fixedFormData.issuer,
        totalInvested: Number(fixedFormData.totalInvested),
        interestRate: Number(fixedFormData.interestRate),
        indexer: fixedFormData.indexer,
        maturityDate: fixedFormData.maturityDate,
      }
      onSave(updated)
    } else {
      const updated = {
        ...investment,
        ticker: variableFormData.ticker,
        name: variableFormData.name,
        subtype: variableFormData.subtype,
        sector: variableFormData.sector,
        quantity: Number(variableFormData.quantity),
        averagePrice: Number(variableFormData.averagePrice),
      }
      onSave(updated)
    }
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>
            Editar {type === "fixed" ? "Renda Fixa" : "Renda Variável"}
          </DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          {type === "fixed" ? (
            <>
              <div className="space-y-2">
                <Label>Nome do Ativo</Label>
                <Input
                  value={fixedFormData.name}
                  onChange={(e) =>
                    setFixedFormData({ ...fixedFormData, name: e.target.value })
                  }
                  required
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Tipo</Label>
                  <Select
                    value={fixedFormData.subtype}
                    onValueChange={(value) =>
                      setFixedFormData({
                        ...fixedFormData,
                        subtype: value as FixedIncomeType,
                      })
                    }
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="CDB">CDB</SelectItem>
                      <SelectItem value="LCI">LCI</SelectItem>
                      <SelectItem value="LCA">LCA</SelectItem>
                      <SelectItem value="TESOURO_DIRETO">Tesouro Direto</SelectItem>
                      <SelectItem value="DEBENTURE">Debênture</SelectItem>
                      <SelectItem value="CRI">CRI</SelectItem>
                      <SelectItem value="CRA">CRA</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label>Instituição</Label>
                  <Input
                    value={fixedFormData.issuer}
                    onChange={(e) =>
                      setFixedFormData({ ...fixedFormData, issuer: e.target.value })
                    }
                    required
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Valor Investido</Label>
                  <Input
                    type="number"
                    value={fixedFormData.totalInvested}
                    onChange={(e) =>
                      setFixedFormData({ ...fixedFormData, totalInvested: e.target.value })
                    }
                    required
                  />
                </div>

                <div className="space-y-2">
                  <Label>Taxa</Label>
                  <Input
                    value={fixedFormData.interestRate}
                    onChange={(e) =>
                      setFixedFormData({ ...fixedFormData, interestRate: e.target.value })
                    }
                    placeholder="Ex: 120"
                    required
                    type="number"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Indexador</Label>
                  <Select
                    value={fixedFormData.indexer}
                    onValueChange={(value) =>
                      setFixedFormData({
                        ...fixedFormData,
                        indexer: value as 'CDI' | 'IPCA' | 'PREFIXADO',
                      })
                    }
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="CDI">CDI</SelectItem>
                      <SelectItem value="IPCA">IPCA</SelectItem>
                      <SelectItem value="PREFIXADO">Prefixado</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label>Data de Vencimento</Label>
                  <Input
                    type="date"
                    value={fixedFormData.maturityDate}
                    onChange={(e) =>
                      setFixedFormData({ ...fixedFormData, maturityDate: e.target.value })
                    }
                    required
                  />
                </div>
              </div>
            </>
          ) : (
            <>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Ticker</Label>
                  <Input
                    value={variableFormData.ticker}
                    onChange={(e) =>
                      setVariableFormData({ ...variableFormData, ticker: e.target.value.toUpperCase() })
                    }
                    required
                  />
                </div>

                <div className="space-y-2">
                  <Label>Tipo</Label>
                  <Select
                    value={variableFormData.subtype}
                    onValueChange={(value) =>
                      setVariableFormData({
                        ...variableFormData,
                        subtype: value as VariableIncomeType,
                      })
                    }
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="ACAO">Ação</SelectItem>
                      <SelectItem value="FII">FII</SelectItem>
                      <SelectItem value="ETF">ETF</SelectItem>
                      <SelectItem value="BDR">BDR</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="space-y-2">
                <Label>Nome do Ativo</Label>
                <Input
                  value={variableFormData.name}
                  onChange={(e) =>
                    setVariableFormData({ ...variableFormData, name: e.target.value })
                  }
                  required
                />
              </div>

              <div className="space-y-2">
                <Label>Setor</Label>
                <Input
                  value={variableFormData.sector}
                  onChange={(e) =>
                    setVariableFormData({ ...variableFormData, sector: e.target.value })
                  }
                  required
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Quantidade</Label>
                  <Input
                    type="number"
                    value={variableFormData.quantity}
                    onChange={(e) =>
                      setVariableFormData({ ...variableFormData, quantity: e.target.value })
                    }
                    required
                  />
                </div>

                <div className="space-y-2">
                  <Label>Preço Médio</Label>
                  <Input
                    type="number"
                    step="0.01"
                    value={variableFormData.averagePrice}
                    onChange={(e) =>
                      setVariableFormData({ ...variableFormData, averagePrice: e.target.value })
                    }
                    required
                  />
                </div>
              </div>
            </>
          )}

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
