import { Toaster } from "@/components/ui/toaster"
import { Toaster as Sonner } from "@/components/ui/sonner"
import { TooltipProvider } from "@/components/ui/tooltip"
import { QueryClient, QueryClientProvider } from "@tanstack/react-query"
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom"
import { AppLayout } from "@/components/layout/AppLayout"
import Login from "./pages/auth/Login"
import Register from "./pages/auth/Register"
import Dashboard from "./pages/dashboard/Dashboard"
import Portfolios from "./pages/portfolio/Portfolios"
import PortfolioDetails from "./pages/portfolio/PortfolioDetails"
import FixedIncome from "./pages/investments/FixedIncome"
import VariableIncome from "./pages/investments/VariableIncome"
import InvestmentDetails from "./pages/investments/InvestmentDetails"
import Import from "./pages/tools/Import"
import Analysis from "./pages/tools/Analysis"
import Simulator from "./pages/tools/Simulator"
import Settings from "./pages/tools/Settings"
import NotFound from "./pages/errors/NotFound"

const queryClient = new QueryClient()

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <Toaster />
      <Sonner />
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/login" element={<Login />} />
          <Route path="/cadastro" element={<Register />} />
          <Route element={<AppLayout />}>
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/carteiras" element={<Portfolios />} />
            <Route path="/carteira/:id" element={<PortfolioDetails />} />
            <Route path="/renda-fixa" element={<FixedIncome />} />
            <Route path="/renda-variavel" element={<VariableIncome />} />
            <Route path="/investimento/:id" element={<InvestmentDetails />} />
            <Route path="/importar" element={<Import />} />
            <Route path="/analise" element={<Analysis />} />
            <Route path="/simulador" element={<Simulator />} />
            <Route path="/configuracoes" element={<Settings />} />
          </Route>
          <Route path="*" element={<NotFound />} />
        </Routes>
      </BrowserRouter>
    </TooltipProvider>
  </QueryClientProvider>
)

export default App
