/**
 * MSW Request Handlers
 * Mock API endpoints for development
 */

import { http, HttpResponse, delay } from 'msw'
import { API_CONFIG } from '@/api/env'
import type {
  RespostaApi,
  AtualizarCarteiraRequest,
  CarteiraDto,
  CriarCarteiraRequest,
  CriarRendaFixaRequest,
  CriarRendaVariavelRequest,
  RespostaPaginada,
  PosicaoInvestimentoDto,
  RegistrarTransacaoRequest,
  RendaFixaDto,
  RendaVariavelDto,
  TransacaoDto,
  UsuarioDto,
  TaxaEconomicaDto,
  TaxaEconomicaHistoricoDto,
  ProjecaoRendaFixaDto,
  ProjecaoRendaFixaConsolidadaDto,
} from '@/api/dtos'
import {
  mockCurrentUser,
  mockPortfolios,
  mockAllInvestments,
  mockFixedIncomeInvestments,
  mockVariableIncomeInvestments,
  mockInvestmentSummary,
  mockPortfolioSummary,
  mockUsers,
} from './data'

// MSW matches paths relative to the current origin automatically
// '/api/v1/...' will be intercepted on any domain (localhost, GitHub Pages, Capacitor)
const BASE_URL = API_CONFIG.VERSION
let activeMockUserId: string | null = null
const mockTransactionsByIdempotencyKey = new Map<string, TransacaoDto>()
const mockHolders: { id: string; grupoId: string; nome: string; parentesco?: string; usuarioId?: string }[] = []
const mockEconomicRates: TaxaEconomicaDto[] = [{
  id: 'rate-1', grupoId: 'group-1', nome: 'Taxa Selic', simbolo: 'SELIC',
  valorAtual: 12.75, valorAnterior: 12.5, variacao: 0.25, unidade: 'Percentual', periodicidade: 'Anual',
  descricao: 'Taxa básica de juros da economia brasileira', origem: 'Banco Central',
  atualizadoEm: new Date().toISOString(), dataReferencia: new Date().toISOString().slice(0, 10), atualizadoPorUserId: 'user-1',
}]
const mockEconomicRateHistory = new Map<string, TaxaEconomicaHistoricoDto[]>()
const mockFixedIncomeConsolidatedProjection: ProjecaoRendaFixaConsolidadaDto = {
  valorObservado: 112500, valorProjetadoBruto: null, quantidadePosicoes: 3, estaCompleta: false,
  posicoesSemProjecao: ['TESOURO-IPCA-2029'],
}


/**
 * Helper to create API response
 */
function createResponse<T>(data: T, message?: string): RespostaApi<T> {
  return {
    dados: data,
    sucesso: true,
    mensagem: message,
  }
}

/**
 * Helper to create paginated response
 */
function createPaginatedResponse<T>(
  data: T[],
  page = 1,
  pageSize = 10
): RespostaPaginada<T> {
  const totalCount = data.length
  const totalPages = Math.ceil(totalCount / pageSize)
  const start = (page - 1) * pageSize
  const end = start + pageSize
  const paginatedData = data.slice(start, end)

  return {
    dados: paginatedData,
    sucesso: true,
    paginacao: {
      pagina: page,
      itensPorPagina: pageSize,
      totalItens: totalCount,
      totalPaginas: totalPages,
      temProximaPagina: page < totalPages,
      temPaginaAnterior: page > 1,
    },
  }
}

/**
 * Middleware to check authentication and permissions
 */
function checkPermission(request: Request, requiredRole?: 'edit' | 'admin') {
  const cookies = request.headers.get('cookie') || ''
  let userId = ''

  const authorization = request.headers.get('authorization') || ''
  const accessToken = authorization.match(/^Bearer\s+mock-access-token-(.+)$/i)?.[1]
  if (accessToken) userId = accessToken

  const refreshCookie = cookies.split(';').find(cookie => cookie.trim().startsWith('refresh_token='))
  if (!userId && refreshCookie) userId = refreshCookie.split('=').slice(1).join('=')
  if (!userId) userId = activeMockUserId ?? ''

  if (!userId) {
    return { authorized: false, status: 401, message: 'Authentication is required.' }
  }

  const user = mockUsers.find(u => u.id === userId)

  if (!user) {
    return { authorized: false, status: 401, message: 'Usuário inválido' }
  }

  // Check role
  if (requiredRole) {
    const role = user.perfil as string
    if (requiredRole === 'admin' && role !== 'admin') {
      return { authorized: false, status: 403, message: 'Acesso negado: Requer privilégios de Admin' }
    }
    if (requiredRole === 'edit' && role === 'view') {
      return { authorized: false, status: 403, message: 'Acesso negado: Apenas leitura' }
    }
  }

  return { authorized: true, user }
}

export const handlers = [
  // Auth endpoints
  http.post(`${BASE_URL}/auth/login`, async ({ request }) => {
    const body = await request.json() as { email?: string; senha?: string }
    await delay(500)

    const user = mockUsers.find(u => u.email === body.email)

    // Simple password check (In real app, hash check)
    // For mock: password is 'password' for all, or match specific rules if needed.
    // We'll just check if user exists for now or simple "password" string.
    if (!user || body.senha !== 'password') {
      return HttpResponse.json(
        { sucesso: false, mensagem: 'Credenciais inválidas' },
        { status: 401 }
      )
    }

    // Set cookie
    return HttpResponse.json(createResponse({
      tokenAcesso: `mock-access-token-${user.id}`,
      expiraEmSegundos: 900,
      usuario: user,
      requerConfirmacaoEmail: false,
    }), {
      headers: {
        'Set-Cookie': `refresh_token=${user.id}; HttpOnly; Path=/api/v1/auth; SameSite=Lax`,
      }
    })
  }),

  http.post(`${BASE_URL}/auth/register`, async ({ request }) => {
    const body = await request.json() as { nome?: string; email?: string; senha?: string }
    await delay(300)

    if (!body.nome || !body.email || !body.senha || mockUsers.some(user => user.email === body.email)) {
      return HttpResponse.json(
        { sucesso: false, mensagem: 'Não foi possível criar esta conta.' },
        { status: 409 }
      )
    }

    const now = new Date().toISOString()
    const user: UsuarioDto = {
      id: `user-${Date.now()}`,
      nome: body.nome,
      email: body.email,
      perfil: 'user',
      emailVerificado: true,
      ativo: true,
      criadoEm: now,
      atualizadoEm: now,
    }
    mockUsers.push(user)
    activeMockUserId = user.id

    return HttpResponse.json(createResponse({
      tokenAcesso: `mock-access-token-${user.id}`,
      expiraEmSegundos: 900,
      usuario: user,
      requerConfirmacaoEmail: false,
    }), {
      headers: {
        'Set-Cookie': `refresh_token=${user.id}; HttpOnly; Path=/api/v1/auth; SameSite=Lax`,
      }
    })
  }),

  http.post(`${BASE_URL}/auth/refresh`, async ({ request }) => {
    const check = checkPermission(request)
    if (!check.authorized || !check.user) {
      return HttpResponse.json({ success: false, message: check.message }, { status: check.status })
    }

    return HttpResponse.json(createResponse({
      tokenAcesso: `mock-access-token-${check.user.id}`,
      expiraEmSegundos: 900,
      usuario: check.user,
      requerConfirmacaoEmail: false,
    }))
  }),

  http.post(`${BASE_URL}/auth/logout`, async () => {
    activeMockUserId = null
    return HttpResponse.json(createResponse(null), {
      headers: {
        'Set-Cookie': 'refresh_token=; HttpOnly; Path=/api/v1/auth; SameSite=Lax; Max-Age=0',
      }
    })
  }),

  http.get(`${BASE_URL}/auth/me`, async ({ request }) => {
    await delay(300)
    const check = checkPermission(request)
    if (!check.authorized) {
      return HttpResponse.json(
        { success: false, message: check.message },
        { status: check.status }
      )
    }
    return HttpResponse.json(createResponse(check.user))
  }),

  http.get(`${BASE_URL}/market-data/history`, async () =>
    HttpResponse.json(createResponse([]))),

  http.get(`${BASE_URL}/market-data/quotes`, async ({ request }) => {
    const check = checkPermission(request)
    if (!check.authorized) {
      return HttpResponse.json({ success: false, message: check.message }, { status: check.status })
    }

    const requestedSymbols = new URL(request.url).searchParams.get('simbolos')
      ?.split(',')
      .map(symbol => symbol.trim().toUpperCase())
      .filter(Boolean) ?? []
    const quotes = mockAllInvestments
      .filter(investment => investment.tipo === 'variable_income' && requestedSymbols.includes(investment.ticker.toUpperCase()))
      .map(investment => ({
        simbolo: investment.ticker,
        nome: investment.nome,
        preco: investment.precoAtual + 1,
        observadoEmUtc: '2024-12-18T00:00:00.000Z',
        moeda: investment.moeda,
        setor: investment.setor,
        subtipo: investment.subtipo,
        origem: 'demo',
      }))

    return HttpResponse.json(createResponse(quotes))
  }),

  http.patch(`${BASE_URL}/auth/me`, async ({ request }) => {
    await delay(500)
    const check = checkPermission(request)
    if (!check.authorized || !check.user) {
      return HttpResponse.json(
        { success: false, message: check.message || 'Não autorizado' },
        { status: check.status || 401 }
      )
    }

    const body = await request.json() as Partial<UsuarioDto>
    const user = check.user

    // Update fields
    if (body.nome) user.nome = body.nome
    if (body.email) user.email = body.email
    if (body.avatar) user.avatar = body.avatar

    // Update in mockUsers array (reference is already there but to be safe)
    const index = mockUsers.findIndex(u => u.id === user.id)
    if (index !== -1) {
      mockUsers[index] = { ...mockUsers[index], ...body }
    }

    return HttpResponse.json(createResponse(user, 'Perfil atualizado com sucesso'))
  }),

  // User Management endpoints (Admin only)
  http.get(`${BASE_URL}/users`, async ({ request }) => {
    // const check = checkPermission(request, 'admin')
    // if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('pagina') || '1')
    const pageSize = parseInt(url.searchParams.get('itensPorPagina') || '10')
    const search = url.searchParams.get('search')

    let users = [...mockUsers]

    if (search) {
      const lowerSearch = search.toLowerCase()
      users = users.filter(u =>
        u.nome.toLowerCase().includes(lowerSearch) ||
        u.email.toLowerCase().includes(lowerSearch)
      )
    }

    return HttpResponse.json(createPaginatedResponse(users, page, pageSize))
  }),

  http.post(`${BASE_URL}/users`, async ({ request }) => {
    const check = checkPermission(request, 'admin')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(500)
    const body = await request.json() as Partial<UsuarioDto>

    // Check if email already exists
    if (mockUsers.some(u => u.email === body.email)) {
      return HttpResponse.json(
        { success: false, message: 'E-mail já cadastrado' },
        { status: 400 }
      )
    }

    const newUser: UsuarioDto = {
      id: `${Date.now()}`,
      nome: body.nome || '',
      email: body.email || '',
      perfil: body.perfil || 'view',
      ativo: body.ativo ?? true,
      emailVerificado: true, // Auto-verify for admin created users
      parentesco: body.parentesco,
      criadoEm: new Date().toISOString(),
      atualizadoEm: new Date().toISOString(),
    }

    mockUsers.push(newUser)

    return HttpResponse.json(createResponse(newUser, 'Usuário criado com sucesso'))
  }),

  http.patch(`${BASE_URL}/users/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'admin')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const body = await request.json() as Partial<UsuarioDto>
    const index = mockUsers.findIndex(u => u.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { success: false, message: 'Usuário não encontrado' },
        { status: 404 }
      )
    }

    const updated = {
      ...mockUsers[index],
      ...body,
      updatedAt: new Date().toISOString(),
    }

    mockUsers[index] = updated

    return HttpResponse.json(createResponse(updated, 'Usuário atualizado com sucesso'))
  }),

  http.delete(`${BASE_URL}/users/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'admin')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const index = mockUsers.findIndex(u => u.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { success: false, message: 'Usuário não encontrado' },
        { status: 404 }
      )
    }

    // Prevent deleting yourself
    if (check.user?.id === id) {
      return HttpResponse.json(
        { success: false, message: 'Não é possível excluir o próprio usuário' },
        { status: 400 }
      )
    }

    mockUsers.splice(index, 1)

    return HttpResponse.json(createResponse(null, 'Usuário excluído com sucesso'))
  }),

  // Group and institution endpoints
  http.get(BASE_URL + '/grupos-carteiras', async () =>
    HttpResponse.json(createResponse([{ id: 'group-1', nome: 'Grupo principal', papel: 'Admin' }]))),
  http.post(BASE_URL + '/grupos-carteiras', async ({ request }) => {
    const body = await request.json() as { nome: string }
    return HttpResponse.json(createResponse({ id: 'group-created', nome: body.nome, papel: 'Admin' }))
  }),
  http.get(BASE_URL + '/grupos-carteiras/:groupId/membros', async () =>
    HttpResponse.json(createResponse([{ id: 'member-1', usuarioId: 'mock-admin', email: 'admin@investpro.com', nome: 'Admin', papel: 'Admin', ativo: true }]))),
  http.post(BASE_URL + '/grupos-carteiras/:groupId/convites', async () =>
    HttpResponse.json(createResponse({ emailEnviado: true, mensagem: 'O Supabase enviou um link de convite; ele expira em uma hora.' }))),
  http.get(BASE_URL + '/grupos-carteiras/convites-pendentes', async () =>
    HttpResponse.json(createResponse([]))),
  http.post(BASE_URL + '/grupos-carteiras/:groupId/convites/:invitationId/aceitar', async () =>
    HttpResponse.json(createResponse(true))),
  http.put(BASE_URL + '/grupos-carteiras/:groupId/membros/:memberId/papel', async () =>
    HttpResponse.json(createResponse(true))),
  http.delete(BASE_URL + '/grupos-carteiras/:groupId/membros/:memberId', async () =>
    HttpResponse.json(createResponse(true))),
  http.get(BASE_URL + '/grupos-carteiras/:groupId/titulares', ({ params }) =>
    HttpResponse.json(createResponse(mockHolders.filter(holder => holder.grupoId === params.groupId)))),
  http.post(BASE_URL + '/grupos-carteiras/:groupId/titulares', async ({ params, request }) => {
    const body = await request.json() as { nome: string; parentesco?: string; usuarioId?: string }
    const holder = { ...body, id: crypto.randomUUID(), grupoId: String(params.groupId) }
    mockHolders.push(holder)
    return HttpResponse.json(createResponse(holder))
  }),
  http.put(BASE_URL + '/grupos-carteiras/:groupId/titulares/:id', async ({ params, request }) => {
    const body = await request.json() as { nome: string; parentesco?: string; usuarioId?: string }
    const holder = mockHolders.find(item => item.id === params.id && item.grupoId === params.groupId)
    if (!holder) return new HttpResponse(null, { status: 404 })
    Object.assign(holder, body)
    return HttpResponse.json(createResponse(holder))
  }),
  http.get(BASE_URL + '/instituicoes-financeiras', async () =>
    HttpResponse.json(createResponse([
      { id: '10000000-0000-4000-8000-000000000001', nome: 'Banco do Brasil', categoria: 'Banco', personalizada: false }, { id: 'institution-itau', nome: 'Itaú', categoria: 'Banco', personalizada: false },
      { id: 'institution-xp', nome: 'XP', categoria: 'Corretora', personalizada: false }, { id: 'institution-clear', nome: 'Clear', categoria: 'Corretora', personalizada: false },
      { id: 'institution-btg', nome: 'BTG Pactual DTVM', categoria: 'DTVM', personalizada: false },
    ]))),

  // Portfolio aggregate endpoints must be registered before the id route.
  http.get(BASE_URL + '/portfolios/resumo-geral', async () => {
    const total = mockPortfolios.reduce((sum, item) => sum + item.valorTotal, 0)
    const invested = mockPortfolios.reduce((sum, item) => sum + item.totalInvestido, 0)
    const gain = mockPortfolios.reduce((sum, item) => sum + item.ganhoTotal, 0)
    return HttpResponse.json(createResponse({ valorTotal: total, totalInvestido: invested, ganhoTotal: gain, quantidadeCarteiras: mockPortfolios.length }))
  }),
  http.get(BASE_URL + '/portfolios/projecao-renda-fixa', async () =>
    HttpResponse.json(createResponse(mockFixedIncomeConsolidatedProjection))),

  // Portfolio endpoints
  http.get(`${BASE_URL}/portfolios`, async ({ request }) => {
    await delay(400)
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('pagina') || '1')
    const pageSize = parseInt(url.searchParams.get('itensPorPagina') || '10')

    return HttpResponse.json(createPaginatedResponse(mockPortfolios, page, pageSize))
  }),

  http.get(`${BASE_URL}/portfolios/:id`, async ({ params }) => {
    await delay(300)
    const { id } = params
    const portfolio = mockPortfolios.find(p => p.id === id)

    if (!portfolio) {
      return HttpResponse.json(
        { success: false, message: 'Portfolio não encontrado' },
        { status: 404 }
      )
    }

    return HttpResponse.json(createResponse(portfolio))
  }),

  http.get(`${BASE_URL}/portfolios/:id/summary`, async ({ params }) => {
    await delay(400)
    const { id } = params

    if (id !== 'portfolio-1') {
      return HttpResponse.json(
        { success: false, message: 'Portfolio não encontrado' },
        { status: 404 }
      )
    }

    return HttpResponse.json(createResponse(mockPortfolioSummary))
  }),

  http.get(`${BASE_URL}/portfolios/:id/history`, async () =>
    HttpResponse.json(createResponse([]))),

  http.post(`${BASE_URL}/portfolios`, async ({ request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(500)
    const body = await request.json() as CriarCarteiraRequest
    const now = new Date().toISOString()
    const newPortfolio: CarteiraDto = {
      id: `portfolio-${Date.now()}`,
      nome: body.nome,
      descricao: body.descricao,
      grupoId: body.grupoId,
      titular: mockHolders.find(item => item.id === body.titularId)?.nome ?? body.titular ?? 'Titular exemplo',
      titularId: body.titularId,
      parentesco: body.parentesco,
      instituicaoFinanceiraId: body.instituicaoFinanceiraId,
      titularVinculado: !!body.titularUsuarioId,
      tipoInstituicao: body.tipoInstituicao,
      instituicaoFinanceira: body.instituicaoFinanceira,
      visibilidade: body.visibilidade,
      posicoes: [],
      quantidadeAtivos: 0,
      valorTotal: 0,
      totalInvestido: 0,
      ganhoTotal: 0,
      percentualGanho: 0,
      moeda: 'BRL',
        criadoEm: now,
      atualizadoEm: now,
    }

    mockPortfolios.push(newPortfolio)

    return HttpResponse.json(createResponse(newPortfolio, 'Portfolio criado com sucesso'))
  }),

  http.patch(`${BASE_URL}/portfolios/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const body = await request.json() as AtualizarCarteiraRequest
    const index = mockPortfolios.findIndex(p => p.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { success: false, message: 'Portfolio não encontrado' },
        { status: 404 }
      )
    }

    const updated: CarteiraDto = {
      ...mockPortfolios[index],
      ...body,
      atualizadoEm: new Date().toISOString(),
    }

    mockPortfolios[index] = updated

    return HttpResponse.json(createResponse(updated, 'Portfolio atualizado com sucesso'))
  }),

  http.delete(`${BASE_URL}/portfolios/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const index = mockPortfolios.findIndex(p => p.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { success: false, message: 'Portfolio não encontrado' },
        { status: 404 }
      )
    }

    mockPortfolios.splice(index, 1)

    return HttpResponse.json(createResponse(null, 'Portfolio deletado com sucesso'))
  }),

  // Investment endpoints
  http.get(`${BASE_URL}/investments`, async ({ request }) => {
    await delay(400)
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('pagina') || '1')
    const pageSize = parseInt(url.searchParams.get('itensPorPagina') || '10')
    const type = url.searchParams.get('tipo')
    const search = url.searchParams.get('busca')
    const sortBy = url.searchParams.get('ordenarPor')
    const sortOrder = url.searchParams.get('ordem') || 'asc'
    const subtype = url.searchParams.get('subtipo')
    const issuer = url.searchParams.get('emissor')
    const sector = url.searchParams.get('setor')

    let investments: (RendaFixaDto | RendaVariavelDto)[] = [...mockAllInvestments]

    if (type === 'fixed_income') investments = [...mockFixedIncomeInvestments]
    if (type === 'variable_income') investments = [...mockVariableIncomeInvestments]
    if (subtype) investments = investments.filter(investment => investment.subtipo === subtype)
    if (issuer) investments = investments.filter(investment => investment.emissor?.toLowerCase().includes(issuer.toLowerCase()))
    if (sector) investments = investments.filter(investment => investment.setor?.toLowerCase().includes(sector.toLowerCase()))

    if (search) {
      const query = search.toLowerCase()
      investments = investments.filter(investment =>
        investment.nome.toLowerCase().includes(query) ||
        investment.ticker.toLowerCase().includes(query) ||
        investment.emissor?.toLowerCase().includes(query),
      )
    }

    if (sortBy) {
      const sortKey = sortBy as keyof PosicaoInvestimentoDto
      investments.sort((left, right) => {
        const leftValue = left[sortKey]
        const rightValue = right[sortKey]
        if (leftValue === rightValue) return 0
        if (leftValue == null) return 1
        if (rightValue == null) return -1

        const comparison = typeof leftValue === 'number' && typeof rightValue === 'number'
          ? leftValue - rightValue
          : String(leftValue).localeCompare(String(rightValue), 'pt-BR', { numeric: true, sensitivity: 'base' })
        return sortOrder === 'desc' ? -comparison : comparison
      })
    }

    return HttpResponse.json(createPaginatedResponse(investments, page, pageSize))
  }),

  http.get(`${BASE_URL}/portfolios/:portfolioId/investments`, async ({ params, request }) => {
    await delay(400)
    const { portfolioId } = params
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('pagina') || '1')
    const pageSize = parseInt(url.searchParams.get('itensPorPagina') || '10')

    const investments = mockAllInvestments.filter(investment => investment.carteiraId === portfolioId)
    return HttpResponse.json(createPaginatedResponse(investments, page, pageSize))
  }),

  // Summary MUST come before :id to avoid path collision
  http.get(`${BASE_URL}/investments/summary`, async () => {
    await delay(400)
    return HttpResponse.json(createResponse(mockInvestmentSummary))
  }),

  // Dividends
  http.get(`${BASE_URL}/investments/dividends`, async () => {
    await delay(400)
    return HttpResponse.json(createPaginatedResponse([]))
  }),

  // Transactions
  http.post(`${BASE_URL}/transactions`, async ({ request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    const body = await request.json() as RegistrarTransacaoRequest
    const previous = mockTransactionsByIdempotencyKey.get(body.chaveIdempotencia)
    if (previous) return HttpResponse.json(createResponse(previous))
    if (!mockPortfolios.some(portfolio => portfolio.id === body.carteiraId)) {
      return HttpResponse.json({ success: false, message: 'Carteira não encontrada.' }, { status: 404 })
    }

    const investment = mockAllInvestments.find(item => item.carteiraId === body.carteiraId && item.ticker?.toUpperCase() === body.ticker?.toUpperCase())
    if (body.tipo === 'Sell' && (!investment || investment.quantidade < body.quantidade)) {
      return HttpResponse.json({ success: false, message: 'Quantidade disponível insuficiente.' }, { status: 409 })
    }

    const transaction: TransacaoDto = {
      id: `transaction-${crypto.randomUUID()}`,
      carteiraId: body.carteiraId,
      ativoId: investment?.ativoId,
      ticker: body.ticker,
      tipo: body.tipo,
      quantidade: body.quantidade,
      precoUnitario: body.precoUnitario,
      taxas: body.taxas,
      valorTotal: body.quantidade * body.precoUnitario + (body.tipo === 'Buy' ? body.taxas : -body.taxas),
      ganhoRealizado: 0,
      custoBaseRealizado: 0,
      modalidadeFiscal: body.modalidadeFiscal ?? "NaoInformada",
      dataTransacao: body.dataTransacao,
      observacoes: body.observacoes,
    }
    mockTransactionsByIdempotencyKey.set(body.chaveIdempotencia, transaction)
    await delay(100)
    return HttpResponse.json(createResponse(transaction), { status: 201 })
  }),

  http.get(`${BASE_URL}/benchmarks/cdi`, ({ request }) => {
    const query = new URL(request.url).searchParams
    return HttpResponse.json(createResponse({
      dataDe: query.get('dataDe') ?? '2026-09-01',
      dataAte: query.get('dataAte') ?? '2026-09-29',
      origem: 'Banco Central do Brasil — SGS série 12 (CDI, percentual ao dia)',
      atualizadoEmUtc: '2026-09-29T12:00:00.000Z',
      pontos: [
        { data: '2026-09-01', taxaDiariaPercentual: 0.055, indiceBase100: 100.055 },
        { data: '2026-09-15', taxaDiariaPercentual: 0.055, indiceBase100: 100.82 },
      ],
    }))
  }),

  http.get(`${BASE_URL}/investments/:id/transactions`, async ({ params }) => {
    await delay(400)
    const { id } = params
    const investment = mockAllInvestments.find(item => item.id === id)
    const mockTransactions: TransacaoDto[] = [
      {
        id: 'transaction-1',
        carteiraId: investment?.carteiraId ?? 'portfolio-1',
        ativoId: investment?.ativoId,
        ticker: investment?.ticker,
        tipo: 'Buy',
        quantidade: 50,
        precoUnitario: 37.80,
        taxas: 0,
        valorTotal: 1890,
        ganhoRealizado: 0,
        custoBaseRealizado: 0,
        modalidadeFiscal: "NaoInformada",
        dataTransacao: '2024-12-10T12:00:00.000Z',
      },
      {
        id: 'transaction-2',
        carteiraId: investment?.carteiraId ?? 'portfolio-1',
        ativoId: investment?.ativoId,
        ticker: investment?.ticker,
        tipo: 'Sell',
        quantidade: 10,
        precoUnitario: 38.50,
        taxas: 0,
        valorTotal: 385,
        ganhoRealizado: 0,
        custoBaseRealizado: 0,
        modalidadeFiscal: "NaoInformada",
        dataTransacao: '2024-11-05T12:00:00.000Z',
      },
    ]
    return HttpResponse.json(createPaginatedResponse(mockTransactions))
  }),

  http.get(`${BASE_URL}/investments/:id/history`, async () => HttpResponse.json(createResponse([
    { data: '2026-09-01T00:00:00.000Z', preco: 1000, origem: 'statement', ajustado: false },
    { data: '2026-09-15T00:00:00.000Z', preco: 1025, origem: 'statement', ajustado: false },
  ]))),

  http.get(`${BASE_URL}/investments/:id/projecao-renda-fixa`, ({ params }) => {
    const asset = mockFixedIncomeInvestments.find(item => item.id === params.id)
    if (!asset) return new HttpResponse(null, { status: 404 })
    const projection: ProjecaoRendaFixaDto = {
      posicaoId: asset.id, ticker: asset.ticker, valorObservado: asset.valorAtual, observadoEmUtc: asset.atualizadoEm,
      valorProjetadoBruto: null, dataVencimento: asset.dataVencimento ?? new Date().toISOString().slice(0, 10), estadoProjecao: 'Indisponivel',
      motivo: asset.situacao === 'matured' ? 'Título vencido; a posição permanece registrada e não movimenta o caixa.' : 'A taxa de referência do grupo precisa ser atualizada para liberar a estimativa.',
    }
    return HttpResponse.json(createResponse(projection))
  }),
  http.get(`${BASE_URL}/investments/:id`, async ({ params }) => {
    await delay(300)
    const { id } = params
    const investment = mockAllInvestments.find(inv => inv.id === id)

    if (!investment) {
      return HttpResponse.json(
    { sucesso: false, mensagem: 'Investimento não encontrado' },
        { status: 404 }
      )
    }

    return HttpResponse.json(createResponse(investment))
  }),

  http.post(`${BASE_URL}/investments/fixed-income`, async ({ request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(500)
    const body = await request.json() as CriarRendaFixaRequest
    if (new Date(body.dataCompra) > new Date()) {
      return HttpResponse.json({ sucesso: false, mensagem: 'A data de compra não pode estar no futuro.' }, { status: 400 })
    }
    const existing = mockFixedIncomeInvestments.find(
      investment => investment.id === `fixed-${body.chaveIdempotencia}`,
    )
    if (existing) return HttpResponse.json(createResponse(existing))

    const now = new Date().toISOString()
    const currentPrice = body.valorPrincipal > 0 ? body.valorExtrato / body.valorPrincipal : 0
    const gain = body.valorExtrato - body.valorPrincipal
    const newInvestment: RendaFixaDto = {
      id: `fixed-${body.chaveIdempotencia}`,
      ativoId: `asset-fixed-${body.chaveIdempotencia}`,
      situacao: 'open',
      carteiraId: body.carteiraId,
      ticker: `RF-${body.chaveIdempotencia.replace(/-/g, '').slice(0, 12).toUpperCase()}`,
      nome: body.nome,
      tipo: 'fixed_income',
      subtipo: body.subtipo,
      emissor: body.emissor,
      quantidade: body.valorPrincipal,
      precoMedio: 1,
      precoAtual: currentPrice,
      totalInvestido: body.valorPrincipal,
      valorAtual: body.valorExtrato,
      ganho: gain,
      percentualGanho: body.valorPrincipal > 0 ? gain / body.valorPrincipal * 100 : 0,
      moeda: 'BRL',
      taxaJuros: body.taxaJuros,
      indexador: body.indexador,
      dataCompra: body.dataCompra,
      dataVencimento: body.dataVencimento,
      criadoEm: now,
      atualizadoEm: now,
    }

    mockFixedIncomeInvestments.push(newInvestment)
    mockAllInvestments.push(newInvestment)

    return HttpResponse.json(createResponse(newInvestment, 'Investimento criado com sucesso'))
  }),

  http.post(`${BASE_URL}/investments/variable-income`, async ({ request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(500)
    const body = await request.json() as CriarRendaVariavelRequest
    const existing = mockVariableIncomeInvestments.find(
      investment => investment.id === `var-${body.chaveIdempotencia}`,
    )
    if (existing) return HttpResponse.json(createResponse(existing))

    const now = new Date().toISOString()
    const totalInvested = body.quantidade * body.precoUnitario + body.taxas
    const currentValue = body.quantidade * body.precoUnitario
    const gain = currentValue - totalInvested
    const newInvestment: RendaVariavelDto = {
      id: `var-${body.chaveIdempotencia}`,
      ativoId: `asset-var-${body.chaveIdempotencia}`,
      situacao: 'open',
      carteiraId: body.carteiraId,
      ticker: body.ticker,
      nome: body.nome ?? body.ticker,
      tipo: 'variable_income',
      subtipo: body.subtipo,
      setor: body.setor,
      quantidade: body.quantidade,
      precoMedio: body.precoUnitario,
      precoAtual: body.precoUnitario,
      totalInvestido: totalInvested,
      valorAtual: currentValue,
      ganho: gain,
      percentualGanho: totalInvested > 0 ? gain / totalInvested * 100 : 0,
      moeda: 'BRL',
      criadoEm: now,
      atualizadoEm: now,
    }

    mockVariableIncomeInvestments.push(newInvestment)
    mockAllInvestments.push(newInvestment)

    return HttpResponse.json(createResponse(newInvestment, 'Investimento criado com sucesso'))
  }),

  http.patch(`${BASE_URL}/investments/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const body = await request.json() as Partial<Omit<PosicaoInvestimentoDto, 'id' | 'ativoId' | 'tipo' | 'subtipo'>>
    const index = mockAllInvestments.findIndex(inv => inv.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { sucesso: false, mensagem: 'Investimento não encontrado' },
        { status: 404 }
      )
    }

    const existing = mockAllInvestments[index]
    const updated = { ...existing, ...body, atualizadoEm: new Date().toISOString() }
    mockAllInvestments[index] = updated

    // Also update in specific lists
    const fixedIndex = mockFixedIncomeInvestments.findIndex(inv => inv.id === id)
    if (updated.tipo === 'fixed_income' && fixedIndex !== -1) mockFixedIncomeInvestments[fixedIndex] = updated as RendaFixaDto

    const variableIndex = mockVariableIncomeInvestments.findIndex(inv => inv.id === id)
    if (updated.tipo === 'variable_income' && variableIndex !== -1) mockVariableIncomeInvestments[variableIndex] = updated as RendaVariavelDto


    return HttpResponse.json(createResponse(updated, 'Investimento atualizado com sucesso'))
  }),

  http.delete(`${BASE_URL}/investments/:id`, async ({ params, request }) => {
    const check = checkPermission(request, 'edit')
    if (!check.authorized) return HttpResponse.json({ message: check.message }, { status: check.status })

    await delay(400)
    const { id } = params
    const index = mockAllInvestments.findIndex(inv => inv.id === id)

    if (index === -1) {
      return HttpResponse.json(
        { success: false, message: 'Investimento não encontrado' },
        { status: 404 }
      )
    }

    mockAllInvestments.splice(index, 1)

    // Remove from specific lists
    const fixedIndex = mockFixedIncomeInvestments.findIndex(inv => inv.id === id)
    if (fixedIndex !== -1) mockFixedIncomeInvestments.splice(fixedIndex, 1)

    const variableIndex = mockVariableIncomeInvestments.findIndex(inv => inv.id === id)
    if (variableIndex !== -1) mockVariableIncomeInvestments.splice(variableIndex, 1)

    return HttpResponse.json(createResponse(null, 'Investimento deletado com sucesso'))
  }),

  // Chat Endpoint
  http.post(`${BASE_URL}/chat`, async ({ request }) => {
    await delay(1000)
    const body = await request.json() as { message: string, userId: string }

    return HttpResponse.json({
      message: `Olá! Recebi sua mensagem: "${body.message}". Como sou uma IA simulada, não posso processar solicitações reais ainda, mas estou aqui para ajudar!`
    })
  }),

  // Group-scoped economic rates endpoints with the same Portuguese DTO contract as the API.
  http.get(`${BASE_URL}/taxes`, ({ request }) => {
    const groupId = new URL(request.url).searchParams.get('grupoId')
    return HttpResponse.json(createResponse(mockEconomicRates.filter(rate => rate.grupoId === groupId)))
  }),
  http.get(`${BASE_URL}/taxes/:id/historico`, ({ params, request }) => {
    const groupId = new URL(request.url).searchParams.get('grupoId')
    const rate = mockEconomicRates.find(item => item.id === params.id && item.grupoId === groupId)
    if (!rate) return new HttpResponse(null, { status: 404 })
    return HttpResponse.json(createResponse(mockEconomicRateHistory.get(rate.id) ?? []))
  }),
  http.get(`${BASE_URL}/taxes/:id`, ({ params, request }) => {
    const groupId = new URL(request.url).searchParams.get('grupoId')
    const rate = mockEconomicRates.find(item => item.id === params.id && item.grupoId === groupId)
    return rate ? HttpResponse.json(createResponse(rate)) : new HttpResponse(null, { status: 404 })
  }),
  http.post(`${BASE_URL}/taxes`, async ({ request }) => {
    const body = await request.json() as Omit<TaxaEconomicaDto, 'id' | 'grupoId' | 'variacao' | 'atualizadoEm' | 'atualizadoPorUserId'>
    const now = new Date().toISOString()
    const rate: TaxaEconomicaDto = {
      ...body, id: crypto.randomUUID(), grupoId: new URL(request.url).searchParams.get('grupoId') ?? '',
      variacao: body.valorAtual - body.valorAnterior, atualizadoEm: now, atualizadoPorUserId: activeMockUserId ?? 'user-1',
    }
    mockEconomicRates.push(rate)
    return HttpResponse.json(createResponse(rate, 'Taxa adicionada com sucesso'))
  }),
  http.put(`${BASE_URL}/taxes/:id`, async ({ params, request }) => {
    const groupId = new URL(request.url).searchParams.get('grupoId')
    const rate = mockEconomicRates.find(item => item.id === params.id && item.grupoId === groupId)
    if (!rate) return new HttpResponse(null, { status: 404 })
    const body = await request.json() as Omit<TaxaEconomicaDto, 'id' | 'grupoId' | 'variacao' | 'atualizadoEm' | 'atualizadoPorUserId'>
    const now = new Date().toISOString()
    const history = mockEconomicRateHistory.get(rate.id) ?? []
    history.unshift({
      valorAnterior: rate.valorAtual, valorNovo: body.valorAtual, unidadeAnterior: rate.unidade, unidadeNova: body.unidade,
      periodicidadeAnterior: rate.periodicidade, periodicidadeNova: body.periodicidade, origem: body.origem,
      dataReferencia: body.dataReferencia, responsavelUserId: activeMockUserId ?? rate.atualizadoPorUserId, atualizadoEmUtc: now,
    })
    mockEconomicRateHistory.set(rate.id, history)
    Object.assign(rate, body, { variacao: body.valorAtual - body.valorAnterior, atualizadoEm: now, atualizadoPorUserId: activeMockUserId ?? rate.atualizadoPorUserId })
    return HttpResponse.json(createResponse(rate, 'Taxa atualizada com sucesso'))
  }),
  http.delete(`${BASE_URL}/taxes/:id`, ({ params, request }) => {
    const groupId = new URL(request.url).searchParams.get('grupoId')
    const index = mockEconomicRates.findIndex(item => item.id === params.id && item.grupoId === groupId)
    if (index < 0) return new HttpResponse(null, { status: 404 })
    mockEconomicRates.splice(index, 1)
    mockEconomicRateHistory.delete(String(params.id))
    return HttpResponse.json(createResponse(null, 'Taxa removida com sucesso'))
  }),
  // Simulation endpoints
  http.post(`${BASE_URL}/simulation`, async ({ request }) => {
    await delay(500)
    const body = await request.json() as {
      valorInicial: number
      aporteMensal: number
      anos: number
      taxaJurosAnual: number
      estrategia?: string
    }

    if (body.valorInicial < 0 || body.aporteMensal < 0 || body.anos < 1) {
      return HttpResponse.json({
        type: 'https://httpstatuses.io/400',
        title: 'Bad request.',
        detail: 'Parâmetros de simulação inválidos.',
        status: 400,
      }, { status: 400 })
    }

    const totalMonths = body.anos * 12
    const monthlyRate = Math.pow(1 + body.taxaJurosAnual / 100, 1 / 12) - 1
    const pontos: Array<{ mes: number; investido: number; total: number; juros: number }> = []

    let currentAmount = body.valorInicial
    let totalInvested = body.valorInicial

    for (let i = 0; i <= totalMonths; i++) {
      pontos.push({
        mes: i,
        investido: Number(totalInvested.toFixed(2)),
        total: Number(currentAmount.toFixed(2)),
        juros: Number((currentAmount - totalInvested).toFixed(2)),
      })
      if (i < totalMonths) {
        currentAmount = currentAmount * (1 + monthlyRate) + body.aporteMensal
        totalInvested += body.aporteMensal
      }
    }

    return HttpResponse.json(createResponse({
      pontos,
      valorFinal: pontos[pontos.length - 1].total,
      totalInvestido: pontos[pontos.length - 1].investido,
      totalJuros: pontos[pontos.length - 1].juros,
      nomeEstrategia: body.estrategia === 'montecarlo' ? 'Estatístico (Monte Carlo)' : 'Matemático (Determinístico)',
    }))
  }),

  http.get(`${BASE_URL}/simulation/strategies`, async () => {
    await delay(200)
    return HttpResponse.json(createResponse([
      { id: 'deterministic', nome: 'Matemático (Determinístico)', descricao: 'Simulação baseada em juros compostos com taxa fixa' },
      { id: 'montecarlo', nome: 'Estatístico (Monte Carlo)', descricao: 'Simulação probabilística com volatilidade' },
    ]))
  }),
]
