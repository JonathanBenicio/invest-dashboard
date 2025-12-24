import { useState, useEffect } from 'react'
import { Search, Loader2 } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Button } from '@/components/ui/button'
import { brapiService } from '@/api/services/brapi.service'
import type { BrapiQuote } from '@/api/dtos'

interface StockSearchProps {
  onSelect: (quote: BrapiQuote) => void
  defaultValue?: string
}

export function StockSearch({ onSelect, defaultValue = '' }: StockSearchProps) {
  const [open, setOpen] = useState(false)
  const [value, setValue] = useState(defaultValue)
  const [search, setSearch] = useState('')
  const [results, setResults] = useState<string[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!search || search.length < 2) {
      setResults([])
      return
    }

    const delayDebounceFn = setTimeout(async () => {
      setLoading(true)
      try {
        const response = await brapiService.getAvailableTickers(search)
        setResults(response.stocks.slice(0, 10)) // Limit to 10 results
      } catch (error) {
        console.error('Error searching tickers:', error)
      } finally {
        setLoading(false)
      }
    }, 500)

    return () => clearTimeout(delayDebounceFn)
  }, [search])

  const handleSelect = async (ticker: string) => {
    setValue(ticker)
    setOpen(false)
    setLoading(true)
    try {
      const response = await brapiService.getQuote([ticker])
      if (response.results && response.results.length > 0) {
        const quote = response.results[0]
        onSelect(quote)
      }
    } catch (error) {
      console.error('Error fetching quote:', error)
    } finally {
      setLoading(false)
    }
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
              {results.map((ticker) => (
                <CommandItem
                  key={ticker}
                  value={ticker}
                  onSelect={() => handleSelect(ticker)}
                >
                  {ticker}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
