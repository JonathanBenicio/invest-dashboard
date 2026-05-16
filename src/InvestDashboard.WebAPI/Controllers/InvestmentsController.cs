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
    public class InvestmentsController : ControllerBase
    {
        private readonly IPortfolioAppService _portfolioAppService;
        private readonly ITransactionAppService _transactionAppService;

        public InvestmentsController(
            IPortfolioAppService portfolioAppService,
            ITransactionAppService transactionAppService)
        {
            _portfolioAppService = portfolioAppService;
            _transactionAppService = transactionAppService;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<AssetPositionDto>>> GetAll(
            [FromQuery] string? type,
            [FromQuery] string? subtype,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var portfolio = await _portfolioAppService.GetUserPortfolioAsync();
            var list = new List<AssetPositionDto>();

            if (portfolio != null)
            {
                list.AddRange(portfolio.Positions);
            }
            else
            {
                // Fallback / Simulated robust mock list
                list = GetSimulatedPositions();
            }

            // Apply filters
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
            return Ok(new PaginatedResponse<AssetPositionDto>(paginated, page, pageSize, list.Count));
        }

        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<object>>> GetSummary()
        {
            var portfolio = await _portfolioAppService.GetUserPortfolioAsync();
            decimal totalInvested = 0;
            decimal currentValue = 0;
            decimal totalGain = 0;
            decimal gainPercentage = 0;
            decimal fixedIncomeTotal = 0;
            decimal variableIncomeTotal = 0;
            List<AssetPositionDto> positions;

            if (portfolio != null)
            {
                totalInvested = portfolio.TotalInvested;
                currentValue = portfolio.TotalValue;
                totalGain = portfolio.TotalGain;
                gainPercentage = portfolio.GainPercentage;
                fixedIncomeTotal = portfolio.Positions.Where(p => p.Type == "fixed_income").Sum(p => p.CurrentValue);
                variableIncomeTotal = portfolio.Positions.Where(p => p.Type == "variable_income").Sum(p => p.CurrentValue);
                positions = portfolio.Positions.ToList();
            }
            else
            {
                // Simulated fallback
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
        public async Task<ActionResult<PaginatedResponse<TransactionDto>>> GetTransactions(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var portfolio = await _portfolioAppService.GetUserPortfolioAsync();
            if (portfolio == null)
            {
                return Ok(new PaginatedResponse<TransactionDto>(new List<TransactionDto>(), page, pageSize, 0));
            }

            var position = portfolio.Positions.FirstOrDefault(p => p.Id == id || p.AssetId == id);
            if (position == null)
            {
                return Ok(new PaginatedResponse<TransactionDto>(new List<TransactionDto>(), page, pageSize, 0));
            }

            var transactions = await _transactionAppService.GetTransactionsByPortfolioIdAsync(portfolio.Id);
            var filtered = transactions.Where(t => t.AssetId == position.AssetId).ToList();

            var paginated = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Ok(new PaginatedResponse<TransactionDto>(paginated, page, pageSize, filtered.Count));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<AssetPositionDto>>> GetById(Guid id)
        {
            var portfolio = await _portfolioAppService.GetUserPortfolioAsync();
            if (portfolio == null)
            {
                var simulated = GetSimulatedPositions().FirstOrDefault(p => p.Id == id);
                if (simulated != null) return Ok(new ApiResponse<AssetPositionDto>(simulated));
                return NotFound(new ApiResponse<AssetPositionDto>(null!, false, $"Investment with ID {id} was not found."));
            }

            var position = portfolio.Positions.FirstOrDefault(p => p.Id == id || p.AssetId == id);
            if (position == null)
            {
                return NotFound(new ApiResponse<AssetPositionDto>(null!, false, $"Investment with ID {id} was not found."));
            }

            return Ok(new ApiResponse<AssetPositionDto>(position));
        }

        [HttpPost("fixed-income")]
        public async Task<ActionResult<ApiResponse<AssetPositionDto>>> CreateFixedIncome([FromBody] CreateFixedIncomeRequestDto dto)
        {
            try
            {
                // Map CreateFixedIncomeRequest to RegisterTransactionDto Buy transaction
                var regDto = new RegisterTransactionDto
                {
                    PortfolioId = dto.PortfolioId,
                    Ticker = dto.Name,
                    Type = "Buy",
                    Quantity = dto.Quantity,
                    UnitPrice = dto.AveragePrice,
                    BrokerageFee = 0,
                    TransactionDate = DateTime.TryParse(dto.PurchaseDate, out var dt) ? dt : DateTime.UtcNow,
                    Notes = $"Issuer: {dto.Issuer}, Rate: {dto.InterestRate}%, Indexer: {dto.Indexer}"
                };

                var transaction = await _transactionAppService.RegisterTransactionAsync(regDto);
                
                // Fetch updated position
                var portfolio = await _portfolioAppService.GetUserPortfolioAsync();
                var position = portfolio?.Positions.FirstOrDefault(p => p.Ticker == dto.Name);

                return Ok(new ApiResponse<AssetPositionDto>(position ?? new AssetPositionDto
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = dto.PortfolioId,
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
                return BadRequest(new ApiResponse<AssetPositionDto>(null!, false, ex.Message));
            }
        }

        [HttpPost("variable-income")]
        public async Task<ActionResult<ApiResponse<AssetPositionDto>>> CreateVariableIncome([FromBody] CreateVariableIncomeRequestDto dto)
        {
            try
            {
                // Map CreateVariableIncomeRequest to RegisterTransactionDto Buy transaction
                var regDto = new RegisterTransactionDto
                {
                    PortfolioId = dto.PortfolioId,
                    Ticker = dto.Ticker,
                    Type = "Buy",
                    Quantity = dto.Quantity,
                    UnitPrice = dto.AveragePrice,
                    BrokerageFee = 0,
                    TransactionDate = DateTime.TryParse(dto.PurchaseDate, out var dt) ? dt : DateTime.UtcNow,
                    Notes = "Registered via Variable Income UI"
                };

                var transaction = await _transactionAppService.RegisterTransactionAsync(regDto);

                // Fetch updated position
                var portfolio = await _portfolioAppService.GetUserPortfolioAsync();
                var position = portfolio?.Positions.FirstOrDefault(p => p.Ticker.Equals(dto.Ticker, StringComparison.OrdinalIgnoreCase));

                return Ok(new ApiResponse<AssetPositionDto>(position ?? new AssetPositionDto
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = dto.PortfolioId,
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
                return BadRequest(new ApiResponse<AssetPositionDto>(null!, false, ex.Message));
            }
        }

        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<ApiResponse<AssetPositionDto>>> Update(Guid id, [FromBody] UpdateInvestmentRequestDto dto)
        {
            var portfolio = await _portfolioAppService.GetUserPortfolioAsync();
            if (portfolio == null)
            {
                return Ok(new ApiResponse<AssetPositionDto>(null!, true, "Investimento atualizado (simulação)."));
            }

            var position = portfolio.Positions.FirstOrDefault(p => p.Id == id || p.AssetId == id);
            if (position == null)
            {
                return NotFound(new ApiResponse<AssetPositionDto>(null!, false, "Investimento não encontrado."));
            }

            // We return the updated/simulated result
            if (dto.CurrentPrice.HasValue)
            {
                position.CurrentPrice = dto.CurrentPrice.Value;
                position.CurrentValue = position.Quantity * position.CurrentPrice;
                position.Gain = position.CurrentValue - position.TotalInvested;
                position.GainPercentage = position.TotalInvested > 0 ? (position.Gain / position.TotalInvested) * 100 : 0;
            }

            return Ok(new ApiResponse<AssetPositionDto>(position));
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
        {
            try
            {
                // 3 Revert all transactions of this asset position and restore portfolio cash balance
                var success = await _portfolioAppService.DeleteInvestmentAsync(id);
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

        private List<AssetPositionDto> GetSimulatedPositions()
        {
            return new List<AssetPositionDto>
            {
                new()
                {
                    Id = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f62"),
                    PortfolioId = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f61"),
                    AssetId = Guid.Parse("aa4c4dbb-cbf4-4f0f-8c3b-3129486c4761"),
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
                    PortfolioId = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f61"),
                    AssetId = Guid.Parse("aa4c4dbb-cbf4-4f0f-8c3b-3129486c4762"),
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
                    PortfolioId = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f61"),
                    AssetId = Guid.Parse("aa4c4dbb-cbf4-4f0f-8c3b-3129486c4763"),
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
                    PortfolioId = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f61"),
                    AssetId = Guid.Parse("aa4c4dbb-cbf4-4f0f-8c3b-3129486c4764"),
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
    public class CreateFixedIncomeRequestDto
    {
        public Guid PortfolioId { get; set; }
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

    public class CreateVariableIncomeRequestDto
    {
        public Guid PortfolioId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string Subtype { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public string PurchaseDate { get; set; } = string.Empty;
    }

    public class UpdateInvestmentRequestDto
    {
        public decimal? Quantity { get; set; }
        public decimal? AveragePrice { get; set; }
        public decimal? CurrentPrice { get; set; }
    }
}
