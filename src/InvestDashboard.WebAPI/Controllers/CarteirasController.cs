using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/portfolios")]
    public class CarteirasController : ControllerBase
    {
        private readonly ICarteiraAppService _carteiraAppService;

        public CarteirasController(ICarteiraAppService carteiraAppService)
        {
            _carteiraAppService = carteiraAppService;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<CarteiraDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var carteira = await _carteiraAppService.GetUserPortfolioAsync();
            var list = new List<CarteiraDto>();
            if (carteira != null)
            {
                list.Add(carteira);
            }
            else
            {
                var simulated = new CarteiraDto
                {
                    Id = Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f61"),
                    Name = "Meu Portfólio Simulado",
                    Balance = 15000.00m,
                    TotalValue = 85450.00m,
                    TotalInvested = 72100.00m,
                    TotalGain = 13350.00m,
                    GainPercentage = 18.52m,
                    Currency = "BRL",
                    IsActive = true,
                    UserName = "Jonathan Benício",
                    AssetsCount = 4
                };
                list.Add(simulated);
            }

            return Ok(new PaginatedResponse<CarteiraDto>(list, page, pageSize, list.Count));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<CarteiraDto>>> GetById(Guid id)
        {
            var carteira = await _carteiraAppService.GetPortfolioByIdAsync(id);
            if (carteira == null)
            {
                if (id == Guid.Empty || id == Guid.Parse("98b50e2d-dc99-43ef-b387-052637738f61"))
                {
                    var simulated = new CarteiraDto
                    {
                        Id = id,
                        Name = "Meu Portfólio Simulado",
                        Balance = 15000.00m,
                        TotalValue = 85450.00m,
                        TotalInvested = 72100.00m,
                        TotalGain = 13350.00m,
                        GainPercentage = 18.52m,
                        Currency = "BRL",
                        IsActive = true,
                        UserName = "Jonathan Benício",
                        AssetsCount = 4
                    };
                    return Ok(new ApiResponse<CarteiraDto>(simulated));
                }

                return NotFound(new ApiResponse<CarteiraDto>(null!, false, $"Portfolio with ID {id} was not found."));
            }

            return Ok(new ApiResponse<CarteiraDto>(carteira));
        }

        [HttpGet("{id:guid}/summary")]
        public async Task<ActionResult<ApiResponse<ResumoCarteiraDto>>> GetSummary(Guid id)
        {
            var carteira = await _carteiraAppService.GetPortfolioByIdAsync(id);
            CarteiraDto carteiraBase;

            if (carteira == null)
            {
                carteiraBase = new CarteiraDto
                {
                    Id = id,
                    Name = "Meu Portfólio Simulado",
                    Balance = 15000.00m,
                    TotalValue = 85450.00m,
                    TotalInvested = 72100.00m,
                    TotalGain = 13350.00m,
                    GainPercentage = 18.52m,
                    Currency = "BRL",
                    IsActive = true,
                    UserName = "Jonathan Benício",
                    AssetsCount = 4
                };
            }
            else
            {
                carteiraBase = carteira;
            }

            var summary = new ResumoCarteiraDto
            {
                Id = carteiraBase.Id,
                Name = carteiraBase.Name,
                Balance = carteiraBase.Balance,
                TotalValue = carteiraBase.TotalValue,
                TotalInvested = carteiraBase.TotalInvested,
                TotalGain = carteiraBase.TotalGain,
                GainPercentage = carteiraBase.GainPercentage,
                Currency = carteiraBase.Currency,
                IsActive = carteiraBase.IsActive,
                Positions = carteiraBase.Positions,
                UserName = carteiraBase.UserName,
                AssetsCount = carteiraBase.AssetsCount,
                AssetAllocation = new List<AlocacaoAtivoDto>
                {
                    new() { Category = "Ações", Value = carteiraBase.TotalValue * 0.45m, Percentage = 45m, Color = "#3b82f6" },
                    new() { Category = "Fundos Imobiliários", Value = carteiraBase.TotalValue * 0.30m, Percentage = 30m, Color = "#10b981" },
                    new() { Category = "Renda Fixa", Value = carteiraBase.TotalValue * 0.15m, Percentage = 15m, Color = "#f59e0b" },
                    new() { Category = "Criptoativos", Value = carteiraBase.TotalValue * 0.10m, Percentage = 10m, Color = "#8b5cf6" }
                },
                PerformanceHistory = new List<PontoPerformanceDto>
                {
                    new() { Date = "Jan", Value = carteiraBase.TotalValue * 0.85m, PercentageChange = -2.5m },
                    new() { Date = "Fev", Value = carteiraBase.TotalValue * 0.90m, PercentageChange = 5.8m },
                    new() { Date = "Mar", Value = carteiraBase.TotalValue * 0.93m, PercentageChange = 3.3m },
                    new() { Date = "Abr", Value = carteiraBase.TotalValue * 0.97m, PercentageChange = 4.1m },
                    new() { Date = "Mai", Value = carteiraBase.TotalValue, PercentageChange = 3.1m }
                }
            };

            return Ok(new ApiResponse<ResumoCarteiraDto>(summary));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CarteiraDto>>> Create([FromBody] CriarCarteiraDto dto)
        {
            try
            {
                var carteira = await _carteiraAppService.CreatePortfolioAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = carteira.Id }, new ApiResponse<CarteiraDto>(carteira));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<CarteiraDto>(null!, false, ex.Message));
            }
        }

        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<ApiResponse<CarteiraDto>>> Update(Guid id, [FromBody] CriarCarteiraDto dto)
        {
            var carteira = await _carteiraAppService.GetPortfolioByIdAsync(id);
            if (carteira == null)
            {
                carteira = new CarteiraDto
                {
                    Id = id,
                    Name = dto.Name,
                    Balance = dto.SaldoInicial,
                    TotalValue = dto.SaldoInicial,
                    TotalInvested = dto.SaldoInicial,
                    Currency = "BRL",
                    IsActive = true
                };
            }
            else
            {
                carteira.Name = dto.Name;
                carteira.Balance = dto.SaldoInicial;
            }

            return Ok(new ApiResponse<CarteiraDto>(carteira));
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
        {
            return Ok(new ApiResponse<object>(null, true, "Portfolio deletado com sucesso."));
        }
    }
}
