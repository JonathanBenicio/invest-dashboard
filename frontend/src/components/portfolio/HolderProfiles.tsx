import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { holderService, type TitularDto, type SalvarTitular } from '@/api/services/holder.service'
import type { MembroGrupoDto } from '@/api/dtos'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { useToast } from '@/hooks/use-toast'

export function HolderProfiles({ groupId, members }: { groupId: string; members: MembroGrupoDto[] }) {
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<TitularDto | null>(null)
  const [name, setName] = useState('')
  const [relationship, setRelationship] = useState('')
  const [userId, setUserId] = useState('')
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const profiles = useQuery({ queryKey: ['holders', groupId], queryFn: () => holderService.list(groupId) })
  const save = useMutation({
    mutationFn: (request: SalvarTitular) => editing
      ? holderService.update(groupId, editing.id, request)
      : holderService.create(groupId, request),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['holders', groupId] }),
        queryClient.invalidateQueries({ queryKey: ['portfolios'] }),
      ])
      setOpen(false)
      toast({ title: 'Perfil de titular salvo' })
    },
    onError: error => toast({ title: 'Não foi possível salvar o titular', description: error.message, variant: 'destructive' }),
  })
  const edit = (profile: TitularDto | null) => {
    setEditing(profile)
    setName(profile?.nome ?? '')
    setRelationship(profile?.parentesco ?? '')
    setUserId(profile?.usuarioId ?? '')
    setOpen(true)
  }
  return <section className="space-y-3">
    <div className="flex items-center justify-between gap-3"><h2 className="text-lg font-semibold">Titulares das carteiras</h2><Button variant="outline" onClick={() => edit(null)}>Novo titular</Button></div>
    <p className="text-sm text-muted-foreground">O parentesco é informativo. Carteiras particulares são acessíveis ao membro vinculado e aos Admins; sem vínculo, somente aos Admins.</p>
    {profiles.isLoading ? <p>Carregando titulares…</p> : profiles.isError ? <p role="alert">Não foi possível carregar os titulares.</p> : profiles.data?.dados.map(profile => <Card key={profile.id}><CardContent className="flex items-center gap-3 p-4"><div className="flex-1"><p className="font-medium">{profile.nome}</p><p className="text-sm text-muted-foreground">{profile.parentesco ? `${profile.parentesco} · ` : ''}{profile.usuarioId ? 'Login vinculado' : 'Sem login vinculado'}</p></div><Button variant="outline" onClick={() => edit(profile)}>Editar titular</Button></CardContent></Card>)}
    <Dialog open={open} onOpenChange={setOpen}><DialogContent><DialogHeader><DialogTitle>{editing ? 'Editar titular' : 'Novo titular'}</DialogTitle></DialogHeader>
      <form className="space-y-4" onSubmit={event => { event.preventDefault(); save.mutate({ nome: name.trim(), parentesco: relationship.trim() || undefined, usuarioId: userId || undefined }) }}>
        <div className="space-y-2"><Label htmlFor="holder-profile-name">Nome</Label><Input id="holder-profile-name" required maxLength={160} value={name} onChange={event => setName(event.target.value)} /></div>
        <div className="space-y-2"><Label htmlFor="holder-profile-relationship">Parentesco (opcional)</Label><Input id="holder-profile-relationship" maxLength={80} value={relationship} onChange={event => setRelationship(event.target.value)} /></div>
        <div className="space-y-2"><Label htmlFor="holder-profile-user">Membro ativo vinculado</Label><select id="holder-profile-user" className="h-10 w-full rounded-md border bg-background px-3" value={userId} onChange={event => setUserId(event.target.value)}><option value="">Sem vínculo com login</option>{members.filter(member => member.ativo).map(member => <option key={member.id} value={member.usuarioId}>{member.nome || member.email}</option>)}</select></div>
        <Button type="submit" disabled={save.isPending}>Salvar titular</Button>
      </form>
    </DialogContent></Dialog>
  </section>
}
