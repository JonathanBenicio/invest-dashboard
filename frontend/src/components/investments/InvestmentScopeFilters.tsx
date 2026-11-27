import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import type { CarteiraDto, GrupoCarteirasDto } from '@/api/dtos'

interface InvestmentScopeFiltersProps {
  groups: GrupoCarteirasDto[]
  portfolios: CarteiraDto[]
  groupId: string
  holderId: string
  portfolioId: string
  onGroupChange: (id: string) => void
  onHolderChange: (id: string) => void
  onPortfolioChange: (id: string) => void
}

export function InvestmentScopeFilters({
  groups,
  portfolios,
  groupId,
  holderId,
  portfolioId,
  onGroupChange,
  onHolderChange,
  onPortfolioChange,
}: InvestmentScopeFiltersProps) {
  const groupPortfolios = groupId ? portfolios.filter(portfolio => portfolio.grupoId === groupId) : []
  const holders = [...new Map(groupPortfolios
    .filter(portfolio => portfolio.titularId)
    .map(portfolio => [portfolio.titularId!, portfolio.titular])).entries()]
    .map(([id, nome]) => ({ id, nome }))
  const holderPortfolios = holderId
    ? groupPortfolios.filter(portfolio => portfolio.titularId === holderId)
    : groupPortfolios
  const selectablePortfolios = groupId ? holderPortfolios : portfolios

  return <div role="group" aria-label="Escopo dos investimentos" className="grid min-w-0 grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
    <div className="grid min-w-0 gap-1.5">
      <Label htmlFor="investment-group-filter">Grupo</Label>
      <Select value={groupId || 'all-groups'} onValueChange={value => onGroupChange(value === 'all-groups' ? '' : value)}>
        <SelectTrigger id="investment-group-filter"><SelectValue /></SelectTrigger>
        <SelectContent>
          <SelectItem value="all-groups">Todos os grupos</SelectItem>
          {groups.map(group => <SelectItem key={group.id} value={group.id}>{group.nome}</SelectItem>)}
        </SelectContent>
      </Select>
    </div>
    <div className="grid min-w-0 gap-1.5">
      <Label htmlFor="investment-holder-filter">Titular</Label>
      <Select disabled={!groupId} value={holderId || 'all-holders'} onValueChange={value => onHolderChange(value === 'all-holders' ? '' : value)}>
        <SelectTrigger id="investment-holder-filter"><SelectValue placeholder={groupId ? 'Todos os titulares' : 'Selecione um grupo'} /></SelectTrigger>
        <SelectContent>
          <SelectItem value="all-holders">Todos os titulares</SelectItem>
          {holders.map(holder => <SelectItem key={holder.id} value={holder.id}>{holder.nome || 'Titular sem nome'}</SelectItem>)}
        </SelectContent>
      </Select>
    </div>
    <div className="grid min-w-0 gap-1.5">
      <Label htmlFor="investment-portfolio-filter">Carteira</Label>
      <Select disabled={selectablePortfolios.length === 0} value={portfolioId || 'all-portfolios'} onValueChange={value => onPortfolioChange(value === 'all-portfolios' ? '' : value)}>
        <SelectTrigger id="investment-portfolio-filter"><SelectValue placeholder={groupId ? 'Todas as carteiras' : 'Selecione um grupo'} /></SelectTrigger>
        <SelectContent>
          <SelectItem value="all-portfolios">Todas as carteiras</SelectItem>
          {selectablePortfolios.map(portfolio => <SelectItem key={portfolio.id} value={portfolio.id}>
            {portfolio.nome}{portfolio.titular ? ` · ${portfolio.titular}` : ' · Sem titular'}{!groupId && ` · ${groups.find(group => group.id === portfolio.grupoId)?.nome ?? 'Sem grupo'}`}
          </SelectItem>)}
        </SelectContent>
      </Select>
    </div>
  </div>
}
