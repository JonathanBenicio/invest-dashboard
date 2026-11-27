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
import { ArrowUpDown, ChevronDown, MoreHorizontal, Eye, PlusCircle, MinusCircle, Pencil, Trash2, ArrowUpRight, ArrowDownRight } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { DropdownMenu, DropdownMenuCheckboxItem, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Badge } from "@/components/ui/badge"
import { RendaVariavelDto } from "@/api/dtos"
import type { PosicaoInvestimentoConsolidadoDto } from "@/api/dtos/investment.dto"
import { formatCurrency } from "@/lib/utils"
import { formatQuoteObservation } from "@/hooks/use-market-quotes"
import { InvestmentEvaluationSummary } from "@/components/investments/InvestmentEvaluationSummary"

interface VariableIncomeTableProps {
  data: (PosicaoInvestimentoConsolidadoDto | RendaVariavelDto)[]
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
  onEdit: (asset: RendaVariavelDto) => void
  onDelete: (asset: RendaVariavelDto) => void
}

export function VariableIncomeTable({
  data,
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
}: VariableIncomeTableProps) {
  const navigate = useNavigate()
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({})
  const isConsolidated = (asset: PosicaoInvestimentoConsolidadoDto | RendaVariavelDto): asset is PosicaoInvestimentoConsolidadoDto => 'escopo' in asset
  const abrirDetalhes = (asset: PosicaoInvestimentoConsolidadoDto | RendaVariavelDto) => {
    if (isConsolidated(asset) && asset.escopo !== 'carteira' && asset.grupoId) {
      void navigate({ to: '/investimento/$id', params: { id: asset.id }, search: {
        type: 'variable', groupId: asset.grupoId, assetId: asset.ativoId,
        holderId: selectedHolderId, situacao: selectedSituation, view: 'group',
      } })
      return
    }
    const positionId = isConsolidated(asset) ? asset.posicaoId ?? asset.id : asset.id
    void navigate({ to: '/investimento/$id', params: { id: positionId }, search: {
      type: 'variable', groupId: asset.grupoId ?? undefined, holderId: asset.titularId ?? undefined,
      portfolioId: asset.carteiraId ?? undefined, situacao: selectedSituation, view: 'portfolio',
    } })
  }
  const positionForAction = (asset: PosicaoInvestimentoConsolidadoDto | RendaVariavelDto): RendaVariavelDto | null => {
    if (!isConsolidated(asset)) return asset
    return asset.escopo === 'carteira' && asset.posicaoId && asset.carteiraId
      ? { ...asset, id: asset.posicaoId, carteiraId: asset.carteiraId } as unknown as RendaVariavelDto
      : null
  }

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'ACAO': return 'bg-primary/10 text-primary'
      case 'FII': return 'bg-warning/10 text-warning'
      case 'ETF': return 'bg-success/10 text-success'
      case 'BDR': return 'bg-info/10 text-info'
      default: return 'bg-secondary text-secondary-foreground'
    }
  }

  const columns: ColumnDef<PosicaoInvestimentoConsolidadoDto | RendaVariavelDto>[] = [
    {
      accessorKey: "ticker",
      header: ({ column }) => {
        return (
          <Button
            variant="ghost"
            onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          >
            Ticker
            <ArrowUpDown className="ml-2 h-4 w-4" />
          </Button>
        )
      },
      cell: ({ row }) => {
        const asset = row.original
        return <div>
          <p className="font-medium">{row.getValue("ticker")}</p>
          <p className="text-xs text-muted-foreground">{asset.nome}</p>
          {isConsolidated(asset) && asset.escopo !== 'carteira' && <p className="text-xs text-muted-foreground">{asset.grupoNome ?? 'Grupo'} · {asset.quantidadeCarteiras} {asset.quantidadeCarteiras === 1 ? 'carteira' : 'carteiras'}</p>}
        </div>
      },
    },
    {
      accessorKey: "subtipo",
      header: "Tipo",
      cell: ({ row }) => <Badge className={getTypeColor(row.getValue("subtipo"))} variant="secondary">{row.getValue("subtipo")}</Badge>,
    },
    {
        accessorKey: "setor",
        header: "Setor",
        cell: ({ row }) => <div className="text-sm">{row.getValue("setor")}</div>,
    },
    {
      accessorKey: "quantidade",
      header: ({ column }) => (
        <div className="text-right">
            Qtd
        </div>
      ),
      cell: ({ row }) => <div className="text-right">{row.getValue("quantidade")}</div>,
    },
    {
      accessorKey: "precoMedio",
      header: ({ column }) => (
        <div className="text-right">
          PM
        </div>
      ),
      cell: ({ row }) => <div className="text-right">{formatCurrency(row.getValue("precoMedio"))}</div>,
    },
    {
      accessorKey: "valorAtual",
      header: () => (
        <div className="text-right">
          Valor atual
        </div>
      ),
      cell: ({ row }) => {
        const asset = row.original
        const { origemPrecoAtual, precoObservadoEmUtc, precoAtual } = asset
        return (
          <div className="text-right">
            <p>{formatCurrency(asset.valorAtual)}</p>
            <p className="text-xs text-muted-foreground">
              {precoAtual == null
                ? isConsolidated(asset) && asset.escopo !== 'carteira' ? 'Preço varia entre carteiras' : 'Preço não informado'
                : origemPrecoAtual && precoObservadoEmUtc
                  ? `${formatCurrency(precoAtual)} · ${formatQuoteObservation(origemPrecoAtual.toUpperCase(), precoObservadoEmUtc)}`
                  : `${formatCurrency(precoAtual)} por unidade · Último preço salvo`}
            </p>
            {isConsolidated(row.original) && <InvestmentEvaluationSummary investment={row.original} className="mt-1" />}
          </div>
        )
      },
    },
    {
        id: "totalValue",
        header: ({ column }) => <div className="text-right">Total</div>,
        cell: ({ row }) => <div className="text-right font-medium">{formatCurrency(row.original.valorAtual)}</div>
    },
    {
        id: "profit",
        header: ({ column }) => <div className="text-right">Resultado</div>,
        cell: ({ row }) => {
            const profit = row.original.ganho
            const profitPercent = row.original.percentualGanho
            return (
                <div className={`flex items-center justify-end gap-1 ${profit >= 0 ? 'text-success' : 'text-destructive'}`}>
                    {profit >= 0 ? <ArrowUpRight className="h-3 w-3" /> : <ArrowDownRight className="h-3 w-3" />}
                    <span className="font-medium">{profitPercent >= 0 ? '+' : ''}{profitPercent.toFixed(2)}%</span>
                </div>
            )
        }
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
              {individual && <DropdownMenuItem onClick={() => navigate({ to: '/investimento/$id', params: { id: individual.id }, search: { type: 'variable', action: 'buy', situacao: selectedSituation } })}>
                <PlusCircle className="h-4 w-4 mr-2 text-success" />
                Comprar
              </DropdownMenuItem>}
              {individual && <DropdownMenuItem onClick={() => navigate({ to: '/investimento/$id', params: { id: individual.id }, search: { type: 'variable', action: 'sell', situacao: selectedSituation } })}>
                <MinusCircle className="h-4 w-4 mr-2 text-destructive" />
                Vender
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
          placeholder="Buscar ticker..."
          value={globalFilter ?? ""}
          onChange={(event) => setGlobalFilter(event.target.value)}
          className="max-w-sm"
        />
        <div className="flex gap-2 w-full sm:w-auto">
             <Input
                placeholder="Filtrar por setor..."
                value={(table.getColumn("setor")?.getFilterValue() as string) ?? ""}
                onChange={(event) =>
                  table.getColumn("setor")?.setFilterValue(event.target.value)
                }
                className="max-w-sm"
            />
            <Select
              value={(table.getColumn("subtipo")?.getFilterValue() as string) ?? "all"}
              onValueChange={(value) => table.getColumn("subtipo")?.setFilterValue(value === "all" ? undefined : value)}
            >
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Tipo" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos os tipos</SelectItem>
                <SelectItem value="ACAO">Ação</SelectItem>
                <SelectItem value="FII">FII</SelectItem>
                <SelectItem value="ETF">ETF</SelectItem>
                <SelectItem value="BDR">BDR</SelectItem>
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
            const profit = asset.ganho
            const profitPercent = asset.percentualGanho
            const consolidated = isConsolidated(asset)

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
                      {asset.ticker}
                    </CardTitle>
                    <p className="text-xs text-muted-foreground line-clamp-1">
                      {asset.nome}
                    </p>
                    {consolidated && asset.escopo !== 'carteira' && <p className="text-xs text-muted-foreground line-clamp-1">
                      {asset.grupoNome ?? 'Grupo'} · {asset.quantidadeCarteiras} {asset.quantidadeCarteiras === 1 ? 'carteira' : 'carteiras'}{asset.titular ? ` · ${asset.titular}` : ''}
                    </p>}
                  </div>
                  <Badge className={getTypeColor(asset.subtipo)} variant="secondary">
                    {asset.subtipo}
                  </Badge>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <p className="text-muted-foreground text-xs mb-1">Preço Atual</p>
                      <p className="font-semibold">{asset.precoAtual == null
                        ? consolidated && asset.escopo !== 'carteira' ? 'Varia por carteira' : 'Preço não informado'
                        : formatCurrency(asset.precoAtual)}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-xs mb-1">Resultado</p>
                      <div className={`font-semibold flex items-center gap-1 ${profit >= 0 ? 'text-success' : 'text-destructive'}`}>
                        {profit >= 0 ? <ArrowUpRight className="h-3 w-3" /> : <ArrowDownRight className="h-3 w-3" />}
                        {profitPercent >= 0 ? '+' : ''}{profitPercent.toFixed(2)}%
                      </div>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-xs mb-1">Total</p>
                      <p className="font-semibold">{formatCurrency(asset.valorAtual)}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-xs mb-1">Qtd</p>
                      <p className="font-medium">{asset.quantidade}</p>
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
                  // Hide columns on tablet
                  const isHiddenOnTablet = ['sector', 'quantity', 'averagePrice'].includes(header.column.id)

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
                     const isHiddenOnTablet = ['sector', 'quantity', 'averagePrice'].includes(cell.column.id)

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
