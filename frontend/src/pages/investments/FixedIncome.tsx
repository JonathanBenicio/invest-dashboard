import { useState, useMemo } from "react"
import { useNavigate } from "@tanstack/react-router"
import { Plus } from "lucide-react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { mockPortfolios, formatCurrency, type FixedIncomeAsset } from "@/lib/mock-data"
import { useToast } from "@/hooks/use-toast"
import { EditInvestmentDialog } from "@/components/dialogs/EditInvestmentDialog"
import { DeleteConfirmDialog } from "@/components/dialogs/DeleteConfirmDialog"
import { FixedIncomeProjection } from "@/components/projections/FixedIncomeProjection"
import { useFixedIncomeInvestments } from "@/hooks/use-investments"
import { FixedIncomeTable } from "@/components/investments/FixedIncomeTable"
import { investmentService } from "@/api/services/investment.service"
import type { FixedIncomeDto, InvestmentFilters, FixedIncomeType, CreateFixedIncomeRequest } from "@/api/dtos"
import { PaginationState, SortingState, ColumnFiltersState } from "@tanstack/react-table"

export default function FixedIncome() {
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)
  const [selectedAsset, setSelectedAsset] = useState<FixedIncomeDto | null>(null)
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

    // Map column filters to API params
    const subtypeFilter = columnFilters.find(f => f.id === 'subtype')?.value
    if (subtypeFilter) {
      apiFilters.subtype = subtypeFilter as FixedIncomeType
    }

    const issuerFilter = columnFilters.find(f => f.id === 'issuer')?.value
    if (issuerFilter) {
      apiFilters.issuer = issuerFilter as string
    }

    return apiFilters
  }, [pagination, sorting, columnFilters, globalFilter])

  const { data: investmentsData, isLoading, refetch } = useFixedIncomeInvestments(filters)

  const assets = (investmentsData?.data || []) as FixedIncomeDto[]
  const pageCount = investmentsData?.pagination?.totalPages || 0

  const totalInvested = assets.reduce((acc, asset) => acc + asset.totalInvested, 0)
  const totalCurrent = assets.reduce((acc, asset) => acc + asset.currentValue, 0)
  const totalProfit = totalCurrent - totalInvested

  const assetTypes = ['CDB', 'LCI', 'LCA', 'TESOURO_DIRETO', 'DEBENTURE', 'CRI', 'CRA']

  const handleAddAsset = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const formData = new FormData(e.currentTarget)

    const newAssetData: CreateFixedIncomeRequest = {
      portfolioId: formData.get('portfolioId') as string,
      name: formData.get('name') as string,
      subtype: formData.get('type') as FixedIncomeType,
      issuer: formData.get('institution') as string,
      quantity: 1, // Defaulting to 1 for simplicity if not in form
      averagePrice: parseFloat(formData.get('investedValue') as string),
      interestRate: parseFloat(formData.get('rate')?.toString().replace('%', '') || '0'),
      indexer: formData.get('rateType') as 'CDI' | 'IPCA' | 'PREFIXADO',
      purchaseDate: formData.get('purchaseDate') as string,
      maturityDate: formData.get('maturityDate') as string,
    }

    try {
      await investmentService.createFixedIncome(newAssetData)
      setIsDialogOpen(false)
      toast({
        title: "Ativo adicionado",
        description: `${newAssetData.name} foi adicionado à sua carteira.`,
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

  const handleEditAsset = async (updatedAsset: FixedIncomeDto) => {
    if (!selectedAsset) return

    try {
       await investmentService.update(selectedAsset.id, updatedAsset as any)

       setIsEditDialogOpen(false)
        toast({
        title: "Ativo atualizado",
        description: "O ativo foi atualizado com sucesso.",
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

  const openEditDialog = (asset: FixedIncomeDto) => {
    setSelectedAsset(asset)
    setIsEditDialogOpen(true)
  }

  const openDeleteDialog = (asset: FixedIncomeDto) => {
    setSelectedAsset(asset)
    setIsDeleteDialogOpen(true)
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Renda Fixa</h1>
          <p className="text-muted-foreground">Gerencie seus ativos de renda fixa</p>
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
              <DialogTitle>Adicionar Ativo de Renda Fixa</DialogTitle>
              <DialogDescription>
                Preencha os dados do novo ativo
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
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="rateType">Indexador</Label>
                    <Select name="rateType" defaultValue="CDI">
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
                  <div className="grid gap-2">
                    <Label htmlFor="liquidity">Liquidez</Label>
                    <Select name="liquidity" defaultValue="Diária">
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
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="purchaseDate">Data de Compra</Label>
                    <Input id="purchaseDate" name="purchaseDate" type="date" required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="maturityDate">Data de Vencimento</Label>
                    <Input id="maturityDate" name="maturityDate" type="date" required />
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

      <FixedIncomeProjection assets={assets.map(a => ({
        id: a.id,
        name: a.name,
        type: a.subtype,
        institution: a.issuer,
        investedValue: a.totalInvested,
        currentValue: a.currentValue,
        rate: a.interestRate?.toString() || '0',
        rateType: a.indexer || 'CDI',
        purchaseDate: a.purchaseDate,
        maturityDate: a.maturityDate,
        liquidity: 'No vencimento'
      } as unknown as FixedIncomeAsset))} />

      {/* Table */}
      <Card>
        <CardHeader>
            <CardTitle>Meus Investimentos</CardTitle>
        </CardHeader>
        <CardContent>
            <FixedIncomeTable
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

      <EditInvestmentDialog
        open={isEditDialogOpen}
        onOpenChange={setIsEditDialogOpen}
        investment={selectedAsset}
        type="fixed"
        onSave={handleEditAsset}
      />

      <DeleteConfirmDialog
        open={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
        title="Excluir Ativo"
        description={`Tem certeza que deseja excluir "${selectedAsset?.name}"? Esta ação não pode ser desfeita.`}
        onConfirm={() => {
          if (selectedAsset) {
            handleDeleteAsset(selectedAsset.id)
          }
        }}
      />
    </div>
  )
}
