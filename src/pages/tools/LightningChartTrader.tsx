import React, { useEffect, useRef } from 'react'
import { createChart, ColorType, CrosshairMode, CandlestickSeries } from 'lightweight-charts'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"

// Mock data generator for Lightweight Charts
const generateData = () => {
  const data: any[] = []
  let date = new Date('2024-01-01')
  let open = 100
  for (let i = 0; i < 100; i++) {
    const close = open + (Math.random() - 0.5) * 5
    const high = Math.max(open, close) + Math.random() * 2
    const low = Math.min(open, close) - Math.random() * 2

    data.push({
      time: date.toISOString().split('T')[0], // Lightweight charts uses 'time'
      open,
      high,
      low,
      close,
    })

    open = close
    date.setDate(date.getDate() + 1)
  }
  return data
}

export default function LightningChartTrader() {
  const chartContainerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!chartContainerRef.current) return

    const chart = createChart(chartContainerRef.current, {
      layout: {
        background: { type: ColorType.Solid, color: 'transparent' },
        textColor: '#D9D9D9',
      },
      grid: {
        vertLines: { color: 'rgba(255, 255, 255, 0.1)' },
        horzLines: { color: 'rgba(255, 255, 255, 0.1)' },
      },
      width: chartContainerRef.current.clientWidth,
      height: 600,
      crosshair: {
        mode: CrosshairMode.Normal,
      },
    })

    const candlestickSeries = chart.addSeries(CandlestickSeries, {
      upColor: '#26a69a',
      downColor: '#ef5350',
      borderVisible: false,
      wickUpColor: '#26a69a',
      wickDownColor: '#ef5350',
    })

    const data = generateData()
    candlestickSeries.setData(data)

    // Responsive resize
    const handleResize = () => {
      if (chartContainerRef.current) {
        chart.applyOptions({ width: chartContainerRef.current.clientWidth })
      }
    }

    window.addEventListener('resize', handleResize)

    return () => {
      window.removeEventListener('resize', handleResize)
      chart.remove()
    }
  }, [])

  return (
    <div className="container mx-auto p-4 md:p-6 space-y-6">
      <header className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold tracking-tight">Lightning Trader</h1>
        <p className="text-muted-foreground">
          Gráficos interativos e de alta performance powered by Lightweight Charts.
        </p>
      </header>

      <div className="grid grid-cols-1 gap-6">
        <Card className="overflow-hidden">
          <CardHeader>
            <CardTitle>Candlestick Analysis</CardTitle>
            <CardDescription>
              Visualização de preços OHLC com alta precisão e performance.
            </CardDescription>
          </CardHeader>
          <CardContent className="p-0">
            <div
              ref={chartContainerRef}
              className="w-full h-[600px] bg-background"
            />
          </CardContent>
        </Card>
      </div>

      <section className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Performance</CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Renderização ultra-rápida utilizando Canvas HTML5.
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Interatividade</CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Zoom, pan e crosshair precisos.
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Lightweight</CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Biblioteca leve e eficiente para visualização de dados financeiros.
          </CardContent>
        </Card>
      </section>
    </div>
  )
}
