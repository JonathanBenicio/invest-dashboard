import { useState, useMemo } from "react"
import { Link, useNavigate } from "@tanstack/react-router"
import { portfolioDetailsRoute } from "../../router"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ArrowLeft, Wallet, TrendingUp, TrendingDown, PiggyBank, Building2, Plus, Target, Calendar } from "lucide-react"
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, BarChart, Bar, Legend } from "recharts"
import { useToast } from "@/hooks/use-toast"
import { formatCurrency, formatPercentage } from "@/lib/mock-data"
import { DeleteConfirmDialog } from "@/components/dialogs/DeleteConfirmDialog"
import { EditInvestmentDialog } from "@/components/dialogs/EditInvestmentDialog"
import { usePortfolio, usePortfolioSummary } from "@/hooks/use-portfolios"
import { useFixedIncomeInvestments } from "@/hooks/use-investments"
import { useVariableIncomeInvestments } from "@/hooks/use-variable-income"
import { FixedIncomeTable } from "@/components/investments/FixedIncomeTable"
import { VariableIncomeTable } from "@/components/investments/VariableIncomeTable"
import { investmentService } from "@/api/services/investment.service"
import type { FixedIncomeDto, VariableIncomeDto, InvestmentFilters, VariableIncomeType, FixedIncomeType } from "@/api/dtos"
import { PaginationState, SortingState, ColumnFiltersState } from "@tanstack/react-table"
import { Badge } from "@/components/ui/badge"

export default function PortfolioDetails() {
  const { id } = portfolioDetailsRoute.useParams()
  const navigate = useNavigate()
  const { toast } = useToast()

  // Queries
  const { data: portfolioResponse, isLoading: isLoadingPortfolio } = usePortfolio(id)
  const { data: summaryResponse } = usePortfolioSummary(id)

  const portfolio = portfolioResponse?.data
  const summary = summaryResponse?.data

  // Fixed Income Table State
  const [fixedPagination, setFixedPagination] = useState<PaginationState>({ pageIndex: 0, pageSize: 5 })
  const [fixedSorting, setFixedSorting] = useState<SortingState>([])
  const [fixedColumnFilters, setFixedColumnFilters] = useState<ColumnFiltersState>([])
  const [fixedGlobalFilter, setFixedGlobalFilter] = useState("")

  // Variable Income Table State
  const [variablePagination, setVariablePagination] = useState<PaginationState>({ pageIndex: 0, pageSize: 5 })
  const [variableSorting, setVariableSorting] = useState<SortingState>([])
  const [variableColumnFilters, setVariableColumnFilters] = useState<ColumnFiltersState>([])
  const [variableGlobalFilter, setVariableGlobalFilter] = useState("")

  // Filters construction
  const fixedFilters: InvestmentFilters = useMemo(() => {
    const filters: InvestmentFilters = {
      portfolioId: id,
      page: fixedPagination.pageIndex + 1,
      pageSize: fixedPagination.pageSize,
      search: fixedGlobalFilter || undefined,
      sortBy: fixedSorting[0]?.id,
      sortOrder: fixedSorting[0]?.desc ? 'desc' : 'asc',
    }
    const subtype = fixedColumnFilters.find(f => f.id === 'subtype')?.value
    if (subtype) filters.subtype = subtype as FixedIncomeType
    const issuer = fixedColumnFilters.find(f => f.id === 'issuer')?.value
    if (issuer) filters.issuer = issuer as string
    return filters
  }, [id, fixedPagination, fixedSorting, fixedColumnFilters, fixedGlobalFilter])

  const variableFilters: InvestmentFilters = useMemo(() => {
    const filters: InvestmentFilters = {
      portfolioId: id,
      page: variablePagination.pageIndex + 1,
      pageSize: variablePagination.pageSize,
      search: variableGlobalFilter || undefined,
      sortBy: variableSorting[0]?.id,
      sortOrder: variableSorting[0]?.desc ? 'desc' : 'asc',
    }
    const subtype = variableColumnFilters.find(f => f.id === 'subtype')?.value
    if (subtype) filters.subtype = subtype as VariableIncomeType
    const sector = variableColumnFilters.find(f => f.id === 'sector')?.value
    if (sector) (filters as any).sector = sector
    return filters
  }, [id, variablePagination, variableSorting, variableColumnFilters, variableGlobalFilter])

  // Data fetching
  const { data: fixedResponse, isLoading: isLoadingFixed, refetch: refetchFixed } = useFixedIncomeInvestments(fixedFilters)
  const { data: variableResponse, isLoading: isLoadingVariable, refetch: refetchVariable } = useVariableIncomeInvestments(variableFilters)

  const fixedAssets = (fixedResponse?.data || []) as FixedIncomeDto[]
  const variableAssets = (variableResponse?.data || []) as VariableIncomeDto[]

  // Edit/Delete State
  const [editingInvestment, setEditingInvestment] = useState<FixedIncomeDto | VariableIncomeDto | null>(null)
  const [editingType, setEditingType] = useState<"fixed" | "variable">("fixed")
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [deletingInvestment, setDeletingInvestment] = useState<{ asset: FixedIncomeDto | VariableIncomeDto; type: "fixed" | "variable" } | null>(null)
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)

  // Handlers
  const handleEditFixed = (asset: FixedIncomeDto) => {
    setEditingInvestment(asset)
    setEditingType("fixed")
    setIsEditDialogOpen(true)
  }

  const handleEditVariable = (asset: VariableIncomeDto) => {
    setEditingInvestment(asset)
    setEditingType("variable")
    setIsEditDialogOpen(true)
  }

  const handleSaveInvestment = async (updated: any) => {
    if (!editingInvestment) return
    try {
        await investmentService.update(editingInvestment.id, updated)
        setIsEditDialogOpen(false)
        toast({ title: "Investimento atualizado", description: "Sucesso." })
        if (editingType === "fixed") refetchFixed()
        else refetchVariable()
    } catch (e) {
        toast({ title: "Erro", description: "Falha ao atualizar.", variant: "destructive" })
    }
  }

  const handleDeleteClick = (asset: FixedIncomeDto | VariableIncomeDto, type: "fixed" | "variable") => {
    setDeletingInvestment({ asset, type })
    setIsDeleteDialogOpen(true)
  }

  const handleConfirmDelete = async () => {
    if (!deletingInvestment) return
    try {
        await investmentService.delete(deletingInvestment.asset.id)
        setIsDeleteDialogOpen(false)
        toast({ title: "Excluído", description: "Investimento removido." })
        if (deletingInvestment.type === "fixed") refetchFixed()
        else refetchVariable()
    } catch (e) {
        toast({ title: "Erro", description: "Falha ao excluir.", variant: "destructive" })
    }
  }

  if (isLoadingPortfolio) return <div className="p-8 flex justify-center">Carregando carteira...</div>
  if (!portfolio) return <div className="p-8 flex justify-center">Carteira não encontrada.</div>

  const isProfit = portfolio.totalGain >= 0

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div className="flex items-center gap-4">
          <Link to="/carteiras">
            <Button variant="ghost" size="icon">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <div className="flex items-center gap-3">
              <span className="text-3xl">{portfolio.bankLogo}</span>
              <div>
                <h1 className="text-2xl font-bold">{portfolio.name}</h1>
                <p className="text-muted-foreground flex items-center gap-2">
                  <Building2 className="h-4 w-4" />
                  {portfolio.bankName}
                </p>
              </div>
            </div>
          </div>
        </div>
        <Button onClick={() => navigate({ to: '/renda-fixa' })}>
          <Plus className="mr-2 h-4 w-4" />
          Adicionar Investimento
        </Button>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Patrimônio Total
            </CardTitle>
            <Wallet className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(portfolio.totalValue)}</div>
            <p className="text-xs text-muted-foreground mt-1">
              {portfolio.assetsCount} ativos na carteira
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Total Investido
            </CardTitle>
            <PiggyBank className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(portfolio.totalInvested)}</div>
            <p className="text-xs text-muted-foreground mt-1">
              Valor aportado
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Lucro/Prejuízo
            </CardTitle>
            {isProfit ? (
              <TrendingUp className="h-4 w-4 text-success" />
            ) : (
              <TrendingDown className="h-4 w-4 text-destructive" />
            )}
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${isProfit ? 'text-success' : 'text-destructive'}`}>
              {formatCurrency(portfolio.totalGain)}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Resultado líquido
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Rentabilidade
            </CardTitle>
            {isProfit ? (
              <TrendingUp className="h-4 w-4 text-success" />
            ) : (
              <TrendingDown className="h-4 w-4 text-destructive" />
            )}
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${isProfit ? 'text-success' : 'text-destructive'}`}>
              {formatPercentage(portfolio.gainPercentage)}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Desde o início
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="investments" className="space-y-4">
        <TabsList>
          <TabsTrigger value="investments">Investimentos</TabsTrigger>
          <TabsTrigger value="charts">Gráficos</TabsTrigger>
        </TabsList>

        {/* Investments Tab */}
        <TabsContent value="investments" className="space-y-4">

          {/* Fixed Income Table */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Renda Fixa</CardTitle>
            </CardHeader>
            <CardContent>
              <FixedIncomeTable
                data={fixedAssets}
                pageCount={fixedResponse?.pagination?.totalPages || 0}
                pagination={fixedPagination}
                setPagination={setFixedPagination}
                sorting={fixedSorting}
                setSorting={setFixedSorting}
                columnFilters={fixedColumnFilters}
                setColumnFilters={setFixedColumnFilters}
                globalFilter={fixedGlobalFilter}
                setGlobalFilter={setFixedGlobalFilter}
                isLoading={isLoadingFixed}
                onEdit={handleEditFixed}
                onDelete={(asset) => handleDeleteClick(asset, "fixed")}
              />
            </CardContent>
          </Card>

          {/* Variable Income Table */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Renda Variável</CardTitle>
            </CardHeader>
            <CardContent>
                <VariableIncomeTable
                    data={variableAssets}
                    pageCount={variableResponse?.pagination?.totalPages || 0}
                    pagination={variablePagination}
                    setPagination={setVariablePagination}
                    sorting={variableSorting}
                    setSorting={setVariableSorting}
                    columnFilters={variableColumnFilters}
                    setColumnFilters={setVariableColumnFilters}
                    globalFilter={variableGlobalFilter}
                    setGlobalFilter={setVariableGlobalFilter}
                    isLoading={isLoadingVariable}
                    onEdit={handleEditVariable}
                    onDelete={(asset) => handleDeleteClick(asset, "variable")}
                />
            </CardContent>
          </Card>
        </TabsContent>

        {/* Charts Tab */}
        <TabsContent value="charts" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            {/* Evolution Chart */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Evolução Patrimonial</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="h-[300px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={summary?.performanceHistory || []}>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                      <XAxis dataKey="date" className="text-xs" />
                      <YAxis
                        tickFormatter={(value) => `${(value / 1000).toFixed(0)}k`}
                        className="text-xs"
                      />
                      <Tooltip
                        formatter={(value: number) => formatCurrency(value)}
                        labelFormatter={(label) => `Data: ${label}`}
                      />
                      <Line
                        type="monotone"
                        dataKey="value"
                        name="Patrimônio"
                        stroke="hsl(var(--primary))"
                        strokeWidth={2}
                        dot={{ fill: 'hsl(var(--primary))' }}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>

            {/* Allocation Chart */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Alocação por Tipo</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="h-[300px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={summary?.assetAllocation || []}
                        cx="50%"
                        cy="50%"
                        innerRadius={60}
                        outerRadius={100}
                        dataKey="value"
                        label={({ category, percentage }) => `${category}: ${percentage}%`}
                      >
                        {(summary?.assetAllocation || []).map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={entry.color || 'hsl(var(--primary))'} />
                        ))}
                      </Pie>
                      <Tooltip formatter={(value: number) => formatCurrency(value)} />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>

      {/* Edit Investment Dialog */}
      <EditInvestmentDialog
        open={isEditDialogOpen}
        onOpenChange={setIsEditDialogOpen}
        investment={editingInvestment}
        type={editingType}
        onSave={handleSaveInvestment}
      />

      {/* Delete Confirmation Dialog */}
      <DeleteConfirmDialog
        open={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
        title="Excluir Investimento"
        description="Tem certeza?"
        onConfirm={handleConfirmDelete}
      />
    </div>
  )
}
