import { useParams, Link } from "react-router-dom";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ArrowLeft, Wallet, TrendingUp, TrendingDown, PiggyBank, Building2, MoreHorizontal, Eye, Plus, Target, Calendar } from "lucide-react";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, BarChart, Bar, Legend } from "recharts";
import { 
  mockPortfolios, 
  fixedIncomeAssets, 
  variableIncomeAssets, 
  transactions,
  formatCurrency, 
  formatPercentage,
  formatDate,
  type FixedIncomeAsset
} from "@/lib/mock-data";

const portfolioEvolution = [
  { month: 'Jul', value: 150000 },
  { month: 'Ago', value: 158000 },
  { month: 'Set', value: 162000 },
  { month: 'Out', value: 168000 },
  { month: 'Nov', value: 172000 },
  { month: 'Dez', value: 175800 },
];

const portfolioAllocation = [
  { name: 'Renda Fixa', value: 65, color: 'hsl(220, 70%, 50%)' },
  { name: 'Ações', value: 20, color: 'hsl(145, 65%, 42%)' },
  { name: 'FIIs', value: 10, color: 'hsl(38, 92%, 50%)' },
  { name: 'ETFs', value: 5, color: 'hsl(280, 65%, 55%)' },
];

const monthlyProfitability = [
  { month: 'Jul', carteira: 1.2, cdi: 0.9 },
  { month: 'Ago', carteira: 1.5, cdi: 0.95 },
  { month: 'Set', carteira: 0.8, cdi: 0.85 },
  { month: 'Out', carteira: 1.8, cdi: 0.92 },
  { month: 'Nov', carteira: 1.1, cdi: 0.88 },
  { month: 'Dez', carteira: 1.4, cdi: 0.9 },
];

export default function PortfolioDetails() {
  const { id } = useParams();
  const portfolio = mockPortfolios.find(p => p.id === id);

  if (!portfolio) {
    return (
      <div className="flex flex-col items-center justify-center h-[50vh] gap-4">
        <p className="text-muted-foreground">Carteira não encontrada</p>
        <Link to="/carteiras">
          <Button variant="outline">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Voltar para Carteiras
          </Button>
        </Link>
      </div>
    );
  }

  const profit = portfolio.totalValue - portfolio.totalInvested;
  const isProfit = profit >= 0;

  // Mock investments for this portfolio
  const portfolioFixedIncome = fixedIncomeAssets.slice(0, 3);
  const portfolioVariableIncome = variableIncomeAssets.slice(0, 4);
  const portfolioTransactions = transactions.slice(0, 5);

  // Calculate projected values for all fixed income assets in the portfolio
  const calculateTotalProjection = () => {
    let totalProjectedValue = 0;
    let totalProjectedProfit = 0;

    portfolioFixedIncome.forEach(asset => {
      const today = new Date();
      const maturity = new Date(asset.maturityDate);
      const purchase = new Date(asset.purchaseDate);

      const daysRemaining = Math.max(0, Math.ceil((maturity.getTime() - today.getTime()) / (1000 * 60 * 60 * 24)));
      const yearsRemaining = daysRemaining / 365;

      let annualRate = 0;
      const CDI_RATE = 0.1215; // 12.15% CDI
      const IPCA_RATE = 0.045; // 4.5% IPCA

      if (asset.rateType === 'CDI') {
        const cdiPercentage = parseFloat(asset.rate.replace('%', '')) / 100;
        annualRate = CDI_RATE * cdiPercentage;
      } else if (asset.rateType === 'IPCA') {
        const spreadMatch = asset.rate.match(/[\d,]+/g);
        const spread = spreadMatch ? parseFloat(spreadMatch[spreadMatch.length - 1].replace(',', '.')) / 100 : 0.06;
        annualRate = IPCA_RATE + spread;
      } else {
        annualRate = parseFloat(asset.rate.replace('%', '').replace(',', '.')) / 100;
      }

      const projectedValue = asset.currentValue * Math.pow(1 + annualRate, yearsRemaining);
      const projectedProfit = projectedValue - asset.investedValue;

      totalProjectedValue += projectedValue;
      totalProjectedProfit += projectedProfit;
    });

    const totalInvested = portfolioFixedIncome.reduce((acc, asset) => acc + asset.investedValue, 0);
    const totalProjectedProfitPercentage = totalInvested > 0 ? (totalProjectedProfit / totalInvested) * 100 : 0;

    return {
      totalProjectedValue,
      totalProjectedProfit,
      totalProjectedProfitPercentage
    };
  };

  const projection = calculateTotalProjection();

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div className="flex items-center gap-4">
          <Link to="/carteiras">
            <Button variant="ghost" size="icon">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <div className="flex items-center gap-3">
              <span className="text-3xl">{portfolio.bankLogo}</span>
              <div>
                <h1 className="text-2xl font-bold">{portfolio.name}</h1>
                <p className="text-muted-foreground flex items-center gap-2">
                  <Building2 className="h-4 w-4" />
                  {portfolio.bankName}
                </p>
              </div>
            </div>
          </div>
        </div>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          Adicionar Investimento
        </Button>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Patrimônio Total
            </CardTitle>
            <Wallet className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(portfolio.totalValue)}</div>
            <p className="text-xs text-muted-foreground mt-1">
              {portfolio.assetsCount} ativos na carteira
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Total Investido
            </CardTitle>
            <PiggyBank className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(portfolio.totalInvested)}</div>
            <p className="text-xs text-muted-foreground mt-1">
              Valor aportado
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Lucro/Prejuízo
            </CardTitle>
            {isProfit ? (
              <TrendingUp className="h-4 w-4 text-finance-profit" />
            ) : (
              <TrendingDown className="h-4 w-4 text-finance-loss" />
            )}
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${isProfit ? 'text-finance-profit' : 'text-finance-loss'}`}>
              {formatCurrency(profit)}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Resultado líquido
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Rentabilidade
            </CardTitle>
            {isProfit ? (
              <TrendingUp className="h-4 w-4 text-finance-profit" />
            ) : (
              <TrendingDown className="h-4 w-4 text-finance-loss" />
            )}
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${isProfit ? 'text-finance-profit' : 'text-finance-loss'}`}>
              {formatPercentage(portfolio.profitability)}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Desde o início
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="investments" className="space-y-4">
        <TabsList>
          <TabsTrigger value="investments">Investimentos</TabsTrigger>
          <TabsTrigger value="history">Histórico</TabsTrigger>
          <TabsTrigger value="charts">Gráficos</TabsTrigger>
        </TabsList>

        {/* Investments Tab */}
        <TabsContent value="investments" className="space-y-4">

          {/* Fixed Income Projection Summary */}
          {portfolioFixedIncome.length > 0 && (
            <Card className="border-primary/20 bg-gradient-to-r from-primary/5 to-transparent">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-lg flex items-center gap-2">
                  <Target className="h-5 w-5 text-primary" />
                  Projeção Consolidada Renda Fixa
                </CardTitle>
                <Badge variant="outline" className="flex items-center gap-1">
                  <Calendar className="h-3 w-3" />
                  No Vencimento
                </Badge>
              </CardHeader>
              <CardContent>
                <div className="grid gap-4 sm:grid-cols-3">
                  <div>
                    <p className="text-sm text-muted-foreground">Valor Projetado Total</p>
                    <p className="text-2xl font-bold text-primary">
                      {formatCurrency(projection.totalProjectedValue)}
                    </p>
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">Lucro Projetado Total</p>
                    <p className="text-2xl font-bold text-finance-profit">
                      {formatCurrency(projection.totalProjectedProfit)}
                    </p>
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">Rentabilidade Total</p>
                    <p className="text-2xl font-bold text-finance-profit">
                      {formatPercentage(projection.totalProjectedProfitPercentage)}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Fixed Income */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Renda Fixa</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Ativo</TableHead>
                    <TableHead>Tipo</TableHead>
                    <TableHead>Taxa</TableHead>
                    <TableHead className="text-right">Investido</TableHead>
                    <TableHead className="text-right">Atual</TableHead>
                    <TableHead className="text-right">Rent.</TableHead>
                    <TableHead className="w-[50px]"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {portfolioFixedIncome.map((asset) => {
                    const profit = ((asset.currentValue - asset.investedValue) / asset.investedValue) * 100;
                    return (
                      <TableRow key={asset.id}>
                        <TableCell className="font-medium">{asset.name}</TableCell>
                        <TableCell>
                          <Badge variant="outline">{asset.type}</Badge>
                        </TableCell>
                        <TableCell>{asset.rate}</TableCell>
                        <TableCell className="text-right">{formatCurrency(asset.investedValue)}</TableCell>
                        <TableCell className="text-right">{formatCurrency(asset.currentValue)}</TableCell>
                        <TableCell className={`text-right ${profit >= 0 ? 'text-finance-profit' : 'text-finance-loss'}`}>
                          {formatPercentage(profit)}
                        </TableCell>
                        <TableCell>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button variant="ghost" size="icon">
                                <MoreHorizontal className="h-4 w-4" />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end">
                              <Link to={`/investimento/${asset.id}?type=fixed`}>
                                <DropdownMenuItem>
                                  <Eye className="mr-2 h-4 w-4" />
                                  Ver Detalhes
                                </DropdownMenuItem>
                              </Link>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </CardContent>
          </Card>

          {/* Variable Income */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Renda Variável</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Ticker</TableHead>
                    <TableHead>Tipo</TableHead>
                    <TableHead className="text-right">Qtd</TableHead>
                    <TableHead className="text-right">PM</TableHead>
                    <TableHead className="text-right">Cotação</TableHead>
                    <TableHead className="text-right">Rent.</TableHead>
                    <TableHead className="w-[50px]"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {portfolioVariableIncome.map((asset) => {
                    const profit = ((asset.currentPrice - asset.averagePrice) / asset.averagePrice) * 100;
                    return (
                      <TableRow key={asset.id}>
                        <TableCell>
                          <div>
                            <div className="font-medium">{asset.ticker}</div>
                            <div className="text-xs text-muted-foreground">{asset.name}</div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline">{asset.type}</Badge>
                        </TableCell>
                        <TableCell className="text-right">{asset.quantity}</TableCell>
                        <TableCell className="text-right">{formatCurrency(asset.averagePrice)}</TableCell>
                        <TableCell className="text-right">{formatCurrency(asset.currentPrice)}</TableCell>
                        <TableCell className={`text-right ${profit >= 0 ? 'text-finance-profit' : 'text-finance-loss'}`}>
                          {formatPercentage(profit)}
                        </TableCell>
                        <TableCell>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button variant="ghost" size="icon">
                                <MoreHorizontal className="h-4 w-4" />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end">
                              <Link to={`/investimento/${asset.id}?type=variable`}>
                                <DropdownMenuItem>
                                  <Eye className="mr-2 h-4 w-4" />
                                  Ver Detalhes
                                </DropdownMenuItem>
                              </Link>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>

        {/* History Tab */}
        <TabsContent value="history">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Histórico de Transações</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Data</TableHead>
                    <TableHead>Tipo</TableHead>
                    <TableHead>Ativo</TableHead>
                    <TableHead className="text-right">Quantidade</TableHead>
                    <TableHead className="text-right">Preço</TableHead>
                    <TableHead className="text-right">Total</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {portfolioTransactions.map((transaction) => (
                    <TableRow key={transaction.id}>
                      <TableCell>{formatDate(transaction.date)}</TableCell>
                      <TableCell>
                        <Badge 
                          variant={transaction.type === 'Compra' || transaction.type === 'Aporte' ? 'default' : 'destructive'}
                        >
                          {transaction.type}
                        </Badge>
                      </TableCell>
                      <TableCell className="font-medium">{transaction.asset}</TableCell>
                      <TableCell className="text-right">{transaction.quantity || '-'}</TableCell>
                      <TableCell className="text-right">
                        {transaction.price ? formatCurrency(transaction.price) : '-'}
                      </TableCell>
                      <TableCell className="text-right font-medium">
                        {formatCurrency(transaction.total)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Charts Tab */}
        <TabsContent value="charts" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            {/* Evolution Chart */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Evolução Patrimonial</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="h-[300px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={portfolioEvolution}>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                      <XAxis dataKey="month" className="text-xs" />
                      <YAxis 
                        tickFormatter={(value) => `${(value / 1000).toFixed(0)}k`}
                        className="text-xs"
                      />
                      <Tooltip 
                        formatter={(value: number) => formatCurrency(value)}
                        labelFormatter={(label) => `Mês: ${label}`}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="value" 
                        name="Patrimônio"
                        stroke="hsl(var(--primary))" 
                        strokeWidth={2}
                        dot={{ fill: 'hsl(var(--primary))' }}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>

            {/* Allocation Chart */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Alocação por Tipo</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="h-[300px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={portfolioAllocation}
                        cx="50%"
                        cy="50%"
                        innerRadius={60}
                        outerRadius={100}
                        dataKey="value"
                        label={({ name, value }) => `${name}: ${value}%`}
                      >
                        {portfolioAllocation.map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={entry.color} />
                        ))}
                      </Pie>
                      <Tooltip formatter={(value: number) => `${value}%`} />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>

            {/* Profitability Comparison */}
            <Card className="md:col-span-2">
              <CardHeader>
                <CardTitle className="text-lg">Rentabilidade Mensal vs CDI</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="h-[300px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={monthlyProfitability}>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                      <XAxis dataKey="month" className="text-xs" />
                      <YAxis tickFormatter={(value) => `${value}%`} className="text-xs" />
                      <Tooltip formatter={(value: number) => `${value}%`} />
                      <Legend />
                      <Bar dataKey="carteira" name="Carteira" fill="hsl(var(--primary))" radius={[4, 4, 0, 0]} />
                      <Bar dataKey="cdi" name="CDI" fill="hsl(var(--muted-foreground))" radius={[4, 4, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
