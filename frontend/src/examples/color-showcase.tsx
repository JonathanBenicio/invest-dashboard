import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckCircle2, AlertTriangle, Info, AlertCircle } from "lucide-react"

/**
 * Exemplo de uso das novas cores do design system:
 * - success (verde): para estados positivos, confirmações, ganhos
 * - warning (amarelo): para avisos, atenção
 * - info (azul): para informações neutras
 * - destructive (vermelho): já existia, para erros e perdas
 */
export default function ColorShowcase() {
  return (
    <div className="p-8 space-y-6">
      <div>
        <h1 className="text-2xl font-bold mb-2">Design System - Cores Semânticas</h1>
        <p className="text-muted-foreground">
          Exemplos de uso das cores success, warning, info e destructive
        </p>
      </div>

      {/* Badges */}
      <Card>
        <CardHeader>
          <CardTitle>Badges</CardTitle>
          <CardDescription>Diferentes estados visuais</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-2">
          <Badge className="bg-success text-success-foreground">
            <CheckCircle2 className="h-3 w-3 mr-1" />
            Success
          </Badge>
          <Badge className="bg-warning text-warning-foreground">
            <AlertTriangle className="h-3 w-3 mr-1" />
            Warning
          </Badge>
          <Badge className="bg-info text-info-foreground">
            <Info className="h-3 w-3 mr-1" />
            Info
          </Badge>
          <Badge className="bg-destructive text-destructive-foreground">
            <AlertCircle className="h-3 w-3 mr-1" />
            Destructive
          </Badge>
        </CardContent>
      </Card>

      {/* Buttons */}
      <Card>
        <CardHeader>
          <CardTitle>Buttons</CardTitle>
          <CardDescription>Botões com cores semânticas</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-2">
          <Button className="bg-success hover:bg-success/90 text-success-foreground">
            Confirmar
          </Button>
          <Button className="bg-warning hover:bg-warning/90 text-warning-foreground">
            Atenção
          </Button>
          <Button className="bg-info hover:bg-info/90 text-info-foreground">
            Informação
          </Button>
          <Button variant="destructive">
            Excluir
          </Button>
        </CardContent>
      </Card>

      {/* Text Colors */}
      <Card>
        <CardHeader>
          <CardTitle>Text Colors</CardTitle>
          <CardDescription>Cores para textos e ícones</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex items-center gap-2">
            <CheckCircle2 className="h-5 w-5 text-success" />
            <span className="text-success font-medium">Lucro de +12.5%</span>
          </div>
          <div className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-warning" />
            <span className="text-warning font-medium">Atenção: Vencimento próximo</span>
          </div>
          <div className="flex items-center gap-2">
            <Info className="h-5 w-5 text-info" />
            <span className="text-info font-medium">Informação importante</span>
          </div>
          <div className="flex items-center gap-2">
            <AlertCircle className="h-5 w-5 text-destructive" />
            <span className="text-destructive font-medium">Prejuízo de -8.3%</span>
          </div>
        </CardContent>
      </Card>

      {/* Backgrounds */}
      <Card>
        <CardHeader>
          <CardTitle>Background Colors</CardTitle>
          <CardDescription>Diferentes fundos com contraste adequado</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="p-4 rounded-lg bg-success text-success-foreground">
            <p className="font-medium">✓ Operação realizada com sucesso</p>
          </div>
          <div className="p-4 rounded-lg bg-warning text-warning-foreground">
            <p className="font-medium">⚠ Revise as informações antes de continuar</p>
          </div>
          <div className="p-4 rounded-lg bg-info text-info-foreground">
            <p className="font-medium">ℹ Nova funcionalidade disponível</p>
          </div>
          <div className="p-4 rounded-lg bg-destructive text-destructive-foreground">
            <p className="font-medium">✗ Erro ao processar solicitação</p>
          </div>
        </CardContent>
      </Card>

      {/* Border Colors */}
      <Card>
        <CardHeader>
          <CardTitle>Border Colors</CardTitle>
          <CardDescription>Bordas com cores semânticas</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="p-4 rounded-lg border-2 border-success bg-success/10">
            <p className="text-sm">Border success</p>
          </div>
          <div className="p-4 rounded-lg border-2 border-warning bg-warning/10">
            <p className="text-sm">Border warning</p>
          </div>
          <div className="p-4 rounded-lg border-2 border-info bg-info/10">
            <p className="text-sm">Border info</p>
          </div>
          <div className="p-4 rounded-lg border-2 border-destructive bg-destructive/10">
            <p className="text-sm">Border destructive</p>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
