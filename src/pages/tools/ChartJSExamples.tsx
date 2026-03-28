import React, { useMemo } from 'react'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  TimeScale,
  Title,
  Tooltip,
  Legend,
  BarElement,
  PointElement,
  LineElement,
} from 'chart.js'
import { Chart } from 'react-chartjs-2'
import { CandlestickController, CandlestickElement, OhlcController, OhlcElement } from 'chartjs-chart-financial'
import 'chartjs-adapter-date-fns'
import { enUS } from 'date-fns/locale'

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  TimeScale,
  BarElement,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  CandlestickController,
  CandlestickElement,
  OhlcController,
  OhlcElement
)

const generateData = () => {
  const data: any[] = []
  let date = new Date('2024-12-01T00:00:00')
  let open = 150

  for (let i = 0; i < 30; i++) {
    const close = open + (Math.random() - 0.5) * 10
    const high = Math.max(open, close) + Math.random() * 5
    const low = Math.min(open, close) - Math.random() * 5

    data.push({
      x: date.getTime(),
      o: open,
      h: high,
      l: low,
      c: close,
    })

    open = close
    date = new Date(date.getTime() + 24 * 60 * 60 * 1000)
  }
  return data
}

export default function ChartJSExamples() {
  const chartData = useMemo(() => generateData(), [])

  const candlestickData = {
    datasets: [
      {
        label: 'AAPL Candlestick',
        data: chartData,
        backgroundColor: '#26a69a',
        borderColor: '#26a69a',
        color: {
          up: '#26a69a',
          down: '#ef5350',
          unchanged: '#999',
        },
      },
    ],
  }

  const ohlcData = {
    datasets: [
      {
        label: 'AAPL OHLC',
        data: chartData,
        borderColor: '#2196f3',
        color: {
          up: '#26a69a',
          down: '#ef5350',
          unchanged: '#999',
        },
      },
    ],
  }

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: true,
        position: 'top' as const,
      },
    },
    scales: {
      x: {
        type: 'time' as const,
        time: {
          unit: 'day' as const,
        },
        adapters: {
          date: {
            locale: enUS,
          },
        },
        grid: {
          display: false,
        },
      },
      y: {
        beginAtZero: false,
        grid: {
          color: 'rgba(255, 255, 255, 0.1)',
        },
      },
    },
  }

  return (
    <div className="container mx-auto p-4 md:p-6 space-y-6">
      <header className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold tracking-tight">Chart.js Financeiro</h1>
        <p className="text-muted-foreground">
          Exemplos de gráficos financeiros especializados usando Chart.js e react-chartjs-2.
        </p>
      </header>

      <Tabs defaultValue="candlestick" className="space-y-6">
        <TabsList className="grid w-full max-w-md grid-cols-2">
          <TabsTrigger value="candlestick">Candlestick</TabsTrigger>
          <TabsTrigger value="ohlc">OHLC</TabsTrigger>
        </TabsList>

        <TabsContent value="candlestick" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Gráfico de Candlestick</CardTitle>
              <CardDescription>Visualização clássica de preços com corpo e sombras.</CardDescription>
            </CardHeader>
            <CardContent className="h-[500px]">
              <Chart type="candlestick" data={candlestickData} options={options} />
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="ohlc" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Gráfico OHLC</CardTitle>
              <CardDescription>Representação Open-High-Low-Close simplificada.</CardDescription>
            </CardHeader>
            <CardContent className="h-[500px]">
              <Chart type="ohlc" data={ohlcData} options={options} />
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      <section className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Por que usar Chart.js Financial?</CardTitle>
          </CardHeader>
          <CardContent className="text-sm space-y-2 text-muted-foreground">
            <p>• Integração nativa com o ecossistema robusto do Chart.js.</p>
            <p>• Altamente customizável através de plugins e escalas de tempo.</p>
            <p>• Excelente performance para conjuntos de dados médios.</p>
            <p>• Suporte para múltiplos tipos de séries no mesmo canvas.</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Dica Prática</CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground italic">
            "Para trading em tempo real com milhares de pontos, Lightweight Charts costuma ser mais performático,
            mas para dashboards informativos e relatórios, o Chart.js oferece uma flexibilidade visual imbatível."
          </CardContent>
        </Card>
      </section>
    </div>
  )
}
