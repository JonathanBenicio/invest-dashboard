import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { groupService } from '@/api/services/group.service'
import type { MembroGrupoDto, ResultadoConviteGrupoDto } from '@/api/dtos'
import { useToast } from '@/hooks/use-toast'
import { HolderProfiles } from '@/components/portfolio/HolderProfiles'

type ConviteRecente = {
  grupoId: string
  grupoNome: string
  email: string
  papel: MembroGrupoDto['papel']
  resultado: ResultadoConviteGrupoDto
}

const dataHora = (data: string | undefined) => data
  ? new Date(data).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
  : null

export default function Groups() {
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [groupId, setGroupId] = useState('')
  const [groupName, setGroupName] = useState('')
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<MembroGrupoDto['papel']>('Investidor')
  const [invitationResult, setInvitationResult] = useState<ConviteRecente | null>(null)
  const groupsQuery = useQuery({ queryKey: ['portfolio-groups'], queryFn: groupService.list })
  const invitationsQuery = useQuery({ queryKey: ['pending-group-invitations'], queryFn: groupService.pendingInvitations })
  const groups = groupsQuery.data?.dados ?? []
  const invitations = invitationsQuery.data?.dados ?? []
  const selectedGroup = groups.find(group => group.id === groupId)
    ?? (groups.length === 1 ? groups[0] : undefined)

  const membersQuery = useQuery({
    queryKey: ['portfolio-group-members', selectedGroup?.id],
    queryFn: () => groupService.members(selectedGroup!.id),
    enabled: !!selectedGroup,
  })
  const members = membersQuery.data?.dados ?? []
  const normalizedEmail = email.trim().toLowerCase()
  const matchingMember = members.find(member => member.email.trim().toLowerCase() === normalizedEmail)
  const isActiveMember = matchingMember?.ativo === true
  const refresh = () => queryClient.invalidateQueries({ queryKey: ['portfolio-group-members', selectedGroup?.id] })

  const createGroup = useMutation({
    mutationFn: groupService.create,
    onSuccess: async response => {
      await queryClient.invalidateQueries({ queryKey: ['portfolio-groups'] })
      setGroupId(response.dados.id)
      setGroupName('')
      toast({ title: 'Grupo criado', description: 'Você entrou como administrador.' })
    },
    onError: error => toast({ title: 'Não foi possível criar o grupo', description: error.message, variant: 'destructive' }),
  })

  const invite = useMutation({
    mutationFn: () => groupService.invite(selectedGroup!.id, normalizedEmail, role),
    onSuccess: response => {
      setInvitationResult({
        grupoId: selectedGroup!.id,
        grupoNome: selectedGroup!.nome,
        email: normalizedEmail,
        papel: role,
        resultado: response.dados,
      })
      setEmail('')
      toast({ title: 'Convite registrado', description: 'Veja abaixo como a pessoa deve aceitar.' })
    },
    onError: error => toast({ title: 'Não foi possível criar o convite', description: error.message, variant: 'destructive' }),
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
    onError: error => toast({ title: 'Não foi possível aceitar o convite', description: error.message, variant: 'destructive' }),
  })

  const updateRole = useMutation({
    mutationFn: ({ member, papel }: { member: MembroGrupoDto; papel: MembroGrupoDto['papel'] }) =>
      groupService.updateRole(selectedGroup!.id, member.id, papel),
    onSuccess: refresh,
    onError: error => toast({ title: 'Não foi possível alterar o papel', description: error.message, variant: 'destructive' }),
  })

  const deactivate = useMutation({
    mutationFn: (member: MembroGrupoDto) => groupService.deactivate(selectedGroup!.id, member.id),
    onSuccess: refresh,
    onError: error => toast({ title: 'Não foi possível desativar o membro', description: error.message, variant: 'destructive' }),
  })

  const isAdmin = selectedGroup?.papel === 'Admin'
  const canCheckMembers = !membersQuery.isLoading && !membersQuery.isError

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <header>
        <h1 className="text-2xl font-bold">Grupos e usuários</h1>
        <p className="text-muted-foreground">Escolha o grupo antes de gerenciar seus participantes.</p>
      </header>

      {invitationsQuery.isError && <p role="alert" className="text-sm text-destructive">Não foi possível carregar convites recebidos. Atualize a página e tente novamente.</p>}
      {invitations.length > 0 && <section className="space-y-3" aria-labelledby="received-invitations-heading">
        <h2 id="received-invitations-heading" className="text-lg font-semibold">Convites para sua conta</h2>
        {invitations.map(invitation => <Card key={invitation.id}><CardContent className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center">
          <div className="min-w-0 flex-1"><p className="font-medium">{invitation.grupoNome}</p><p className="text-sm text-muted-foreground">Acesso: {invitation.papel}. Aceite até {dataHora(invitation.expiraEmUtc)}.</p></div>
          <Button onClick={() => acceptInvitation.mutate({ grupoId: invitation.grupoId, conviteId: invitation.id })} disabled={acceptInvitation.isPending}>Aceitar convite</Button>
        </CardContent></Card>)}
      </section>}

      {groupsQuery.isLoading && <p role="status">Carregando grupos…</p>}
      {groupsQuery.isError && <div role="alert" className="space-y-2 text-sm text-destructive"><p>Não foi possível carregar os grupos. Nenhum convite será enviado até confirmar o grupo de destino.</p><Button variant="outline" onClick={() => void groupsQuery.refetch()}>Tentar novamente</Button></div>}
      {!groupsQuery.isLoading && !groupsQuery.isError && groups.length === 0 && <Card><CardContent className="space-y-4 p-5">
        <h2 className="font-semibold">Crie seu primeiro grupo</h2>
        <form className="flex flex-col gap-3 sm:flex-row" onSubmit={event => { event.preventDefault(); createGroup.mutate(groupName.trim()) }}>
          <Input aria-label="Nome do grupo" placeholder="Nome do grupo" maxLength={120} required value={groupName} onChange={event => setGroupName(event.target.value)} />
          <Button disabled={createGroup.isPending || !groupName.trim()}>Criar grupo</Button>
        </form>
      </CardContent></Card>}

      {groups.length > 0 && !groupsQuery.isLoading && !groupsQuery.isError && <section className="space-y-5">
        <Card><CardContent className="space-y-3 p-5">
          <Label htmlFor="group-selector">1. Grupo de destino</Label>
          <select id="group-selector" className="h-10 w-full rounded-md border bg-background px-3" value={selectedGroup?.id ?? ''} onChange={event => {
            setGroupId(event.target.value)
            setInvitationResult(null)
          }}>
            {groups.length > 1 && <option value="">Selecione o grupo</option>}
            {groups.map(group => <option key={group.id} value={group.id}>{group.nome} · {group.papel}</option>)}
          </select>
        </CardContent></Card>

        {selectedGroup && <>
          {isAdmin && <Card><CardContent className="space-y-4 p-5">
            <div>
              <h2 className="font-semibold">2. Adicionar pessoa a {selectedGroup.nome}</h2>
              <p className="mt-1 text-sm text-muted-foreground">A pessoa entra no grupo depois de aceitar o convite com este mesmo e-mail.</p>
            </div>
            <form className="grid gap-3 sm:grid-cols-[1fr_180px_auto] sm:items-end" onSubmit={event => { event.preventDefault(); invite.mutate() }}>
              <div className="space-y-2"><Label htmlFor="invite-email">E-mail da conta</Label><Input id="invite-email" type="email" autoComplete="email" required value={email} onChange={event => { setEmail(event.target.value); setInvitationResult(null) }} placeholder="pessoa@exemplo.com" /></div>
              <div className="space-y-2"><Label htmlFor="invite-role">Acesso no grupo</Label><select id="invite-role" className="h-10 w-full rounded-md border bg-background px-3" value={role} onChange={event => { setRole(event.target.value as MembroGrupoDto['papel']); setInvitationResult(null) }}><option value="Investidor">Investidor</option><option value="Consulta">Consulta</option><option value="Admin">Administrador</option></select></div>
              <Button disabled={invite.isPending || !normalizedEmail || !canCheckMembers || isActiveMember}>{invite.isPending ? 'Criando convite…' : '3. Criar convite'}</Button>
            </form>
            {membersQuery.isLoading && <p role="status" className="text-sm text-muted-foreground">Verificando se a pessoa já pertence a este grupo…</p>}
            {membersQuery.isError && <p role="alert" className="text-sm text-destructive">Não foi possível verificar os membros. Tente novamente antes de convidar.</p>}
            {isActiveMember && <p role="status" className="rounded-md border p-3 text-sm">Esta pessoa já é membro ativo de {selectedGroup.nome}. Altere o acesso na lista de membros abaixo.</p>}
            {matchingMember && !matchingMember.ativo && <p role="status" className="rounded-md border p-3 text-sm">Esta pessoa tem um vínculo desativado. Aceitar o convite reativará o acesso no grupo.</p>}
            <p className="text-xs text-muted-foreground">Se a pessoa já tem conta, ela deve entrar com esse e-mail e aceitar o convite nesta tela. Se ainda não tem conta, deve concluir o fluxo do link enviado. A tela seguinte explica o passo conforme o convite.</p>
            {invitationResult?.grupoId === selectedGroup.id && <div role="status" className="space-y-1 rounded-md border border-info/30 bg-info/5 p-3 text-sm">
              <p className="font-medium">Convite para {invitationResult.email} · {invitationResult.grupoNome} · {invitationResult.papel}</p>
              <p>{invitationResult.resultado.emailEnviado
                ? 'E-mail enviado pelo Supabase. A pessoa deve abrir o link e concluir o acesso antes de ele expirar.'
                : 'Nenhum e-mail novo foi enviado. A pessoa deve entrar com este endereço, abrir Grupos e usuários e aceitar o convite.'}</p>
              {invitationResult.resultado.expiraEmUtc && <p>Convite válido até {dataHora(invitationResult.resultado.expiraEmUtc)}.</p>}
            </div>}
          </CardContent></Card>}

          {isAdmin && <details className="rounded-lg border p-4">
            <summary className="cursor-pointer font-medium">Titulares das carteiras</summary>
            <div className="mt-4"><HolderProfiles key={selectedGroup.id} groupId={selectedGroup.id} members={members} /></div>
          </details>}

          <section className="space-y-3" aria-labelledby="members-heading">
            <h2 id="members-heading" className="text-lg font-semibold">Membros de {selectedGroup.nome}</h2>
            {membersQuery.isLoading ? <p role="status" className="py-8 text-center text-muted-foreground">Carregando membros…</p>
              : membersQuery.isError ? <Card><CardContent className="p-5 text-sm text-muted-foreground">Não foi possível carregar os membros. Administradores do grupo podem ver esta lista.</CardContent></Card>
                : members.map(member => <Card key={member.id}><CardContent className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center">
                  <div className="min-w-0 flex-1"><p className="font-medium">{member.nome || member.email}</p><p className="truncate text-sm text-muted-foreground">{member.email}</p></div>
                  <span className="text-sm text-muted-foreground">{member.ativo ? 'Ativo' : 'Desativado'}</span>
                  {isAdmin && member.ativo && <>
                    <select aria-label={`Papel de ${member.email}`} className="h-9 rounded-md border bg-background px-2" value={member.papel} onChange={event => updateRole.mutate({ member, papel: event.target.value as MembroGrupoDto['papel'] })} disabled={updateRole.isPending}><option value="Admin">Administrador</option><option value="Investidor">Investidor</option><option value="Consulta">Consulta</option></select>
                    <Button type="button" variant="outline" onClick={() => deactivate.mutate(member)} disabled={member.papel === 'Admin' || deactivate.isPending}>Desativar</Button>
                  </>}
                </CardContent></Card>)}
            {!membersQuery.isLoading && !membersQuery.isError && members.length === 0 && <p className="text-sm text-muted-foreground">Nenhum membro cadastrado.</p>}
          </section>
        </>}
      </section>}
    </div>
  )
}
