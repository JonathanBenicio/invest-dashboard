import { useState } from "react";
import { User, Bell, Target, Download, Palette, Shield } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Progress } from "@/components/ui/progress";
import { currentUser, formatCurrency } from "@/lib/mock-data";
import { useToast } from "@/hooks/use-toast";

export default function Settings() {
  const [name, setName] = useState(currentUser.name);
  const [email, setEmail] = useState(currentUser.email);
  const { toast } = useToast();

  const allocationGoals = [
    { name: 'Renda Fixa', current: 55, target: 50 },
    { name: 'Ações', value: 25, current: 28, target: 30 },
    { name: 'FIIs', current: 12, target: 15 },
    { name: 'ETFs', current: 3, target: 3 },
    { name: 'BDRs', current: 2, target: 2 },
  ];

  const handleSaveProfile = () => {
    toast({
      title: "Perfil atualizado",
      description: "Suas informações foram salvas com sucesso.",
    });
  };

  const handleExport = (format: string) => {
    toast({
      title: "Exportação iniciada",
      description: `Seus dados estão sendo exportados em formato ${format}.`,
    });
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Configurações</h1>
        <p className="text-muted-foreground">Gerencie seu perfil e preferências</p>
      </div>

      <Tabs defaultValue="profile" className="space-y-6">
        <TabsList>
          <TabsTrigger value="profile">
            <User className="h-4 w-4 mr-2" />
            Perfil
          </TabsTrigger>
          <TabsTrigger value="notifications">
            <Bell className="h-4 w-4 mr-2" />
            Notificações
          </TabsTrigger>
          <TabsTrigger value="goals">
            <Target className="h-4 w-4 mr-2" />
            Metas
          </TabsTrigger>
          <TabsTrigger value="export">
            <Download className="h-4 w-4 mr-2" />
            Exportar
          </TabsTrigger>
        </TabsList>

        <TabsContent value="profile">
          <div className="grid gap-6 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Informações Pessoais</CardTitle>
                <CardDescription>Atualize seus dados de perfil</CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="flex items-center gap-4">
                  <Avatar className="h-20 w-20">
                    <AvatarFallback className="text-xl bg-primary/10 text-primary">
                      {name.split(' ').map(n => n[0]).join('')}
                    </AvatarFallback>
                  </Avatar>
                  <div>
                    <Button variant="outline" size="sm">Alterar foto</Button>
                    <p className="text-xs text-muted-foreground mt-1">JPG, PNG. Máx 2MB.</p>
                  </div>
                </div>

                <div className="space-y-4">
                  <div className="grid gap-2">
                    <Label htmlFor="name">Nome completo</Label>
                    <Input 
                      id="name" 
                      value={name} 
                      onChange={(e) => setName(e.target.value)}
                    />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="email">E-mail</Label>
                    <Input 
                      id="email" 
                      type="email" 
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                    />
                  </div>
                </div>

                <Button onClick={handleSaveProfile}>Salvar alterações</Button>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Preferências</CardTitle>
                <CardDescription>Personalize sua experiência</CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Tema</Label>
                    <p className="text-xs text-muted-foreground">Escolha o tema da interface</p>
                  </div>
                  <Select defaultValue="light">
                    <SelectTrigger className="w-[130px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="light">Claro</SelectItem>
                      <SelectItem value="dark">Escuro</SelectItem>
                      <SelectItem value="system">Sistema</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <Separator />

                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Moeda padrão</Label>
                    <p className="text-xs text-muted-foreground">Moeda para exibição de valores</p>
                  </div>
                  <Select defaultValue="brl">
                    <SelectTrigger className="w-[130px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="brl">R$ (BRL)</SelectItem>
                      <SelectItem value="usd">$ (USD)</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <Separator />

                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Formato de data</Label>
                    <p className="text-xs text-muted-foreground">Como exibir as datas</p>
                  </div>
                  <Select defaultValue="ddmmyyyy">
                    <SelectTrigger className="w-[130px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="ddmmyyyy">DD/MM/AAAA</SelectItem>
                      <SelectItem value="mmddyyyy">MM/DD/AAAA</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="notifications">
          <Card>
            <CardHeader>
              <CardTitle>Notificações</CardTitle>
              <CardDescription>Configure como deseja ser notificado</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Resumo diário</Label>
                  <p className="text-xs text-muted-foreground">Receba um resumo da carteira por e-mail</p>
                </div>
                <Switch defaultChecked />
              </div>

              <Separator />

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Alertas de proventos</Label>
                  <p className="text-xs text-muted-foreground">Notificar sobre pagamentos de dividendos</p>
                </div>
                <Switch defaultChecked />
              </div>

              <Separator />

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Alertas de vencimento</Label>
                  <p className="text-xs text-muted-foreground">Notificar sobre vencimentos de renda fixa</p>
                </div>
                <Switch defaultChecked />
              </div>

              <Separator />

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Variações significativas</Label>
                  <p className="text-xs text-muted-foreground">Alertar sobre variações acima de 5%</p>
                </div>
                <Switch />
              </div>

              <Separator />

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Novidades do sistema</Label>
                  <p className="text-xs text-muted-foreground">Receber atualizações sobre novas funcionalidades</p>
                </div>
                <Switch defaultChecked />
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="goals">
          <Card>
            <CardHeader>
              <CardTitle>Metas de Alocação</CardTitle>
              <CardDescription>Defina e acompanhe suas metas de alocação</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              {allocationGoals.map((goal) => (
                <div key={goal.name} className="space-y-2">
                  <div className="flex justify-between items-center">
                    <Label>{goal.name}</Label>
                    <div className="flex items-center gap-2">
                      <span className="text-sm text-muted-foreground">
                        Atual: {goal.current}%
                      </span>
                      <span className="text-sm font-medium">
                        Meta: {goal.target}%
                      </span>
                    </div>
                  </div>
                  <div className="flex gap-2 items-center">
                    <Progress 
                      value={(goal.current / goal.target) * 100} 
                      className="flex-1"
                    />
                    <Input 
                      type="number" 
                      defaultValue={goal.target}
                      className="w-20 text-center"
                      min={0}
                      max={100}
                    />
                    <span className="text-sm text-muted-foreground">%</span>
                  </div>
                </div>
              ))}

              <Separator />

              <Button>Salvar metas</Button>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="export">
          <Card>
            <CardHeader>
              <CardTitle>Exportar Dados</CardTitle>
              <CardDescription>Baixe seus dados em diferentes formatos</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="grid gap-4 md:grid-cols-2">
                <Card className="border-dashed">
                  <CardContent className="pt-6">
                    <div className="flex items-center gap-4">
                      <div className="h-12 w-12 rounded-lg bg-success/10 flex items-center justify-center">
                        <Download className="h-6 w-6 text-success" />
                      </div>
                      <div className="flex-1">
                        <p className="font-medium">Planilha Excel</p>
                        <p className="text-xs text-muted-foreground">Todos os ativos e transações</p>
                      </div>
                      <Button variant="outline" onClick={() => handleExport('Excel')}>
                        Exportar
                      </Button>
                    </div>
                  </CardContent>
                </Card>

                <Card className="border-dashed">
                  <CardContent className="pt-6">
                    <div className="flex items-center gap-4">
                      <div className="h-12 w-12 rounded-lg bg-primary/10 flex items-center justify-center">
                        <Download className="h-6 w-6 text-primary" />
                      </div>
                      <div className="flex-1">
                        <p className="font-medium">Arquivo CSV</p>
                        <p className="text-xs text-muted-foreground">Formato universal de dados</p>
                      </div>
                      <Button variant="outline" onClick={() => handleExport('CSV')}>
                        Exportar
                      </Button>
                    </div>
                  </CardContent>
                </Card>

                <Card className="border-dashed">
                  <CardContent className="pt-6">
                    <div className="flex items-center gap-4">
                      <div className="h-12 w-12 rounded-lg bg-destructive/10 flex items-center justify-center">
                        <Download className="h-6 w-6 text-destructive" />
                      </div>
                      <div className="flex-1">
                        <p className="font-medium">Relatório PDF</p>
                        <p className="text-xs text-muted-foreground">Resumo formatado da carteira</p>
                      </div>
                      <Button variant="outline" onClick={() => handleExport('PDF')}>
                        Exportar
                      </Button>
                    </div>
                  </CardContent>
                </Card>

                <Card className="border-dashed">
                  <CardContent className="pt-6">
                    <div className="flex items-center gap-4">
                      <div className="h-12 w-12 rounded-lg bg-warning/10 flex items-center justify-center">
                        <Download className="h-6 w-6 text-warning" />
                      </div>
                      <div className="flex-1">
                        <p className="font-medium">IRPF</p>
                        <p className="text-xs text-muted-foreground">Dados para declaração</p>
                      </div>
                      <Button variant="outline" onClick={() => handleExport('IRPF')}>
                        Exportar
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
