import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { formatCurrency } from "@/lib/utils";
import { simulationService } from "@/api/services";
import type { SimulacaoResponse, SimulacaoEstrategia } from "@/api/dtos";
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend
} from "recharts";

export default function Simulator() {
  const [initialAmount, setInitialAmount] = useState<number>(1000);
  const [monthlyContribution, setMonthlyContribution] = useState<number>(500);
  const [years, setYears] = useState<number>(5);
  const [interestRate, setInterestRate] = useState<number>(10);
  const [strategy, setStrategy] = useState<SimulacaoEstrategia['id']>("deterministic");
  const [volatility, setVolatility] = useState<number>(15);
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<SimulacaoResponse | null>(null);
  const [simulationError, setSimulationError] = useState<string | null>(null);
  const { data: strategiesResponse, isLoading: isLoadingStrategies, isError: isStrategiesError } = useQuery({
    queryKey: ['simulation', 'strategies'],
    queryFn: () => simulationService.getStrategies(),
  });
  const strategies = strategiesResponse?.dados ?? [];

  const handleSimulate = async () => {
    setSimulationError(null);
    setIsLoading(true);
    try {
      const response = await simulationService.simulate({
        valorInicial: initialAmount,
        aporteMensal: monthlyContribution,
        anos: years,
        taxaJurosAnual: interestRate,
        estrategia: strategy,
        volatilidade: strategy === 'montecarlo' ? volatility : undefined,
        numeroSimulacoes: strategy === 'montecarlo' ? 1000 : undefined,
      });
      setResult(response.dados);
    } catch (error) {
      setSimulationError(error instanceof Error
        ? error.message
        : "Não foi possível executar a simulação. Tente novamente.");
    } finally {
      setIsLoading(false);
    }
  };

  const simulationData = result?.pontos ?? [];
  const finalResult = simulationData[simulationData.length - 1];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Simulador de Investimentos</h1>
        <p className="text-muted-foreground">Simule o crescimento do seu patrimônio com diferentes estratégias</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-6">
        {/* Input Section */}
        <Card className="md:col-span-4 h-fit">
          <CardHeader>
            <CardTitle>Parâmetros</CardTitle>
            <CardDescription>Defina os valores da simulação</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="initialAmount">Investimento Inicial (R$)</Label>
              <Input
                id="initialAmount"
                type="number"
                min="0"
                value={initialAmount}
                onChange={(e) => setInitialAmount(Number(e.target.value))}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="monthlyContribution">Aporte Mensal (R$)</Label>
              <Input
                id="monthlyContribution"
                type="number"
                min="0"
                value={monthlyContribution}
                onChange={(e) => setMonthlyContribution(Number(e.target.value))}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="years">Prazo (Anos)</Label>
              <Input
                id="years"
                type="number"
                min="1"
                max="50"
                value={years}
                onChange={(e) => setYears(Number(e.target.value))}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="interestRate">Taxa de Juros Anual (%)</Label>
              <Input
                id="interestRate"
                type="number"
                min="0"
                step="0.1"
                value={interestRate}
                onChange={(e) => setInterestRate(Number(e.target.value))}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="strategy">Estratégia</Label>
                            <Select
                value={strategy}
                onValueChange={value => {
                  if (value === 'deterministic' || value === 'montecarlo') setStrategy(value)
                }}
                disabled={isLoadingStrategies || isStrategiesError || strategies.length === 0}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Selecione a estratégia" />
                </SelectTrigger>
                <SelectContent>
                  {strategies.map(item => <SelectItem key={item.id} value={item.id}>{item.nome}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            {strategy === "montecarlo" && (
              <div className="space-y-2">
                <Label htmlFor="volatility">Volatilidade Anual (%)</Label>
                <Input
                  id="volatility"
                  type="number"
                  min="1"
                  max="100"
                  step="1"
                  value={volatility}
                  onChange={(e) => setVolatility(Number(e.target.value))}
                />
              </div>
            )}
            <Button
              className="w-full"
              onClick={handleSimulate}
              disabled={isLoading || isLoadingStrategies || isStrategiesError || strategies.length === 0}
            >
              {isLoading ? "Simulando..." : "Simular"}
            </Button>
          </CardContent>
        </Card>

        {/* Results Section */}
        <div className="md:col-span-8 space-y-6">
          {/* Summary Cards */}
          {result && finalResult && (
            <>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-muted-foreground">Total Investido</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold">{formatCurrency(finalResult.investido)}</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-muted-foreground">Total em Juros</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-success">{formatCurrency(finalResult.juros)}</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm font-medium text-muted-foreground">Valor Total Final</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-primary">{formatCurrency(finalResult.total)}</div>
                  </CardContent>
                </Card>
              </div>

              <Card>
                <CardHeader>
                  <CardTitle>Evolução do Patrimônio</CardTitle>
                  <CardDescription>
                    Projeção do crescimento ao longo do tempo — {result.nomeEstrategia}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="h-[300px] w-full">
                    <ResponsiveContainer width="100%" height="100%">
                      <AreaChart
                        data={simulationData}
                        margin={{ top: 10, right: 30, left: 0, bottom: 0 }}
                      >
                        <defs>
                          <linearGradient id="colorTotal" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.8}/>
                            <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0}/>
                          </linearGradient>
                          <linearGradient id="colorInvested" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="5%" stopColor="hsl(var(--muted-foreground))" stopOpacity={0.3}/>
                            <stop offset="95%" stopColor="hsl(var(--muted-foreground))" stopOpacity={0}/>
                          </linearGradient>
                        </defs>
                        <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                        <XAxis
                          dataKey="mes"
                          tickFormatter={(value) => `${Math.floor(value / 12)} anos`}
                          minTickGap={30}
                        />
                        <YAxis
                          tickFormatter={(value) =>
                            new Intl.NumberFormat('pt-BR', { notation: "compact", compactDisplay: "short" }).format(value)
                          }
                        />
                        <Tooltip
                          formatter={(value: number) => formatCurrency(value)}
                          labelFormatter={(label) => `${Math.floor(label / 12)} anos e ${label % 12} meses`}
                          contentStyle={{ backgroundColor: 'hsl(var(--background))', borderColor: 'hsl(var(--border))' }}
                        />
                        <Legend />
                        <Area
                          type="monotone"
                          dataKey="total"
                          name="Valor Total"
                          stroke="hsl(var(--primary))"
                          fillOpacity={1}
                          fill="url(#colorTotal)"
                        />
                        <Area
                          type="monotone"
                          dataKey="investido"
                          name="Total Investido"
                          stroke="hsl(var(--muted-foreground))"
                          fillOpacity={1}
                          fill="url(#colorInvested)"
                        />
                      </AreaChart>
                    </ResponsiveContainer>
                  </div>
                </CardContent>
              </Card>
            </>
          )}

          {isStrategiesError && (
            <div role="alert" className="rounded-md border border-destructive/40 p-4 text-sm text-destructive">
              Não foi possível carregar as estratégias pela API. Atualize a página para tentar novamente.
            </div>
          )}

          {simulationError && (
            <div role="alert" className="rounded-md border border-destructive/40 p-4 text-sm text-destructive">
              Falha ao executar a simulação: {simulationError}
            </div>
          )}

          {isLoading && (
            <div role="status" className="rounded-md border p-4 text-sm text-muted-foreground">
              Calculando com a API do simulador...
            </div>
          )}

          {!result && !isLoading && !simulationError && !isStrategiesError && (
            <Card>
              <CardHeader>
                <CardTitle>Simulação</CardTitle>
                <CardDescription>Configure os parâmetros ao lado e clique em "Simular" para ver os resultados</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex items-center justify-center h-[300px] text-muted-foreground">
                  Aguardando simulação...
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
