import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Plus,
  Wallet,
  Building2,
  User,
  MoreHorizontal,
  Eye,
  Pencil,
  Trash2,
  TrendingUp,
  Briefcase
} from "lucide-react"
import { useNavigate } from "@tanstack/react-router"
import { useToast } from "@/hooks/use-toast"
import { mockBanks, mockPortfolios, mockUsers, type Portfolio } from "@/lib/mock-data"
import { DeleteConfirmDialog } from "@/components/dialogs/DeleteConfirmDialog"
import { EditPortfolioDialog } from "@/components/dialogs/EditPortfolioDialog"
import { useAuth } from "@/context/AuthContext"

const Portfolios = () => {
  const navigate = useNavigate()
  const { toast } = useToast()
  const { hasPermission } = useAuth()
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [portfolios, setPortfolios] = useState(mockPortfolios)
  const [formData, setFormData] = useState({
    name: "",
    bankId: "",
    userId: "",
    description: "",
  })

  // Edit/Delete state
  const [editingPortfolio, setEditingPortfolio] = useState<Portfolio | null>(null)
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [deletingPortfolio, setDeletingPortfolio] = useState<Portfolio | null>(null)
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const bank = mockBanks.find(b => b.id === formData.bankId)
    const user = mockUsers.find(u => u.id === formData.userId)

    const newPortfolio = {
      id: `portfolio-${Date.now()}`,
      name: formData.name,
      bankId: formData.bankId,
      bankName: bank?.name || "",
      bankLogo: bank?.logo || "",
      userId: formData.userId,
      userName: user?.name || "",
      userEmail: user?.email || "",
      description: formData.description,
      totalValue: 0,
      totalInvested: 0,
      profitability: 0,
      assetsCount: 0,
      createdAt: new Date().toISOString(),
    }

    setPortfolios([...portfolios, newPortfolio])
    setFormData({ name: "", bankId: "", userId: "", description: "" })
    setIsDialogOpen(false)
    toast({
      title: "Carteira criada",
      description: `A carteira "${formData.name}" foi criada com sucesso.`,
    })
  }

  const handleEditPortfolio = (portfolio: Portfolio) => {
    setEditingPortfolio(portfolio)
    setIsEditDialogOpen(true)
  }

  const handleSaveEdit = (updatedPortfolio: Portfolio) => {
    setPortfolios(portfolios.map(p => 
      p.id === updatedPortfolio.id ? updatedPortfolio : p
    ))
    toast({
      title: "Carteira atualizada",
      description: `A carteira "${updatedPortfolio.name}" foi atualizada com sucesso.`,
    })
  }

  const handleDeleteClick = (portfolio: Portfolio) => {
    setDeletingPortfolio(portfolio)
    setIsDeleteDialogOpen(true)
  }

  const handleConfirmDelete = () => {
    if (deletingPortfolio) {
      setPortfolios(portfolios.filter(p => p.id !== deletingPortfolio.id))
      toast({
        title: "Carteira excluída",
        description: `A carteira "${deletingPortfolio.name}" foi excluída com sucesso.`,
        variant: "destructive",
      })
      setIsDeleteDialogOpen(false)
      setDeletingPortfolio(null)
    }
  }

  const totalPatrimony = portfolios.reduce((acc, p) => acc + p.totalValue, 0)
  const totalInvested = portfolios.reduce((acc, p) => acc + p.totalInvested, 0)
  const totalProfit = totalPatrimony - totalInvested
  const avgProfitability = portfolios.length > 0
    ? portfolios.reduce((acc, p) => acc + p.profitability, 0) / portfolios.length
    : 0

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Carteiras</h1>
          <p className="text-muted-foreground">Gerencie suas carteiras de investimentos</p>
        </div>

        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          {hasPermission('edit') && (
            <DialogTrigger asChild>
              <Button className="gap-2">
                <Plus className="h-4 w-4" />
                Nova Carteira
              </Button>
            </DialogTrigger>
          )}
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Criar Nova Carteira</DialogTitle>
            </DialogHeader>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="name">Nome da Carteira</Label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="Ex: Carteira Principal"
                  required
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="bank">Banco/Corretora</Label>
                <Select
                  value={formData.bankId}
                  onValueChange={(value) => setFormData({ ...formData, bankId: value })}
                  required
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Selecione o banco" />
                  </SelectTrigger>
                  <SelectContent>
                    {mockBanks.map((bank) => (
                      <SelectItem key={bank.id} value={bank.id}>
                        <div className="flex items-center gap-2">
                          <span>{bank.logo}</span>
                          <span>{bank.name}</span>
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="user">Titular</Label>
                <Select
                  value={formData.userId}
                  onValueChange={(value) => setFormData({ ...formData, userId: value })}
                  required
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Selecione o titular" />
                  </SelectTrigger>
                  <SelectContent>
                    {mockUsers.map((user) => (
                      <SelectItem key={user.id} value={user.id}>
                        <div className="flex flex-col">
                          <span>{user.name}</span>
                          <span className="text-xs text-muted-foreground">{user.email}</span>
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">Descrição (opcional)</Label>
                <Input
                  id="description"
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  placeholder="Descrição da carteira"
                />
              </div>

              <div className="flex gap-2 justify-end">
                <Button type="button" variant="outline" onClick={() => setIsDialogOpen(false)}>
                  Cancelar
                </Button>
                <Button type="submit">Criar Carteira</Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Patrimônio Total
            </CardTitle>
            <Wallet className="h-4 w-4 text-primary" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {totalPatrimony.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Total Investido
            </CardTitle>
            <Briefcase className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {totalInvested.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Lucro/Prejuízo
            </CardTitle>
            <TrendingUp className="h-4 w-4 text-success" />
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${totalProfit >= 0 ? "text-success" : "text-destructive"}`}>
              {totalProfit.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Rentabilidade Média
            </CardTitle>
            <TrendingUp className="h-4 w-4 text-success" />
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${avgProfitability >= 0 ? "text-success" : "text-destructive"}`}>
              {avgProfitability.toFixed(2)}%
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Portfolios Table */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Wallet className="h-5 w-5" />
            Minhas Carteiras
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Carteira</TableHead>
                <TableHead>Banco/Corretora</TableHead>
                <TableHead>Titular</TableHead>
                <TableHead className="text-right">Valor Total</TableHead>
                <TableHead className="text-right">Investido</TableHead>
                <TableHead className="text-right">Rentabilidade</TableHead>
                <TableHead className="text-center">Ativos</TableHead>
                <TableHead className="w-[50px]"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {portfolios.map((portfolio) => (
                <TableRow key={portfolio.id} className="cursor-pointer hover:bg-muted/50">
                  <TableCell>
                    <div className="flex flex-col">
                      <span className="font-medium">{portfolio.name}</span>
                      {portfolio.description && (
                        <span className="text-xs text-muted-foreground">{portfolio.description}</span>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <span className="text-lg">{portfolio.bankLogo}</span>
                      <span>{portfolio.bankName}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
                        <User className="h-4 w-4 text-primary" />
                      </div>
                      <div className="flex flex-col">
                        <span className="text-sm">{portfolio.userName}</span>
                        <span className="text-xs text-muted-foreground">{portfolio.userEmail}</span>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell className="text-right font-medium">
                    {portfolio.totalValue.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
                  </TableCell>
                  <TableCell className="text-right">
                    {portfolio.totalInvested.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
                  </TableCell>
                  <TableCell className="text-right">
                    <Badge variant={portfolio.profitability >= 0 ? "default" : "destructive"}>
                      {portfolio.profitability >= 0 ? "+" : ""}{portfolio.profitability.toFixed(2)}%
                    </Badge>
                  </TableCell>
                  <TableCell className="text-center">
                    <Badge variant="outline">{portfolio.assetsCount}</Badge>
                  </TableCell>
                  <TableCell>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="icon">
                          <MoreHorizontal className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={() => navigate({ to: '/carteira/$id', params: { id: portfolio.id } })}>
                          <Eye className="h-4 w-4 mr-2" />
                          Ver Detalhes
                        </DropdownMenuItem>
                        {hasPermission('edit') && (
                          <>
                            <DropdownMenuItem onClick={() => handleEditPortfolio(portfolio)}>
                              <Pencil className="h-4 w-4 mr-2" />
                              Editar
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              className="text-destructive"
                              onClick={() => handleDeleteClick(portfolio)}
                            >
                              <Trash2 className="h-4 w-4 mr-2" />
                              Excluir
                            </DropdownMenuItem>
                          </>
                        )}
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Portfolio Cards Grid (Mobile-friendly alternative view) */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 lg:hidden">
        {portfolios.map((portfolio) => (
          <Card key={portfolio.id} className="cursor-pointer hover:shadow-md transition-shadow" onClick={() => navigate({ to: '/carteira/$id', params: { id: portfolio.id } })}>
            <CardHeader className="pb-2">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="text-2xl">{portfolio.bankLogo}</span>
                  <div>
                    <CardTitle className="text-lg">{portfolio.name}</CardTitle>
                    <p className="text-sm text-muted-foreground">{portfolio.bankName}</p>
                  </div>
                </div>
                <Badge variant={portfolio.profitability >= 0 ? "default" : "destructive"}>
                  {portfolio.profitability >= 0 ? "+" : ""}{portfolio.profitability.toFixed(2)}%
                </Badge>
              </div>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <User className="h-4 w-4" />
                <span>{portfolio.userName}</span>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-xs text-muted-foreground">Valor Total</p>
                  <p className="font-semibold">
                    {portfolio.totalValue.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
                  </p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Ativos</p>
                  <p className="font-semibold">{portfolio.assetsCount}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Edit Portfolio Dialog */}
      <EditPortfolioDialog
        open={isEditDialogOpen}
        onOpenChange={setIsEditDialogOpen}
        portfolio={editingPortfolio}
        onSave={handleSaveEdit}
      />

      {/* Delete Confirmation Dialog */}
      <DeleteConfirmDialog
        open={isDeleteDialogOpen}
        onOpenChange={setIsDeleteDialogOpen}
        title="Excluir Carteira"
        description={`Tem certeza que deseja excluir a carteira "${deletingPortfolio?.name}"? Esta ação não pode ser desfeita e todos os investimentos associados serão removidos.`}
        onConfirm={handleConfirmDelete}
      />
    </div>
  )
}

export default Portfolios
