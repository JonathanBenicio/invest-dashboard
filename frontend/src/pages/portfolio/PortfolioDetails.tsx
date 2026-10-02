import { useState, useMemo } from "react"
import { useQuery } from "@tanstack/react-query"
import { Link, useNavigate } from "@tanstack/react-router"
import { portfolioDetailsRoute } from "../../router"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ArrowLeft, Wallet, TrendingUp, TrendingDown, PiggyBank, Building2, Plus, Target, Calendar } from "lucide-react"
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, BarChart, Bar, Legend } from "recharts"
import { useToast } from "@/hooks/use-toast"
import { formatCurrency, formatPercentage } from "@/lib/utils"
import { DeleteConfirmDialog } from "@/components/dialogs/DeleteConfirmDialog"
import { EditInvestmentDialog } from "@/components/dialogs/EditInvestmentDialog"
import { usePortfolio, usePortfolioSummary } from "@/hooks/use-portfolios"
import { useFixedIncomeInvestments } from "@/hooks/use-investments"
import { useVariableIncomeInvestments } from "@/hooks/use-variable-income"
import { useMarketQuotes } from "@/hooks/use-market-quotes"
import { FixedIncomeTable } from "@/components/investments/FixedIncomeTable"
import { VariableIncomeTable } from "@/components/investments/VariableIncomeTable"
import { investmentService } from "@/api/services/investment.service"
import { portfolioService } from "@/api/services/portfolio.service"
import type { PosicaoInvestimentoDto, RendaFixaDto, RendaVariavelDto, InvestimentoFiltros, TipoRendaVariavel, TipoRendaFixa } from "@/api/dtos"
import { PaginationState, SortingState, ColumnFiltersState } from "@tanstack/react-table"

export default function PortfolioDetails() {
  const { id } = portfolioDetailsRoute.useParams()
  const navigate = useNavigate()
  const { toast } = useToast()

  // Queries
  const { data: portfolioResponse, isLoading: isLoadingPortfolio, isError: isPortfolioError, refetch: refetchPortfolio } = usePortfolio(id)
  const { data: summaryResponse, isError: isSummaryError, refetch: refetchSummary } = usePortfolioSummary(id)
  const historyRange = useMemo(() => {
    const today = new Date()
    const from = new Date(today)
    from.setFullYear(from.getFullYear() - 1)
    return {
      fromDate: from.toISOString().slice(0, 10),
      toDate: today.toISOString().slice(0, 10),
    }
  }, [])
  const { data: historyResponse, isLoading: isLoadingHistory, isError: isHistoryError, refetch: refetchHistory } = useQuery({
    queryKey: ["portfolio-history", id, historyRange],
    queryFn: () => portfolioService.getHistory(id, historyRange.fromDate, historyRange.toDate),
  })

  const portfolio = portfolioResponse?.dados
  const summary = summaryResponse?.dados
  const completeHistory = historyResponse?.dados
    .filter(point => point.estaCompleto && point.valorTotal !== null)
    .map(point => ({
      date: new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "short" }).format(new Date(point.data)),
      value: point.valorTotal as number,
    })) ?? []
  const hasIncompleteHistory = historyResponse?.dados.some(point => !point.estaCompleto) ?? false

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
  const fixedFilters: InvestimentoFiltros = useMemo(() => {
    const filters: InvestimentoFiltros = {
      carteiraId: id,
      pagina: fixedPagination.pageIndex + 1,
      itensPorPagina: fixedPagination.pageSize,
      busca: fixedGlobalFilter || undefined,
      ordenarPor: fixedSorting[0]?.id,
      ordem: fixedSorting[0]?.desc ? 'desc' : 'asc',
    }
    const subtype = fixedColumnFilters.find(f => f.id === 'subtipo')?.value
    if (subtype) filters.subtipo = subtype as TipoRendaFixa
    const issuer = fixedColumnFilters.find(f => f.id === 'emissor')?.value
    if (issuer) filters.emissor = issuer as string
    return filters
  }, [id, fixedPagination, fixedSorting, fixedColumnFilters, fixedGlobalFilter])

  const variableFilters: InvestimentoFiltros = useMemo(() => {
    const filters: InvestimentoFiltros = {
      carteiraId: id,
      pagina: variablePagination.pageIndex + 1,
      itensPorPagina: variablePagination.pageSize,
      busca: variableGlobalFilter || undefined,
      ordenarPor: variableSorting[0]?.id,
      ordem: variableSorting[0]?.desc ? 'desc' : 'asc',
    }
    const subtype = variableColumnFilters.find(f => f.id === 'subtipo')?.value
    if (subtype) filters.subtipo = subtype as TipoRendaVariavel
    const sector = variableColumnFilters.find(f => f.id === 'setor')?.value
    if (typeof sector === 'string') filters.setor = sector
    return filters
  }, [id, variablePagination, variableSorting, variableColumnFilters, variableGlobalFilter])

  // Data fetching
  const { data: fixedResponse, isLoading: isLoadingFixed, isError: isFixedError, refetch: refetchFixed } = useFixedIncomeInvestments(fixedFilters)
  const { data: variableResponse, isLoading: isLoadingVariable, isError: isVariableError, refetch: refetchVariable } = useVariableIncomeInvestments(variableFilters)

  const fixedAssets = (fixedResponse?.dados || []) as RendaFixaDto[]

  // Extrair tickers para assinar via SignalR
  const tickers = useMemo(() => {
    return (variableResponse?.dados || []).map(asset => asset.ticker)
  }, [variableResponse?.dados])

  const { quotesBySymbol, unavailableSymbols, isLoading: isLoadingQuotes } = useMarketQuotes(tickers)

  // Mapeia os ativos de renda variável injetando os preços em tempo real
  const variableAssets = useMemo(() => {
    const originalAssets = (variableResponse?.dados || []) as RendaVariavelDto[]
    return originalAssets.map(asset => {
      const quote = quotesBySymbol.get(asset.ticker.toUpperCase())
      if (quote) {
        return {
          ...asset,
          precoAtual: quote.preco,
          origemPrecoAtual: quote.origem,
          precoObservadoEmUtc: quote.observadoEmUtc,
          valorAtual: asset.quantidade * quote.preco,
          ganho: (asset.quantidade * quote.preco) - asset.totalInvestido,
          percentualGanho: asset.totalInvestido > 0 ? (((asset.quantidade * quote.preco) - asset.totalInvestido) / asset.totalInvestido) * 100 : 0
        }
      }
      return asset
    })
  }, [variableResponse?.dados, quotesBySymbol])

  // Edit/Delete State
  const [editingInvestment, setEditingInvestment] = useState<RendaFixaDto | RendaVariavelDto | null>(null)
  const [editingType, setEditingType] = useState<"fixed" | "variable">("fixed")
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [deletingInvestment, setDeletingInvestment] = useState<{ asset: RendaFixaDto | RendaVariavelDto; type: "fixed" | "variable" } | null>(null)
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)

  // Handlers
  const handleEditFixed = (asset: RendaFixaDto) => {
    setEditingInvestment(asset)
    setEditingType("fixed")
    setIsEditDialogOpen(true)
  }

  const handleEditVariable = (asset: RendaVariavelDto) => {
    setEditingInvestment(asset)
    setEditingType("variable")
    setIsEditDialogOpen(true)
  }

  const handleSaveInvestment = async (updated: PosicaoInvestimentoDto, valuationDate: string) => {
    if (!editingInvestment) return
    try {
        await investmentService.update(editingInvestment.id, {
          valorTotal: updated.valorAtual,
          data: new Date(`${valuationDate}T12:00:00`).toISOString(),
        })
        setIsEditDialogOpen(false)
        toast({ title: "Investimento atualizado", description: "Sucesso." })
        if (editingType === "fixed") refetchFixed()
        else refetchVariable()
    } catch (e) {
        toast({ title: "Erro", description: "Falha ao atualizar.", variant: "destructive" })
    }
  }

  const handleDeleteClick = (asset: RendaFixaDto | RendaVariavelDto, type: "fixed" | "variable") => {
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
  if (isPortfolioError) return <div className="space-y-4 p-8"><p role="alert" className="text-destructive">Não foi possível carregar a carteira.</p><Button variant="outline" onClick={() => void refetchPortfolio()}>Tentar novamente</Button></div>
  if (!portfolio) return <div className="p-8 flex justify-center">Carteira não encontrada.</div>

  const isProfit = portfolio.ganhoTotal >= 0

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
              <div>
                <h1 className="text-2xl font-bold">{portfolio.nome}</h1>
                <p className="text-muted-foreground flex items-center gap-2">
                  {portfolio.descricao ?? 'Investimentos desta carteira'}
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

      {(isSummaryError || isFixedError || isVariableError) && (
        <div role="alert" className="rounded-md border border-destructive/40 p-4 text-sm text-destructive">
          Não foi possível carregar todos os dados da carteira.
          <Button className="ml-3" variant="outline" size="sm" onClick={() => void Promise.all([refetchSummary(), refetchFixed(), refetchVariable()])}>Tentar novamente</Button>
        </div>
      )}

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
            <div className="text-2xl font-bold">{formatCurrency(portfolio.valorTotal)}</div>
            <p className="text-xs text-muted-foreground mt-1">
              {portfolio.quantidadeAtivos} ativos na carteira
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
            <div className="text-2xl font-bold">{formatCurrency(portfolio.totalInvestido)}</div>
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
              {formatCurrency(portfolio.ganhoTotal)}
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
              {formatPercentage(portfolio.percentualGanho)}
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
          {!isLoadingQuotes && unavailableSymbols.length > 0 && (
            <p role="status" className="rounded-md border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm text-amber-800 dark:text-amber-200">
              Cotação indisponível para {unavailableSymbols.join(", ")}. O último preço salvo foi mantido.
            </p>
          )}

          {/* Fixed Income Table */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Renda Fixa</CardTitle>
            </CardHeader>
            <CardContent>
              <FixedIncomeTable
                data={fixedAssets}
                pageCount={fixedResponse?.paginacao?.totalPaginas || 0}
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
                    pageCount={variableResponse?.paginacao?.totalPaginas || 0}
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
                <CardTitle className="text-lg">Evolução da carteira</CardTitle>
                <CardDescription>
                  {hasIncompleteHistory
                    ? "O gráfico mostra apenas datas completas; períodos sem preço para algum ativo foram omitidos."
                    : "Valores históricos calculados com operações e preços armazenados."}
                </CardDescription>
              </CardHeader>
              <CardContent>
                {isLoadingHistory ? (
                  <div className="flex h-[300px] items-center justify-center text-sm text-muted-foreground">Carregando histórico...</div>
                ) : isHistoryError ? (
                  <div role="alert" className="flex h-[300px] flex-col items-center justify-center gap-3 px-6 text-center text-sm text-destructive">
                    Não foi possível carregar o histórico da carteira.
                    <Button variant="outline" size="sm" onClick={() => void refetchHistory()}>Tentar novamente</Button>
                  </div>
                ) : completeHistory.length < 2 ? (
                  <div className="flex h-[300px] items-center justify-center px-6 text-center text-sm text-muted-foreground">
                    Ainda não há pontos completos suficientes. A série cresce conforme chegam cotações diárias e avaliações de extrato.
                  </div>
                ) : (
                  <div className="h-[300px]">
                    <ResponsiveContainer width="100%" height="100%">
                      <LineChart data={completeHistory}>
                        <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                        <XAxis dataKey="date" className="text-xs" />
                        <YAxis tickFormatter={(value) => `${(value / 1000).toFixed(0)}k`} className="text-xs" />
                        <Tooltip formatter={(value: number) => formatCurrency(value)} />
                        <Line type="monotone" dataKey="value" name="Patrimônio" stroke="hsl(var(--primary))" strokeWidth={2} dot={false} />
                      </LineChart>
                    </ResponsiveContainer>
                  </div>
                )}
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
                        data={summary?.alocacaoAtivos || []}
                        cx="50%"
                        cy="50%"
                        innerRadius={60}
                        outerRadius={100}
                        dataKey="valor"
                        nameKey="categoria"
                        label={({ categoria, percentual }) => `${categoria}: ${percentual}%`}
                      >
                        {(summary?.alocacaoAtivos || []).map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={entry.cor || 'hsl(var(--primary))'} />
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
