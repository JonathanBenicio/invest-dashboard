// Base DTOs
export type {
  ApiResponse,
  PaginatedResponse,
  PaginationParams,
  BaseEntity,
  ErrorResponse,
} from './base.dto'

// Auth DTOs
export type {
  LoginRequest,
  RegisterRequest,
  UserDto,
  AuthResponse,
  TokenResponse,
  PasswordResetRequest,
  PasswordChangeRequest,
} from './auth.dto'

// Portfolio DTOs
export type {
  CarteiraDto,
  ResumoCarteiraDto,
  AlocacaoAtivoDto,
  PontoPerformanceDto,
  CriarCarteiraRequest,
  AtualizarCarteiraRequest,
  CarteiraFiltros,
} from './portfolio.dto'

// Investment DTOs
export type {
  TipoInvestimento,
  TipoRendaFixa,
  TipoRendaVariavel,
  PosicaoInvestimentoDto,
  RendaFixaDto,
  RendaVariavelDto,
  CriarRendaFixaRequest,
  CriarRendaVariavelRequest,
  AtualizarInvestimentoRequest,
  InvestimentoFiltros,
  ResumoInvestimentoDto,
} from './investment.dto'

// Brapi DTOs
export type {
  BrapiQuote,
  BrapiQuoteResponse,
  BrapiAvailableResponse,
  BrapiHistoricalData,
  BrapiHistoricalResponse,
} from './brapi.dto'

// Taxes DTOs
export type {
  TaxaEconomicaDto,
  CriarTaxaEconomicaRequest,
  AtualizarTaxaEconomicaRequest,
} from './taxes.dto'

// Simulation DTOs
export type {
  SimulacaoRequest,
  SimulacaoPontoDto,
  SimulacaoResponse,
  SimulacaoEstrategia,
} from './simulation.dto'

// Transaction DTOs
export type {
  TransacaoDto,
  RegistrarTransacaoRequest,
  TransacaoFiltros,
} from './transacao.dto'
