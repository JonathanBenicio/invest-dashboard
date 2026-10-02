import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { groupService } from '@/api/services/group.service'
import type { MembroGrupoDto } from '@/api/dtos'
import { useToast } from '@/hooks/use-toast'
import { HolderProfiles } from '@/components/portfolio/HolderProfiles'

export default function Groups() {
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [groupId, setGroupId] = useState('')
  const [groupName, setGroupName] = useState('')
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<MembroGrupoDto['papel']>('Investidor')
  const groupsQuery = useQuery({ queryKey: ['portfolio-groups'], queryFn: groupService.list })
  const invitationsQuery = useQuery({ queryKey: ['pending-group-invitations'], queryFn: groupService.pendingInvitations })
  const groups = groupsQuery.data?.dados ?? []
  const invitations = invitationsQuery.data?.dados ?? []
  const selectedGroup = groups.find(group => group.id === groupId) ?? groups[0]
  const membersQuery = useQuery({
    queryKey: ['portfolio-group-members', selectedGroup?.id],
    queryFn: () => groupService.members(selectedGroup!.id),
    enabled: !!selectedGroup,
  })
  const refresh = () => queryClient.invalidateQueries({ queryKey: ['portfolio-group-members', selectedGroup?.id] })
  const createGroup = useMutation({
    mutationFn: groupService.create,
    onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['portfolio-groups'] }); setGroupName(''); toast({ title: 'Grupo criado', description: 'Você entrou como Admin.' }) },
    onError: error => toast({ title: 'Não foi possível criar o grupo', description: error.message, variant: 'destructive' }),
  })
  const invite = useMutation({
    mutationFn: () => groupService.invite(selectedGroup!.id, email.trim(), role),
    onSuccess: response => {
      setEmail('')
      toast({
        title: response.dados.emailEnviado ? 'Convite enviado' : 'Convite registrado',
        description: response.dados.mensagem,
      })
    },
    onError: error => toast({ title: 'Não foi possível enviar o convite', description: error.message, variant: 'destructive' }),
  })
  const acceptInvitation = useMutation({
    mutationFn: ({ grupoId, conviteId }: { grupoId: string; conviteId: string }) =>
      groupService.acceptInvitation(grupoId, conviteId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['portfolio-groups'] }),
        queryClient.invalidateQueries({ queryKey: ['pending-group-invitations'] }),
      ])
      toast({ title: 'Convite aceito', description: 'O grupo agora aparece nas suas carteiras.' })
    },
    onError: error => toast({ title: 'Não foi possível aceitar', description: error.message, variant: 'destructive' }),
  })
  const updateRole = useMutation({
    mutationFn: ({ member, papel }: { member: MembroGrupoDto; papel: MembroGrupoDto['papel'] }) => groupService.updateRole(selectedGroup!.id, member.id, papel),
    onSuccess: refresh,
    onError: error => toast({ title: 'Não foi possível alterar o papel', description: error.message, variant: 'destructive' }),
  })
  const deactivate = useMutation({
    mutationFn: (member: MembroGrupoDto) => groupService.deactivate(selectedGroup!.id, member.id),
    onSuccess: refresh,
    onError: error => toast({ title: 'Não foi possível desativar o membro', description: error.message, variant: 'destructive' }),
  })
  const isAdmin = selectedGroup?.papel === 'Admin'

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      {invitations.length > 0 && <section className="space-y-3"><h2 className="text-lg font-semibold">Convites recebidos</h2>{invitations.map(invitation => <Card key={invitation.id}><CardContent className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center"><div className="min-w-0 flex-1"><p className="font-medium">{invitation.grupoNome}</p><p className="text-sm text-muted-foreground">Papel: {invitation.papel} · Expira em {new Date(invitation.expiraEmUtc).toLocaleString('pt-BR')}</p></div><Button onClick={() => acceptInvitation.mutate({ grupoId: invitation.grupoId, conviteId: invitation.id })} disabled={acceptInvitation.isPending}>Aceitar convite</Button></CardContent></Card>)}</section>}
      <header><h1 className="text-2xl font-bold">Grupos e usuários</h1><p className="text-muted-foreground">Convide pessoas e escolha o acesso delas neste grupo.</p></header>
      {groups.length > 0 && <div className="space-y-2"><Label htmlFor="group-selector">Grupo</Label><select id="group-selector" className="h-10 w-full rounded-md border bg-background px-3 sm:max-w-md" value={selectedGroup?.id ?? ''} onChange={event => setGroupId(event.target.value)}>{groups.map(group => <option key={group.id} value={group.id}>{group.nome} · {group.papel}</option>)}</select></div>}
      {groups.length === 0 && <Card><CardContent className="space-y-4 p-5"><h2 className="font-semibold">Crie seu primeiro grupo</h2><form className="flex flex-col gap-3 sm:flex-row" onSubmit={event => { event.preventDefault(); createGroup.mutate(groupName.trim()) }}><Input aria-label="Nome do grupo" placeholder="Nome do grupo" maxLength={120} required value={groupName} onChange={event => setGroupName(event.target.value)} /><Button disabled={createGroup.isPending}>Criar grupo</Button></form></CardContent></Card>}
      {selectedGroup && <>
        {isAdmin && <HolderProfiles key={selectedGroup.id} groupId={selectedGroup.id} members={membersQuery.data?.dados ?? []} />}
        {isAdmin && <Card><CardContent className="space-y-4 p-5"><h2 className="font-semibold">Convidar usuário</h2><form className="grid gap-3 sm:grid-cols-[1fr_180px_auto] sm:items-end" onSubmit={event => { event.preventDefault(); invite.mutate() }}><div className="space-y-2"><Label htmlFor="invite-email">E-mail</Label><Input id="invite-email" type="email" required value={email} onChange={event => setEmail(event.target.value)} placeholder="pessoa@exemplo.com" /></div><div className="space-y-2"><Label htmlFor="invite-role">Papel no grupo</Label><select id="invite-role" className="h-10 w-full rounded-md border bg-background px-3" value={role} onChange={event => setRole(event.target.value as MembroGrupoDto['papel'])}><option value="Investidor">Investidor</option><option value="Consulta">Consulta</option><option value="Admin">Admin</option></select></div><Button disabled={invite.isPending}>Enviar convite</Button></form><p className="text-xs text-muted-foreground">O link do Supabase expira em uma hora; contas existentes podem aceitar o convite nesta tela.</p></CardContent></Card>}
        <section className="space-y-3"><h2 className="text-lg font-semibold">Membros</h2>{membersQuery.isLoading ? <p className="py-8 text-center text-muted-foreground">Carregando membros…</p> : membersQuery.isError ? <Card><CardContent className="p-5 text-sm text-muted-foreground">Não foi possível carregar os membros. Administradores do grupo podem ver esta lista.</CardContent></Card> : membersQuery.data?.dados.map(member => <Card key={member.id}><CardContent className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center"><div className="min-w-0 flex-1"><p className="font-medium">{member.nome || member.email}</p><p className="truncate text-sm text-muted-foreground">{member.email}</p></div><span className="text-sm text-muted-foreground">{member.ativo ? 'Ativo' : 'Desativado'}</span>{isAdmin && member.ativo && <><select aria-label={`Papel de ${member.email}`} className="h-9 rounded-md border bg-background px-2" value={member.papel} onChange={event => updateRole.mutate({ member, papel: event.target.value as MembroGrupoDto['papel'] })} disabled={updateRole.isPending}><option value="Admin">Admin</option><option value="Investidor">Investidor</option><option value="Consulta">Consulta</option></select><Button variant="outline" onClick={() => deactivate.mutate(member)} disabled={member.papel === 'Admin' || deactivate.isPending}>Desativar</Button></>}</CardContent></Card>)}{!membersQuery.isLoading && !membersQuery.isError && (membersQuery.data?.dados.length ?? 0) === 0 && <p className="text-sm text-muted-foreground">Nenhum membro cadastrado.</p>}</section>
      </>}
    </div>
  )
}
