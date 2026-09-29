using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.DTOs.Trading;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/investments")]
public sealed class InvestimentosController(
    ICarteiraAppService portfolios,
    ITransacaoAppService transactions) : ControllerBase
{
    private const int MaximumPageSize = 100;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<PosicaoInvestimentoDto>>> GetAll(
        [FromQuery] Guid? portfolioId,
        [FromQuery] string? type,
        [FromQuery] string? subtype,
        [FromQuery] string? issuer,
        [FromQuery] string? sector,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        ValidatePage(page, pageSize);
        if (portfolioId.HasValue && await portfolios.GetPortfolioByIdAsync(portfolioId.Value) is null)
            return NotFound(new ApiResponse<object>(null!, false, "Portfolio not found."));

        var positions = await FilterPositionsAsync(portfolioId, type, subtype, issuer, sector, status, search);
        var ordered = SortPositions(positions, sortBy, sortOrder);
        var pageItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new PaginatedResponse<PosicaoInvestimentoDto>(pageItems, page, pageSize, ordered.Count));
    }

    [HttpGet("/api/v1/portfolios/{portfolioId:guid}/investments")]
    public async Task<ActionResult<PaginatedResponse<PosicaoInvestimentoDto>>> GetByPortfolio(
        Guid portfolioId,
        [FromQuery] string? type,
        [FromQuery] string? subtype,
        [FromQuery] string? issuer,
        [FromQuery] string? sector,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return await GetAll(portfolioId, type, subtype, issuer, sector, status, search, sortBy, sortOrder, page, pageSize);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<object>>> GetSummary([FromQuery] Guid? portfolioId)
    {
        var portfolioList = new List<CarteiraDto>();
        if (portfolioId.HasValue)
        {
            var portfolio = await portfolios.GetPortfolioByIdAsync(portfolioId.Value);
            if (portfolio is null)
                return NotFound(new ApiResponse<object>(null!, false, "Portfolio not found."));
            portfolioList.Add(portfolio);
        }
        else
        {
            var page = 1;
            PaginatedResponse<CarteiraDto> result;
            do
            {
                result = await portfolios.GetUserPortfoliosAsync(page++, 100);
                portfolioList.AddRange(result.Data);
            }
            while (result.Pagination.HasNextPage);
        }

        var positions = portfolioList.SelectMany(portfolio => portfolio.Positions).ToList();
        var openPositions = positions.Where(position => position.Status == "open").ToList();
        var invested = portfolioList.Sum(portfolio => portfolio.TotalInvested);
        var current = portfolioList.Sum(portfolio => portfolio.TotalValue);
        var gain = portfolioList.Sum(portfolio => portfolio.TotalGain);
        var realizedGain = portfolioList.Sum(portfolio => portfolio.RealizedGain);
        var unrealizedGain = portfolioList.Sum(portfolio => portfolio.UnrealizedGain);
        var summary = new
        {
            TotalInvested = invested,
            CurrentValue = current,
            TotalGain = gain,
            GainPercentage = invested > 0 ? gain / invested * 100 : 0,
            RealizedGain = realizedGain,
            UnrealizedGain = unrealizedGain,
            FixedIncomeTotal = openPositions.Where(position => position.Type == "fixed_income").Sum(position => position.CurrentValue),
            VariableIncomeTotal = openPositions.Where(position => position.Type == "variable_income").Sum(position => position.CurrentValue),
            TopPerformers = openPositions.OrderByDescending(position => position.GainPercentage).Take(3),
            WorstPerformers = openPositions.OrderBy(position => position.GainPercentage).Take(3)
        };

        return Ok(new ApiResponse<object>(summary));
    }

    [HttpGet("dividends")]
    public ActionResult<PaginatedResponse<object>> GetDividends([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        ValidatePage(page, pageSize);
        return Ok(new PaginatedResponse<object>([], page, pageSize, 0));
    }

    [HttpGet("{id:guid}/transactions")]
    public async Task<ActionResult<PaginatedResponse<TransacaoDto>>> GetTransactions(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        ValidatePage(page, pageSize);
        var position = await portfolios.GetPositionByIdAsync(id);
        if (position is null)
            return NotFound(new ApiResponse<object>(null!, false, "Investment not found."));

        var all = await transactions.GetTransactionsByPortfolioIdAsync(position.CarteiraId);
        var matching = all.Where(transaction => transaction.AtivoId == position.AtivoId).ToList();
        var pageItems = matching.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new PaginatedResponse<TransacaoDto>(pageItems, page, pageSize, matching.Count));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PosicaoInvestimentoDto>>> GetById(Guid id)
    {
        var position = await portfolios.GetPositionByIdAsync(id);
        return position is null
            ? NotFound(new ApiResponse<PosicaoInvestimentoDto>(null!, false, "Investment not found."))
            : Ok(new ApiResponse<PosicaoInvestimentoDto>(position));
    }

    [HttpPost("fixed-income")]
    public async Task<ActionResult<ApiResponse<PosicaoInvestimentoDto>>> CreateFixedIncome([FromBody] CriarRendaFixaDto request)
    {
        var ticker = CreateFixedIncomeTicker(request.IdempotencyKey);
        var transaction = await transactions.RegisterTransactionAsync(new RegistrarTransacaoDto
        {
            CarteiraId = request.CarteiraId,
            Ticker = ticker,
            Type = "Buy",
            AssetClass = "RENDA_FIXA",
            Name = request.Name,
            Issuer = request.Issuer,
            Subtype = request.Subtype,
            Quantity = request.Principal,
            UnitPrice = 1m,
            InitialStatementValue = request.StatementValue,
            InterestRate = request.InterestRate,
            Indexer = request.Indexer,
            MaturityDate = request.MaturityDate,
            TransactionDate = request.PurchaseDate,
            IdempotencyKey = request.IdempotencyKey
        });

        var position = await portfolios.GetUserPositionsAsync(request.CarteiraId);
        var created = position.FirstOrDefault(item => item.AtivoId == transaction.AtivoId);
        return created is null
            ? StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>(null!, false, "The investment could not be loaded after saving."))
            : CreatedAtAction(nameof(GetById), new { id = created.Id }, new ApiResponse<PosicaoInvestimentoDto>(created));
    }

    [HttpPost("variable-income")]
    public async Task<ActionResult<ApiResponse<PosicaoInvestimentoDto>>> CreateVariableIncome([FromBody] CriarRendaVariavelDto request)
    {
        var transaction = await transactions.RegisterTransactionAsync(new RegistrarTransacaoDto
        {
            CarteiraId = request.CarteiraId,
            Ticker = request.Ticker,
            Type = "Buy",
            AssetClass = request.Subtype,
            Name = request.Name,
            Sector = request.Sector,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            BrokerageFee = request.Fees,
            TransactionDate = request.TransactionDate,
            IdempotencyKey = request.IdempotencyKey
        });

        var positions = await portfolios.GetUserPositionsAsync(request.CarteiraId);
        var created = positions.FirstOrDefault(item => item.AtivoId == transaction.AtivoId);
        return created is null
            ? StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>(null!, false, "The investment could not be loaded after saving."))
            : CreatedAtAction(nameof(GetById), new { id = created.Id }, new ApiResponse<PosicaoInvestimentoDto>(created));
    }

    [HttpPost("{id:guid}/valuations")]
    public async Task<ActionResult<ApiResponse<PosicaoInvestimentoDto>>> AddStatementValuation(Guid id, [FromBody] RegistrarAvaliacaoDto request)
    {
        var position = await portfolios.UpdatePositionValuationAsync(id, request.TotalValue, request.Date);
        return position is null
            ? NotFound(new ApiResponse<PosicaoInvestimentoDto>(null!, false, "Investment not found."))
            : Ok(new ApiResponse<PosicaoInvestimentoDto>(position));
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PrecoHistoricoDto>>>> GetHistory(Guid id, [FromQuery] DateTime? fromDate)
    {
        var position = await portfolios.GetPositionByIdAsync(id);
        if (position is null)
            return NotFound(new ApiResponse<IReadOnlyList<PrecoHistoricoDto>>(null!, false, "Investment not found."));

        return Ok(new ApiResponse<IReadOnlyList<PrecoHistoricoDto>>(
            await portfolios.GetPriceHistoryAsync(id, fromDate)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var deleted = await portfolios.DeleteInvestmentAsync(id);
        return deleted
            ? Ok(new ApiResponse<bool>(true))
            : NotFound(new ApiResponse<bool>(false, false, "Investment not found."));
    }

    private async Task<List<PosicaoInvestimentoDto>> FilterPositionsAsync(
        Guid? portfolioId,
        string? type,
        string? subtype,
        string? issuer,
        string? sector,
        string? status,
        string? search)
    {
        var positions = await portfolios.GetUserPositionsAsync(portfolioId);
        return positions
            .Where(position => string.IsNullOrWhiteSpace(type) || position.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .Where(position => string.IsNullOrWhiteSpace(subtype) || position.Subtype.Equals(subtype, StringComparison.OrdinalIgnoreCase))
            .Where(position => string.IsNullOrWhiteSpace(issuer) || position.Issuer?.Contains(issuer, StringComparison.OrdinalIgnoreCase) == true)
            .Where(position => string.IsNullOrWhiteSpace(sector) || position.Sector?.Contains(sector, StringComparison.OrdinalIgnoreCase) == true)
            .Where(position => string.IsNullOrWhiteSpace(status) || position.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
            .Where(position => string.IsNullOrWhiteSpace(search)
                || position.Ticker.Contains(search, StringComparison.OrdinalIgnoreCase)
                || position.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static List<PosicaoInvestimentoDto> SortPositions(
        List<PosicaoInvestimentoDto> positions,
        string? sortBy,
        string? sortOrder)
    {
        var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        Func<PosicaoInvestimentoDto, object> key = sortBy?.ToLowerInvariant() switch
        {
            "name" => position => position.Name,
            "ticker" => position => position.Ticker,
            "totalinvested" => position => position.TotalInvested,
            "currentvalue" => position => position.CurrentValue,
            "gainpercentage" => position => position.GainPercentage,
            _ => position => position.Ticker
        };

        return descending
            ? positions.OrderByDescending(key).ToList()
            : positions.OrderBy(key).ToList();
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > MaximumPageSize)
            throw new ArgumentException("Page must be positive and page size must be between 1 and 100.");
    }

    private static string CreateFixedIncomeTicker(Guid idempotencyKey)
    {
        var hash = SHA256.HashData(idempotencyKey.ToByteArray());
        return "RF" + Convert.ToHexString(hash)[..18];
    }

    public sealed class CriarRendaFixaDto
    {
        [JsonPropertyName("portfolioId")]
        [Required]
        public Guid CarteiraId { get; set; }
        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;
        [Required, StringLength(20)]
        public string Subtype { get; set; } = string.Empty;
        [Required, StringLength(200)]
        public string Issuer { get; set; } = string.Empty;
        [Range(typeof(decimal), "0.00000001", "100000000000000", ParseLimitsInInvariantCulture = true)]
        public decimal Principal { get; set; }
        [Range(typeof(decimal), "0", "100000000000000", ParseLimitsInInvariantCulture = true)]
        public decimal StatementValue { get; set; }
        [Range(typeof(decimal), "0", "10000", ParseLimitsInInvariantCulture = true)]
        public decimal InterestRate { get; set; }
        [Required, StringLength(20)]
        public string Indexer { get; set; } = string.Empty;
        public DateTime MaturityDate { get; set; }
        public DateTime PurchaseDate { get; set; }
        [Required]
        public Guid IdempotencyKey { get; set; }
    }

    public sealed class CriarRendaVariavelDto
    {
        [JsonPropertyName("portfolioId")]
        [Required]
        public Guid CarteiraId { get; set; }
        [Required, StringLength(20)]
        public string Ticker { get; set; } = string.Empty;
        [Required, RegularExpression("^(ACAO|FII|ETF|BDR|CRYPTO)$")]
        public string Subtype { get; set; } = string.Empty;
        [StringLength(200)]
        public string? Name { get; set; }
        [StringLength(100)]
        public string? Sector { get; set; }
        [Range(typeof(decimal), "0.00000001", "100000000000000", ParseLimitsInInvariantCulture = true)]
        public decimal Quantity { get; set; }
        [Range(typeof(decimal), "0.00000001", "100000000000000", ParseLimitsInInvariantCulture = true)]
        public decimal UnitPrice { get; set; }
        [Range(typeof(decimal), "0", "100000000000000", ParseLimitsInInvariantCulture = true)]
        public decimal Fees { get; set; }
        public DateTime TransactionDate { get; set; }
        [Required]
        public Guid IdempotencyKey { get; set; }
    }

    public sealed class RegistrarAvaliacaoDto
    {
        [Range(typeof(decimal), "0", "100000000000000", ParseLimitsInInvariantCulture = true)]
        public decimal TotalValue { get; set; }
        public DateTime Date { get; set; }
    }
}
