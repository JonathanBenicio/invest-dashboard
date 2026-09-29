import { useState } from 'react'
import { useNavigate } from '@tanstack/react-router'
import { MoreHorizontal, Plus, Wallet } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { DeleteConfirmDialog } from '@/components/dialogs/DeleteConfirmDialog'
import { EditPortfolioDialog } from '@/components/dialogs/EditPortfolioDialog'
import { useToast } from '@/hooks/use-toast'
import { usePortfolios } from '@/hooks/use-portfolios'
import { portfolioService } from '@/api/services/portfolio.service'
import { formatCurrency } from '@/lib/utils'
import type { AtualizarCarteiraRequest, CarteiraDto } from '@/api/dtos'

const PAGE_SIZE = 10

export default function Portfolios() {
  const navigate = useNavigate()
  const { toast } = useToast()
  const [page, setPage] = useState(1)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [editing, setEditing] = useState<CarteiraDto | null>(null)
  const [deleting, setDeleting] = useState<CarteiraDto | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  const { data, isLoading, isError, refetch } = usePortfolios({ page, pageSize: PAGE_SIZE })
  const portfolios = data?.data ?? []
  const totalValue = portfolios.reduce((sum, portfolio) => sum + portfolio.totalValue, 0)
  const totalInvested = portfolios.reduce((sum, portfolio) => sum + portfolio.totalInvested, 0)
  const totalGain = portfolios.reduce((sum, portfolio) => sum + portfolio.totalGain, 0)
  const totalCount = data?.pagination.totalCount ?? 0
  const pageCount = data?.pagination.totalPages ?? 0

  const createPortfolio = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSaving(true)
    try {
      await portfolioService.create({ name: name.trim(), description: description.trim() || undefined })
      setName('')
      setDescription('')
      setPage(1)
      await refetch()
      setIsCreateOpen(false)
      toast({ title: 'Carteira criada', description: 'A carteira foi salva.' })
    } catch (error) {
      toast({
        title: 'Não foi possível criar a carteira',
        description: error instanceof Error ? error.message : 'Tente novamente.',
        variant: 'destructive',
      })
    } finally {
      setIsSaving(false)
    }
  }

  const updatePortfolio = async (id: string, update: AtualizarCarteiraRequest) => {
    setIsSaving(true)
    try {
      await portfolioService.update(id, update)
      await refetch()
      toast({ title: 'Carteira atualizada', description: 'As alterações foram salvas.' })
    } catch (error) {
      toast({
        title: 'Não foi possível atualizar a carteira',
        description: error instanceof Error ? error.message : 'Tente novamente.',
        variant: 'destructive',
      })
    } finally {
      setIsSaving(false)
    }
  }

  const deletePortfolio = async () => {
    if (!deleting) return
    setIsDeleting(true)
    try {
      await portfolioService.delete(deleting.id)
      if (portfolios.length === 1 && page > 1) setPage(current => current - 1)
      await refetch()
      setDeleting(null)
      toast({ title: 'Carteira excluída', description: 'A carteira e suas movimentações foram removidas.' })
    } catch (error) {
      toast({
        title: 'Não foi possível excluir a carteira',
        description: error instanceof Error ? error.message : 'Tente novamente.',
        variant: 'destructive',
      })
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Carteiras</h1>
          <p className="text-muted-foreground">Acompanhe suas posições por carteira.</p>
        </div>
        <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
          <DialogTrigger asChild>
            <Button><Plus className="mr-2 h-4 w-4" />Nova carteira</Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>Criar carteira</DialogTitle></DialogHeader>
            <form onSubmit={createPortfolio} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="portfolio-name">Nome</Label>
                <Input
                  id="portfolio-name"
                  value={name}
                  onChange={event => setName(event.target.value)}
                  maxLength={100}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="portfolio-description">Descrição (opcional)</Label>
                <Input
                  id="portfolio-description"
                  value={description}
                  onChange={event => setDescription(event.target.value)}
                  maxLength={500}
                />
              </div>
              <div className="flex justify-end">
                <Button type="submit" disabled={isSaving}>{isSaving ? 'Salvando…' : 'Criar carteira'}</Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <Card><CardContent className="pt-6"><p className="text-sm text-muted-foreground">Patrimônio acompanhado</p><p className="mt-2 text-2xl font-bold">{formatCurrency(totalValue)}</p></CardContent></Card>
        <Card><CardContent className="pt-6"><p className="text-sm text-muted-foreground">Custo das posições abertas</p><p className="mt-2 text-2xl font-bold">{formatCurrency(totalInvested)}</p></CardContent></Card>
        <Card><CardContent className="pt-6"><p className="text-sm text-muted-foreground">Resultado não realizado</p><p className="mt-2 text-2xl font-bold">{formatCurrency(totalGain)}</p></CardContent></Card>
      </div>

      {isLoading ? (
        <p className="py-12 text-center text-muted-foreground">Carregando carteiras…</p>
      ) : isError ? (
        <Card><CardContent className="flex flex-col items-center gap-3 py-12 text-center">
          <p>Não foi possível carregar suas carteiras.</p>
          <Button variant="outline" onClick={() => void refetch()}>Tentar novamente</Button>
        </CardContent></Card>
      ) : portfolios.length === 0 ? (
        <Card><CardContent className="flex flex-col items-center gap-4 py-14 text-center">
          <Wallet className="h-10 w-10 text-muted-foreground" />
          <div>
            <h2 className="text-lg font-semibold">Nenhuma carteira cadastrada</h2>
            <p className="mt-1 text-sm text-muted-foreground">Crie sua primeira carteira para começar a acompanhar posições.</p>
          </div>
          <Button onClick={() => setIsCreateOpen(true)}><Plus className="mr-2 h-4 w-4" />Criar carteira</Button>
        </CardContent></Card>
      ) : (
        <div className="space-y-3">
          {portfolios.map(portfolio => (
            <Card key={portfolio.id}>
              <CardContent className="flex flex-wrap items-center gap-4 p-4 sm:p-5">
                <button
                  type="button"
                  className="min-w-0 flex-1 text-left"
                  onClick={() => navigate({ to: '/carteira/$id', params: { id: portfolio.id } })}
                >
                  <span className="block truncate font-semibold">{portfolio.name}</span>
                  {portfolio.description && <span className="mt-1 block truncate text-sm text-muted-foreground">{portfolio.description}</span>}
                </button>
                <div className="min-w-28">
                  <p className="text-xs text-muted-foreground">Patrimônio</p>
                  <p className="font-semibold">{formatCurrency(portfolio.totalValue)}</p>
                </div>
                <div className="min-w-28">
                  <p className="text-xs text-muted-foreground">Posições</p>
                  <p className="font-semibold">{portfolio.assetsCount}</p>
                </div>
                <Button variant="outline" onClick={() => navigate({ to: '/carteira/$id', params: { id: portfolio.id } })}>Abrir</Button>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" aria-label={`Ações da carteira ${portfolio.name}`}>
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => setEditing(portfolio)}>Editar</DropdownMenuItem>
                    <DropdownMenuItem className="text-destructive" onClick={() => setDeleting(portfolio)}>Excluir</DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {pageCount > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">{totalCount} carteiras · Página {page} de {pageCount}</p>
          <div className="flex gap-2">
            <Button variant="outline" disabled={page <= 1} onClick={() => setPage(current => current - 1)}>Anterior</Button>
            <Button variant="outline" disabled={page >= pageCount} onClick={() => setPage(current => current + 1)}>Próxima</Button>
          </div>
        </div>
      )}

      <EditPortfolioDialog
        open={editing !== null}
        onOpenChange={open => { if (!open) setEditing(null) }}
        portfolio={editing}
        onSave={(id, update) => { void updatePortfolio(id, update) }}
      />
      <DeleteConfirmDialog
        open={deleting !== null}
        onOpenChange={open => { if (!open && !isDeleting) setDeleting(null) }}
        title="Excluir carteira?"
        description="A carteira e suas movimentações associadas serão removidas. Esta ação não pode ser desfeita."
        onConfirm={() => { void deletePortfolio() }}
      />
    </div>
  )
}
