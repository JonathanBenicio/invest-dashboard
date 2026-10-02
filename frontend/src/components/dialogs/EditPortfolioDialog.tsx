import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { financialInstitutionService } from '@/api/services/financial-institution.service'
import { groupService } from '@/api/services/group.service'
import type { AtualizarCarteiraRequest, CarteiraDto } from '@/api/dtos'

interface EditPortfolioDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  portfolio: CarteiraDto | null
  canManageTitular: boolean
  onSave: (portfolioId: string, update: AtualizarCarteiraRequest) => void
}

export function EditPortfolioDialog({ open, onOpenChange, portfolio, canManageTitular, onSave }: EditPortfolioDialogProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [holder, setHolder] = useState('')
  const [relationship, setRelationship] = useState('')
  const [holderUserSelection, setHolderUserSelection] = useState('__keep__')
  const [institution, setInstitution] = useState('')
  const [institutionId, setInstitutionId] = useState('')
  const [visibility, setVisibility] = useState<'Particular' | 'PublicaDoGrupo'>('Particular')
  const institutionsQuery = useQuery({
    queryKey: ['financial-institutions', portfolio?.grupoId],
    queryFn: () => financialInstitutionService.list(portfolio!.grupoId),
    enabled: open && !!portfolio?.grupoId,
  })
  const membersQuery = useQuery({
    queryKey: ['portfolio-group-members-for-holder-edit', portfolio?.grupoId],
    queryFn: () => groupService.members(portfolio!.grupoId!),
    enabled: open && canManageTitular && !!portfolio?.grupoId,
  })
  const institutions = useMemo(() => institutionsQuery.data?.dados ?? [], [institutionsQuery.data])
  const members = membersQuery.data?.dados ?? []

  useEffect(() => {
    setName(portfolio?.nome ?? '')
    setDescription(portfolio?.descricao ?? '')
    setHolder(portfolio?.titular ?? '')
    setRelationship(portfolio?.parentesco ?? '')
    setHolderUserSelection('__keep__')
    setInstitution(portfolio?.instituicaoFinanceira ?? '')
    setInstitutionId(portfolio?.instituicaoFinanceiraId ?? '')
    setVisibility(portfolio?.visibilidade ?? 'Particular')
  }, [portfolio])

  useEffect(() => {
    if (!portfolio || portfolio.instituicaoFinanceiraId || !portfolio.instituicaoFinanceira || institutions.length === 0) return
    const match = institutions.find(item => item.nome.toLocaleLowerCase('pt-BR') === portfolio.instituicaoFinanceira?.toLocaleLowerCase('pt-BR'))
    setInstitutionId(match?.id ?? 'custom')
  }, [institutions, portfolio])

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!portfolio) return
    const selectedInstitution = institutions.find(item => item.id === institutionId)
    onSave(portfolio.id, {
      nome: name.trim(),
      descricao: description.trim() || undefined,
      titular: holder.trim(),
      titularId: portfolio.titularId,
      titularUsuarioId: holderUserSelection !== '__keep__' && holderUserSelection !== '__unlink__' ? holderUserSelection : undefined,
      desvincularTitular: holderUserSelection === '__unlink__',
      parentesco: relationship.trim() || undefined,
      instituicaoFinanceiraId: institutionId && institutionId !== 'custom' ? institutionId : undefined,
      instituicaoFinanceira: institutionId === 'custom' ? institution.trim() : institutionId ? selectedInstitution?.nome : '',
      tipoInstituicao: selectedInstitution?.categoria ?? (institutionId === 'custom' ? 'Outra' : undefined),
      visibilidade: visibility,
    })
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Editar carteira</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="edit-name">Nome</Label>
            <Input
              id="edit-name"
              value={name}
              onChange={event => setName(event.target.value)}
              maxLength={100}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="edit-holder">Titular</Label>
            <Input id="edit-holder" value={holder} onChange={event => setHolder(event.target.value)} maxLength={160} required disabled={!canManageTitular} />
            {canManageTitular && <>
              <Label htmlFor="edit-holder-member">Vínculo com login do grupo</Label>
              <select id="edit-holder-member" className="h-10 w-full rounded-md border bg-background px-3" value={holderUserSelection} onChange={event => {
                setHolderUserSelection(event.target.value)
                const member = members.find(item => item.usuarioId === event.target.value)
                if (member) setHolder(member.nome || member.email)
              }}>
                <option value="__keep__">{portfolio?.titularVinculado ? 'Manter vínculo atual' : 'Sem vínculo com login'}</option>
                {portfolio?.titularVinculado && <option value="__unlink__">Desvincular do login</option>}
                {members.filter(member => member.ativo).map(member => <option key={member.id} value={member.usuarioId}>{member.nome || member.email} · {member.email}</option>)}
              </select>
              <Label htmlFor="edit-relationship">Parentesco (informativo)</Label>
              <Input id="edit-relationship" value={relationship} onChange={event => setRelationship(event.target.value)} maxLength={80} />
            </>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="edit-institution">Instituição financeira</Label>
            <select id="edit-institution" className="h-10 w-full rounded-md border bg-background px-3" value={institutionId} onChange={event => { setInstitutionId(event.target.value); setInstitution('') }} required>
              <option value="" disabled>Selecione uma instituição</option>
              {institutions.map(item => <option key={item.id} value={item.id}>{item.nome} · {item.categoria}</option>)}
              <option value="custom">Outra — cadastrar neste grupo</option>
            </select>
            {institutionId === 'custom' && <Input aria-label="Nome da instituição personalizada" value={institution} onChange={event => setInstitution(event.target.value)} maxLength={120} required />}
          </div>
          <div className="space-y-2"><Label htmlFor="edit-visibility">Visibilidade</Label><select id="edit-visibility" className="h-10 w-full rounded-md border bg-background px-3" value={visibility} onChange={event => setVisibility(event.target.value as typeof visibility)}><option value="Particular">Particular</option><option value="PublicaDoGrupo">Pública do grupo</option></select></div>
          <div className="space-y-2">
            <Label htmlFor="edit-description">Descrição (opcional)</Label>
            <Input
              id="edit-description"
              value={description}
              onChange={event => setDescription(event.target.value)}
              maxLength={500}
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancelar</Button>
            <Button type="submit">Salvar</Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
