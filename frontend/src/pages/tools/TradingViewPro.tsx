import React from 'react'
import TradingViewAdvanced from '@/components/charts/TradingViewAdvanced'
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"

export default function TradingViewPro() {
  return (
    <div className="container mx-auto p-4 md:p-6 space-y-6 h-[calc(100vh-100px)]">
      <header className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold tracking-tight">TradingView Pro</h1>
        <p className="text-muted-foreground">
          Gráfico interativo avançado do TradingView.
        </p>
      </header>

      <div className="grid grid-cols-1 gap-6 h-full pb-6">
        <Card className="overflow-hidden h-full flex flex-col">
          <CardHeader className="py-4">
            <CardTitle>Análise Técnica Avançada</CardTitle>
          </CardHeader>
          <CardContent className="p-0 flex-1 relative min-h-[500px]">
            <TradingViewAdvanced />
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
