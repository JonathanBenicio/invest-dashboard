import { useState, useMemo, useRef } from "react"
import { useQuery, useQueryClient } from "@tanstack/react-query"
import { useNavigate } from "@tanstack/react-router"
import { Plus } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { formatCurrency, localDateInputToISOString, toLocalDateInputValue } from "@/lib/utils"
import { useToast } from "@/hooks/use-toast"
import { EditInvestmentDialog } from "@/components/dialogs/EditInvestmentDialog"
import { DeleteConfirmDialog } from "@/components/dialogs/DeleteConfirmDialog"
import { useFixedIncomeInvestments } from "@/hooks/use-investments"
import { usePortfolios } from "@/hooks/use-portfolios"
import { FixedIncomeTable } from "@/components/investments/FixedIncomeTable"
import { investmentService } from "@/api/services/investment.service"
import { portfolioService } from "@/api/services/portfolio.service"
import { groupService } from "@/api/services/group.service"
import type { RendaFixaDto, InvestimentoFiltros, TipoRendaFixa, CriarRendaFixaRequest } from "@/api/dtos"
import { PaginationState, SortingState, ColumnFiltersState } from "@tanstack/react-table"

export default function FixedIncome() {
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const [selectedAsset, setSelectedAsset] = useState<RendaFixaDto | null>(null)
  const [groupFilterId, setGroupFilterId] = useState("")
  const queryClient = useQueryClient()
  const { toast } = useToast()
  const { data: portfolioResponse, isError: isPortfolioError, refetch: refetchPortfolios } = usePortfolios({ pagina: 1, itensPorPagina: 100 })
  const { data: groupsResponse } = useQuery({ queryKey: ['portfolio-groups'], queryFn: groupService.list })
  const groups = groupsResponse?.dados ?? []
  const selectedGroup = groups.find(group => group.id === groupFilterId)
  const groupNames = Object.fromEntries(groups.map(group => [group.id, group.nome]))
  const portfolios = portfolioResponse?.dados ?? []
  const selectablePortfolios = groupFilterId
    ? portfolios.filter(portfolio => portfolio.grupoId === groupFilterId)
    : portfolios
  const idempotencyKey = useRef(crypto.randomUUID())

  // Table State
  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: 10,
  })
  const [sorting, setSorting] = useState<SortingState>([])
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([])
  const [globalFilter, setGlobalFilter] = useState("")

  // Construct filters for API
  const filters: InvestimentoFiltros = useMemo(() => {
    const apiFilters: InvestimentoFiltros = {
      pagina: pagination.pageIndex + 1,
      itensPorPagina: pagination.pageSize,
      grupoId: groupFilterId || undefined,
      busca: globalFilter || undefined,
      ordenarPor: sorting[0]?.id,
      ordem: sorting[0]?.desc ? 'desc' : 'asc',
    }

    // Map column filters to API params
    const subtypeFilter = columnFilters.find(f => f.id === 'subtipo')?.value
    if (subtypeFilter) {
      apiFilters.subtipo = subtypeFilter as TipoRendaFixa
    }

    const issuerFilter = columnFilters.find(f => f.id === 'emissor')?.value
    if (issuerFilter) {
      apiFilters.emissor = issuerFilter as string
    }

    return apiFilters
  }, [pagination, sorting, columnFilters, globalFilter, groupFilterId])

  const { data: investmentsData, isLoading, isError, refetch } = useFixedIncomeInvestments(filters)
  const projectionQuery = useQuery({
    queryKey: ['fixed-income-projection', groupFilterId],
    queryFn: () => portfolioService.getFixedIncomeProjection(groupFilterId || undefined),
  })
  const projection = projectionQuery.data?.dados

  const assets = (investmentsData?.dados || []) as RendaFixaDto[]
  const pageCount = investmentsData?.paginacao?.totalPaginas || 0

  const totalInvested = assets.reduce((acc, asset) => acc + asset.totalInvestido, 0)
  const totalCurrent = assets.reduce((acc, asset) => acc + asset.valorAtual, 0)
  const totalProfit = totalCurrent - totalInvested

  const assetTypes = ['CDB', 'LCI', 'LCA', 'TESOURO_DIRETO', 'DEBENTURE', 'CRI', 'CRA']

  const handleAddAsset = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const formData = new FormData(e.currentTarget)

    const newAssetData: CriarRendaFixaRequest = {
      carteiraId: formData.get('portfolioId') as string,
      nome: formData.get('name') as string,
      subtipo: formData.get('type') as TipoRendaFixa,
      emissor: formData.get('institution') as string,
      valorPrincipal: Number(formData.get('investedValue')),
      valorExtrato: Number(formData.get('statementValue')),
      taxaJuros: parseFloat(formData.get('rate')?.toString().replace('%', '') || '0'),
      indexador: formData.get('rateType') as 'CDI' | 'IPCA' | 'SELIC' | 'PREFIXADO',
      dataCompra: localDateInputToISOString(String(formData.get('purchaseDate'))),
      dataVencimento: new Date(`${formData.get('maturityDate')}T12:00:00`).toISOString(),
      chaveIdempotencia: idempotencyKey.current,
      liquidez: String(formData.get('liquidity') ?? '').trim() || undefined,
      convencao: String(formData.get('convention') ?? '').trim() || undefined,
    }

    try {
      await investmentService.createFixedIncome(newAssetData)
      idempotencyKey.current = crypto.randomUUID()
      setIsDialogOpen(false)
      toast({
        title: "Ativo adicionado",
        description: `${newAssetData.nome} foi adicionado à sua carteira.`,
      })
      await Promise.all([
        refetch(),
        queryClient.invalidateQueries({ queryKey: ['fixed-income-projection'] }),
      ])
    } catch (error) {
      toast({
        title: "Erro ao adicionar",
        description: "Ocorreu um erro ao adicionar o ativo.",
        variant: "destructive",
      })
    }
  }

  const handleEditAsset = async (updatedAsset: RendaFixaDto, valuationDate: string) => {
    if (!selectedAsset) return

    try {
       await investmentService.update(selectedAsset.id, {
         valorTotal: updatedAsset.valorAtual,
         data: new Date(`${valuationDate}T12:00:00`).toISOString(),
       })

       setIsEditDialogOpen(false)
        toast({
        title: "Ativo atualizado",
        description: "O ativo foi atualizado com sucesso.",
        })
        await Promise.all([
          refetch(),
          queryClient.invalidateQueries({ queryKey: ['fixed-income-projection'] }),
        ])
    } catch (error) {
         toast({
        title: "Erro ao atualizar",
        description: "Falha ao atualizar.",
        variant: "destructive",
        })
    }
  }


  const handleDeleteAsset = async (id: string) => {
    try {
        await investmentService.delete(id)
        setIsDeleteDialogOpen(false)
        setSelectedAsset(null)
        toast({
        title: "Ativo removido",
        description: "O ativo foi removido da sua carteira.",
        })
        await Promise.all([
          refetch(),
          queryClient.invalidateQueries({ queryKey: ['fixed-income-projection'] }),
        ])
    } catch (error) {
        toast({
            title: "Erro ao remover",
            description: "Falha ao remover o ativo.",
            variant: "destructive"
        })
    }
  }

  const openEditDialog = (asset: RendaFixaDto) => {
    setSelectedAsset(asset)
    setIsEditDialogOpen(true)
  }

  const openDeleteDialog = (asset: RendaFixaDto) => {
    setSelectedAsset(asset)
    setIsDeleteDialogOpen(true)
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Renda Fixa</h1>
          <p className="text-muted-foreground">Acompanhe contratos e informe avaliações do extrato.</p>
        </div>
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          <Label htmlFor="fixed-income-group">Grupo</Label>
          <select id="fixed-income-group" className="h-10 rounded-md border bg-background px-3" value={groupFilterId} onChange={event => {
            setGroupFilterId(event.target.value)
            setPagination(current => ({ ...current, pageIndex: 0 }))
          }}>
            <option value="">Todos os grupos</option>
            {groups.map(group => <option key={group.id} value={group.id}>{group.nome}</option>)}
          </select>
        </div>
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogTrigger asChild>
            <Button disabled={selectablePortfolios.length === 0}>
              <Plus className="h-4 w-4 mr-2" />
              Adicionar contrato
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-md">
            <DialogHeader>
              <DialogTitle>Adicionar Ativo de Renda Fixa</DialogTitle>
              <DialogDescription>
                Preencha os dados do novo ativo
              </DialogDescription>
            </DialogHeader>
            <form onSubmit={handleAddAsset}>
              <div className="grid gap-4 py-4">
                <div className="grid gap-2">
                  <Label htmlFor="portfolioId">Carteira</Label>
                  <Select name="portfolioId" required>
                    <SelectTrigger>
                      <SelectValue placeholder="Selecione a carteira" />
                    </SelectTrigger>
                    <SelectContent>
                      {selectablePortfolios.map((portfolio) => (
                        <SelectItem key={portfolio.id} value={portfolio.id}>
                          {portfolio.nome}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="name">Nome do Ativo</Label>
                  <Input id="name" name="name" placeholder="Ex: CDB Banco Inter" required />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="type">Tipo</Label>
                    <Select name="type" defaultValue="CDB">
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {assetTypes.map(type => (
                          <SelectItem key={type} value={type}>{type}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="institution">Instituição</Label>
                    <Input id="institution" name="institution" placeholder="Ex: Banco Inter" required />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="investedValue">Valor Investido</Label>
                    <Input id="investedValue" name="investedValue" type="number" step="0.01" placeholder="10000" required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="rate">Taxa</Label>
                    <Input id="rate" name="rate" placeholder="Ex: 120" required />
                  </div>
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="statementValue">Valor atual do extrato</Label>
                  <Input id="statementValue" name="statementValue" type="number" min="0" step="0.01" placeholder="10000" required />
                </div>
                <div className="grid gap-2">
                    <Label htmlFor="rateType">Indexador</Label>
                    <Select name="rateType" defaultValue="CDI">
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="CDI">CDI</SelectItem>
                        <SelectItem value="IPCA">IPCA</SelectItem>
                        <SelectItem value="SELIC">SELIC</SelectItem>
                        <SelectItem value="PREFIXADO">Prefixado</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="purchaseDate">Data de Compra</Label>
                    <Input id="purchaseDate" name="purchaseDate" type="date" defaultValue={toLocalDateInputValue()} max={toLocalDateInputValue()} required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="maturityDate">Data de Vencimento</Label>
                    <Input id="maturityDate" name="maturityDate" type="date" required />
                  </div>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="grid gap-2"><Label htmlFor="liquidity">Liquidez (opcional)</Label><Input id="liquidity" name="liquidity" maxLength={80} placeholder="Ex.: diária após carência" /></div>
                <div className="grid gap-2"><Label htmlFor="convention">Convenção de cálculo (opcional)</Label><Input id="convention" name="convention" maxLength={80} placeholder="Ex.: dias úteis, base 252" /></div>
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

      {isPortfolioError && <div role="alert" className="rounded-md border border-destructive/40 p-4 text-sm text-destructive">Não foi possível carregar carteiras para registrar um contrato. <Button variant="outline" size="sm" onClick={() => void refetchPortfolios()}>Tentar novamente</Button></div>}
      {isError && <div role="alert" className="rounded-md border border-destructive/40 p-4 text-sm text-destructive">Não foi possível carregar as posições de renda fixa. <Button variant="outline" size="sm" onClick={() => void refetch()}>Tentar novamente</Button></div>}
      {!isLoading && !isError && assets.length === 0 && <p role="status" className="rounded-md border p-4 text-sm text-muted-foreground">Nenhuma posição de renda fixa encontrada.</p>}

      {/* Summary Cards */}
      <Card>
        <CardHeader><CardTitle>Projeção bruta até o vencimento</CardTitle><p className="text-sm text-muted-foreground">{selectedGroup ? `Grupo: ${selectedGroup.nome}` : 'Todos os grupos acessíveis'}</p></CardHeader>
        <CardContent className="space-y-2">
          {projectionQuery.isLoading ? <p className="text-sm text-muted-foreground">Calculando com os últimos valores de extrato…</p> :
            projectionQuery.isError ? <p className="text-sm text-muted-foreground">Não foi possível carregar a projeção consolidada.</p> :
            projection?.quantidadePosicoes === 0 ? <p className="text-sm text-muted-foreground">Nenhuma posição de renda fixa acessível.</p> :
            projection?.estaCompleta ? <div className="grid gap-3 sm:grid-cols-2"><p>Valor observado: <strong>{formatCurrency(projection.valorObservado)}</strong></p><p>Estimativa bruta: <strong>{formatCurrency(projection.valorProjetadoBruto ?? 0)}</strong></p></div> :
            <><p className="text-sm text-muted-foreground">Sem projeção consolidada porque faltam dados ou premissas para todas as posições.</p><p className="text-sm">Valor observado: <strong>{formatCurrency(projection?.valorObservado ?? 0)}</strong></p><p className="text-xs text-muted-foreground">Posições pendentes: {projection?.posicoesSemProjecao.join(', ')}</p></>}
          <p className="text-xs text-muted-foreground">Estimativa bruta a partir de extrato, sem impostos, aportes ou reinvestimento. Vencimentos preservam o valor na posição e não movimentam caixa.</p>
        </CardContent>
      </Card>
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total Investido (Página)</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(totalInvested)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Valor Atual (Página)</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(totalCurrent)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Rentabilidade (Página)</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${totalProfit >= 0 ? 'text-success' : 'text-destructive'}`}>
              {formatCurrency(totalProfit)}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Table */}
      <Card>
        <CardHeader>
            <CardTitle>Meus Investimentos</CardTitle>
        </CardHeader>
        <CardContent>
            <FixedIncomeTable
                data={assets}
                groupNames={groupNames}
                pageCount={pageCount}
                pagination={pagination}
                setPagination={setPagination}
                sorting={sorting}
                setSorting={setSorting}
                columnFilters={columnFilters}
                setColumnFilters={setColumnFilters}
                globalFilter={globalFilter}
                setGlobalFilter={setGlobalFilter}
                isLoading={isLoading}
                onEdit={openEditDialog}
                onDelete={openDeleteDialog}
            />
        </CardContent>
      </Card>

      <EditInvestmentDialog
        open={isEditDialogOpen}
        onOpenChange={setIsEditDialogOpen}
        investment={selectedAsset}
        type="fixed"
        onSave={(updated, date) => {
          if (updated.tipo === 'fixed_income') void handleEditAsset(updated as RendaFixaDto, date)
        }}
      />

      <DeleteConfirmDialog
        open={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
        title="Excluir lançamento incorreto?"
        description={`Isso apagará todas as movimentações e avaliações de ${selectedAsset?.nome}. Para retirar o investimento e preservar o histórico, registre um resgate.`}
        onConfirm={() => {
          if (selectedAsset) {
            handleDeleteAsset(selectedAsset.id)
          }
        }}
      />
    </div>
  )
}
