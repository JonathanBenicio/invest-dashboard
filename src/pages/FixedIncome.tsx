import { useState } from "react";
import { Plus, Filter, Search, MoreHorizontal, Pencil, Trash2 } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { fixedIncomeAssets, formatCurrency, formatDate, type FixedIncomeAsset } from "@/lib/mock-data";
import { useToast } from "@/hooks/use-toast";

export default function FixedIncome() {
  const [assets, setAssets] = useState(fixedIncomeAssets);
  const [searchTerm, setSearchTerm] = useState("");
  const [typeFilter, setTypeFilter] = useState<string>("all");
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const { toast } = useToast();

  const filteredAssets = assets.filter(asset => {
    const matchesSearch = asset.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         asset.institution.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesType = typeFilter === "all" || asset.type === typeFilter;
    return matchesSearch && matchesType;
  });

  const totalInvested = assets.reduce((acc, asset) => acc + asset.investedValue, 0);
  const totalCurrent = assets.reduce((acc, asset) => acc + asset.currentValue, 0);
  const totalProfit = totalCurrent - totalInvested;

  const assetTypes = ['CDB', 'LCI', 'LCA', 'Tesouro Direto', 'Debênture', 'CRI', 'CRA'];

  const handleAddAsset = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    
    const newAsset: FixedIncomeAsset = {
      id: Date.now().toString(),
      name: formData.get('name') as string,
      type: formData.get('type') as FixedIncomeAsset['type'],
      institution: formData.get('institution') as string,
      investedValue: parseFloat(formData.get('investedValue') as string),
      currentValue: parseFloat(formData.get('investedValue') as string),
      rate: formData.get('rate') as string,
      rateType: formData.get('rateType') as FixedIncomeAsset['rateType'],
      purchaseDate: formData.get('purchaseDate') as string,
      maturityDate: formData.get('maturityDate') as string,
      liquidity: formData.get('liquidity') as FixedIncomeAsset['liquidity'],
    };

    setAssets([...assets, newAsset]);
    setIsDialogOpen(false);
    toast({
      title: "Ativo adicionado",
      description: `${newAsset.name} foi adicionado à sua carteira.`,
    });
  };

  const handleDeleteAsset = (id: string) => {
    setAssets(assets.filter(a => a.id !== id));
    toast({
      title: "Ativo removido",
      description: "O ativo foi removido da sua carteira.",
    });
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Renda Fixa</h1>
          <p className="text-muted-foreground">Gerencie seus ativos de renda fixa</p>
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
              <DialogTitle>Adicionar Ativo de Renda Fixa</DialogTitle>
              <DialogDescription>
                Preencha os dados do novo ativo
              </DialogDescription>
            </DialogHeader>
            <form onSubmit={handleAddAsset}>
              <div className="grid gap-4 py-4">
                <div className="grid gap-2">
                  <Label htmlFor="name">Nome do Ativo</Label>
                  <Input id="name" name="name" placeholder="Ex: CDB Banco Inter" required />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="type">Tipo</Label>
                    <Select name="type" defaultValue="CDB">
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
                  <div className="grid gap-2">
                    <Label htmlFor="institution">Instituição</Label>
                    <Input id="institution" name="institution" placeholder="Ex: Banco Inter" required />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="investedValue">Valor Investido</Label>
                    <Input id="investedValue" name="investedValue" type="number" step="0.01" placeholder="10000" required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="rate">Taxa</Label>
                    <Input id="rate" name="rate" placeholder="Ex: 120%" required />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="rateType">Indexador</Label>
                    <Select name="rateType" defaultValue="CDI">
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="CDI">CDI</SelectItem>
                        <SelectItem value="IPCA">IPCA</SelectItem>
                        <SelectItem value="Prefixado">Prefixado</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="liquidity">Liquidez</Label>
                    <Select name="liquidity" defaultValue="Diária">
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Diária">Diária</SelectItem>
                        <SelectItem value="No vencimento">No vencimento</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label htmlFor="purchaseDate">Data de Compra</Label>
                    <Input id="purchaseDate" name="purchaseDate" type="date" required />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="maturityDate">Data de Vencimento</Label>
                    <Input id="maturityDate" name="maturityDate" type="date" required />
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
            <CardTitle className="text-sm font-medium text-muted-foreground">Rentabilidade</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${totalProfit >= 0 ? 'text-success' : 'text-destructive'}`}>
              {formatCurrency(totalProfit)}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Buscar por nome ou instituição..."
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
                  <TableHead className="text-right">Investido</TableHead>
                  <TableHead className="text-right">Atual</TableHead>
                  <TableHead>Taxa</TableHead>
                  <TableHead>Vencimento</TableHead>
                  <TableHead>Liquidez</TableHead>
                  <TableHead className="w-[50px]"></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredAssets.map((asset) => {
                  const profit = ((asset.currentValue - asset.investedValue) / asset.investedValue) * 100;
                  return (
                    <TableRow key={asset.id}>
                      <TableCell>
                        <div>
                          <p className="font-medium">{asset.name}</p>
                          <p className="text-xs text-muted-foreground">{asset.institution}</p>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant="secondary">{asset.type}</Badge>
                      </TableCell>
                      <TableCell className="text-right">{formatCurrency(asset.investedValue)}</TableCell>
                      <TableCell className="text-right">
                        <div>
                          <p className="font-medium">{formatCurrency(asset.currentValue)}</p>
                          <p className={`text-xs ${profit >= 0 ? 'text-success' : 'text-destructive'}`}>
                            {profit >= 0 ? '+' : ''}{profit.toFixed(2)}%
                          </p>
                        </div>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm">{asset.rate} {asset.rateType}</span>
                      </TableCell>
                      <TableCell>{formatDate(asset.maturityDate)}</TableCell>
                      <TableCell>
                        <Badge variant={asset.liquidity === 'Diária' ? 'default' : 'outline'}>
                          {asset.liquidity}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="icon" className="h-8 w-8">
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
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
    </div>
  );
}
