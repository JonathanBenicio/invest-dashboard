import { useState } from "react"
import { useNavigate } from "@tanstack/react-router"
import {
  ColumnDef,
  ColumnFiltersState,
  SortingState,
  VisibilityState,
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  useReactTable,
  PaginationState,
} from "@tanstack/react-table"
import { ArrowUpDown, ChevronDown, MoreHorizontal, Eye, PlusCircle, MinusCircle, Pencil, Trash2, Calendar, TrendingUp } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { DropdownMenu, DropdownMenuCheckboxItem, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Badge } from "@/components/ui/badge"
import { RendaFixaDto } from "@/api/dtos"
import type { PosicaoInvestimentoConsolidadoDto } from "@/api/dtos/investment.dto"
import { formatCurrency } from "@/lib/utils"
import { InvestmentEvaluationSummary } from "@/components/investments/InvestmentEvaluationSummary"

function formatarTaxa(asset: Pick<PosicaoInvestimentoConsolidadoDto, 'taxaJuros' | 'indexador'>): string {
  const taxa = asset.taxaJuros == null ? "Não informada" : `${asset.taxaJuros}%`
  return asset.indexador ? `${taxa} · ${asset.indexador}` : taxa
}

interface FixedIncomeTableProps {
  data: (PosicaoInvestimentoConsolidadoDto | RendaFixaDto)[]
  groupNames?: Record<string, string>
  selectedHolderId?: string
  selectedSituation?: 'closed' | 'all'
  pageCount: number
  pagination: PaginationState
  setPagination: React.Dispatch<React.SetStateAction<PaginationState>>
  sorting: SortingState
  setSorting: React.Dispatch<React.SetStateAction<SortingState>>
  columnFilters: ColumnFiltersState
  setColumnFilters: React.Dispatch<React.SetStateAction<ColumnFiltersState>>
  globalFilter: string
  setGlobalFilter: React.Dispatch<React.SetStateAction<string>>
  isLoading: boolean
  onEdit: (asset: RendaFixaDto) => void
  onDelete: (asset: RendaFixaDto) => void
}

export function FixedIncomeTable({
  data,
  groupNames = {},
  selectedHolderId,
  selectedSituation,
  pageCount,
  pagination,
  setPagination,
  sorting,
  setSorting,
  columnFilters,
  setColumnFilters,
  globalFilter,
  setGlobalFilter,
  isLoading,
  onEdit,
  onDelete,
}: FixedIncomeTableProps) {
  const navigate = useNavigate()
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({})
  const isConsolidated = (asset: PosicaoInvestimentoConsolidadoDto | RendaFixaDto): asset is PosicaoInvestimentoConsolidadoDto => 'escopo' in asset
  const abrirDetalhes = (asset: PosicaoInvestimentoConsolidadoDto | RendaFixaDto) => {
    if (isConsolidated(asset) && asset.escopo !== 'carteira' && asset.grupoId) {
      void navigate({ to: '/investimento/$id', params: { id: asset.id }, search: {
        type: 'fixed', groupId: asset.grupoId, assetId: asset.ativoId,
        holderId: selectedHolderId, situacao: selectedSituation, view: 'group',
      } })
      return
    }
    const positionId = isConsolidated(asset) ? asset.posicaoId ?? asset.id : asset.id
    void navigate({ to: '/investimento/$id', params: { id: positionId }, search: {
      type: 'fixed', groupId: asset.grupoId ?? undefined, holderId: asset.titularId ?? undefined,
      portfolioId: asset.carteiraId ?? undefined, situacao: selectedSituation, view: 'portfolio',
    } })
  }
  const positionForAction = (asset: PosicaoInvestimentoConsolidadoDto | RendaFixaDto): RendaFixaDto | null => {
    if (!isConsolidated(asset)) return asset
    return asset.escopo === 'carteira' && asset.posicaoId && asset.carteiraId
      ? { ...asset, id: asset.posicaoId, carteiraId: asset.carteiraId } as unknown as RendaFixaDto
      : null
  }

  const columns: ColumnDef<PosicaoInvestimentoConsolidadoDto | RendaFixaDto>[] = [
    {
      accessorKey: "nome",
      header: ({ column }) => {
        return (
          <Button
            variant="ghost"
            onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          >
            Ativo
            <ArrowUpDown className="ml-2 h-4 w-4" />
          </Button>
        )
      },
      cell: ({ row }) => (
        <div>
          <p className="font-medium">{row.getValue("nome")}</p>
        </div>
      ),
    },
    {
      accessorKey: "emissor",
      header: ({ column }) => {
        return (
          <Button
            variant="ghost"
            onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          >
            Emissor
            <ArrowUpDown className="ml-2 h-4 w-4" />
          </Button>
        )
      },
      cell: ({ row }) => <div className="text-sm">{row.getValue("emissor")}</div>,
    },
    {
      id: "carteira",
      accessorFn: row => row.carteiraNome ?? "",
      header: "Carteira / grupo",
      cell: ({ row }) => {
        const asset = row.original
        const consolidated = isConsolidated(asset)
        const groupName = consolidated
          ? asset.grupoNome ?? (asset.grupoId ? groupNames[asset.grupoId] : undefined)
          : (asset.grupoId ? groupNames[asset.grupoId] : undefined)
        if (consolidated && asset.escopo !== 'carteira') return <div className="min-w-36 text-sm">
          <p className="font-medium">{groupName || 'Grupo'}</p>
          <p className="text-xs text-muted-foreground">{asset.quantidadeCarteiras} {asset.quantidadeCarteiras === 1 ? 'carteira' : 'carteiras'}</p>
          {(asset.titular || asset.titularNome) && <p className="text-xs text-muted-foreground">{asset.titular || asset.titularNome}</p>}
        </div>
        return <div className="min-w-36 text-sm">
          <p className="font-medium">{asset.carteiraNome || "Carteira"}</p>
          <p className="text-xs text-muted-foreground">{groupName || "Grupo sem nome"}</p>
          {(asset.titular || (!consolidated && asset.instituicaoFinanceira)) && <p className="text-xs text-muted-foreground">{[asset.titular, !consolidated ? asset.instituicaoFinanceira : undefined].filter(Boolean).join(" · ")}</p>}
        </div>
      },
    },
    {
      accessorKey: "subtipo",
      header: "Tipo",
      cell: ({ row }) => <Badge variant="secondary">{row.getValue("subtipo")}</Badge>,
    },
    {
      accessorKey: "situacao",
      header: "Situação",
      cell: ({ row }) => <Badge variant="outline" className={row.original.situacao === "matured" ? "border-warning/50 text-warning" : undefined}>{isConsolidated(row.original) && row.original.situacao === "mixed" ? "Situações diferentes" : row.original.situacao === "matured" ? "Vencido" : row.original.situacao === "closed" ? "Encerrado" : "Em carteira"}</Badge>,
    },
    {
      accessorKey: "totalInvestido",
      header: ({ column }) => (
        <div className="text-right">
          <Button
            variant="ghost"
            onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          >
            Investido
            <ArrowUpDown className="ml-2 h-4 w-4" />
          </Button>
        </div>
      ),
      cell: ({ row }) => <div className="text-right">{formatCurrency(row.getValue("totalInvestido"))}</div>,
    },
    {
      accessorKey: "valorAtual",
      header: ({ column }) => (
        <div className="text-right">
          <Button
            variant="ghost"
            onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          >
            Atual
            <ArrowUpDown className="ml-2 h-4 w-4" />
          </Button>
        </div>
      ),
      cell: ({ row }) => {
        const profit = row.original.percentualGanho
        return (
          <div className="text-right">
            <p className="font-medium">{formatCurrency(row.getValue("valorAtual"))}</p>
            <p className={`text-xs ${profit >= 0 ? 'text-success' : 'text-destructive'}`}>
              {profit >= 0 ? '+' : ''}{profit.toFixed(2)}%
            </p>
            {isConsolidated(row.original) && <InvestmentEvaluationSummary investment={row.original} className="mt-1" />}
          </div>
        )
      },
    },
    {
      accessorKey: "taxaJuros",
      header: "Taxa",
      cell: ({ row }) => <span className="text-sm">{formatarTaxa(row.original)}</span>,
    },
    {
      accessorKey: "dataVencimento",
      header: "Vencimento",
      cell: ({ row }) => {
        const dataVencimento = row.original.dataVencimento
        return <div>{dataVencimento ? dataVencimento.slice(0, 10).split('-').reverse().join('/') : 'Sem data'}</div>
      },
    },
    {
      id: "actions",
      enableHiding: false,
      cell: ({ row }) => {
        const asset = row.original
        const individual = positionForAction(asset)

        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => abrirDetalhes(asset)}>
                <Eye className="h-4 w-4 mr-2" />
                Ver Detalhes
              </DropdownMenuItem>
              {individual?.situacao === 'open' && <DropdownMenuItem onClick={() => navigate({ to: '/investimento/$id', params: { id: individual.id }, search: { type: 'fixed', action: 'buy', situacao: selectedSituation } })}>
                <PlusCircle className="h-4 w-4 mr-2 text-success" />
                Aportar
              </DropdownMenuItem>}
              {individual && individual.situacao !== 'closed' && <DropdownMenuItem onClick={() => navigate({ to: '/investimento/$id', params: { id: individual.id }, search: { type: 'fixed', action: 'sell', situacao: selectedSituation } })}>
                <MinusCircle className="h-4 w-4 mr-2 text-destructive" />
                Resgatar
              </DropdownMenuItem>}
              {individual && <DropdownMenuItem onClick={() => onEdit(individual)}>
                <Pencil className="h-4 w-4 mr-2" />
                Editar
              </DropdownMenuItem>}
              {individual && <DropdownMenuItem
                className="text-destructive"
                onClick={() => onDelete(individual)}
              >
                <Trash2 className="h-4 w-4 mr-2" />
                Excluir
              </DropdownMenuItem>}
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ]

  const table = useReactTable({
    data,
    columns,
    pageCount,
    state: {
      sorting,
      columnFilters,
      columnVisibility,
      rowSelection: {},
      pagination,
      globalFilter,
    },
    enableRowSelection: true,
    manualPagination: true,
    manualSorting: true,
    manualFiltering: true,
    onPaginationChange: setPagination,
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onGlobalFilterChange: setGlobalFilter,
    onColumnVisibilityChange: setColumnVisibility,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getSortedRowModel: getSortedRowModel(),
  })

  return (
    <div className="w-full">
      <div className="flex flex-col sm:flex-row items-center py-4 gap-4">
        <Input
          placeholder="Buscar geral..."
          value={globalFilter ?? ""}
          onChange={(event) => setGlobalFilter(event.target.value)}
          className="max-w-sm"
        />
        <div className="flex gap-2 w-full sm:w-auto">
             <Input
                placeholder="Filtrar por emissor..."
                value={(table.getColumn("emissor")?.getFilterValue() as string) ?? ""}
                onChange={(event) =>
                  table.getColumn("emissor")?.setFilterValue(event.target.value)
                }
                className="max-w-sm"
            />
            {/* Note: In a real implementation with manual filtering, we need to map column filters to API params manually in the parent component */}
            <Select
              value={(table.getColumn("subtipo")?.getFilterValue() as string) ?? "all"}
              onValueChange={(value) => table.getColumn("subtipo")?.setFilterValue(value === "all" ? undefined : value)}
            >
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Tipo" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos os tipos</SelectItem>
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
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" className="ml-auto">
              Colunas <ChevronDown className="ml-2 h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {table
              .getAllColumns()
              .filter((column) => column.getCanHide())
              .map((column) => {
                return (
                  <DropdownMenuCheckboxItem
                    key={column.id}
                    className="capitalize"
                    checked={column.getIsVisible()}
                    onCheckedChange={(value) =>
                      column.toggleVisibility(!!value)
                    }
                  >
                    {column.id}
                  </DropdownMenuCheckboxItem>
                )
              })}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
      {/* Mobile Card View */}
      <div className="space-y-4 md:hidden">
        {table.getRowModel().rows?.length ? (
          table.getRowModel().rows.map((row) => {
            const asset = row.original
            const profit = asset.percentualGanho
            const consolidated = isConsolidated(asset)
            const groupName = consolidated
              ? asset.grupoNome ?? (asset.grupoId ? groupNames[asset.grupoId] : undefined)
              : (asset.grupoId ? groupNames[asset.grupoId] : undefined)
            const scopeLabel = consolidated && asset.escopo !== 'carteira'
              ? `${groupName ?? 'Grupo'} · ${asset.quantidadeCarteiras} ${asset.quantidadeCarteiras === 1 ? 'carteira' : 'carteiras'}`
              : `${asset.carteiraNome || 'Carteira'} · ${groupName ?? 'Sem grupo'}`
            const holderLabel = [asset.titular, !consolidated ? asset.instituicaoFinanceira : undefined].filter(Boolean).join(' · ')
            return (
              <Card
                key={row.id}
                className="cursor-pointer hover:shadow-md transition-shadow"
                role="button"
                tabIndex={0}
                onClick={() => abrirDetalhes(asset)}
                onKeyDown={event => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault()
                    abrirDetalhes(asset)
                  }
                }}
              >
                <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2">
                  <div className="space-y-1">
                    <CardTitle className="text-base font-medium line-clamp-1">
                      {asset.nome}
                    </CardTitle>
                    <p className="text-xs text-muted-foreground line-clamp-1">
                      {asset.emissor}
                    </p>
                    <p className="text-xs text-muted-foreground line-clamp-1">
                      {scopeLabel}
                    </p>
                    {holderLabel && <p className="text-xs text-muted-foreground line-clamp-1">{holderLabel}</p>}
                  </div>
                  <Badge variant="secondary" className="shrink-0">
                    {asset.subtipo}
                  </Badge>
                  {consolidated && asset.situacao === 'mixed'
                    ? <Badge variant="outline">Situações diferentes</Badge>
                    : asset.situacao === 'matured' && <Badge variant="outline" className="border-warning/50 text-warning">Vencido</Badge>}
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <p className="text-muted-foreground text-xs mb-1">Valor Atual</p>
                      <p className="font-semibold">{formatCurrency(asset.valorAtual)}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-xs mb-1">Resultado</p>
                      <p className={`font-semibold flex items-center gap-1 ${profit >= 0 ? 'text-success' : 'text-destructive'}`}>
                        {profit >= 0 ? '+' : ''}{profit.toFixed(2)}%
                      </p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-xs mb-1">Vencimento</p>
                      <p className="font-medium flex items-center gap-1">
                        <Calendar className="h-3 w-3" />
                        {asset.dataVencimento ? asset.dataVencimento.slice(0, 10).split('-').reverse().join('/') : 'Sem data'}
                      </p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-xs mb-1">Taxa</p>
                      <p className="font-medium flex items-center gap-1">
                        <TrendingUp className="h-3 w-3" />
                        {formatarTaxa(asset)}
                      </p>
                    </div>
                  </div>
                  {consolidated && <InvestmentEvaluationSummary investment={asset} className="mt-3" />}
                </CardContent>
              </Card>
            )
          })
        ) : (
          <div className="text-center py-8 text-muted-foreground">
            {isLoading ? "Carregando..." : "Nenhum resultado encontrado."}
          </div>
        )}
      </div>

      {/* Desktop Table View */}
      <div className="hidden md:block rounded-md border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => {
                  // Logic to hide less important columns on tablet (md)
                  const isHiddenOnTablet = ['issuer', 'interestRate', 'maturityDate', 'totalInvested'].includes(header.column.id)

                  return (
                    <TableHead
                      key={header.id}
                      className={isHiddenOnTablet ? "hidden lg:table-cell" : ""}
                    >
                      {header.isPlaceholder
                        ? null
                        : flexRender(
                            header.column.columnDef.header,
                            header.getContext()
                          )}
                    </TableHead>
                  )
                })}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  data-state={row.getIsSelected() && "selected"}
                >
                  {row.getVisibleCells().map((cell) => {
                    const isHiddenOnTablet = ['issuer', 'interestRate', 'maturityDate', 'totalInvested'].includes(cell.column.id)

                    return (
                      <TableCell
                        key={cell.id}
                        className={isHiddenOnTablet ? "hidden lg:table-cell" : ""}
                      >
                        {flexRender(
                          cell.column.columnDef.cell,
                          cell.getContext()
                        )}
                      </TableCell>
                    )
                  })}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className="h-24 text-center"
                >
                  {isLoading ? "Carregando..." : "Nenhum resultado encontrado."}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      <div className="flex items-center justify-end space-x-2 py-4">
        <div className="flex-1 text-sm text-muted-foreground">
           Página {table.getState().pagination.pageIndex + 1} de{" "}
           {table.getPageCount()}
        </div>
        <div className="space-x-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}
          >
            Anterior
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => table.nextPage()}
            disabled={!table.getCanNextPage()}
          >
            Próximo
          </Button>
        </div>
      </div>
    </div>
  )
}
