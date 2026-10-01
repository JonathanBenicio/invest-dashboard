import { useState } from "react"
import { TrendingUp, TrendingDown, Calendar, Building2, Edit2, X, History } from "lucide-react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { useToast } from "@/hooks/use-toast"
import { taxesService } from "@/api/services"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import type { TaxaEconomicaDto } from "@/api/dtos"
import { groupService } from "@/api/services/group.service"

export default function Taxas() {
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [selectedRate, setSelectedRate] = useState<TaxaEconomicaDto | null>(null)
  const [historyRate, setHistoryRate] = useState<TaxaEconomicaDto | null>(null)
  const [groupId, setGroupId] = useState('')
  const groupsQuery = useQuery({ queryKey: ['portfolio-groups'], queryFn: groupService.list })
  const groups = groupsQuery.data?.dados ?? []
  const selectedGroup = groups.find(group => group.id === groupId) ?? groups[0]
  const isAdmin = selectedGroup?.papel === 'Admin'

  const { data: ratesData, isLoading, isError, refetch } = useQuery({
    queryKey: ['taxes', selectedGroup?.id],
    queryFn: () => taxesService.getAll(selectedGroup!.id),
    enabled: !!selectedGroup,
  })

  const rates = ratesData?.dados ?? []
  const formatRate = (value: number, unit: TaxaEconomicaDto['unidade']) => `${value.toLocaleString('pt-BR', { maximumFractionDigits: 4 })}${unit === 'Percentual' ? '%' : unit === 'R$/US$' ? ' R$/US$' : ` ${unit}`}`
  const formatDate = (value: string) => new Intl.DateTimeFormat('pt-BR').format(new Date(value))
  const historyQuery = useQuery({
    queryKey: ['taxes', historyRate?.grupoId, 'history', historyRate?.id],
    queryFn: () => taxesService.getHistory(historyRate!.id, historyRate!.grupoId),
    enabled: !!historyRate,
  })

  const createMutation = useMutation({
    mutationFn: (data: Parameters<typeof taxesService.create>[1]) => taxesService.create(selectedGroup!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['taxes', selectedGroup?.id] })
      setIsDialogOpen(false)
      toast({ title: "Taxa adicionada", description: "Taxa adicionada com sucesso." })
    },
    onError: () => {
      toast({ title: "Erro", description: "Não foi possível adicionar a taxa.", variant: "destructive" })
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Parameters<typeof taxesService.update>[2] }) =>
      taxesService.update(id, selectedGroup!.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['taxes', selectedGroup?.id] })
      setIsEditDialogOpen(false)
      setSelectedRate(null)
      toast({ title: "Taxa atualizada", description: "Taxa atualizada com sucesso." })
    },
    onError: () => {
      toast({ title: "Erro", description: "Não foi possível atualizar a taxa.", variant: "destructive" })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => taxesService.delete(id, selectedGroup!.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['taxes', selectedGroup?.id] })
      toast({ title: "Taxa removida", description: "Taxa removida com sucesso." })
    },
    onError: () => {
      toast({ title: "Erro", description: "Não foi possível remover a taxa.", variant: "destructive" })
    },
  })

  const handleAddRate = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const formData = new FormData(e.currentTarget)
    createMutation.mutate({
      nome: formData.get('name') as string,
      simbolo: formData.get('symbol') as string,
      valorAtual: parseFloat(formData.get('currentValue') as string),
      valorAnterior: parseFloat(formData.get('previousValue') as string),
      descricao: formData.get('description') as string,
      origem: formData.get('source') as string,
      unidade: formData.get('unit') as TaxaEconomicaDto['unidade'],
      periodicidade: formData.get('periodicity') as TaxaEconomicaDto['periodicidade'],
      dataReferencia: formData.get('referenceDate') as string,
    })
  }

  const handleEditRate = (updatedRate: TaxaEconomicaDto) => {
    updateMutation.mutate({
      id: updatedRate.id,
      data: {
        nome: updatedRate.nome,
        simbolo: updatedRate.simbolo,
        valorAtual: updatedRate.valorAtual,
        valorAnterior: updatedRate.valorAnterior,
        descricao: updatedRate.descricao,
        origem: updatedRate.origem,
        unidade: updatedRate.unidade,
        periodicidade: updatedRate.periodicidade,
        dataReferencia: updatedRate.dataReferencia,
      },
    })
  }

  const handleDeleteRate = (id: string) => {
    deleteMutation.mutate(id)
  }

  const openEditDialog = (rate: TaxaEconomicaDto) => {
    setSelectedRate(rate)
    setIsEditDialogOpen(true)
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold text-foreground">Taxas e Indicadores</h1>
        <p className="text-muted-foreground">Carregando...</p>
      </div>
    )
  }

  if (!selectedGroup) {
    return <div className="space-y-4"><h1 className="text-2xl font-bold text-foreground">Taxas e Indicadores</h1><p className="text-muted-foreground">Crie um grupo ou aceite um convite antes de acessar indicadores.</p><Button onClick={() => window.location.assign('/usuarios')}>Abrir grupos e usuários</Button></div>
  }

  if (isError) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-bold text-foreground">Taxas e Indicadores</h1>
        <div role="alert" className="rounded-md border border-destructive/40 p-4 text-sm text-destructive">
          Não foi possível carregar as taxas pela API.
          <Button className="ml-3" variant="outline" onClick={() => void refetch()}>Tentar novamente</Button>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Taxas e Indicadores</h1>
          <p className="text-muted-foreground">Gerencie taxas econômicas: SELIC, IPCA, IR e outras</p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
        <select aria-label="Grupo dos indicadores" className="h-10 rounded-md border bg-background px-3" value={selectedGroup.id} onChange={event => setGroupId(event.target.value)}>{groups.map(group => <option key={group.id} value={group.id}>{group.nome} · {group.papel}</option>)}</select>
        {isAdmin && <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
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
                  <Label htmlFor="rate-template">Modelo vazio (opcional)</Label>
                  <select id="rate-template" className="h-10 rounded-md border bg-background px-3" defaultValue="" onChange={event => {
                    const templates: Record<string, { name: string; unit: string; periodicity: string }> = {
                      SELIC: { name: 'Taxa Selic', unit: 'Percentual', periodicity: 'Anual' },
                      CDI: { name: 'CDI', unit: 'Percentual', periodicity: 'Anual' },
                      IPCA: { name: 'IPCA', unit: 'Percentual', periodicity: 'Mensal' },
                      'USD/BRL': { name: 'Dólar comercial', unit: 'R$/US$', periodicity: 'Pontual' },
                      'IR-PF': { name: 'IR-PF (informativo)', unit: 'Percentual', periodicity: 'Pontual' },
                    }
                    const template = templates[event.target.value]
                    const form = event.currentTarget.closest('form')
                    if (!template || !form) return
                    ;(form.elements.namedItem('name') as HTMLInputElement).value = template.name
                    ;(form.elements.namedItem('symbol') as HTMLInputElement).value = event.target.value
                    ;(form.elements.namedItem('unit') as HTMLSelectElement).value = template.unit
                    ;(form.elements.namedItem('periodicity') as HTMLSelectElement).value = template.periodicity
                  }}>
                    <option value="">Selecione um modelo</option><option value="SELIC">SELIC</option><option value="CDI">CDI</option><option value="IPCA">IPCA</option><option value="USD/BRL">USD/BRL</option><option value="IR-PF">IR-PF informativo</option>
                  </select>
                  <p className="text-xs text-muted-foreground">Os valores ficam em branco para cadastro manual. IR-PF não aplica cálculo fiscal.</p>
                </div>
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
                    <Label htmlFor="currentValue">Valor Atual</Label>
                    <Input id="currentValue" name="currentValue" type="number" step="0.01" placeholder="12.75" required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="previousValue">Valor Anterior</Label>
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
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2"><Label htmlFor="unit">Unidade</Label><select id="unit" name="unit" className="h-10 rounded-md border bg-background px-3" defaultValue="Percentual"><option>Percentual</option><option>R$/US$</option><option>BRL</option><option>Pontos</option></select></div>
                  <div className="grid gap-2"><Label htmlFor="periodicity">Periodicidade</Label><select id="periodicity" name="periodicity" className="h-10 rounded-md border bg-background px-3" defaultValue="Mensal"><option value="Diaria">Diária</option><option value="Mensal">Mensal</option><option value="Anual">Anual</option><option value="Pontual">Pontual</option></select></div>
                </div>
                <div className="grid gap-2"><Label htmlFor="referenceDate">Data de referência</Label><Input id="referenceDate" name="referenceDate" type="date" defaultValue={new Date().toISOString().slice(0, 10)} required /></div>
              </div>
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setIsDialogOpen(false)}>
                  Cancelar
                </Button>
                <Button type="submit" variant="success">Adicionar</Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>}
        </div>
      </div>

      {rates.length === 0 ? (
        <Card role="status">
          <CardHeader>
            <CardTitle>Nenhuma taxa configurada</CardTitle>
            <CardDescription>Adicione uma taxa manual para usá-la como referência nas telas de indicadores.</CardDescription>
          </CardHeader>
          <CardContent>
            <Button onClick={() => setIsDialogOpen(true)}>Adicionar primeira taxa</Button>
          </CardContent>
        </Card>
      ) : (
        <>
      {/* Rates Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {rates.map((rate: TaxaEconomicaDto) => (
          <Card key={rate.id} className="flex flex-col">
            <CardHeader className="pb-3">
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle className="text-lg">{rate.simbolo}</CardTitle>
                  <CardDescription className="text-xs mt-1">{rate.origem}</CardDescription>
                </div>
                <div className="flex items-center gap-1">
                  <Button variant="ghost" size="icon" className="h-8 w-8" aria-label={`Histórico de ${rate.simbolo}`} onClick={() => setHistoryRate(rate)}>
                    <History className="h-4 w-4" />
                  </Button>
                  {isAdmin && <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    aria-label={`Editar ${rate.simbolo}`}
                    onClick={() => openEditDialog(rate)}
                  >
                    <Edit2 className="h-4 w-4" />
                  </Button>}
                </div>
              </div>
            </CardHeader>
            <CardContent className="flex-1 space-y-4">
              <div>
                <p className="text-sm text-muted-foreground mb-1">Valor Atual</p>
                <p className="text-3xl font-bold">{formatRate(rate.valorAtual, rate.unidade)}</p>
                <p className="mt-1 text-xs text-muted-foreground">{rate.periodicidade} · referência {formatDate(rate.dataReferencia)}</p>
              </div>

              <div className="flex items-center gap-4">
                <div className="flex-1">
                  <p className="text-xs text-muted-foreground mb-1">Variação</p>
                  <div className="flex items-center gap-2">
                    {rate.variacao > 0 ? (
                      <TrendingUp className="h-4 w-4 text-success" />
                    ) : rate.variacao < 0 ? (
                      <TrendingDown className="h-4 w-4 text-destructive" />
                    ) : null}
                    <p className={`font-semibold ${rate.variacao > 0 ? 'text-success' : rate.variacao < 0 ? 'text-destructive' : 'text-muted-foreground'}`}>
                      {rate.variacao > 0 ? '+' : ''}{formatRate(rate.variacao, rate.unidade)}
                    </p>
                  </div>
                </div>
              </div>

              <div className="text-xs text-muted-foreground flex items-center gap-1 pt-2 border-t">
                <Calendar className="h-3 w-3" />
                Atualizada em {formatDate(rate.atualizadoEm)}
              </div>

              {isAdmin && <Button
                variant="outline"
                size="sm"
                className="w-full text-destructive hover:text-destructive"
                onClick={() => handleDeleteRate(rate.id)}
              >
                <X className="h-3 w-3 mr-1" />
                Remover
              </Button>}
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
                {rates.map((rate: TaxaEconomicaDto) => (
                  <TableRow key={rate.id}>
                    <TableCell>
                      <div>
                        <p className="font-medium">{rate.simbolo}</p>
                        <p className="text-xs text-muted-foreground">{rate.nome}</p>
                      </div>
                    </TableCell>
                    <TableCell className="text-right font-semibold">{formatRate(rate.valorAtual, rate.unidade)}</TableCell>
                    <TableCell className="text-right text-muted-foreground">{formatRate(rate.valorAnterior, rate.unidade)}</TableCell>
                    <TableCell className="text-right">
                      <Badge variant={rate.variacao > 0 ? 'default' : rate.variacao < 0 ? 'destructive' : 'secondary'}>
                        <span className={rate.variacao > 0 ? 'text-success' : rate.variacao < 0 ? 'text-destructive' : ''}>
                          {rate.variacao > 0 ? '+' : ''}{formatRate(rate.variacao, rate.unidade)}
                        </span>
                      </Badge>
                    </TableCell>
                    <TableCell className="text-sm">
                      <div className="flex items-center gap-1">
                        <Building2 className="h-3 w-3 text-muted-foreground" />
                        {rate.origem}
                      </div>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {formatDate(rate.dataReferencia)} · {rate.periodicidade}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
        </>
      )}

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
                nome: formData.get('name') as string,
                simbolo: formData.get('symbol') as string,
                valorAtual: parseFloat(formData.get('currentValue') as string),
                valorAnterior: parseFloat(formData.get('previousValue') as string),
                descricao: formData.get('description') as string,
                origem: formData.get('source') as string,
                unidade: formData.get('edit-unit') as TaxaEconomicaDto['unidade'],
                periodicidade: formData.get('edit-periodicity') as TaxaEconomicaDto['periodicidade'],
                dataReferencia: formData.get('edit-referenceDate') as string,
              })
            }}>
              <div className="grid gap-4 py-4">
                <div className="grid gap-2">
                  <Label htmlFor="edit-name">Nome da Taxa</Label>
                  <Input id="edit-name" name="name" defaultValue={selectedRate.nome} required />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="edit-symbol">Símbolo</Label>
                  <Input id="edit-symbol" name="symbol" defaultValue={selectedRate.simbolo} required />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="edit-currentValue">Valor Atual</Label>
                    <Input id="edit-currentValue" name="currentValue" type="number" step="0.01" defaultValue={selectedRate.valorAtual} required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="edit-previousValue">Valor Anterior</Label>
                    <Input id="edit-previousValue" name="previousValue" type="number" step="0.01" defaultValue={selectedRate.valorAnterior} required />
                  </div>
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="edit-description">Descrição</Label>
                  <Input id="edit-description" name="description" defaultValue={selectedRate.descricao} required />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="edit-source">Fonte</Label>
                  <Input id="edit-source" name="source" defaultValue={selectedRate.origem} required />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2"><Label htmlFor="edit-unit">Unidade</Label><select id="edit-unit" name="edit-unit" className="h-10 rounded-md border bg-background px-3" defaultValue={selectedRate.unidade}><option>Percentual</option><option>R$/US$</option><option>BRL</option><option>Pontos</option></select></div>
                  <div className="grid gap-2"><Label htmlFor="edit-periodicity">Periodicidade</Label><select id="edit-periodicity" name="edit-periodicity" className="h-10 rounded-md border bg-background px-3" defaultValue={selectedRate.periodicidade}><option value="Diaria">Diária</option><option value="Mensal">Mensal</option><option value="Anual">Anual</option><option value="Pontual">Pontual</option></select></div>
                </div>
                <div className="grid gap-2"><Label htmlFor="edit-referenceDate">Data de referência</Label><Input id="edit-referenceDate" name="edit-referenceDate" type="date" defaultValue={selectedRate.dataReferencia} required /></div>
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
      <Dialog open={!!historyRate} onOpenChange={open => { if (!open) setHistoryRate(null) }}>
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Histórico · {historyRate?.simbolo}</DialogTitle>
            <DialogDescription>Valores anteriores e atualizações manuais registradas neste grupo.</DialogDescription>
          </DialogHeader>
          {historyQuery.isLoading ? <p className="py-6 text-sm text-muted-foreground">Carregando histórico…</p> :
            historyQuery.isError ? <div role="alert" className="space-y-3 py-4 text-sm text-destructive">Não foi possível carregar o histórico.<Button variant="outline" size="sm" onClick={() => void historyQuery.refetch()}>Tentar novamente</Button></div> :
              (historyQuery.data?.dados.length ?? 0) === 0 ? <p role="status" className="py-6 text-sm text-muted-foreground">Nenhuma alteração registrada.</p> :
                <div className="overflow-x-auto rounded-md border"><Table>
                  <TableHeader><TableRow><TableHead>Atualizado em</TableHead><TableHead>Referência</TableHead><TableHead className="text-right">Anterior</TableHead><TableHead className="text-right">Novo</TableHead><TableHead>Origem</TableHead><TableHead>Responsável</TableHead></TableRow></TableHeader>
                  <TableBody>{historyQuery.data?.dados.map((entry, index) => <TableRow key={`${entry.atualizadoEmUtc}-${index}`}>
                    <TableCell className="whitespace-nowrap">{formatDate(entry.atualizadoEmUtc)}</TableCell>
                    <TableCell className="whitespace-nowrap">{formatDate(entry.dataReferencia)}</TableCell>
                    <TableCell className="text-right"><div>{entry.unidadeAnterior ? formatRate(entry.valorAnterior, entry.unidadeAnterior) : `${entry.valorAnterior.toLocaleString('pt-BR')} (unidade não registrada)`}</div>{entry.periodicidadeAnterior && <div className="text-xs text-muted-foreground">{entry.periodicidadeAnterior}</div>}</TableCell>
                    <TableCell className="text-right font-medium"><div>{entry.unidadeNova ? formatRate(entry.valorNovo, entry.unidadeNova) : `${entry.valorNovo.toLocaleString('pt-BR')} (unidade não registrada)`}</div>{entry.periodicidadeNova && <div className="text-xs text-muted-foreground">{entry.periodicidadeNova}</div>}</TableCell>
                    <TableCell>{entry.origem || '—'}</TableCell>
                    <TableCell><span title={entry.responsavelUserId}>{entry.responsavelUserId ? `Usuário ${entry.responsavelUserId.slice(0, 8)}` : '—'}</span></TableCell>
                  </TableRow>)}</TableBody>
                </Table></div>}
        </DialogContent>
      </Dialog>
    </div>
  )
}
