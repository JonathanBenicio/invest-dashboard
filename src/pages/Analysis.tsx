import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { 
  ResponsiveContainer, 
  AreaChart, 
  Area, 
  XAxis, 
  YAxis, 
  Tooltip, 
  BarChart,
  Bar,
  LineChart,
  Line,
  PieChart,
  Pie,
  Cell,
  Legend,
  CartesianGrid
} from "recharts";
import { 
  portfolioHistory, 
  sectorAllocation, 
  monthlyDividends, 
  variableIncomeAssets,
  formatCurrency 
} from "@/lib/mock-data";
import { TrendingUp, TrendingDown, Target, Award } from "lucide-react";

export default function Analysis() {
  // Performance comparison data
  const performanceData = [
    { month: 'Jul', portfolio: 5.2, ibovespa: 3.8, cdi: 0.9 },
    { month: 'Ago', portfolio: 3.1, ibovespa: 2.5, cdi: 0.85 },
    { month: 'Set', portfolio: -1.2, ibovespa: -2.1, cdi: 0.82 },
    { month: 'Out', portfolio: 4.5, ibovespa: 3.2, cdi: 0.88 },
    { month: 'Nov', portfolio: 2.8, ibovespa: 1.9, cdi: 0.79 },
    { month: 'Dez', portfolio: 3.9, ibovespa: 2.8, cdi: 0.85 },
  ];

  // Asset performance
  const assetPerformance = variableIncomeAssets.map(asset => ({
    ticker: asset.ticker,
    name: asset.name,
    profit: ((asset.currentPrice - asset.averagePrice) / asset.averagePrice) * 100,
    value: asset.currentPrice * asset.quantity,
  })).sort((a, b) => b.profit - a.profit);

  const sectorColors = [
    'hsl(220, 70%, 50%)',
    'hsl(145, 65%, 42%)',
    'hsl(38, 92%, 50%)',
    'hsl(280, 65%, 55%)',
    'hsl(200, 85%, 50%)',
    'hsl(0, 72%, 51%)',
  ];

  // Key metrics
  const totalDividends = monthlyDividends.reduce((acc, m) => acc + m.received, 0);
  const avgMonthlyDividend = totalDividends / monthlyDividends.filter(m => m.received > 0).length;
  const bestAsset = assetPerformance[0];
  const worstAsset = assetPerformance[assetPerformance.length - 1];

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Análise Detalhada</h1>
          <p className="text-muted-foreground">Performance e indicadores da sua carteira</p>
        </div>
        <Select defaultValue="6m">
          <SelectTrigger className="w-[140px]">
            <SelectValue placeholder="Período" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="1m">1 mês</SelectItem>
            <SelectItem value="3m">3 meses</SelectItem>
            <SelectItem value="6m">6 meses</SelectItem>
            <SelectItem value="1y">1 ano</SelectItem>
            <SelectItem value="all">Todo período</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Key Metrics */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="h-10 w-10 rounded-lg bg-success/10 flex items-center justify-center">
                <TrendingUp className="h-5 w-5 text-success" />
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Melhor Ativo</p>
                <p className="text-lg font-bold">{bestAsset.ticker}</p>
                <p className="text-xs text-success">+{bestAsset.profit.toFixed(2)}%</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="h-10 w-10 rounded-lg bg-destructive/10 flex items-center justify-center">
                <TrendingDown className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Pior Ativo</p>
                <p className="text-lg font-bold">{worstAsset.ticker}</p>
                <p className="text-xs text-destructive">{worstAsset.profit.toFixed(2)}%</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="h-10 w-10 rounded-lg bg-primary/10 flex items-center justify-center">
                <Target className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Dividendos (Total)</p>
                <p className="text-lg font-bold">{formatCurrency(totalDividends)}</p>
                <p className="text-xs text-muted-foreground">últimos 6 meses</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="h-10 w-10 rounded-lg bg-warning/10 flex items-center justify-center">
                <Award className="h-5 w-5 text-warning" />
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Média Mensal</p>
                <p className="text-lg font-bold">{formatCurrency(avgMonthlyDividend)}</p>
                <p className="text-xs text-muted-foreground">em proventos</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="performance" className="space-y-4">
        <TabsList>
          <TabsTrigger value="performance">Performance</TabsTrigger>
          <TabsTrigger value="sectors">Setores</TabsTrigger>
          <TabsTrigger value="dividends">Proventos</TabsTrigger>
          <TabsTrigger value="assets">Por Ativo</TabsTrigger>
        </TabsList>

        <TabsContent value="performance" className="space-y-6">
          <div className="grid gap-6 lg:grid-cols-2">
            {/* Evolution Chart */}
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle>Evolução vs Benchmarks</CardTitle>
                <CardDescription>Compare sua carteira com Ibovespa e CDI</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="h-[350px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={performanceData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="hsl(220, 15%, 90%)" />
                      <XAxis dataKey="month" axisLine={false} tickLine={false} />
                      <YAxis 
                        axisLine={false} 
                        tickLine={false}
                        tickFormatter={(value) => `${value}%`}
                      />
                      <Tooltip 
                        formatter={(value: number) => `${value.toFixed(2)}%`}
                        contentStyle={{ 
                          backgroundColor: 'hsl(0, 0%, 100%)', 
                          border: '1px solid hsl(220, 15%, 90%)',
                          borderRadius: '8px'
                        }}
                      />
                      <Legend />
                      <Line 
                        type="monotone" 
                        dataKey="portfolio" 
                        stroke="hsl(220, 70%, 50%)" 
                        strokeWidth={2}
                        dot={{ fill: 'hsl(220, 70%, 50%)' }}
                        name="Sua Carteira"
                      />
                      <Line 
                        type="monotone" 
                        dataKey="ibovespa" 
                        stroke="hsl(145, 65%, 42%)" 
                        strokeWidth={2}
                        dot={{ fill: 'hsl(145, 65%, 42%)' }}
                        name="Ibovespa"
                      />
                      <Line 
                        type="monotone" 
                        dataKey="cdi" 
                        stroke="hsl(38, 92%, 50%)" 
                        strokeWidth={2}
                        dot={{ fill: 'hsl(38, 92%, 50%)' }}
                        name="CDI"
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="sectors">
          <div className="grid gap-6 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Alocação por Setor</CardTitle>
                <CardDescription>Distribuição percentual por setor</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="h-[300px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={sectorAllocation}
                        cx="50%"
                        cy="50%"
                        outerRadius={100}
                        dataKey="value"
                        label={({ name, value }) => `${name}: ${value}%`}
                        labelLine={false}
                      >
                        {sectorAllocation.map((_, index) => (
                          <Cell key={`cell-${index}`} fill={sectorColors[index % sectorColors.length]} />
                        ))}
                      </Pie>
                      <Tooltip 
                        formatter={(value: number) => `${value}%`}
                        contentStyle={{ 
                          backgroundColor: 'hsl(0, 0%, 100%)', 
                          border: '1px solid hsl(220, 15%, 90%)',
                          borderRadius: '8px'
                        }}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Detalhamento por Setor</CardTitle>
                <CardDescription>Concentração da carteira</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {sectorAllocation.map((sector, index) => (
                    <div key={sector.name} className="flex items-center gap-4">
                      <div 
                        className="h-3 w-3 rounded-full" 
                        style={{ backgroundColor: sectorColors[index % sectorColors.length] }}
                      />
                      <div className="flex-1">
                        <div className="flex justify-between mb-1">
                          <span className="text-sm font-medium">{sector.name}</span>
                          <span className="text-sm text-muted-foreground">{sector.value}%</span>
                        </div>
                        <div className="h-2 rounded-full bg-muted overflow-hidden">
                          <div 
                            className="h-full rounded-full transition-all"
                            style={{ 
                              width: `${sector.value}%`,
                              backgroundColor: sectorColors[index % sectorColors.length]
                            }}
                          />
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="dividends">
          <div className="grid gap-6 lg:grid-cols-2">
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle>Proventos Mensais</CardTitle>
                <CardDescription>Histórico de dividendos recebidos vs projetado</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="h-[300px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={monthlyDividends}>
                      <CartesianGrid strokeDasharray="3 3" stroke="hsl(220, 15%, 90%)" />
                      <XAxis dataKey="month" axisLine={false} tickLine={false} />
                      <YAxis 
                        axisLine={false} 
                        tickLine={false}
                        tickFormatter={(value) => formatCurrency(value)}
                      />
                      <Tooltip 
                        formatter={(value: number) => formatCurrency(value)}
                        contentStyle={{ 
                          backgroundColor: 'hsl(0, 0%, 100%)', 
                          border: '1px solid hsl(220, 15%, 90%)',
                          borderRadius: '8px'
                        }}
                      />
                      <Legend />
                      <Bar 
                        dataKey="received" 
                        fill="hsl(145, 65%, 42%)" 
                        radius={[4, 4, 0, 0]}
                        name="Recebido"
                      />
                      <Bar 
                        dataKey="projected" 
                        fill="hsl(220, 15%, 85%)" 
                        radius={[4, 4, 0, 0]}
                        name="Projetado"
                      />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="assets">
          <Card>
            <CardHeader>
              <CardTitle>Performance por Ativo</CardTitle>
              <CardDescription>Ordenado por rentabilidade</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {assetPerformance.map((asset) => (
                  <div key={asset.ticker} className="flex items-center gap-4 p-3 rounded-lg bg-muted/50">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{asset.ticker}</span>
                        <span className="text-xs text-muted-foreground">{asset.name}</span>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        Valor atual: {formatCurrency(asset.value)}
                      </p>
                    </div>
                    <Badge 
                      variant={asset.profit >= 0 ? "default" : "destructive"}
                      className={asset.profit >= 0 ? "bg-success hover:bg-success/80" : ""}
                    >
                      {asset.profit >= 0 ? '+' : ''}{asset.profit.toFixed(2)}%
                    </Badge>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
