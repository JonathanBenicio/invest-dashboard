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
  PortfolioDto,
  PortfolioSummaryDto,
  AssetAllocationDto,
  PerformancePointDto,
  CreatePortfolioRequest,
  UpdatePortfolioRequest,
  PortfolioFilters,
} from './portfolio.dto'

// Investment DTOs
export type {
  InvestmentType,
  FixedIncomeType,
  VariableIncomeType,
  InvestmentDto,
  FixedIncomeDto,
  VariableIncomeDto,
  CreateFixedIncomeRequest,
  CreateVariableIncomeRequest,
  UpdateInvestmentRequest,
  InvestmentFilters,
  InvestmentSummaryDto,
} from './investment.dto'
