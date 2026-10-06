import { useState, useEffect } from 'react'
import { Search, Loader2 } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Button } from '@/components/ui/button'
import { marketDataService } from '@/api/services/market-data.service'
import type { ResultadoBuscaMercadoDto } from '@/api/dtos'

interface StockSearchProps {
  onSelect: (quote: ResultadoBuscaMercadoDto) => void
  defaultValue?: string
}

export function StockSearch({ onSelect, defaultValue = '' }: StockSearchProps) {
  const [open, setOpen] = useState(false)
  const [value, setValue] = useState(defaultValue)
  const [search, setSearch] = useState('')
  const [results, setResults] = useState<ResultadoBuscaMercadoDto[]>([])
  const [loading, setLoading] = useState(false)
  const [searchError, setSearchError] = useState(false)
  const [retryCount, setRetryCount] = useState(0)

  useEffect(() => {
    if (!search || search.length < 2) {
      setResults([])
      setSearchError(false)
      setLoading(false)
      return
    }

    let cancelled = false
    setResults([])
    setSearchError(false)
    setLoading(true)
    const delayDebounceFn = setTimeout(async () => {
      try {
        const response = await marketDataService.search(search)
        if (!cancelled) setResults(response.dados.slice(0, 10))
      } catch {
        if (!cancelled) {
          setResults([])
          setSearchError(true)
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }, 500)

    return () => {
      cancelled = true
      clearTimeout(delayDebounceFn)
    }
  }, [search, retryCount])

  const handleSelect = (quote: ResultadoBuscaMercadoDto) => {
    setValue(quote.simbolo)
    setOpen(false)
    onSelect(quote)
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className="w-full justify-between"
        >
          {value || "Pesquisar ativo..."}
          {loading ? <Loader2 className="ml-2 h-4 w-4 animate-spin" /> : <Search className="ml-2 h-4 w-4 shrink-0 opacity-50" />}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[var(--radix-popover-trigger-width)] p-0">
        <Command shouldFilter={false}>
          <CommandInput
            placeholder="Digite o ticker (ex: PETR4)..."
            onValueChange={setSearch}
          />
          <CommandList>
            <CommandEmpty>
              {searchError ? (
                <div className="flex flex-col items-center gap-2 p-2">
                  <p role="alert">Não foi possível pesquisar ativos.</p>
                  <Button type="button" variant="ghost" size="sm" onClick={() => setRetryCount(count => count + 1)}>
                    Tentar novamente
                  </Button>
                </div>
              ) : loading ? (
                'Pesquisando ativos...'
              ) : (
                'Nenhum ativo encontrado.'
              )}
            </CommandEmpty>
            <CommandGroup>
              {results.map((quote) => (
                <CommandItem
                  key={quote.simbolo}
                  value={quote.simbolo}
                  onSelect={() => handleSelect(quote)}
                >
                  <div className="flex w-full items-center justify-between gap-3">
                    <span>{quote.simbolo} · {quote.nome}</span>
                  </div>
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
