import React from "react"
import {
  TrendingUp,
  TrendingDown,
  DollarSign,
  PieChart as PieChartIcon,
  BarChart3,
  LineChart as LineChartIcon,
  Info,
  ArrowUpRight,
  ArrowDownRight
} from "lucide-react"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  CardFooter
} from "@/components/ui/card"
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  ChartConfig,
} from "@/components/ui/chart"
import {
  Carousel,
  CarouselContent,
  CarouselItem,
  CarouselNext,
  CarouselPrevious,
} from "@/components/ui/carousel"
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Label,
  LabelList,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  XAxis,
  YAxis,
  Cell
} from "recharts"

const chartData = [
  { month: "Jan", profit: 186, loss: 80 },
  { month: "Fev", profit: 305, loss: 200 },
  { month: "Mar", profit: 237, loss: 120 },
  { month: "Abr", profit: 73, loss: 190 },
  { month: "Mai", profit: 209, loss: 130 },
  { month: "Jun", profit: 214, loss: 140 },
]

const allocationData = [
  { name: "Ações", value: 45, fill: "var(--color-acoes)" },
  { name: "FIIs", value: 25, fill: "var(--color-fiis)" },
  { name: "Renda Fixa", value: 20, fill: "var(--color-rf)" },
  { name: "Cripto", value: 10, fill: "var(--color-cripto)" },
]

const chartConfig = {
  profit: {
    label: "Lucro",
    color: "hsl(var(--chart-1))",
  },
  loss: {
    label: "Prejuízo",
    color: "hsl(var(--chart-2))",
  },
  acoes: {
    label: "Ações",
    color: "hsl(var(--chart-1))",
  },
  fiis: {
    label: "FIIs",
    color: "hsl(var(--chart-2))",
  },
  rf: {
    label: "Renda Fixa",
    color: "hsl(var(--chart-3))",
  },
  cripto: {
    label: "Cripto",
    color: "hsl(var(--chart-4))",
  },
} satisfies ChartConfig

export default function Examples() {
  return (
    <div className="container mx-auto p-4 md:p-6 space-y-8 animate-in fade-in duration-500">
      <header className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold tracking-tight bg-gradient-to-r from-primary to-primary/60 bg-clip-text text-transparent">
          Showcase de Componentes
        </h1>
        <p className="text-muted-foreground max-w-2xl">
          Uma visão completa de elementos de dashboard, desde cards informativos até gráficos complexos
          e visualizações de dados agrupados.
        </p>
      </header>

      {/* Insight Cards Section */}
      <section className="space-y-4">
        <div className="flex items-center gap-2">
          <Info className="h-5 w-5 text-primary" />
          <h2 className="text-xl font-semibold">Cards de Insight</h2>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <Card className="overflow-hidden border-l-4 border-l-green-500">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">Total Lucro</CardTitle>
              <div className="rounded-full bg-green-500/10 p-2 text-green-500">
                <TrendingUp className="h-4 w-4" />
              </div>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">R$ 12.450,00</div>
              <p className="text-xs text-green-500 flex items-center gap-1 mt-1">
                <ArrowUpRight className="h-3 w-3" />
                +12% em relação ao mês anterior
              </p>
            </CardContent>
          </Card>

          <Card className="overflow-hidden border-l-4 border-l-blue-500">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">Patrimônio Total</CardTitle>
              <div className="rounded-full bg-blue-500/10 p-2 text-blue-500">
                <DollarSign className="h-4 w-4" />
              </div>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">R$ 158.320,00</div>
              <p className="text-xs text-muted-foreground mt-1">
                Atualizado há 5 minutos
              </p>
            </CardContent>
          </Card>

          <Card className="overflow-hidden border-l-4 border-l-amber-500">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">Rendimentos Mensais</CardTitle>
              <div className="rounded-full bg-amber-500/10 p-2 text-amber-500">
                <TrendingUp className="h-4 w-4" />
              </div>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">R$ 1.240,50</div>
              <p className="text-xs text-amber-600 flex items-center gap-1 mt-1">
                Meta mensal: 85% atingida
              </p>
            </CardContent>
          </Card>

          <Card className="overflow-hidden border-l-4 border-l-red-500">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">Despesas & Taxas</CardTitle>
              <div className="rounded-full bg-red-500/10 p-2 text-red-500">
                <TrendingDown className="h-4 w-4" />
              </div>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">R$ 450,20</div>
              <p className="text-xs text-red-500 flex items-center gap-1 mt-1">
                <ArrowDownRight className="h-3 w-3" />
                -5% vs. período anterior
              </p>
            </CardContent>
          </Card>
        </div>
      </section>

      {/* Charts Section */}
      <section className="space-y-4">
        <div className="flex items-center gap-2">
          <BarChart3 className="h-5 w-5 text-primary" />
          <h2 className="text-xl font-semibold">Gráficos de Performance</h2>
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Profit vs Loss Bar Chart */}
          <Card className="flex flex-col">
            <CardHeader>
              <CardTitle>Lucro vs Prejuízo Mensal</CardTitle>
              <CardDescription>Comparativo dos últimos 6 meses</CardDescription>
            </CardHeader>
            <CardContent className="flex-1 pb-0">
              <ChartContainer config={chartConfig} className="h-[300px] w-full">
                <BarChart data={chartData}>
                  <XAxis
                    dataKey="month"
                    tickLine={false}
                    tickMargin={10}
                    axisLine={false}
                    tickFormatter={(value) => value.slice(0, 3)}
                  />
                  <ChartTooltip content={<ChartTooltipContent />} />
                  <Bar dataKey="profit" fill="var(--color-profit)" radius={4} />
                  <Bar dataKey="loss" fill="var(--color-loss)" radius={4} />
                </BarChart>
              </ChartContainer>
            </CardContent>
          </Card>

          {/* Allocation Pie Chart */}
          <Card className="flex flex-col">
            <CardHeader className="items-center pb-0">
              <CardTitle>Alocação de Ativos</CardTitle>
              <CardDescription>Distribuição atual da carteira</CardDescription>
            </CardHeader>
            <CardContent className="flex-1 pb-0">
              <ChartContainer
                config={chartConfig}
                className="mx-auto aspect-square max-h-[300px]"
              >
                <PieChart>
                  <ChartTooltip
                    cursor={false}
                    content={<ChartTooltipContent hideLabel />}
                  />
                  <Pie
                    data={allocationData}
                    dataKey="value"
                    nameKey="name"
                    innerRadius={60}
                    strokeWidth={5}
                  >
                    <Label
                      content={({ viewBox }) => {
                        if (viewBox && "cx" in viewBox && "cy" in viewBox) {
                          return (
                            <text
                              x={viewBox.cx}
                              y={viewBox.cy}
                              textAnchor="middle"
                              dominantBaseline="middle"
                            >
                              <tspan
                                x={viewBox.cx}
                                y={viewBox.cy}
                                className="fill-foreground text-3xl font-bold"
                              >
                                100%
                              </tspan>
                              <tspan
                                x={viewBox.cx}
                                y={(viewBox.cy || 0) + 24}
                                className="fill-muted-foreground"
                              >
                                Ativos
                              </tspan>
                            </text>
                          )
                        }
                      }}
                    />
                  </Pie>
                </PieChart>
              </ChartContainer>
            </CardContent>
            <CardFooter className="flex-col gap-2 text-sm">
              <div className="flex items-center gap-2 font-medium leading-none">
                Diversificação alta <TrendingUp className="h-4 w-4" />
              </div>
              <div className="leading-none text-muted-foreground text-center">
                4 classes de ativos principais monitoradas
              </div>
            </CardFooter>
          </Card>
        </div>

        {/* Growth Area Chart */}
        <div className="grid grid-cols-1 gap-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <LineChartIcon className="h-5 w-5" />
                Evolução Patrimonial
              </CardTitle>
              <CardDescription>Crescimento acumulado ao longo do ano</CardDescription>
            </CardHeader>
            <CardContent className="pt-4">
              <ChartContainer config={chartConfig} className="h-[300px] w-full">
                <AreaChart data={chartData} margin={{ left: 12, right: 12 }}>
                  <CartesianGrid vertical={false} strokeDasharray="3 3" opacity={0.3} />
                  <XAxis
                    dataKey="month"
                    tickLine={false}
                    axisLine={false}
                    tickMargin={8}
                    tickFormatter={(value) => value.slice(0, 3)}
                  />
                  <ChartTooltip cursor={false} content={<ChartTooltipContent />} />
                  <defs>
                    <linearGradient id="fillProfit" x1="0" y1="0" x2="0" y2="1">
                      <stop
                        offset="5%"
                        stopColor="var(--color-profit)"
                        stopOpacity={0.8}
                      />
                      <stop
                        offset="95%"
                        stopColor="var(--color-profit)"
                        stopOpacity={0.1}
                      />
                    </linearGradient>
                  </defs>
                  <Area
                    dataKey="profit"
                    type="natural"
                    fill="url(#fillProfit)"
                    fillOpacity={0.4}
                    stroke="var(--color-profit)"
                    stackId="a"
                  />
                </AreaChart>
              </ChartContainer>
            </CardContent>
          </Card>
        </div>
      </section>

      {/* Carousel Section */}
      <section className="space-y-4">
        <div className="flex items-center gap-2">
          <LineChartIcon className="h-5 w-5 text-primary" />
          <h2 className="text-xl font-semibold">Insights & Notícias (Carousel)</h2>
        </div>
        <Carousel className="w-full max-w-5xl mx-auto">
          <CarouselContent className="-ml-1">
            {[1, 2, 3, 4, 5].map((_, index) => (
              <CarouselItem key={index} className="pl-1 md:basis-1/2 lg:basis-1/3">
                <div className="p-1">
                  <Card className="bg-gradient-to-br from-card to-muted/30 border-primary/20">
                    <CardContent className="flex flex-col gap-3 aspect-video items-center justify-center p-6 text-center">
                      <div className="rounded-full bg-primary/10 p-3">
                        <LineChartIcon className="h-6 w-6 text-primary" />
                      </div>
                      <h3 className="font-semibold">Insight #{index + 1}</h3>
                      <p className="text-sm text-muted-foreground italic">
                        "O mercado de tecnologia apresenta forte tendência de alta para o próximo trimestre..."
                      </p>
                    </CardContent>
                  </Card>
                </div>
              </CarouselItem>
            ))}
          </CarouselContent>
          <div className="hidden md:block">
            <CarouselPrevious />
            <CarouselNext />
          </div>
        </Carousel>
      </section>

      {/* Grouped View Section */}
      <section className="space-y-4">
        <div className="flex items-center gap-2">
          <PieChartIcon className="h-5 w-5 text-primary" />
          <h2 className="text-xl font-semibold">Visão Agrupada (Total Lucro)</h2>
        </div>
        <Card className="bg-muted/50 border-none shadow-inner">
          <CardContent className="p-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 items-center">
              <div>
                <h3 className="text-2xl font-bold mb-2">Composição do Lucro</h3>
                <p className="text-muted-foreground mb-6">
                  Detalhamento de onde seus rendimentos estão vindo. A maior parte provém de ganhos de capital em ações.
                </p>
                <div className="space-y-3">
                  {[
                    { label: "Dividendos FIIs", value: "R$ 4.500", color: "bg-blue-500", percent: 36 },
                    { label: "Ganhos Ações", value: "R$ 6.200", color: "bg-green-500", percent: 50 },
                    { label: "Juros RF", value: "R$ 1.750", color: "bg-amber-500", percent: 14 },
                  ].map((item, i) => (
                    <div key={i} className="space-y-1">
                      <div className="flex justify-between text-sm">
                        <span>{item.label}</span>
                        <span className="font-semibold">{item.value}</span>
                      </div>
                      <div className="h-2 w-full bg-background rounded-full overflow-hidden">
                        <div
                          className={`h-full ${item.color} rounded-full`}
                          style={{ width: `${item.percent}%` }}
                        />
                      </div>
                    </div>
                  ))}
                </div>
              </div>
              <div className="flex justify-center border-l border-border md:pl-8">
                <div className="text-center">
                  <div className="text-sm text-muted-foreground uppercase tracking-wider font-semibold mb-1">Total Consolidado</div>
                  <div className="text-5xl font-extrabold text-primary">R$ 12.450</div>
                  <div className="flex items-center justify-center gap-2 mt-2 text-green-500 font-medium">
                    <TrendingUp className="h-5 w-5" />
                    +15.4% este ano
                  </div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </section>
    </div>
  )
}
