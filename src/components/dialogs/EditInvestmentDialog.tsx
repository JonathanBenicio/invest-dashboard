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
import type { FixedIncomeAsset, VariableIncomeAsset } from "@/lib/mock-data"

interface EditInvestmentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  investment: FixedIncomeAsset | VariableIncomeAsset | null
  type: "fixed" | "variable"
  onSave: (investment: FixedIncomeAsset | VariableIncomeAsset) => void
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
    type: "CDB" as FixedIncomeAsset["type"],
    institution: "",
    investedValue: "",
    rate: "",
    rateType: "CDI" as FixedIncomeAsset["rateType"],
    maturityDate: "",
    liquidity: "Diária" as FixedIncomeAsset["liquidity"],
  })

  const [variableFormData, setVariableFormData] = useState({
    ticker: "",
    name: "",
    type: "Ação" as VariableIncomeAsset["type"],
    sector: "",
    quantity: "",
    averagePrice: "",
  })

  useEffect(() => {
    if (!investment) return

    if (type === "fixed") {
      const fixed = investment as FixedIncomeAsset
      setFixedFormData({
        name: fixed.name,
        type: fixed.type,
        institution: fixed.institution,
        investedValue: fixed.investedValue.toString(),
        rate: fixed.rate,
        rateType: fixed.rateType,
        maturityDate: fixed.maturityDate,
        liquidity: fixed.liquidity,
      })
    } else {
      const variable = investment as VariableIncomeAsset
      setVariableFormData({
        ticker: variable.ticker,
        name: variable.name,
        type: variable.type,
        sector: variable.sector,
        quantity: variable.quantity.toString(),
        averagePrice: variable.averagePrice.toString(),
      })
    }
  }, [investment, type])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!investment) return

    if (type === "fixed") {
      const fixed = investment as FixedIncomeAsset
      const updated: FixedIncomeAsset = {
        ...fixed,
        name: fixedFormData.name,
        type: fixedFormData.type,
        institution: fixedFormData.institution,
        investedValue: Number(fixedFormData.investedValue),
        rate: fixedFormData.rate,
        rateType: fixedFormData.rateType,
        maturityDate: fixedFormData.maturityDate,
        liquidity: fixedFormData.liquidity,
      }
      onSave(updated)
    } else {
      const variable = investment as VariableIncomeAsset
      const updated: VariableIncomeAsset = {
        ...variable,
        ticker: variableFormData.ticker,
        name: variableFormData.name,
        type: variableFormData.type,
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
                    value={fixedFormData.type}
                    onValueChange={(value) =>
                      setFixedFormData({
                        ...fixedFormData,
                        type: value as FixedIncomeAsset["type"],
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
                      <SelectItem value="Tesouro Direto">Tesouro Direto</SelectItem>
                      <SelectItem value="Debênture">Debênture</SelectItem>
                      <SelectItem value="CRI">CRI</SelectItem>
                      <SelectItem value="CRA">CRA</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label>Instituição</Label>
                  <Input
                    value={fixedFormData.institution}
                    onChange={(e) =>
                      setFixedFormData({ ...fixedFormData, institution: e.target.value })
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
                    value={fixedFormData.investedValue}
                    onChange={(e) =>
                      setFixedFormData({ ...fixedFormData, investedValue: e.target.value })
                    }
                    required
                  />
                </div>

                <div className="space-y-2">
                  <Label>Taxa</Label>
                  <Input
                    value={fixedFormData.rate}
                    onChange={(e) =>
                      setFixedFormData({ ...fixedFormData, rate: e.target.value })
                    }
                    placeholder="Ex: 120% ou IPCA + 6%"
                    required
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Tipo de Taxa</Label>
                  <Select
                    value={fixedFormData.rateType}
                    onValueChange={(value) =>
                      setFixedFormData({
                        ...fixedFormData,
                        rateType: value as FixedIncomeAsset["rateType"],
                      })
                    }
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="CDI">CDI</SelectItem>
                      <SelectItem value="IPCA">IPCA</SelectItem>
                      <SelectItem value="Prefixado">Prefixado</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label>Liquidez</Label>
                  <Select
                    value={fixedFormData.liquidity}
                    onValueChange={(value) =>
                      setFixedFormData({
                        ...fixedFormData,
                        liquidity: value as FixedIncomeAsset["liquidity"],
                      })
                    }
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Diária">Diária</SelectItem>
                      <SelectItem value="No vencimento">No vencimento</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
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
                    value={variableFormData.type}
                    onValueChange={(value) =>
                      setVariableFormData({
                        ...variableFormData,
                        type: value as VariableIncomeAsset["type"],
                      })
                    }
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Ação">Ação</SelectItem>
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
