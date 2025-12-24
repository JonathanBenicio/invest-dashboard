import React, { useEffect, useRef } from "react"
import { createChart, ColorType, IChartApi, CandlestickSeries } from "lightweight-charts"
import { TechnicalAnalysis, SymbolInfo, Timeline } from "react-ts-tradingview-widgets"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

// Mock data for the custom chart
const initialData = [
  { time: "2024-12-01", open: 140.5, high: 145.2, low: 139.8, close: 144.1 },
  { time: "2024-12-02", open: 144.1, high: 146.5, low: 143.2, close: 145.8 },
  { time: "2024-12-03", open: 145.8, high: 147.1, low: 144.5, close: 146.2 },
  { time: "2024-12-04", open: 146.2, high: 148.5, low: 145.1, close: 147.9 },
  { time: "2024-12-05", open: 147.9, high: 149.2, low: 146.8, close: 148.5 },
  { time: "2024-12-06", open: 148.5, high: 150.1, low: 147.2, close: 149.3 },
  { time: "2024-12-07", open: 149.3, high: 151.5, low: 148.9, close: 150.8 },
  { time: "2024-12-08", open: 150.8, high: 152.2, low: 149.5, close: 151.4 },
  { time: "2024-12-09", open: 151.4, high: 153.5, low: 150.8, close: 152.9 },
  { time: "2024-12-10", open: 152.9, high: 155.1, low: 152.1, close: 154.6 },
  { time: "2024-12-11", open: 154.6, high: 156.4, low: 153.8, close: 155.7 },
  { time: "2024-12-12", open: 155.7, high: 157.2, low: 154.5, close: 156.8 },
  { time: "2024-12-13", open: 156.8, high: 158.5, low: 155.9, close: 157.4 },
  { time: "2024-12-14", open: 157.4, high: 159.1, low: 156.5, close: 158.2 },
  { time: "2024-12-15", open: 158.2, high: 160.5, low: 157.8, close: 159.9 },
  { time: "2024-12-16", open: 159.9, high: 161.2, low: 158.5, close: 160.4 },
  { time: "2024-12-17", open: 160.4, high: 162.5, low: 159.8, close: 161.7 },
  { time: "2024-12-18", open: 161.7, high: 163.4, low: 160.5, close: 162.8 },
  { time: "2024-12-19", open: 162.8, high: 164.5, low: 161.9, close: 163.6 },
  { time: "2024-12-20", open: 163.6, high: 165.2, low: 162.5, close: 164.8 },
]

export default function TraderChart() {
  const chartContainerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<IChartApi | null>(null)
  const candlestickSeriesRef = useRef<any>(null)

  useEffect(() => {
    if (!chartContainerRef.current) return

    const handleResize = () => {
      chartRef.current?.applyOptions({ width: chartContainerRef.current?.clientWidth })
    }

    const chart = createChart(chartContainerRef.current, {
      layout: {
        background: { type: ColorType.Solid, color: "transparent" },
        textColor: "#a1a1aa", // zinc-400 fallback for hsl(var(--foreground))
      },
      grid: {
        vertLines: { color: "#27272a", style: 1 }, // zinc-800 fallback for hsl(var(--border))
        horzLines: { color: "#27272a", style: 1 },
      },
      width: chartContainerRef.current.clientWidth,
      height: 400,
    })

    const candlestickSeries = chart.addSeries(CandlestickSeries, {
      upColor: "#26a69a",
      downColor: "#ef5350",
      borderVisible: false,
      wickUpColor: "#26a69a",
      wickDownColor: "#ef5350",
    })

    candlestickSeries.setData(initialData as any)

    chartRef.current = chart
    candlestickSeriesRef.current = candlestickSeries

    window.addEventListener("resize", handleResize)

    return () => {
      window.removeEventListener("resize", handleResize)
      chart.remove()
    }
  }, [])

  return (
    <div className="container mx-auto p-4 md:p-6 space-y-6">
      <header className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold tracking-tight">Gráfico Trader</h1>
        <p className="text-muted-foreground">
          Análise técnica avançada usando TradingView Lightweight Charts e Widgets.
        </p>
      </header>

      <Tabs defaultValue="custom" className="space-y-6">
        <TabsList className="grid w-full max-w-md grid-cols-2">
          <TabsTrigger value="custom">Gráfico Customizado</TabsTrigger>
          <TabsTrigger value="widgets">Widgets TradingView</TabsTrigger>
        </TabsList>

        <TabsContent value="custom" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Candlestick Progressivo (Mock)</CardTitle>
            </CardHeader>
            <CardContent>
              <div
                ref={chartContainerRef}
                className="w-full relative rounded-md overflow-hidden bg-background border border-border"
              />
            </CardContent>
          </Card>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle>Análise Técnica</CardTitle>
              </CardHeader>
              <CardContent className="h-[400px]">
                <TechnicalAnalysis symbol="NASDAQ:AAPL" colorTheme="dark" width="100%" height="100%" />
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Informações do Ativo</CardTitle>
              </CardHeader>
              <CardContent className="h-[400px]">
                <SymbolInfo symbol="NASDAQ:AAPL" colorTheme="dark" width="100%" locale="br" />
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="widgets" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Timeline do Mercado</CardTitle>
            </CardHeader>
            <CardContent className="h-[600px]">
              <Timeline colorTheme="dark" width="100%" height="100%" />
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}
