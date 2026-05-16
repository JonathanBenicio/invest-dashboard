import { useState, useMemo } from "react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatCurrency } from "@/lib/mock-data";
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

  const simulationData = useMemo(() => {
    const data: { month: number; invested: number; total: number; interest: number }[] = [];
    const monthlyRate = Math.pow(1 + interestRate / 100, 1 / 12) - 1;
    const totalMonths = years * 12;

    let currentAmount = initialAmount;
    let totalInvested = initialAmount;

    for (let i = 0; i <= totalMonths; i++) {
      data.push({
        month: i,
        invested: Number(totalInvested.toFixed(2)),
        total: Number(currentAmount.toFixed(2)),
        interest: Number((currentAmount - totalInvested).toFixed(2)),
      });

      if (i < totalMonths) {
        currentAmount = currentAmount * (1 + monthlyRate) + monthlyContribution;
        totalInvested += monthlyContribution;
      }
    }

    return data;
  }, [initialAmount, monthlyContribution, years, interestRate]);

  const finalResult = simulationData[simulationData.length - 1];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Simulador de Investimentos</h1>
        <p className="text-muted-foreground">Simule o crescimento do seu patrimônio com o poder dos juros compostos</p>
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
          </CardContent>
        </Card>

        {/* Results Section */}
        <div className="md:col-span-8 space-y-6">
          {/* Summary Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Total Investido</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{formatCurrency(finalResult.invested)}</div>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Total em Juros</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-success">{formatCurrency(finalResult.interest)}</div>
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

          {/* Evolution Chart */}
          <Card>
            <CardHeader>
              <CardTitle>Evolução do Patrimônio</CardTitle>
              <CardDescription>Projeção do crescimento ao longo do tempo</CardDescription>
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
                      dataKey="month"
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
                      dataKey="invested"
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
        </div>
      </div>
    </div>
  );
}
