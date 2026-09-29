import { useState, useEffect } from 'react'
import { Search, Loader2 } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Button } from '@/components/ui/button'
import { marketDataService } from '@/api/services/market-data.service'
import type { MarketSearchResultDto } from '@/api/dtos'

interface StockSearchProps {
  onSelect: (quote: MarketSearchResultDto) => void
  defaultValue?: string
}

export function StockSearch({ onSelect, defaultValue = '' }: StockSearchProps) {
  const [open, setOpen] = useState(false)
  const [value, setValue] = useState(defaultValue)
  const [search, setSearch] = useState('')
  const [results, setResults] = useState<MarketSearchResultDto[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!search || search.length < 2) {
      setResults([])
      return
    }

    const delayDebounceFn = setTimeout(async () => {
      setLoading(true)
      try {
        const response = await marketDataService.search(search)
        setResults(response.data.slice(0, 10))
      } catch (error) {
        console.error('Error searching tickers:', error)
      } finally {
        setLoading(false)
      }
    }, 500)

    return () => clearTimeout(delayDebounceFn)
  }, [search])

  const handleSelect = (quote: MarketSearchResultDto) => {
    setValue(quote.symbol)
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
            <CommandEmpty>Nenhum ativo encontrado.</CommandEmpty>
            <CommandGroup>
              {results.map((quote) => (
                <CommandItem
                  key={quote.symbol}
                  value={quote.symbol}
                  onSelect={() => handleSelect(quote)}
                >
                  <div className="flex w-full items-center justify-between gap-3">
                    <span>{quote.symbol} · {quote.name}</span>
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
