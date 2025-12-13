import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Plus, Filter, Search, MoreHorizontal, Pencil, Trash2, ArrowUpRight, ArrowDownRight, Eye } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { variableIncomeAssets, dividends, formatCurrency, formatDate, type VariableIncomeAsset } from "@/lib/mock-data";
import { useToast } from "@/hooks/use-toast";

export default function VariableIncome() {
  const navigate = useNavigate();
  const [assets, setAssets] = useState(variableIncomeAssets);
  const [searchTerm, setSearchTerm] = useState("");
  const [typeFilter, setTypeFilter] = useState<string>("all");
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const { toast } = useToast();

  const filteredAssets = assets.filter(asset => {
    const matchesSearch = asset.ticker.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         asset.name.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesType = typeFilter === "all" || asset.type === typeFilter;
    return matchesSearch && matchesType;
  });

  const totalInvested = assets.reduce((acc, asset) => acc + (asset.averagePrice * asset.quantity), 0);
  const totalCurrent = assets.reduce((acc, asset) => acc + (asset.currentPrice * asset.quantity), 0);
  const totalProfit = totalCurrent - totalInvested;

  const assetTypes = ['Ação', 'FII', 'ETF', 'BDR'];

  const handleAddAsset = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    
    const newAsset: VariableIncomeAsset = {
      id: Date.now().toString(),
      ticker: (formData.get('ticker') as string).toUpperCase(),
      name: formData.get('name') as string,
      type: formData.get('type') as VariableIncomeAsset['type'],
      sector: formData.get('sector') as string,
      quantity: parseInt(formData.get('quantity') as string),
      averagePrice: parseFloat(formData.get('averagePrice') as string),
      currentPrice: parseFloat(formData.get('averagePrice') as string),
      lastUpdate: new Date().toISOString().split('T')[0],
    };

    setAssets([...assets, newAsset]);
    setIsDialogOpen(false);
    toast({
      title: "Ativo adicionado",
      description: `${newAsset.ticker} foi adicionado à sua carteira.`,
    });
  };

  const handleDeleteAsset = (id: string) => {
    setAssets(assets.filter(a => a.id !== id));
    toast({
      title: "Ativo removido",
      description: "O ativo foi removido da sua carteira.",
    });
  };

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'Ação': return 'bg-primary/10 text-primary';
      case 'FII': return 'bg-warning/10 text-warning';
      case 'ETF': return 'bg-success/10 text-success';
      case 'BDR': return 'bg-info/10 text-info';
      default: return 'bg-secondary text-secondary-foreground';
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Renda Variável</h1>
          <p className="text-muted-foreground">Gerencie suas ações, FIIs, ETFs e BDRs</p>
        </div>
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="h-4 w-4 mr-2" />
              Adicionar Ativo
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-md">
            <DialogHeader>
              <DialogTitle>Adicionar Ativo de Renda Variável</DialogTitle>
              <DialogDescription>
                Registre uma nova compra na sua carteira
              </DialogDescription>
            </DialogHeader>
            <form onSubmit={handleAddAsset}>
              <div className="grid gap-4 py-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="ticker">Ticker</Label>
                    <Input id="ticker" name="ticker" placeholder="Ex: PETR4" required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="type">Tipo</Label>
                    <Select name="type" defaultValue="Ação">
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {assetTypes.map(type => (
                          <SelectItem key={type} value={type}>{type}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="name">Nome da Empresa</Label>
                  <Input id="name" name="name" placeholder="Ex: Petrobras PN" required />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="sector">Setor</Label>
                  <Input id="sector" name="sector" placeholder="Ex: Petróleo e Gás" required />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="quantity">Quantidade</Label>
                    <Input id="quantity" name="quantity" type="number" placeholder="100" required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="averagePrice">Preço Médio</Label>
                    <Input id="averagePrice" name="averagePrice" type="number" step="0.01" placeholder="32.50" required />
                  </div>
                </div>
              </div>
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setIsDialogOpen(false)}>
                  Cancelar
                </Button>
                <Button type="submit">Adicionar</Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total Investido</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(totalInvested)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Valor Atual</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatCurrency(totalCurrent)}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Resultado</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold flex items-center gap-2 ${totalProfit >= 0 ? 'text-success' : 'text-destructive'}`}>
              {totalProfit >= 0 ? <ArrowUpRight className="h-5 w-5" /> : <ArrowDownRight className="h-5 w-5" />}
              {formatCurrency(totalProfit)}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="assets" className="space-y-4">
        <TabsList>
          <TabsTrigger value="assets">Ativos</TabsTrigger>
          <TabsTrigger value="dividends">Proventos</TabsTrigger>
        </TabsList>

        <TabsContent value="assets">
          <Card>
            <CardHeader>
              <div className="flex flex-col sm:flex-row gap-4">
                <div className="relative flex-1">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder="Buscar por ticker ou nome..."
                    className="pl-10"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                  />
                </div>
                <Select value={typeFilter} onValueChange={setTypeFilter}>
                  <SelectTrigger className="w-full sm:w-[180px]">
                    <Filter className="h-4 w-4 mr-2" />
                    <SelectValue placeholder="Tipo" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Todos os tipos</SelectItem>
                    {assetTypes.map(type => (
                      <SelectItem key={type} value={type}>{type}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </CardHeader>
            <CardContent>
              <div className="rounded-lg border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Ativo</TableHead>
                      <TableHead>Tipo</TableHead>
                      <TableHead className="text-right">Qtd</TableHead>
                      <TableHead className="text-right">PM</TableHead>
                      <TableHead className="text-right">Atual</TableHead>
                      <TableHead className="text-right">Total</TableHead>
                      <TableHead className="text-right">Resultado</TableHead>
                      <TableHead className="w-[50px]"></TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {filteredAssets.map((asset) => {
                      const total = asset.currentPrice * asset.quantity;
                      const invested = asset.averagePrice * asset.quantity;
                      const profit = total - invested;
                      const profitPercent = ((profit / invested) * 100);
                      
                      return (
                        <TableRow key={asset.id}>
                          <TableCell>
                            <div>
                              <p className="font-medium">{asset.ticker}</p>
                              <p className="text-xs text-muted-foreground">{asset.name}</p>
                            </div>
                          </TableCell>
                          <TableCell>
                            <Badge className={getTypeColor(asset.type)} variant="secondary">
                              {asset.type}
                            </Badge>
                          </TableCell>
                          <TableCell className="text-right">{asset.quantity}</TableCell>
                          <TableCell className="text-right">{formatCurrency(asset.averagePrice)}</TableCell>
                          <TableCell className="text-right">{formatCurrency(asset.currentPrice)}</TableCell>
                          <TableCell className="text-right font-medium">{formatCurrency(total)}</TableCell>
                          <TableCell className="text-right">
                            <div className={`flex items-center justify-end gap-1 ${profit >= 0 ? 'text-success' : 'text-destructive'}`}>
                              {profit >= 0 ? <ArrowUpRight className="h-3 w-3" /> : <ArrowDownRight className="h-3 w-3" />}
                              <span className="font-medium">{profitPercent >= 0 ? '+' : ''}{profitPercent.toFixed(2)}%</span>
                            </div>
                          </TableCell>
                          <TableCell>
                            <DropdownMenu>
                              <DropdownMenuTrigger asChild>
                                <Button variant="ghost" size="icon" className="h-8 w-8">
                                  <MoreHorizontal className="h-4 w-4" />
                                </Button>
                              </DropdownMenuTrigger>
                              <DropdownMenuContent align="end">
                                <DropdownMenuItem onClick={() => navigate(`/investimento/${asset.id}?type=variable`)}>
                                  <Eye className="h-4 w-4 mr-2" />
                                  Ver Detalhes
                                </DropdownMenuItem>
                                <DropdownMenuItem>
                                  <Pencil className="h-4 w-4 mr-2" />
                                  Editar
                                </DropdownMenuItem>
                                <DropdownMenuItem 
                                  className="text-destructive"
                                  onClick={() => handleDeleteAsset(asset.id)}
                                >
                                  <Trash2 className="h-4 w-4 mr-2" />
                                  Excluir
                                </DropdownMenuItem>
                              </DropdownMenuContent>
                            </DropdownMenu>
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="dividends">
          <Card>
            <CardHeader>
              <CardTitle>Histórico de Proventos</CardTitle>
              <CardDescription>Dividendos, JCP e rendimentos recebidos</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="rounded-lg border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Ticker</TableHead>
                      <TableHead>Tipo</TableHead>
                      <TableHead className="text-right">Valor/Cota</TableHead>
                      <TableHead>Data Ex</TableHead>
                      <TableHead>Pagamento</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {dividends.map((dividend) => (
                      <TableRow key={dividend.id}>
                        <TableCell className="font-medium">{dividend.ticker}</TableCell>
                        <TableCell>
                          <Badge variant="outline">{dividend.type}</Badge>
                        </TableCell>
                        <TableCell className="text-right text-success font-medium">
                          {formatCurrency(dividend.value)}
                        </TableCell>
                        <TableCell>{formatDate(dividend.exDate)}</TableCell>
                        <TableCell>{formatDate(dividend.paymentDate)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
