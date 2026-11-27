import { useState, useMemo, useRef } from "react"
import { useQuery } from "@tanstack/react-query"
import { useNavigate, useSearch } from "@tanstack/react-router"
import { Plus, ArrowUpRight, ArrowDownRight } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { formatCurrency, localDateInputToISOString, toLocalDateInputValue } from "@/lib/utils"
import { useToast } from "@/hooks/use-toast"
import { EditInvestmentDialog } from "@/components/dialogs/EditInvestmentDialog"
import { DeleteConfirmDialog } from "@/components/dialogs/DeleteConfirmDialog"
import { useAllAccessiblePortfolios } from "@/hooks/use-portfolios"
import { VariableIncomeTable } from "@/components/investments/VariableIncomeTable"
import { StockSearch } from "@/components/investments/StockSearch"
import { investmentService } from "@/api/services/investment.service"
import { groupService } from "@/api/services/group.service"
import { InvestmentScopeFilters } from "@/components/investments/InvestmentScopeFilters"
import { InvestmentSituationFilter, type InvestmentSituationFilterValue } from "@/components/investments/InvestmentSituationFilter"
import { variableIncomeRoute } from "@/router"
import type { RendaVariavelDto, InvestimentoFiltros, TipoRendaVariavel, CriarRendaVariavelRequest, ResultadoBuscaMercadoDto } from "@/api/dtos"
import type { PosicaoInvestimentoConsolidadoDto } from "@/api/dtos/investment.dto"
import { PaginationState, SortingState, ColumnFiltersState } from "@tanstack/react-table"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

export default function VariableIncome() {
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const [selectedAsset, setSelectedAsset] = useState<RendaVariavelDto | null>(null)
  const [ticker, setTicker] = useState("")
  const [name, setName] = useState("")
  const [sector, setSector] = useState("")
  const idempotencyKey = useRef(crypto.randomUUID())
  const scope = useSearch({ from: variableIncomeRoute.id })
  const groupFilterId = scope.groupId ?? ''
  const holderFilterId = scope.holderId ?? ''
  const portfolioFilterId = scope.portfolioId ?? ''
  const situationFilter: InvestmentSituationFilterValue = scope.situacao ?? 'balance'
  const navigate = useNavigate()
  const { data: portfolioResponse, isError: isPortfolioError, refetch: refetchPortfolios } = useAllAccessiblePortfolios()
  const portfolios = portfolioResponse?.dados ?? []
  const { data: groupsResponse } = useQuery({ queryKey: ['portfolio-groups'], queryFn: groupService.list })
  const groups = groupsResponse?.dados ?? []
  const selectedGroup = groups.find(group => group.id === groupFilterId)
  const selectablePortfolios = portfolios.filter(portfolio =>
    (!groupFilterId || portfolio.grupoId === groupFilterId) &&
    (!holderFilterId || portfolio.titularId === holderFilterId))
  const setScope = (next: { groupId?: string; holderId?: string; portfolioId?: string }) => {
    void navigate({ to: '/renda-variavel', search: { ...scope, ...next } })
  }
  const { toast } = useToast()

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
      titularId: holderFilterId || undefined,
      carteiraId: portfolioFilterId || undefined,
      situacao: situationFilter === 'balance' ? undefined : situationFilter,
      busca: globalFilter || undefined,
      ordenarPor: sorting[0]?.id,
      ordem: sorting[0]?.desc ? 'desc' : 'asc',
    }

    const subtypeFilter = columnFilters.find(f => f.id === 'subtipo')?.value
    if (subtypeFilter) {
      apiFilters.subtipo = subtypeFilter as TipoRendaVariavel
    }

    const sectorFilter = columnFilters.find(f => f.id === 'setor')?.value
    if (typeof sectorFilter === 'string') apiFilters.setor = sectorFilter

    return apiFilters
  }, [pagination, sorting, columnFilters, globalFilter, groupFilterId, holderFilterId, portfolioFilterId, situationFilter])

  const { data: investmentsData, isLoading, isError, refetch } = useQuery({
    queryKey: ['investments', 'consolidated', 'variable-income', filters],
    queryFn: () => investmentService.getConsolidated({ ...filters, tipo: 'variable_income' }),
  })

  // Lista e detalhe usam o mesmo retrato de valores retornado pela API.
  const assets = useMemo(
    () => investmentsData?.dados ?? [],
    [investmentsData?.dados],
  )
  const pageCount = investmentsData?.paginacao?.totalPaginas || 0

  const totalInvested = assets.reduce((acc, asset) => acc + asset.totalInvestido, 0)
  const totalCurrent = assets.reduce((acc, asset) => acc + asset.valorAtual, 0)
  const totalProfit = totalCurrent - totalInvested

  const assetTypes = ['ACAO', 'FII', 'ETF', 'BDR']

  const handleAddAsset = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const formData = new FormData(e.currentTarget)

    const newAssetData: CriarRendaVariavelRequest = {
      carteiraId: formData.get('portfolioId') as string,
      ticker: ticker.toUpperCase(),
      subtipo: formData.get('type') as TipoRendaVariavel,
      quantidade: parseInt(formData.get('quantity') as string),
      precoUnitario: Number(formData.get('averagePrice')),
      taxas: Number(formData.get('fees') || 0),
      dataTransacao: localDateInputToISOString(String(formData.get('transactionDate'))),
      chaveIdempotencia: idempotencyKey.current,
      nome: name,
      setor: sector,
    }

    try {
      await investmentService.createVariableIncome(newAssetData)
      idempotencyKey.current = crypto.randomUUID()
      setIsDialogOpen(false)
      toast({
        title: "Ativo adicionado",
        description: `${newAssetData.ticker} foi adicionado à sua carteira.`,
      })
      refetch()
    } catch (error) {
      toast({
        title: "Erro ao adicionar",
        description: "Ocorreu um erro ao adicionar o ativo.",
        variant: "destructive",
      })
    }
  }

  const handleStockSelect = (quote: ResultadoBuscaMercadoDto) => {
    setTicker(quote.simbolo)
    setName(quote.nome)
    setSector(quote.setor ?? '')
  }

  const handleEditAsset = async (updatedAsset: RendaVariavelDto, valuationDate: string) => {
    if (!selectedAsset) return

    try {
      await investmentService.update(selectedAsset.id, {
        valorTotal: updatedAsset.valorAtual,
        data: new Date(`${valuationDate}T12:00:00`).toISOString(),
      })

      setIsEditDialogOpen(false)
      toast({
        title: "Ativo atualizado",
        description: `${updatedAsset.ticker} foi atualizado com sucesso.`,
      })
      refetch()
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
      refetch()
    } catch (error) {
      toast({
        title: "Erro ao remover",
        description: "Falha ao remover o ativo.",
        variant: "destructive"
      })
    }
  }

  const openEditDialog = (asset: RendaVariavelDto) => {
    setSelectedAsset(asset)
    setIsEditDialogOpen(true)
  }

  const openDeleteDialog = (asset: RendaVariavelDto) => {
    setSelectedAsset(asset)
    setIsDeleteDialogOpen(true)
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Renda Variável</h1>
          <p className="text-muted-foreground">Gerencie suas ações, FIIs, ETFs e BDRs</p>
        </div>
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogTrigger asChild>
            <Button disabled={portfolios.length === 0}>
              <Plus className="h-4 w-4 mr-2" />
              Adicionar Ativo
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-md">
            <DialogHeader>
              <DialogTitle>Adicionar Ativo de Renda Variável</DialogTitle>
              <DialogDescription>
                Registre uma nova compra na sua carteira
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
                  <Label>Pesquisar Ativo (Brapi)</Label>
                  <StockSearch onSelect={handleStockSelect} />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="ticker">Ticker</Label>
                    <Input
                      id="ticker"
                      name="ticker"
                      placeholder="Ex: PETR4"
                      value={ticker}
                      onChange={(e) => setTicker(e.target.value.toUpperCase())}
                      required
                    />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="type">Tipo</Label>
                    <Select name="type" defaultValue="ACAO">
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
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="name">Nome da Empresa</Label>
                  <Input
                    id="name"
                    name="name"
                    placeholder="Ex: Petrobras PN"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    required
                  />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="sector">Setor</Label>
                  <Input
                    id="sector"
                    name="sector"
                    placeholder="Ex: Petróleo e Gás"
                    value={sector}
                    onChange={(e) => setSector(e.target.value)}
                    required
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="quantity">Quantidade</Label>
                    <Input id="quantity" name="quantity" type="number" min="0.00000001" step="any" placeholder="100" required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="averagePrice">Preço Médio</Label>
                    <Input id="averagePrice" name="averagePrice" type="number" min="0.00000001" step="0.0001" placeholder="32.50" required />
                  </div>
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="transactionDate">Data da operação</Label>
                  <Input
                    id="transactionDate"
                    name="transactionDate"
                    type="date"
                    max={toLocalDateInputValue()}
                    defaultValue={toLocalDateInputValue()}
                  />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="fees">Taxas da compra</Label>
                  <Input id="fees" name="fees" type="number" min="0" step="0.01" defaultValue="0" />
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

      <InvestmentScopeFilters
        groups={groups}
        portfolios={portfolios}
        groupId={groupFilterId}
        holderId={holderFilterId}
        portfolioId={portfolioFilterId}
        onGroupChange={groupId => { setPagination(current => ({ ...current, pageIndex: 0 })); setScope({ groupId: groupId || undefined, holderId: undefined, portfolioId: undefined }) }}
        onHolderChange={holderId => { setPagination(current => ({ ...current, pageIndex: 0 })); setScope({ holderId: holderId || undefined, portfolioId: undefined }) }}
        onPortfolioChange={portfolioId => { setPagination(current => ({ ...current, pageIndex: 0 })); setScope({ portfolioId: portfolioId || undefined }) }}
      />
      <InvestmentSituationFilter
        value={situationFilter}
        onChange={value => {
          setPagination(current => ({ ...current, pageIndex: 0 }))
          void navigate({ to: '/renda-variavel', search: { ...scope, situacao: value === 'balance' ? undefined : value } })
        }}
      />
      {selectedGroup && <p className="text-sm text-muted-foreground">
        {portfolioFilterId ? `Carteira selecionada · ${selectablePortfolios.find(portfolio => portfolio.id === portfolioFilterId)?.nome ?? ''}`
          : `${selectedGroup.nome} · ${holderFilterId ? selectablePortfolios[0]?.titular ?? 'titular selecionado' : 'todas as carteiras acessíveis'}`}
      </p>}

      {isPortfolioError && <div role="alert" className="rounded-md border border-destructive/40 p-4 text-sm text-destructive">Não foi possível carregar carteiras para registrar uma operação. <Button variant="outline" size="sm" onClick={() => void refetchPortfolios()}>Tentar novamente</Button></div>}
      {isError && <div role="alert" className="rounded-md border border-destructive/40 p-4 text-sm text-destructive">Não foi possível carregar as posições de renda variável. <Button variant="outline" size="sm" onClick={() => void refetch()}>Tentar novamente</Button></div>}
      {!isLoading && !isError && assets.length === 0 && <p role="status" className="rounded-md border p-4 text-sm text-muted-foreground">Nenhuma posição de renda variável encontrada.</p>}

      {/* Summary Cards */}
      < div className="grid gap-4 md:grid-cols-3" >
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
            <CardTitle className="text-sm font-medium text-muted-foreground">Resultado (Página)</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold flex items-center gap-2 ${totalProfit >= 0 ? 'text-success' : 'text-destructive'}`}>
              {totalProfit >= 0 ? <ArrowUpRight className="h-5 w-5" /> : <ArrowDownRight className="h-5 w-5" />}
              {formatCurrency(totalProfit)}
            </div>
          </CardContent>
        </Card>
      </div >

      {/* Tabs */}
      < Tabs defaultValue="assets" className="space-y-4" >
        <TabsList>
          <TabsTrigger value="assets">Ativos</TabsTrigger>
        </TabsList>

        <TabsContent value="assets">
          <Card>
            <CardContent className="pt-6">
              <VariableIncomeTable
                data={assets}
                selectedHolderId={holderFilterId || undefined}
                selectedSituation={situationFilter === 'balance' ? undefined : situationFilter}
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
        </TabsContent>

      </Tabs >

      <EditInvestmentDialog
        open={isEditDialogOpen}
        onOpenChange={setIsEditDialogOpen}
        investment={selectedAsset}
        type="variable"
        onSave={(updated, date) => {
          if (updated.tipo === 'variable_income') void handleEditAsset(updated as RendaVariavelDto, date)
        }}
      />

      <DeleteConfirmDialog
        open={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
        title="Excluir lançamento incorreto?"
        description={`Isso apagará todas as movimentações e avaliações de ${selectedAsset?.ticker}. Para sair da posição e preservar o histórico, registre uma venda.`}
        onConfirm={() => {
          if (selectedAsset) {
            handleDeleteAsset(selectedAsset.id)
            setIsDeleteDialogOpen(false)
          }
        }}
      />
    </div >
  )
}
