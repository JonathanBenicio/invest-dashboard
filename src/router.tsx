import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  Navigate,
  useRouter,
} from "@tanstack/react-router"
import { AppLayout } from "@/components/layout/AppLayout"
import { useAuthStore } from "@/store/authStore"
import Login from "./pages/auth/Login"
import Register from "./pages/auth/Register"
import { useEffect } from "react"
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
import { z } from "zod"

const investmentSearchSchema = z.object({
  type: z.enum(["fixed", "variable"]).optional(),
  action: z.enum(["buy", "sell"]).optional(),
})

// Auth Guard Component
const AuthGuard = ({ children }: { children: React.ReactNode }) => {
  const { isAuthenticated, isLoading } = useAuthStore()

  if (isLoading) {
    return <div className="min-h-screen flex items-center justify-center">Carregando...</div>
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" />
  }

  return <>{children}</>
}

// Auth Initializer Component
const AuthInitializer = () => {
  const checkAuth = useAuthStore(state => state.checkAuth)

  useEffect(() => {
    checkAuth()
  }, [checkAuth])

  return <Outlet />
}

// Root Route
export const rootRoute = createRootRoute({
  component: AuthInitializer,
})

// Index Route (Redirect to dashboard)
export const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: () => <Navigate to="/dashboard" replace />,
})

// Auth Routes
export const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/login",
  component: Login,
})

export const registerRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/cadastro",
  component: Register,
})

// Layout Route for protected pages
export const layoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: "layout",
  component: () => (
    <AuthGuard>
      <AppLayout />
    </AuthGuard>
  ),
})

// Dashboard and other tools
export const dashboardRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: "/dashboard",
  component: Dashboard,
})

export const portfoliosRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: "/carteiras",
  component: Portfolios,
})

export const portfolioDetailsRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: "/carteira/$id",
  component: PortfolioDetails,
})

export const fixedIncomeRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: "/renda-fixa",
  component: FixedIncome,
})

export const variableIncomeRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: "/renda-variavel",
  component: VariableIncome,
})

export const investmentDetailsRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: "/investimento/$id",
  validateSearch: (search) => investmentSearchSchema.parse(search),
  component: InvestmentDetails,
})

export const importRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: "/importar",
  component: Import,
})

export const analysisRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: "/analise",
  component: Analysis,
})

export const simulatorRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: "/simulador",
  component: Simulator,
})

export const settingsRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: "/configuracoes",
  component: Settings,
})

// 404 Route
export const notFoundRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "*",
  component: NotFound,
})

// Route Tree
const routeTree = rootRoute.addChildren([
  indexRoute,
  loginRoute,
  registerRoute,
  layoutRoute.addChildren([
    dashboardRoute,
    portfoliosRoute,
    portfolioDetailsRoute,
    fixedIncomeRoute,
    variableIncomeRoute,
    investmentDetailsRoute,
    importRoute,
    analysisRoute,
    simulatorRoute,
    settingsRoute,
  ]),
  notFoundRoute,
])

// Create Router
export const router = createRouter({
  routeTree,
  defaultPreload: "intent",
  // Base path for GitHub Pages
  basepath: import.meta.env.PROD ? "/invest-dashboard/" : "/",
})

// Type Safety
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router
  }
}
