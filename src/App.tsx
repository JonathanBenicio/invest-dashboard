import { Toaster } from "@/components/ui/toaster";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AppLayout } from "@/components/layout/AppLayout";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Dashboard from "./pages/Dashboard";
import Portfolios from "./pages/Portfolios";
import PortfolioDetails from "./pages/PortfolioDetails";
import FixedIncome from "./pages/FixedIncome";
import VariableIncome from "./pages/VariableIncome";
import InvestmentDetails from "./pages/InvestmentDetails";
import Import from "./pages/Import";
import Analysis from "./pages/Analysis";
import Simulator from "./pages/Simulator";
import Settings from "./pages/Settings";
import NotFound from "./pages/NotFound";

const queryClient = new QueryClient();

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
);

export default App;
