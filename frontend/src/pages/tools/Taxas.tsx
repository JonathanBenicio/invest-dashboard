import { useState } from "react"
import { TrendingUp, TrendingDown, Calendar, Building2, Edit2, X } from "lucide-react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { economicRates, type EconomicRate } from "@/lib/mock-data"
import { useToast } from "@/hooks/use-toast"

export default function Taxas() {
  const [rates, setRates] = useState(economicRates)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [selectedRate, setSelectedRate] = useState<EconomicRate | null>(null)
  const { toast } = useToast()

  const handleAddRate = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const formData = new FormData(e.currentTarget)

    const newRate: EconomicRate = {
      id: Date.now().toString(),
      name: formData.get('name') as string,
      symbol: formData.get('symbol') as string,
      currentValue: parseFloat(formData.get('currentValue') as string),
      previousValue: parseFloat(formData.get('previousValue') as string),
      variation: 0,
      description: formData.get('description') as string,
      lastUpdate: new Date().toISOString().split('T')[0],
      source: formData.get('source') as string,
    }

    newRate.variation = newRate.currentValue - newRate.previousValue

    setRates([...rates, newRate])
    setIsDialogOpen(false)
    toast({
      title: "Taxa adicionada",
      description: `${newRate.symbol} foi adicionada com sucesso.`,
    })
  }

  const handleEditRate = (updatedRate: EconomicRate | any) => {
    const updatedRateTyped = updatedRate as EconomicRate
    updatedRateTyped.variation = updatedRateTyped.currentValue - updatedRateTyped.previousValue
    setRates(rates.map(r => r.id === updatedRateTyped.id ? updatedRateTyped : r))
    setSelectedRate(null)
    setIsEditDialogOpen(false)
    toast({
      title: "Taxa atualizada",
      description: `${updatedRateTyped.symbol} foi atualizada com sucesso.`,
    })
  }

  const handleDeleteRate = (id: string) => {
    const deletedRate = rates.find(r => r.id === id)
    setRates(rates.filter(r => r.id !== id))
    toast({
      title: "Taxa removida",
      description: `${deletedRate?.symbol} foi removida com sucesso.`,
    })
  }

  const openEditDialog = (rate: EconomicRate) => {
    setSelectedRate(rate)
    setIsEditDialogOpen(true)
  }

  const rateCategories = [
    { name: 'SELIC', rates: rates.filter(r => r.symbol === 'SELIC') },
    { name: 'IPCA', rates: rates.filter(r => r.symbol === 'IPCA') },
    { name: 'CDI', rates: rates.filter(r => r.symbol === 'CDI') },
    { name: 'IR', rates: rates.filter(r => r.symbol === 'IR-PF') },
    { name: 'Câmbio', rates: rates.filter(r => r.symbol === 'USD/BRL') },
  ]

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Taxas e Indicadores</h1>
          <p className="text-muted-foreground">Gerencie taxas econômicas: SELIC, IPCA, IR e outras</p>
        </div>
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <TrendingUp className="h-4 w-4 mr-2" />
              Adicionar Taxa
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-md">
            <DialogHeader>
              <DialogTitle>Adicionar Nova Taxa</DialogTitle>
              <DialogDescription>
                Preencha os dados da nova taxa econômica
              </DialogDescription>
            </DialogHeader>
            <form onSubmit={handleAddRate}>
              <div className="grid gap-4 py-4">
                <div className="grid gap-2">
                  <Label htmlFor="name">Nome da Taxa</Label>
                  <Input id="name" name="name" placeholder="Ex: SELIC" required />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="symbol">Símbolo</Label>
                  <Input id="symbol" name="symbol" placeholder="Ex: SELIC" required />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="currentValue">Valor Atual (%)</Label>
                    <Input id="currentValue" name="currentValue" type="number" step="0.01" placeholder="12.75" required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="previousValue">Valor Anterior (%)</Label>
                    <Input id="previousValue" name="previousValue" type="number" step="0.01" placeholder="12.75" required />
                  </div>
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="description">Descrição</Label>
                  <Input id="description" name="description" placeholder="Descrição da taxa" required />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="source">Fonte</Label>
                  <Input id="source" name="source" placeholder="Ex: Banco Central" required />
                </div>
              </div>
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setIsDialogOpen(false)}>
                  Cancelar
                </Button>
                <Button type="submit" variant="success">Adicionar</Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {/* Rates Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {rates.map((rate) => (
          <Card key={rate.id} className="flex flex-col">
            <CardHeader className="pb-3">
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle className="text-lg">{rate.symbol}</CardTitle>
                  <CardDescription className="text-xs mt-1">{rate.source}</CardDescription>
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-8 w-8"
                  onClick={() => openEditDialog(rate)}
                >
                  <Edit2 className="h-4 w-4" />
                </Button>
              </div>
            </CardHeader>
            <CardContent className="flex-1 space-y-4">
              <div>
                <p className="text-sm text-muted-foreground mb-1">Valor Atual</p>
                <p className="text-3xl font-bold">{rate.currentValue.toFixed(2)}%</p>
              </div>

              <div className="flex items-center gap-4">
                <div className="flex-1">
                  <p className="text-xs text-muted-foreground mb-1">Variação</p>
                  <div className="flex items-center gap-2">
                    {rate.variation > 0 ? (
                      <TrendingUp className="h-4 w-4 text-success" />
                    ) : rate.variation < 0 ? (
                      <TrendingDown className="h-4 w-4 text-destructive" />
                    ) : null}
                    <p className={`font-semibold ${rate.variation > 0 ? 'text-success' : rate.variation < 0 ? 'text-destructive' : 'text-muted-foreground'}`}>
                      {rate.variation > 0 ? '+' : ''}{rate.variation.toFixed(4)}%
                    </p>
                  </div>
                </div>
              </div>

              <div className="text-xs text-muted-foreground flex items-center gap-1 pt-2 border-t">
                <Calendar className="h-3 w-3" />
                {new Date(rate.lastUpdate).toLocaleDateString('pt-BR')}
              </div>

              <Button
                variant="outline"
                size="sm"
                className="w-full text-destructive hover:text-destructive"
                onClick={() => handleDeleteRate(rate.id)}
              >
                <X className="h-3 w-3 mr-1" />
                Remover
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Detailed Table */}
      <Card>
        <CardHeader>
          <CardTitle>Detalhamento de Taxas</CardTitle>
          <CardDescription>Informações completas de todas as taxas registradas</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="rounded-lg border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Taxa/Indicador</TableHead>
                  <TableHead className="text-right">Valor Atual</TableHead>
                  <TableHead className="text-right">Valor Anterior</TableHead>
                  <TableHead className="text-right">Variação</TableHead>
                  <TableHead>Fonte</TableHead>
                  <TableHead>Atualização</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rates.map((rate) => (
                  <TableRow key={rate.id}>
                    <TableCell>
                      <div>
                        <p className="font-medium">{rate.symbol}</p>
                        <p className="text-xs text-muted-foreground">{rate.name}</p>
                      </div>
                    </TableCell>
                    <TableCell className="text-right font-semibold">{rate.currentValue.toFixed(4)}%</TableCell>
                    <TableCell className="text-right text-muted-foreground">{rate.previousValue.toFixed(4)}%</TableCell>
                    <TableCell className="text-right">
                      <Badge variant={rate.variation > 0 ? 'default' : rate.variation < 0 ? 'destructive' : 'secondary'}>
                        <span className={rate.variation > 0 ? 'text-success' : rate.variation < 0 ? 'text-destructive' : ''}>
                          {rate.variation > 0 ? '+' : ''}{rate.variation.toFixed(4)}%
                        </span>
                      </Badge>
                    </TableCell>
                    <TableCell className="text-sm">
                      <div className="flex items-center gap-1">
                        <Building2 className="h-3 w-3 text-muted-foreground" />
                        {rate.source}
                      </div>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {new Date(rate.lastUpdate).toLocaleDateString('pt-BR')}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>

      {/* Edit Dialog */}
      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Editar Taxa</DialogTitle>
            <DialogDescription>
              Atualize os dados da taxa
            </DialogDescription>
          </DialogHeader>
          {selectedRate && (
            <form onSubmit={(e) => {
              e.preventDefault()
              const formData = new FormData(e.currentTarget)
              handleEditRate({
                ...selectedRate,
                name: formData.get('name') as string,
                symbol: formData.get('symbol') as string,
                currentValue: parseFloat(formData.get('currentValue') as string),
                previousValue: parseFloat(formData.get('previousValue') as string),
                description: formData.get('description') as string,
                source: formData.get('source') as string,
              })
            }}>
              <div className="grid gap-4 py-4">
                <div className="grid gap-2">
                  <Label htmlFor="edit-name">Nome da Taxa</Label>
                  <Input id="edit-name" name="name" defaultValue={selectedRate.name} required />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="edit-symbol">Símbolo</Label>
                  <Input id="edit-symbol" name="symbol" defaultValue={selectedRate.symbol} required />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="edit-currentValue">Valor Atual (%)</Label>
                    <Input id="edit-currentValue" name="currentValue" type="number" step="0.01" defaultValue={selectedRate.currentValue} required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="edit-previousValue">Valor Anterior (%)</Label>
                    <Input id="edit-previousValue" name="previousValue" type="number" step="0.01" defaultValue={selectedRate.previousValue} required />
                  </div>
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="edit-description">Descrição</Label>
                  <Input id="edit-description" name="description" defaultValue={selectedRate.description} required />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="edit-source">Fonte</Label>
                  <Input id="edit-source" name="source" defaultValue={selectedRate.source} required />
                </div>
              </div>
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setIsEditDialogOpen(false)}>
                  Cancelar
                </Button>
                <Button type="submit" variant="success">Atualizar</Button>
              </DialogFooter>
            </form>
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
