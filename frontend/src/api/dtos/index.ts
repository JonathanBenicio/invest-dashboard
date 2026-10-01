// Base DTOs
export type {
  RespostaApi,
  RespostaPaginada,
  ParametrosPaginacao,
  EntidadeBase,
  RespostaErro,
} from './base.dto'

// Auth DTOs
export type {
  SolicitacaoLogin,
  SolicitacaoCadastro,
  UsuarioAutenticadoDto,
  SessaoAutenticacaoDto,
  UsuarioDto,
  RespostaAutenticacao,
  RespostaToken,
  SolicitacaoRedefinicaoSenha,
  SolicitacaoAlteracaoSenha,
} from './auth.dto'

// Portfolio DTOs
export type {
  CarteiraDto,
  ResumoCarteiraDto,
  AlocacaoAtivoDto,
  CriarCarteiraRequest,
  AtualizarCarteiraRequest,
  CarteiraFiltros,
  PontoHistoricoCarteiraDto,
  GrupoCarteirasDto,
  MembroGrupoDto,
  ConvitePendenteGrupoDto,
  ResultadoConviteGrupoDto,
  InstituicaoFinanceiraDto,
  ResumoCarteirasDto,
  ProjecaoRendaFixaDto,
  ProjecaoRendaFixaConsolidadaDto,
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
  PrecoHistoricoDto,
  ProventoDto,
} from './investment.dto'

// Brapi DTOs
export type {
  BrapiQuote,
  BrapiQuoteResponse,
  BrapiAvailableResponse,
  BrapiHistoricalData,
  BrapiHistoricalResponse,
} from './brapi.dto'

export type { CotacaoMercadoDto, ResultadoBuscaMercadoDto, PontoHistoricoMercadoDto } from './market-data.dto'
export type { PontoBenchmarkCdiDto, SerieBenchmarkCdiDto } from './benchmark.dto'

// Taxes DTOs
export type {
  TaxaEconomicaDto,
  TaxaEconomicaHistoricoDto,
  CriarTaxaEconomicaRequest,
  AtualizarTaxaEconomicaRequest,
  EstimativaImpostoMensalDto,
  CategoriaImpostoEstimadoDto,
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
