import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
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
import { groupService } from '@/api/services/group.service'
import { financialInstitutionService } from '@/api/services/financial-institution.service'
import { holderService } from '@/api/services/holder.service'
import { formatCurrency } from '@/lib/utils'
import type { AtualizarCarteiraRequest, CarteiraDto } from '@/api/dtos'

const PAGE_SIZE = 10

export default function Portfolios() {
  const navigate = useNavigate()
  const { toast } = useToast()
  const [page, setPage] = useState(1)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [groupId, setGroupId] = useState('')
  const [groupFilterId, setGroupFilterId] = useState('')
  const [groupName, setGroupName] = useState('')
  const [holder, setHolder] = useState('')
  const [holderProfileId, setHolderProfileId] = useState('')
  const [holderUserId, setHolderUserId] = useState('')
  const [relationship, setRelationship] = useState('')
  const [institutionId, setInstitutionId] = useState('')
  const [institution, setInstitution] = useState('')
  const [visibility, setVisibility] = useState<'Particular' | 'PublicaDoGrupo'>('Particular')
  const [isGroupCreateOpen, setIsGroupCreateOpen] = useState(false)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [editing, setEditing] = useState<CarteiraDto | null>(null)
  const [deleting, setDeleting] = useState<CarteiraDto | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)

  const { data, isLoading, isError, refetch } = usePortfolios({ pagina: page, itensPorPagina: PAGE_SIZE, grupoId: groupFilterId || undefined })
  const { data: totalsResponse, refetch: refetchTotals } = useQuery({
    queryKey: ['portfolios-summary-all', groupFilterId],
    queryFn: () => portfolioService.getSummaryAll(groupFilterId || undefined),
  })
  const { data: groupsResponse, refetch: refetchGroups } = useQuery({ queryKey: ['portfolio-groups'], queryFn: groupService.list })
  const groups = groupsResponse?.dados ?? []
  const selectedGroup = groups.find(group => group.id === groupId) ?? groups[0]
  const holdersQuery = useQuery({
    queryKey: ['holders', selectedGroup?.id],
    queryFn: () => holderService.list(selectedGroup!.id),
    enabled: !!selectedGroup,
  })
  const { data: institutionsResponse, refetch: refetchInstitutions } = useQuery({
    queryKey: ['financial-institutions', selectedGroup?.id],
    queryFn: () => financialInstitutionService.list(selectedGroup!.id),
    enabled: !!selectedGroup,
  })
  const institutions = institutionsResponse?.dados ?? []
  const groupMembersQuery = useQuery({
    queryKey: ['portfolio-group-members-for-wallet', selectedGroup?.id],
    queryFn: () => groupService.members(selectedGroup!.id),
    enabled: selectedGroup?.papel === 'Admin',
  })
  const groupMembers = groupMembersQuery.data?.dados ?? []
  const portfolios = data?.dados ?? []
  const totalValue = totalsResponse?.dados.valorTotal ?? 0
  const totalInvested = totalsResponse?.dados.totalInvestido ?? 0
  const totalGain = totalsResponse?.dados.ganhoTotal ?? 0
  const totalCount = data?.paginacao.totalItens ?? 0
  const pageCount = data?.paginacao.totalPaginas ?? 0

  const createPortfolio = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSaving(true)
    try {
      if (!selectedGroup) throw new Error('Crie ou selecione um grupo antes de criar a carteira.')
      const selectedInstitutionId = institutionId && institutionId !== 'custom' ? institutionId : undefined
      const selectedInstitution = institutions.find(item => item.id === selectedInstitutionId)
      const createdPortfolio = await portfolioService.create({
        nome: name.trim(), descricao: description.trim() || undefined,
        grupoId: selectedGroup.id,
        titular: holder.trim() || undefined,
        titularId: holderProfileId || undefined,
        titularUsuarioId: holderUserId || undefined,
        parentesco: relationship.trim() || undefined,
        instituicaoFinanceiraId: selectedInstitutionId,
        instituicaoFinanceira: selectedInstitutionId ? selectedInstitution?.nome : institutionId === 'custom' ? institution.trim() : undefined,
        tipoInstituicao: selectedInstitution?.categoria ?? (institutionId === 'custom' ? 'Outra' : undefined),
        visibilidade: visibility,
      })
      if (institutionId === 'custom' && createdPortfolio.dados.instituicaoFinanceiraId) {
        setInstitutionId(createdPortfolio.dados.instituicaoFinanceiraId)
        await refetchInstitutions()
      }
      setName('')
      setDescription('')
      setHolder('')
      setHolderProfileId('')
      setHolderUserId('')
      setRelationship('')
      setInstitution('')
      setInstitutionId('')
      setVisibility('Particular')
      setPage(1)
      await Promise.all([refetch(), refetchTotals(), refetchInstitutions()])
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

  const createGroup = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSaving(true)
    try {
      const created = await groupService.create(groupName.trim())
      await refetchGroups()
      setGroupId(created.dados.id)
      setGroupName('')
      setIsGroupCreateOpen(false)
      toast({ title: 'Grupo criado', description: 'Você entrou como Admin.' })
    } catch (error) {
      toast({ title: 'Não foi possível criar o grupo', description: error instanceof Error ? error.message : 'Tente novamente.', variant: 'destructive' })
    } finally { setIsSaving(false) }
  }

  const updatePortfolio = async (id: string, update: AtualizarCarteiraRequest) => {
    setIsSaving(true)
    try {
      await portfolioService.update(id, update)
      await Promise.all([refetch(), refetchTotals()])
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
      await Promise.all([refetch(), refetchTotals()])
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
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          <Label htmlFor="portfolio-group-filter">Grupo</Label>
          <select id="portfolio-group-filter" className="h-10 rounded-md border bg-background px-3" value={groupFilterId} onChange={event => {
            setGroupFilterId(event.target.value)
            setPage(1)
          }}>
            <option value="">Todos os grupos</option>
            {groups.map(group => <option key={group.id} value={group.id}>{group.nome}</option>)}
          </select>
        </div>
        <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
          <DialogTrigger asChild>
            <Button><Plus className="mr-2 h-4 w-4" />Nova carteira</Button>
          </DialogTrigger>
          <DialogContent className="max-h-[85vh] overflow-y-auto">
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
              <div className="space-y-2">
                <Label htmlFor="portfolio-group">Grupo</Label>
                <select id="portfolio-group" className="h-10 w-full rounded-md border bg-background px-3" value={selectedGroup?.id ?? ''} onChange={event => { setGroupId(event.target.value); setHolderProfileId(''); setHolder(''); setHolderUserId(''); setInstitutionId(''); setInstitution('') }} required>
                  <option value="" disabled>Selecione um grupo</option>
                  {groups.map(group => <option key={group.id} value={group.id}>{group.nome} · {group.papel}</option>)}
                </select>
                <button type="button" className="text-sm text-primary underline-offset-4 hover:underline" onClick={() => setIsGroupCreateOpen(true)}>Criar um grupo</button>
              </div>
              <div className="space-y-2">
                <Label htmlFor="portfolio-holder-profile">Perfil do titular</Label>
                <select id="portfolio-holder-profile" className="h-10 w-full rounded-md border bg-background px-3" value={holderProfileId} onChange={event => {
                  setHolderProfileId(event.target.value)
                  const profile = holdersQuery.data?.dados.find(item => item.id === event.target.value)
                  setHolder(profile?.nome ?? '')
                  setRelationship(profile?.parentesco ?? '')
                  setHolderUserId('')
                }}><option value="">Criar perfil ao salvar a carteira</option>{holdersQuery.data?.dados.map(profile => <option key={profile.id} value={profile.id}>{profile.nome}{profile.parentesco ? ` · ${profile.parentesco}` : ''}</option>)}</select>
                <Label htmlFor="portfolio-holder">Titular</Label>
                <Input id="portfolio-holder" value={holder} onChange={event => setHolder(event.target.value)} maxLength={160} required={!(selectedGroup?.papel !== 'Admin' && visibility === 'Particular')} disabled={selectedGroup?.papel !== 'Admin' && visibility === 'Particular' || !!holderUserId} placeholder="Nome do titular da carteira" />
                {selectedGroup?.papel === 'Admin' && <div className="space-y-2"><Label htmlFor="portfolio-holder-member">Vincular a membro ativo (opcional)</Label><select id="portfolio-holder-member" className="h-10 w-full rounded-md border bg-background px-3" value={holderUserId} onChange={event => {
                  setHolderUserId(event.target.value)
                  const member = groupMembers.find(item => item.usuarioId === event.target.value)
                  if (member) setHolder(member.nome || member.email)
                }}><option value="">Titular sem vínculo com login</option>{groupMembers.filter(member => member.ativo).map(member => <option key={member.id} value={member.usuarioId}>{member.nome || member.email} · {member.email}</option>)}</select></div>}
                <div className="space-y-2"><Label htmlFor="portfolio-relationship">Parentesco (opcional, informativo)</Label><Input id="portfolio-relationship" value={relationship} onChange={event => setRelationship(event.target.value)} maxLength={80} placeholder="Ex.: mãe, pai ou irmã" /></div>
                {selectedGroup?.papel !== 'Admin' && visibility === 'Particular' && <p className="text-xs text-muted-foreground">A carteira ficará vinculada à sua conta do grupo. Admins também poderão acessá-la.</p>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="portfolio-institution">Banco, corretora ou DTVM</Label>
                <select id="portfolio-institution" className="h-10 w-full rounded-md border bg-background px-3" required value={institutionId} onChange={event => { setInstitutionId(event.target.value); setInstitution('') }}>
                  <option value="" disabled>Selecione uma instituição</option>
                  {institutions.map(item => <option key={item.id} value={item.id}>{item.nome} · {item.categoria}</option>)}
                  <option value="custom">Outra — cadastrar neste grupo</option>
                </select>
                {institutionId === 'custom' && <div className="space-y-2"><Label htmlFor="portfolio-custom-institution">Nome da instituição</Label><Input id="portfolio-custom-institution" value={institution} onChange={event => setInstitution(event.target.value)} maxLength={120} required placeholder="Digite o nome para reutilizar nas carteiras do grupo" /></div>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="portfolio-visibility">Visibilidade</Label>
                <select id="portfolio-visibility" className="h-10 w-full rounded-md border bg-background px-3" value={visibility} onChange={event => setVisibility(event.target.value as typeof visibility)}>
                  <option value="Particular">Particular</option>
                  <option value="PublicaDoGrupo">Pública do grupo</option>
                </select>
              </div>
              <div className="flex justify-end">
                <Button type="submit" disabled={isSaving}>{isSaving ? 'Salvando…' : 'Criar carteira'}</Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
        <Dialog open={isGroupCreateOpen} onOpenChange={setIsGroupCreateOpen}>
          <DialogContent className="max-h-[85vh] overflow-y-auto">
            <DialogHeader><DialogTitle>Criar grupo de carteiras</DialogTitle></DialogHeader>
            <form onSubmit={createGroup} className="space-y-4">
              <div className="space-y-2"><Label htmlFor="group-name">Nome do grupo</Label><Input id="group-name" value={groupName} onChange={event => setGroupName(event.target.value)} maxLength={120} required /></div>
              <div className="flex justify-end"><Button type="submit" disabled={isSaving}>{isSaving ? 'Salvando…' : 'Criar grupo'}</Button></div>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Card><CardContent className="pt-6"><p className="text-sm text-muted-foreground">Patrimônio acompanhado</p><p className="mt-2 text-2xl font-bold">{formatCurrency(totalValue)}</p></CardContent></Card>
        <Card><CardContent className="pt-6"><p className="text-sm text-muted-foreground">Custo das posições abertas</p><p className="mt-2 text-2xl font-bold">{formatCurrency(totalInvested)}</p></CardContent></Card>
        <Card><CardContent className="pt-6"><p className="text-sm text-muted-foreground">Resultado acumulado</p><p className="mt-2 text-2xl font-bold">{formatCurrency(totalGain)}</p></CardContent></Card>
        <Card><CardContent className="pt-6"><p className="text-sm text-muted-foreground">Rentabilidade consolidada</p><p className="mt-2 text-2xl font-bold">{(totalsResponse?.dados.percentualGanho ?? 0).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}%</p></CardContent></Card>
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
                  <span className="block truncate font-semibold">{portfolio.nome}</span>
                  {portfolio.descricao && <span className="mt-1 block truncate text-sm text-muted-foreground">{portfolio.descricao}</span>}
                  <span className="mt-1 block truncate text-xs text-muted-foreground">Grupo: {groups.find(group => group.id === portfolio.grupoId)?.nome ?? 'Sem grupo'} · Titular: {portfolio.titular}{portfolio.instituicaoFinanceira ? ` · ${portfolio.instituicaoFinanceira}` : ''} · {portfolio.visibilidade === 'PublicaDoGrupo' ? 'Pública do grupo' : 'Particular'}</span>
                </button>
                <div className="min-w-28">
                  <p className="text-xs text-muted-foreground">Patrimônio</p>
                  <p className="font-semibold">{formatCurrency(portfolio.valorTotal)}</p>
                </div>
                <div className="min-w-28">
                  <p className="text-xs text-muted-foreground">Posições</p>
                  <p className="font-semibold">{portfolio.quantidadeAtivos}</p>
                </div>
                <Button variant="outline" onClick={() => navigate({ to: '/carteira/$id', params: { id: portfolio.id } })}>Abrir</Button>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" aria-label={`Ações da carteira ${portfolio.nome}`}>
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
        canManageTitular={groups.find(group => group.id === editing?.grupoId)?.papel === 'Admin'}
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
