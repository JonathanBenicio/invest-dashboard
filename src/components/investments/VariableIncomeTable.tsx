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
import { VariableIncomeDto } from "@/api/dtos"
import { formatCurrency } from "@/lib/mock-data"

interface VariableIncomeTableProps {
  data: VariableIncomeDto[]
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
  onEdit: (asset: VariableIncomeDto) => void
  onDelete: (asset: VariableIncomeDto) => void
}

export function VariableIncomeTable({
  data,
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

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'ACAO': return 'bg-primary/10 text-primary'
      case 'FII': return 'bg-warning/10 text-warning'
      case 'ETF': return 'bg-success/10 text-success'
      case 'BDR': return 'bg-info/10 text-info'
      default: return 'bg-secondary text-secondary-foreground'
    }
  }

  const columns: ColumnDef<VariableIncomeDto>[] = [
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
      cell: ({ row }) => (
        <div>
          <p className="font-medium">{row.getValue("ticker")}</p>
          <p className="text-xs text-muted-foreground">{row.original.name}</p>
        </div>
      ),
    },
    {
      accessorKey: "subtype",
      header: "Tipo",
      cell: ({ row }) => <Badge className={getTypeColor(row.getValue("subtype"))} variant="secondary">{row.getValue("subtype")}</Badge>,
    },
    {
        accessorKey: "sector",
        header: "Setor",
        cell: ({ row }) => <div className="text-sm">{row.getValue("sector")}</div>,
    },
    {
      accessorKey: "quantity",
      header: ({ column }) => (
        <div className="text-right">
            Qtd
        </div>
      ),
      cell: ({ row }) => <div className="text-right">{row.getValue("quantity")}</div>,
    },
    {
      accessorKey: "averagePrice",
      header: ({ column }) => (
        <div className="text-right">
          PM
        </div>
      ),
      cell: ({ row }) => <div className="text-right">{formatCurrency(row.getValue("averagePrice"))}</div>,
    },
    {
      accessorKey: "currentValue",
      header: ({ column }) => (
        <div className="text-right">
          Atual
        </div>
      ),
      cell: ({ row }) => <div className="text-right">{formatCurrency(row.original.currentPrice)}</div>,
    },
    {
        id: "totalValue",
        header: ({ column }) => <div className="text-right">Total</div>,
        cell: ({ row }) => <div className="text-right font-medium">{formatCurrency(row.original.currentValue)}</div>
    },
    {
        id: "profit",
        header: ({ column }) => <div className="text-right">Resultado</div>,
        cell: ({ row }) => {
            const profit = row.original.gain
            const profitPercent = row.original.gainPercentage
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

        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => navigate({ to: '/investimento/$id', params: { id: asset.id }, search: { type: 'variable' } })}>
                <Eye className="h-4 w-4 mr-2" />
                Ver Detalhes
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => navigate({ to: '/investimento/$id', params: { id: asset.id }, search: { type: 'variable', action: 'buy' } })}>
                <PlusCircle className="h-4 w-4 mr-2 text-success" />
                Comprar
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => navigate({ to: '/investimento/$id', params: { id: asset.id }, search: { type: 'variable', action: 'sell' } })}>
                <MinusCircle className="h-4 w-4 mr-2 text-destructive" />
                Vender
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => onEdit(asset)}>
                <Pencil className="h-4 w-4 mr-2" />
                Editar
              </DropdownMenuItem>
              <DropdownMenuItem
                className="text-destructive"
                onClick={() => onDelete(asset)}
              >
                <Trash2 className="h-4 w-4 mr-2" />
                Excluir
              </DropdownMenuItem>
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
                value={(table.getColumn("sector")?.getFilterValue() as string) ?? ""}
                onChange={(event) =>
                  table.getColumn("sector")?.setFilterValue(event.target.value)
                }
                className="max-w-sm"
            />
            <Select
              value={(table.getColumn("subtype")?.getFilterValue() as string) ?? "all"}
              onValueChange={(value) => table.getColumn("subtype")?.setFilterValue(value === "all" ? undefined : value)}
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
            const profit = asset.gain
            const profitPercent = asset.gainPercentage

            return (
              <Card key={row.id} className="cursor-pointer hover:shadow-md transition-shadow" onClick={() => navigate({ to: '/investimento/$id', params: { id: asset.id }, search: { type: 'variable' } })}>
                <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2">
                  <div className="space-y-1">
                    <CardTitle className="text-base font-medium line-clamp-1">
                      {asset.ticker}
                    </CardTitle>
                    <p className="text-xs text-muted-foreground line-clamp-1">
                      {asset.name}
                    </p>
                  </div>
                  <Badge className={getTypeColor(asset.subtype)} variant="secondary">
                    {asset.subtype}
                  </Badge>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <p className="text-muted-foreground text-xs mb-1">Preço Atual</p>
                      <p className="font-semibold">{formatCurrency(asset.currentPrice)}</p>
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
                      <p className="font-semibold">{formatCurrency(asset.currentValue)}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground text-xs mb-1">Qtd</p>
                      <p className="font-medium">{asset.quantity}</p>
                    </div>
                  </div>
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
