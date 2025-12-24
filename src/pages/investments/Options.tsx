import React, { useState } from "react"
import {
  Search,
  Filter,
  ArrowRightLeft,
  ChevronRight,
  ChevronDown
} from "lucide-react"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from "@/components/ui/card"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"

const optionsData = [
  { id: 1, type: "CALL", strike: 150.00, expiration: "2025-01-17", premium: 4.50, bid: 4.45, ask: 4.55, delta: 0.65, volume: 1200 },
  { id: 2, type: "PUT", strike: 150.00, expiration: "2025-01-17", premium: 3.20, bid: 3.15, ask: 3.25, delta: -0.35, volume: 850 },
  { id: 3, type: "CALL", strike: 155.00, expiration: "2025-01-17", premium: 2.10, bid: 2.05, ask: 2.15, delta: 0.45, volume: 2100 },
  { id: 4, type: "PUT", strike: 155.00, expiration: "2025-01-17", premium: 5.80, bid: 5.75, ask: 5.85, delta: -0.55, volume: 420 },
  { id: 5, type: "CALL", strike: 160.00, expiration: "2025-01-17", premium: 0.85, bid: 0.80, ask: 0.90, delta: 0.25, volume: 3500 },
]

export default function Options() {
  const [ticker, setTicker] = useState("AAPL")

  return (
    <div className="container mx-auto p-4 md:p-6 space-y-6">
      <header className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Cadeia de Opções</h1>
          <p className="text-muted-foreground">Monitore contratos e gregas de derivativos.</p>
        </div>
        <div className="flex items-center gap-2">
          <div className="relative w-full md:w-64">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Buscar Ativo..."
              value={ticker}
              onChange={(e) => setTicker(e.target.value.toUpperCase())}
              className="pl-8"
            />
          </div>
          <Button variant="outline" size="icon">
            <Filter className="h-4 w-4" />
          </Button>
        </div>
      </header>

      {/* Asset Overview Card */}
      <Card className="bg-gradient-to-r from-background to-muted/50">
        <CardContent className="p-6">
          <div className="flex flex-wrap items-center justify-between gap-6">
            <div className="flex items-center gap-4">
              <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center text-primary font-bold text-xl">
                {ticker[0]}
              </div>
              <div>
                <div className="text-2xl font-bold">{ticker}</div>
                <div className="text-sm text-muted-foreground">Apple Inc.</div>
              </div>
            </div>
            <div className="flex gap-8">
              <div>
                <div className="text-sm text-muted-foreground">Preço Atual</div>
                <div className="text-xl font-bold text-green-500">$ 154.20</div>
              </div>
              <div>
                <div className="text-sm text-muted-foreground">Variação (Dia)</div>
                <div className="text-xl font-bold text-green-500">+1.24%</div>
              </div>
              <div>
                <div className="text-sm text-muted-foreground">IV (Volatilidade Implícita)</div>
                <div className="text-xl font-bold">24.5%</div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Options Chain Table */}
      <Card>
        <CardHeader>
          <CardTitle>Contratos de Janeiro 2025</CardTitle>
          <CardDescription>Vencimento em 17 de Jan (24 dias remaining)</CardDescription>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tipo</TableHead>
                <TableHead>Preço Alvo (Strike)</TableHead>
                <TableHead>Último (Premium)</TableHead>
                <TableHead>Compra (Bid)</TableHead>
                <TableHead>Venda (Ask)</TableHead>
                <TableHead>Delta</TableHead>
                <TableHead className="text-right">Volume</TableHead>
                <TableHead className="w-[50px]"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {optionsData.map((option) => (
                <TableRow key={option.id} className="cursor-pointer hover:bg-muted/50">
                  <TableCell>
                    <Badge variant={option.type === "CALL" ? "default" : "destructive"}>
                      {option.type}
                    </Badge>
                  </TableCell>
                  <TableCell className="font-bold">$ {option.strike.toFixed(2)}</TableCell>
                  <TableCell>$ {option.premium.toFixed(2)}</TableCell>
                  <TableCell className="text-green-500 font-medium">$ {option.bid.toFixed(2)}</TableCell>
                  <TableCell className="text-red-500 font-medium">$ {option.ask.toFixed(2)}</TableCell>
                  <TableCell className="font-mono">{option.delta.toFixed(2)}</TableCell>
                  <TableCell className="text-right text-muted-foreground">{option.volume.toLocaleString()}</TableCell>
                  <TableCell>
                    <ChevronRight className="h-4 w-4 text-muted-foreground" />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Greeks Explanation Card */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold flex items-center gap-2">
              <span className="h-2 w-2 rounded-full bg-blue-500"></span>
              Delta
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">
              Sensibilidade do preço da opção em relação à mudança de $1 no preço do ativo subjacente.
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold flex items-center gap-2">
              <span className="h-2 w-2 rounded-full bg-purple-500"></span>
              Theta
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">
              Desvalorização temporal da opção conforme o vencimento se aproxima (Time Decay).
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold flex items-center gap-2">
              <span className="h-2 w-2 rounded-full bg-amber-500"></span>
              Gamma
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">
              Taxa de variação do Delta conforme o preço do ativo subjacente se move.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
