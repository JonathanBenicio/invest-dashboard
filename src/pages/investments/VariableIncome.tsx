import { useState, useMemo } from "react"
import { useNavigate } from "@tanstack/react-router"
import { Plus, ArrowUpRight, ArrowDownRight } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { mockPortfolios, formatCurrency, formatDate } from "@/lib/mock-data"
import { useToast } from "@/hooks/use-toast"
import { EditInvestmentDialog } from "@/components/dialogs/EditInvestmentDialog"
import { DeleteConfirmDialog } from "@/components/dialogs/DeleteConfirmDialog"
import { useVariableIncomeInvestments, useDividends } from "@/hooks/use-variable-income"
import { VariableIncomeTable } from "@/components/investments/VariableIncomeTable"
import { StockSearch } from "@/components/investments/StockSearch"
import { investmentService } from "@/api/services/investment.service"
import type { VariableIncomeDto, InvestmentFilters, VariableIncomeType, CreateVariableIncomeRequest, BrapiQuote } from "@/api/dtos"
import { PaginationState, SortingState, ColumnFiltersState } from "@tanstack/react-table"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"

export default function VariableIncome() {
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const [selectedAsset, setSelectedAsset] = useState<VariableIncomeDto | null>(null)
  const [selectedQuote, setSelectedQuote] = useState<BrapiQuote | null>(null)
  const [ticker, setTicker] = useState("")
  const [name, setName] = useState("")
  const [sector, setSector] = useState("")
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
  const filters: InvestmentFilters = useMemo(() => {
    const apiFilters: InvestmentFilters = {
      page: pagination.pageIndex + 1,
      pageSize: pagination.pageSize,
      search: globalFilter || undefined,
      sortBy: sorting[0]?.id,
      sortOrder: sorting[0]?.desc ? 'desc' : 'asc',
    }

    const subtypeFilter = columnFilters.find(f => f.id === 'subtype')?.value
    if (subtypeFilter) {
      apiFilters.subtype = subtypeFilter as VariableIncomeType
    }

    // Add sector filter logic if DTO supported it explicitly or reuse generic filters
    // Our service mock supports 'sector' query param via loose casting in service or if we add it to DTO.
    // For now, let's assume we can pass it.
    const sectorFilter = columnFilters.find(f => f.id === 'sector')?.value
    if (sectorFilter) {
      (apiFilters as any).sector = sectorFilter
    }

    return apiFilters
  }, [pagination, sorting, columnFilters, globalFilter])

  const { data: investmentsData, isLoading, refetch } = useVariableIncomeInvestments(filters)
  const { data: dividendsData } = useDividends()

  const assets = (investmentsData?.data || []) as VariableIncomeDto[]
  const pageCount = investmentsData?.pagination?.totalPages || 0

  const dividends = dividendsData?.data || []

  const totalInvested = assets.reduce((acc, asset) => acc + asset.totalInvested, 0)
  const totalCurrent = assets.reduce((acc, asset) => acc + asset.currentValue, 0)
  const totalProfit = totalCurrent - totalInvested

  const assetTypes = ['ACAO', 'FII', 'ETF', 'BDR']

  const handleAddAsset = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const formData = new FormData(e.currentTarget)

    const newAssetData: CreateVariableIncomeRequest = {
      portfolioId: formData.get('portfolioId') as string,
      ticker: ticker.toUpperCase(),
      subtype: formData.get('type') as VariableIncomeType,
      quantity: parseInt(formData.get('quantity') as string),
      averagePrice: parseFloat(formData.get('averagePrice') as string),
      purchaseDate: new Date().toISOString(),
      ...({
        name: name,
        sector: sector,
      } as any)
    }

    try {
      await investmentService.createVariableIncome(newAssetData)
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

  const handleStockSelect = (quote: BrapiQuote) => {
    setSelectedQuote(quote)
    setTicker(quote.symbol)
    setName(quote.shortName || quote.longName)
    // Brapi doesn't always provide sector in the quote, but let's try to infer or leave it
    // In a real app we might want to fetch more details if needed
  }

  const handleEditAsset = async (updatedAsset: VariableIncomeDto) => {
    if (!selectedAsset) return

    try {
      // Since API expects UpdateInvestmentRequest but mock allows any, we pass what we have
      await investmentService.update(selectedAsset.id, updatedAsset as any)

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

  const openEditDialog = (asset: VariableIncomeDto) => {
    setSelectedAsset(asset)
    setIsEditDialogOpen(true)
  }

  const openDeleteDialog = (asset: VariableIncomeDto) => {
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
            <Button>
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
                  <Select name="portfolioId" defaultValue={mockPortfolios[0]?.id}>
                    <SelectTrigger>
                      <SelectValue placeholder="Selecione a carteira" />
                    </SelectTrigger>
                    <SelectContent>
                      {mockPortfolios.map((portfolio) => (
                        <SelectItem key={portfolio.id} value={portfolio.id}>
                          {portfolio.name}
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
                    <Input id="quantity" name="quantity" type="number" placeholder="100" required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="averagePrice">Preço Médio</Label>
                    <Input id="averagePrice" name="averagePrice" type="number" step="0.01" placeholder="32.50" required />
                  </div>
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
          <TabsTrigger value="dividends">Proventos</TabsTrigger>
        </TabsList>

        <TabsContent value="assets">
          <Card>
            <CardContent className="pt-6">
              <VariableIncomeTable
                data={assets}
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

        <TabsContent value="dividends">
          <Card>
            <CardHeader>
              <CardTitle>Histórico de Proventos</CardTitle>
              <CardDescription>Dividendos, JCP e rendimentos recebidos</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="rounded-lg border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Ticker</TableHead>
                      <TableHead>Tipo</TableHead>
                      <TableHead className="text-right">Valor/Cota</TableHead>
                      <TableHead>Data Ex</TableHead>
                      <TableHead>Pagamento</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {dividends.map((dividend: any) => (
                      <TableRow key={dividend.id}>
                        <TableCell className="font-medium">{dividend.ticker}</TableCell>
                        <TableCell>
                          <Badge variant="outline">{dividend.type}</Badge>
                        </TableCell>
                        <TableCell className="text-right text-success font-medium">
                          {formatCurrency(dividend.value)}
                        </TableCell>
                        <TableCell>{formatDate(dividend.exDate)}</TableCell>
                        <TableCell>{formatDate(dividend.paymentDate)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs >

      <EditInvestmentDialog
        open={isEditDialogOpen}
        onOpenChange={setIsEditDialogOpen}
        investment={selectedAsset}
        type="variable"
        onSave={handleEditAsset}
      />

      <DeleteConfirmDialog
        open={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
        title="Excluir Ativo"
        description={`Tem certeza que deseja excluir "${selectedAsset?.ticker}"? Esta ação não pode ser desfeita.`}
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
