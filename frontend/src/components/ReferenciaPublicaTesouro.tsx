import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { tesouroDiretoService, type TituloTesouroPublico } from '@/api/services/tesouro-direto.service'

// O ano ao final do nome é frequentemente exibido pelo portal, mas o catálogo
// público o apresenta como vencimento. Nenhuma associação é gravada aqui.
function nomeComparavel(nome: string, vencimento: string) {
  const ano = vencimento.slice(0, 4)
  return nome.normalize('NFD').replace(/[\u0300-\u036f]/g, '').trim()
    .replace(new RegExp(`\\s+${ano}$`), '').replace(/\s+/g, ' ').toLocaleLowerCase('pt-BR')
}

export function ReferenciaPublicaTesouro({ titulo, vencimento }: { titulo: string; vencimento: string }) {
  const [candidatos, setCandidatos] = useState<TituloTesouroPublico[] | null>(null)
  const [cotacao, setCotacao] = useState<{ titulo: TituloTesouroPublico; data: string; valor: number; origem: string } | null>(null)
  const [ocupado, setOcupado] = useState(false)
  const [erro, setErro] = useState('')

  async function buscar() {
    setOcupado(true); setErro(''); setCotacao(null)
    try {
      const resposta = await tesouroDiretoService.titulos()
      setCandidatos(resposta.dados.titulos.filter(item => item.dataVencimento === vencimento &&
        nomeComparavel(item.tipoTitulo, vencimento) === nomeComparavel(titulo, vencimento)))
    } catch {
      setCandidatos(null); setErro('Não foi possível consultar os títulos públicos. Tente novamente.')
    } finally { setOcupado(false) }
  }

  async function consultar(item: TituloTesouroPublico) {
    setOcupado(true); setErro(''); setCotacao(null)
    try {
      const resposta = await tesouroDiretoService.cotacoes(item, item.ultimaDataDisponivel, item.ultimaDataDisponivel)
      const ultima = resposta.dados.cotacoes.at(-1)
      if (!ultima) { setErro('Não há cotação para a data informada pelo catálogo.'); return }
      setCotacao({ titulo: item, data: ultima.dataBase, valor: ultima.puBaseManha, origem: resposta.dados.origem })
    } catch {
      setErro('Não foi possível consultar a cotação. Tente novamente.')
    } finally { setOcupado(false) }
  }

  return <section className="space-y-3 rounded-md border p-3 text-sm">
    <h3 className="font-semibold">Referência pública</h3>
    <p>Compare nome e vencimento antes de consultar. A cotação é informativa e não associa nem altera sua posição.</p>
    <Button type="button" variant="outline" disabled={ocupado} onClick={() => void buscar()}>Buscar títulos públicos correspondentes</Button>
    {ocupado && <p role="status">Consultando dados públicos...</p>}
    {erro && <p role="alert" className="text-destructive">{erro}</p>}
    {candidatos && candidatos.length === 0 && <p role="status">Nenhum título com nome e vencimento correspondentes foi encontrado.</p>}
    {candidatos && candidatos.length > 0 && <ul className="space-y-2">{candidatos.map(item => <li key={`${item.tipoTitulo}|${item.dataVencimento}`} className="flex flex-wrap items-center gap-2">
      <span>{item.tipoTitulo} · vencimento {item.dataVencimento.split('-').reverse().join('/')}</span>
      <Button type="button" variant="outline" disabled={ocupado} onClick={() => void consultar(item)}>Ver cotação deste título</Button>
    </li>)}</ul>}
    {cotacao && <p role="status">{cotacao.titulo.tipoTitulo}: PU base {cotacao.valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} em {cotacao.data.split('-').reverse().join('/')} · fonte: {cotacao.origem}. Este preço não representa necessariamente o valor líquido da sua posição.</p>}
  </section>
}
