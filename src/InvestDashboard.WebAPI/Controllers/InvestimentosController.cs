using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.DTOs.Trading;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/investments")]
    public class InvestimentosController : ControllerBase
    {
        private readonly ICarteiraAppService _carteiraAppService;
        private readonly ITransacaoAppService _transacaoAppService;

        public InvestimentosController(
            ICarteiraAppService carteiraAppService,
            ITransacaoAppService transacaoAppService)
        {
            _carteiraAppService = carteiraAppService;
            _transacaoAppService = transacaoAppService;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<PosicaoInvestimentoDto>>> GetAll(
            [FromQuery] string? type,
            [FromQuery] string? subtype,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var carteira = await _carteiraAppService.GetUserPortfolioAsync();
            var list = new List<PosicaoInvestimentoDto>();

            if (carteira != null)
            {
                list.AddRange(carteira.Positions);
            }
            else
            {
                list = GetSimulatedPositions();
            }

            if (!string.IsNullOrEmpty(type))
            {
                list = list.Where(p => p.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (!string.IsNullOrEmpty(subtype))
            {
                list = list.Where(p => p.Subtype.Equals(subtype, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (!string.IsNullOrEmpty(search))
            {
                list = list.Where(p => p.Ticker.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                       p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var paginated = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Ok(new PaginatedResponse<PosicaoInvestimentoDto>(paginated, page, pageSize, list.Count));
        }

        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<object>>> GetSummary()
        {
            var carteira = await _carteiraAppService.GetUserPortfolioAsync();
            decimal totalInvested = 0;
            decimal currentValue = 0;
            decimal totalGain = 0;
            decimal gainPercentage = 0;
            decimal fixedIncomeTotal = 0;
            decimal variableIncomeTotal = 0;
            List<PosicaoInvestimentoDto> positions;

            if (carteira != null)
            {
                totalInvested = carteira.TotalInvested;
                currentValue = carteira.TotalValue;
                totalGain = carteira.TotalGain;
                gainPercentage = carteira.GainPercentage;
                fixedIncomeTotal = carteira.Positions.Where(p => p.Type == "fixed_income").Sum(p => p.CurrentValue);
                variableIncomeTotal = carteira.Positions.Where(p => p.Type == "variable_income").Sum(p => p.CurrentValue);
                positions = carteira.Positions.ToList();
            }
            else
            {
                positions = GetSimulatedPositions();
                totalInvested = positions.Sum(p => p.TotalInvested);
                currentValue = positions.Sum(p => p.CurrentValue);
                totalGain = currentValue - totalInvested;
                gainPercentage = totalInvested > 0 ? (totalGain / totalInvested) * 100 : 0;
                fixedIncomeTotal = positions.Where(p => p.Type == "fixed_income").Sum(p => p.CurrentValue);
                variableIncomeTotal = positions.Where(p => p.Type == "variable_income").Sum(p => p.CurrentValue);
            }

            var topPerformers = positions.OrderByDescending(p => p.GainPercentage).Take(3).ToList();
            var worstPerformers = positions.OrderBy(p => p.GainPercentage).Take(3).ToList();

            var summary = new
            {
                TotalInvested = totalInvested,
                CurrentValue = currentValue,
                TotalGain = totalGain,
                GainPercentage = gainPercentage,
                FixedIncomeTotal = fixedIncomeTotal,
                VariableIncomeTotal = variableIncomeTotal,
                TopPerformers = topPerformers,
                WorstPerformers = worstPerformers
            };

            return Ok(new ApiResponse<object>(summary));
        }

        [HttpGet("dividends")]
        public async Task<ActionResult<PaginatedResponse<object>>> GetDividends(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Simulated Dividends history matching standard list
            var list = new List<object>
            {
                new { Id = Guid.NewGuid(), Ticker = "PETR4", Name = "Petrobras", Amount = 145.20m, Type = "Dividendo", PaymentDate = DateTime.UtcNow.AddDays(-5) },
                new { Id = Guid.NewGuid(), Ticker = "VALE3", Name = "Vale S.A.", Amount = 98.40m, Type = "JCP", PaymentDate = DateTime.UtcNow.AddDays(-12) },
                new { Id = Guid.NewGuid(), Ticker = "ITUB4", Name = "Itaú Unibanco", Amount = 42.10m, Type = "Dividendo", PaymentDate = DateTime.UtcNow.AddDays(-25) },
                new { Id = Guid.NewGuid(), Ticker = "MXRF11", Name = "Maxi Renda FII", Amount = 72.00m, Type = "Rendimento", PaymentDate = DateTime.UtcNow.AddDays(-30) },
                new { Id = Guid.NewGuid(), Ticker = "PETR4", Name = "Petrobras", Amount = 125.50m, Type = "Dividendo", PaymentDate = DateTime.UtcNow.AddMonths(-2) }
            };

            var paginated = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Ok(new PaginatedResponse<object>(paginated, page, pageSize, list.Count));
        }

        [HttpGet("{id:guid}/transactions")]
        public async Task<ActionResult<PaginatedResponse<TransacaoDto>>> GetTransactions(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var carteira = await _carteiraAppService.GetUserPortfolioAsync();
            if (carteira == null)
            {
                return Ok(new PaginatedResponse<TransacaoDto>(new List<TransacaoDto>(), page, pageSize, 0));
            }

            var position = carteira.Positions.FirstOrDefault(p => p.Id == id || p.AtivoId == id);
            if (position == null)
            {
                return Ok(new PaginatedResponse<TransacaoDto>(new List<TransacaoDto>(), page, pageSize, 0));
            }

            var transactions = await _transacaoAppService.GetTransactionsByPortfolioIdAsync(carteira.Id);
            var filtered = transactions.Where(t => t.AtivoId == position.AtivoId).ToList();

            var paginated = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Ok(new PaginatedResponse<TransacaoDto>(paginated, page, pageSize, filtered.Count));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<PosicaoInvestimentoDto>>> GetById(Guid id)
        {
            var carteira = await _carteiraAppService.GetUserPortfolioAsync();
            if (carteira == null)
            {
                var simulated = GetSimulatedPositions().FirstOrDefault(p => p.Id == id);
                if (simulated != null) return Ok(new ApiResponse<PosicaoInvestimentoDto>(simulated));
                return NotFound(new ApiResponse<PosicaoInvestimentoDto>(null!, false, $"Investment with ID {id} was not found."));
            }

            var position = carteira.Positions.FirstOrDefault(p => p.Id == id || p.AtivoId == id);
            if (position == null)
            {
                return NotFound(new ApiResponse<PosicaoInvestimentoDto>(null!, false, $"Investment with ID {id} was not found."));
            }

            return Ok(new ApiResponse<PosicaoInvestimentoDto>(position));
        }

        [HttpPost("fixed-income")]
        public async Task<ActionResult<ApiResponse<PosicaoInvestimentoDto>>> CreateFixedIncome([FromBody] CriarRendaFixaDto dto)
        {
            try
            {
                var regDto = new RegistrarTransacaoDto
                {
                    CarteiraId = dto.CarteiraId,
                    Ticker = dto.Name,
                    Type = "Buy",
                    Quantity = dto.Quantity,
                    UnitPrice = dto.AveragePrice,
                    BrokerageFee = 0,
                    TransactionDate = DateTime.TryParse(dto.PurchaseDate, out var dt) ? dt : DateTime.UtcNow,
                    Notes = $"Issuer: {dto.Issuer}, Rate: {dto.InterestRate}%, Indexer: {dto.Indexer}"
                };

                var transaction = await _transacaoAppService.RegisterTransactionAsync(regDto);
                
                var carteira = await _carteiraAppService.GetUserPortfolioAsync();
                var position = carteira?.Positions.FirstOrDefault(p => p.Ticker == dto.Name);

                return Ok(new ApiResponse<PosicaoInvestimentoDto>(position ?? new PosicaoInvestimentoDto
                {
                    Id = Guid.NewGuid(),
                    CarteiraId = dto.CarteiraId,
                    Name = dto.Name,
                    Ticker = dto.Name,
                    Type = "fixed_income",
                    Subtype = dto.Subtype,
                    Quantity = dto.Quantity,
                    AveragePrice = dto.AveragePrice,
                    CurrentPrice = dto.AveragePrice,
                    TotalInvested = dto.Quantity * dto.AveragePrice,
                    CurrentValue = dto.Quantity * dto.AveragePrice,
                    Gain = 0,
                    GainPercentage = 0,
                    Currency = "BRL"
                }));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<PosicaoInvestimentoDto>(null!, false, ex.Message));
            }
        }

        [HttpPost("variable-income")]
        public async Task<ActionResult<ApiResponse<PosicaoInvestimentoDto>>> CreateVariableIncome([FromBody] CriarRendaVariavelDto dto)
        {
            try
            {
                var regDto = new RegistrarTransacaoDto
                {
                    CarteiraId = dto.CarteiraId,
                    Ticker = dto.Ticker,
                    Type = "Buy",
                    Quantity = dto.Quantity,
                    UnitPrice = dto.AveragePrice,
                    BrokerageFee = 0,
                    TransactionDate = DateTime.TryParse(dto.PurchaseDate, out var dt) ? dt : DateTime.UtcNow,
                    Notes = "Registered via Variable Income UI"
                };

                var transaction = await _transacaoAppService.RegisterTransactionAsync(regDto);

                var carteira = await _carteiraAppService.GetUserPortfolioAsync();
                var position = carteira?.Positions.FirstOrDefault(p => p.Ticker.Equals(dto.Ticker, StringComparison.OrdinalIgnoreCase));

                return Ok(new ApiResponse<PosicaoInvestimentoDto>(position ?? new PosicaoInvestimentoDto
                {
                    Id = Guid.NewGuid(),
                    CarteiraId = dto.CarteiraId,
                    Name = dto.Ticker,
                    Ticker = dto.Ticker,
                    Type = "variable_income",
                    Subtype = dto.Subtype,
                    Quantity = dto.Quantity,
                    AveragePrice = dto.AveragePrice,
                    CurrentPrice = dto.AveragePrice,
                    TotalInvested = dto.Quantity * dto.AveragePrice,
                    CurrentValue = dto.Quantity * dto.AveragePrice,
                    Gain = 0,
                    GainPercentage = 0,
                    Currency = "BRL"
                }));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<PosicaoInvestimentoDto>(null!, false, ex.Message));
            }
        }

        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<ApiResponse<PosicaoInvestimentoDto>>> Update(Guid id, [FromBody] AtualizarInvestimentoDto dto)
        {
            var carteira = await _carteiraAppService.GetUserPortfolioAsync();
            if (carteira == null)
            {
                return Ok(new ApiResponse<PosicaoInvestimentoDto>(null!, true, "Investimento atualizado (simulação)."));
            }

            var position = carteira.Positions.FirstOrDefault(p => p.Id == id || p.AtivoId == id);
            if (position == null)
            {
                return NotFound(new ApiResponse<PosicaoInvestimentoDto>(null!, false, "Investimento não encontrado."));
            }

            if (dto.CurrentPrice.HasValue)
            {
                position.CurrentPrice = dto.CurrentPrice.Value;
                position.CurrentValue = position.Quantity * position.CurrentPrice;
                position.Gain = position.CurrentValue - position.TotalInvested;
                position.GainPercentage = position.TotalInvested > 0 ? (position.Gain / position.TotalInvested) * 100 : 0;
            }

            return Ok(new ApiResponse<PosicaoInvestimentoDto>(position));
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
        {
            try
            {
                var success = await _carteiraAppService.DeleteInvestmentAsync(id);
                if (!success)
                {
                    return NotFound(new ApiResponse<object>(null!, false, "Investimento não encontrado ou não pertence ao usuário."));
                }

                return Ok(new ApiResponse<object>(null!, true, "Investimento removido e saldo recomposto com sucesso."));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(null!, false, ex.Message));
            }
        }

        private List<PosicaoInvestimentoDto> GetSimulatedPositions()
        {
            return new List<PosicaoInvestimentoDto>
            {
                new()
                {
                    Id = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f62"),
                    CarteiraId = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f61"),
                    AtivoId = Guid.Parse("aa4c4dbb-cbf4-4f0f-8c3b-3129486c4761"),
                    Name = "PETR4 Equity",
                    Ticker = "PETR4",
                    Type = "variable_income",
                    Subtype = "ACAO",
                    Quantity = 200,
                    AveragePrice = 32.50m,
                    CurrentPrice = 38.45m,
                    TotalInvested = 6500.00m,
                    CurrentValue = 7690.00m,
                    Gain = 1190.00m,
                    GainPercentage = 18.31m,
                    Currency = "BRL"
                },
                new()
                {
                    Id = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f63"),
                    CarteiraId = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f61"),
                    AtivoId = Guid.Parse("aa4c4dbb-cbf4-4f0f-8c3b-3129486c4762"),
                    Name = "VALE3 Equity",
                    Ticker = "VALE3",
                    Type = "variable_income",
                    Subtype = "ACAO",
                    Quantity = 100,
                    AveragePrice = 68.20m,
                    CurrentPrice = 72.10m,
                    TotalInvested = 6820.00m,
                    CurrentValue = 7210.00m,
                    Gain = 390.00m,
                    GainPercentage = 5.71m,
                    Currency = "BRL"
                },
                new()
                {
                    Id = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f64"),
                    CarteiraId = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f61"),
                    AtivoId = Guid.Parse("aa4c4dbb-cbf4-4f0f-8c3b-3129486c4763"),
                    Name = "Maxi Renda FII",
                    Ticker = "MXRF11",
                    Type = "variable_income",
                    Subtype = "FII",
                    Quantity = 500,
                    AveragePrice = 10.15m,
                    CurrentPrice = 10.65m,
                    TotalInvested = 5075.00m,
                    CurrentValue = 5325.00m,
                    Gain = 250.00m,
                    GainPercentage = 4.92m,
                    Currency = "BRL"
                },
                new()
                {
                    Id = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f65"),
                    CarteiraId = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f61"),
                    AtivoId = Guid.Parse("aa4c4dbb-cbf4-4f0f-8c3b-3129486c4764"),
                    Name = "CDB Banco Inter 110% CDI",
                    Ticker = "CDB INTER",
                    Type = "fixed_income",
                    Subtype = "CDB",
                    Quantity = 1,
                    AveragePrice = 15000.00m,
                    CurrentPrice = 15450.00m,
                    TotalInvested = 15000.00m,
                    CurrentValue = 15450.00m,
                    Gain = 450.00m,
                    GainPercentage = 3.00m,
                    Currency = "BRL"
                }
            };
        }
    }

    // Dynamic simple DTO structures for incoming request mappings
    public class CriarRendaFixaDto
    {
        public Guid CarteiraId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subtype { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal InterestRate { get; set; }
        public string? Indexer { get; set; }
        public string MaturityDate { get; set; } = string.Empty;
        public string PurchaseDate { get; set; } = string.Empty;
    }

    public class CriarRendaVariavelDto
    {
        public Guid CarteiraId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string Subtype { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public string PurchaseDate { get; set; } = string.Empty;
    }

    public class AtualizarInvestimentoDto
    {
        public decimal? Quantity { get; set; }
        public decimal? AveragePrice { get; set; }
        public decimal? CurrentPrice { get; set; }
    }
}
