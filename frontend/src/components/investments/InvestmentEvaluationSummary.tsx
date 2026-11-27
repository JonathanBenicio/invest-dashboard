import type { PosicaoInvestimentoConsolidadoDto } from '@/api/dtos/investment.dto'

function formatCivilDate(value: string) {
  const date = value.slice(0, 10)
  return /^\d{4}-\d{2}-\d{2}$/.test(date) ? date.split('-').reverse().join('/') : value
}

interface InvestmentEvaluationSummaryProps {
  investment: PosicaoInvestimentoConsolidadoDto
  className?: string
}

export function InvestmentEvaluationSummary({ investment, className = '' }: InvestmentEvaluationSummaryProps) {
  const earliest = investment.dataAvaliacaoMaisAntiga
  const latest = investment.dataAvaliacaoMaisRecente
  const hasRange = Boolean(earliest && latest)

  return <div className={`break-words text-xs text-muted-foreground ${className}`}>
    {investment.datasAvaliacaoDiferentes && hasRange
      ? <p>Datas de avaliação diferentes: {formatCivilDate(earliest!)} a {formatCivilDate(latest!)}</p>
      : hasRange && earliest === latest
        ? <p>Avaliação em {formatCivilDate(earliest!)}</p>
        : hasRange
          ? <p>Avaliações entre {formatCivilDate(earliest!)} e {formatCivilDate(latest!)}</p>
          : <p>Data de avaliação indisponível</p>}
    {!investment.datasAvaliacaoCompletas && hasRange && <p>Há posições sem data de avaliação.</p>}
  </div>
}
