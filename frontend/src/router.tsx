import { useEffect } from 'react'
import {
  createRootRoute,
  createRoute,
  createRouter,
  Navigate,
  Outlet,
} from '@tanstack/react-router'
import { AppLayout } from '@/components/layout/AppLayout'
import { useAuthStore } from '@/store/authStore'
import Dashboard from './pages/dashboard/Dashboard'
import FixedIncome from './pages/investments/FixedIncome'
import InvestmentDetails from './pages/investments/InvestmentDetails'
import VariableIncome from './pages/investments/VariableIncome'
import Login from './pages/auth/Login'
import Register from './pages/auth/Register'
import AcceptInvitation from './pages/auth/AcceptInvitation'
import NotFound from './pages/errors/NotFound'
import PortfolioDetails from './pages/portfolio/PortfolioDetails'
import Portfolios from './pages/portfolio/Portfolios'
import Groups from './pages/portfolio/Groups'
import Settings from './pages/tools/Settings'
import Simulator from './pages/tools/Simulator'
import Taxas from './pages/tools/Taxas'
import Analysis from './pages/tools/Analysis'
import Import from './pages/tools/Import'
import { z } from 'zod'

const investmentSearchSchema = z.object({
  type: z.enum(['fixed', 'variable']).optional(),
  action: z.enum(['buy', 'sell']).optional(),
})

const AuthGuard = ({ children }: { children: React.ReactNode }) => {
  const { isAuthenticated, isLoading } = useAuthStore()
  if (isLoading) return <div className="flex min-h-screen items-center justify-center">Carregando...</div>
  if (!isAuthenticated) return <Navigate to="/login" />
  return <>{children}</>
}

const AdminGuard = ({ children }: { children: React.ReactNode }) => {
  const { user, isLoading } = useAuthStore()
  if (isLoading) return <div className="flex min-h-screen items-center justify-center">Carregando...</div>
  if (user?.perfil !== 'admin') return <Navigate to="/dashboard" />
  return <>{children}</>
}

const AuthInitializer = () => {
  const checkAuth = useAuthStore(state => state.checkAuth)
  useEffect(() => { void checkAuth() }, [checkAuth])
  return <Outlet />
}

export const rootRoute = createRootRoute({ component: AuthInitializer })

export const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: () => <Navigate to="/dashboard" replace />,
})

export const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  component: Login,
})

export const registerRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/cadastro',
  component: Register,
})

export const acceptInvitationRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/convite/aceitar',
  component: AcceptInvitation,
})

export const layoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: 'layout',
  component: () => <AuthGuard><AppLayout /></AuthGuard>,
})

export const dashboardRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/dashboard',
  component: Dashboard,
})

export const portfoliosRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/carteiras',
  component: Portfolios,
})

export const groupsRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/usuarios',
  component: Groups,
})

export const portfolioDetailsRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/carteira/$id',
  component: PortfolioDetails,
})

export const fixedIncomeRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/renda-fixa',
  component: FixedIncome,
})

export const variableIncomeRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/renda-variavel',
  component: VariableIncome,
})

export const investmentDetailsRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/investimento/$id',
  validateSearch: search => investmentSearchSchema.parse(search),
  component: InvestmentDetails,
})

export const simulatorRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/simulador',
  component: Simulator,
})

export const analysisRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/analise',
  component: Analysis,
})

export const importRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/importar',
  component: Import,
})

export const settingsRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/configuracoes',
  component: Settings,
})

export const taxasRoute = createRoute({
  getParentRoute: () => layoutRoute,
  path: '/taxas',
  component: Taxas,
})

export const notFoundRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '*',
  component: NotFound,
})

const routeTree = rootRoute.addChildren([
  indexRoute,
  loginRoute,
  registerRoute,
  acceptInvitationRoute,
  layoutRoute.addChildren([
    dashboardRoute,
    portfoliosRoute,
    groupsRoute,
    portfolioDetailsRoute,
    fixedIncomeRoute,
    variableIncomeRoute,
    investmentDetailsRoute,
    simulatorRoute,
    analysisRoute,
    importRoute,
    settingsRoute,
    taxasRoute,
  ]),
  notFoundRoute,
])

export const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
  basepath: import.meta.env.BASE_URL,
})

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
