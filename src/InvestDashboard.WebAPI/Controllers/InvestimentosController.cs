using System.Security.Cryptography;
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

    [HttpGet("{positionId:guid}/projecao-renda-fixa")]
    public async Task<ActionResult<RespostaApi<ProjecaoRendaFixaDto>>> GetFixedIncomeProjection(Guid positionId)
    {
        var estimate = await portfolios.GetFixedIncomeProjectionAsync(positionId);
        return estimate is null
            ? NotFound(new RespostaApi<ProjecaoRendaFixaDto>(null!, false, "Fixed income position not found."))
            : Ok(new RespostaApi<ProjecaoRendaFixaDto>(estimate));
    }

    [HttpGet]
    public async Task<ActionResult<RespostaPaginada<PosicaoInvestimentoDto>>> GetAll(
        [FromQuery(Name = "carteiraId")] Guid? portfolioId,
        [FromQuery(Name = "grupoId")] Guid? grupoId,
        [FromQuery(Name = "tipo")] string? type,
        [FromQuery(Name = "subtipo")] string? subtype,
        [FromQuery(Name = "emissor")] string? issuer,
        [FromQuery(Name = "setor")] string? sector,
        [FromQuery(Name = "situacao")] string? status,
        [FromQuery(Name = "busca")] string? search,
        [FromQuery(Name = "ordenarPor")] string? sortBy,
        [FromQuery(Name = "ordem")] string? sortOrder,
        [FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "itensPorPagina")] int pageSize = 10)
    {
        ValidatePage(page, pageSize);
        if (portfolioId.HasValue && await portfolios.GetPortfolioByIdAsync(portfolioId.Value) is null)
            return NotFound(new RespostaApi<object>(null!, false, "Portfolio not found."));

        var positions = await FilterPositionsAsync(portfolioId, grupoId, type, subtype, issuer, sector, status, search);
        var ordered = SortPositions(positions, sortBy, sortOrder);
        var pageItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new RespostaPaginada<PosicaoInvestimentoDto>(pageItems, page, pageSize, ordered.Count));
    }

    [HttpGet("/api/v1/portfolios/{portfolioId:guid}/investments")]
    public async Task<ActionResult<RespostaPaginada<PosicaoInvestimentoDto>>> GetByPortfolio(
        Guid portfolioId,
        [FromQuery(Name = "tipo")] string? type,
        [FromQuery(Name = "subtipo")] string? subtype,
        [FromQuery(Name = "emissor")] string? issuer,
        [FromQuery(Name = "setor")] string? sector,
        [FromQuery(Name = "situacao")] string? status,
        [FromQuery(Name = "busca")] string? search,
        [FromQuery(Name = "ordenarPor")] string? sortBy,
        [FromQuery(Name = "ordem")] string? sortOrder,
        [FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "itensPorPagina")] int pageSize = 10)
    {
        return await GetAll(portfolioId, null, type, subtype, issuer, sector, status, search, sortBy, sortOrder, page, pageSize);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<RespostaApi<ResumoInvestimentoDto>>> GetSummary(
        [FromQuery(Name = "carteiraId")] Guid? portfolioId)
    {
        var portfolioList = new List<CarteiraDto>();
        if (portfolioId.HasValue)
        {
            var portfolio = await portfolios.GetPortfolioByIdAsync(portfolioId.Value);
            if (portfolio is null)
                return NotFound(new RespostaApi<ResumoInvestimentoDto>(null!, false, "Portfolio not found."));
            portfolioList.Add(portfolio);
        }
        else
        {
            var page = 1;
            RespostaPaginada<CarteiraDto> result;
            do
            {
                result = await portfolios.GetUserPortfoliosAsync(page++, 100);
                portfolioList.AddRange(result.Dados);
            }
            while (result.Paginacao.TemProximaPagina);
        }

        var positions = portfolioList.SelectMany(portfolio => portfolio.Positions).ToList();
        var openPositions = positions.Where(position => position.Status != "closed").ToList();
        var invested = portfolioList.Sum(portfolio => portfolio.TotalInvested);
        var current = portfolioList.Sum(portfolio => portfolio.TotalValue);
        var gain = portfolioList.Sum(portfolio => portfolio.TotalGain);
        var realizedGain = portfolioList.Sum(portfolio => portfolio.RealizedGain);
        var unrealizedGain = portfolioList.Sum(portfolio => portfolio.UnrealizedGain);
        var summary = new ResumoInvestimentoDto
        {
            TotalInvestido = invested,
            ValorAtual = current,
            GanhoTotal = gain,
            PercentualGanho = invested > 0 ? gain / invested * 100 : 0,
            GanhoRealizado = realizedGain,
            GanhoNaoRealizado = unrealizedGain,
            TotalRendaFixa = openPositions.Where(position => position.Type == "fixed_income").Sum(position => position.CurrentValue),
            TotalRendaVariavel = openPositions.Where(position => position.Type == "variable_income").Sum(position => position.CurrentValue),
            MelhoresPosicoes = openPositions.OrderByDescending(position => position.GainPercentage).Take(3).ToList(),
            PioresPosicoes = openPositions.OrderBy(position => position.GainPercentage).Take(3).ToList()
        };

        return Ok(new RespostaApi<ResumoInvestimentoDto>(summary));
    }

    [HttpGet("dividends")]
    public ActionResult<RespostaPaginada<object>> GetDividends(
        [FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "itensPorPagina")] int pageSize = 10)
    {
        ValidatePage(page, pageSize);
        return Ok(new RespostaPaginada<object>([], page, pageSize, 0));
    }

    [HttpGet("{id:guid}/transactions")]
    public async Task<ActionResult<RespostaPaginada<TransacaoDto>>> GetTransactions(
        Guid id,
        [FromQuery(Name = "pagina")] int page = 1,
        [FromQuery(Name = "itensPorPagina")] int pageSize = 10)
    {
        ValidatePage(page, pageSize);
        var position = await portfolios.GetPositionByIdAsync(id);
        if (position is null)
            return NotFound(new RespostaApi<object>(null!, false, "Investment not found."));

        var all = await transactions.GetTransactionsByPortfolioIdAsync(position.CarteiraId);
        var matching = all.Where(transaction => transaction.AtivoId == position.AtivoId).ToList();
        var pageItems = matching.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new RespostaPaginada<TransacaoDto>(pageItems, page, pageSize, matching.Count));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RespostaApi<PosicaoInvestimentoDto>>> GetById(Guid id)
    {
        var position = await portfolios.GetPositionByIdAsync(id);
        return position is null
            ? NotFound(new RespostaApi<PosicaoInvestimentoDto>(null!, false, "Investment not found."))
            : Ok(new RespostaApi<PosicaoInvestimentoDto>(position));
    }

    [HttpPost("fixed-income")]
    public async Task<ActionResult<RespostaApi<PosicaoInvestimentoDto>>> CreateFixedIncome([FromBody] CriarRendaFixaDto request)
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
            Liquidity = request.Liquidity,
            Convention = request.Convention,
            TransactionDate = request.PurchaseDate,
            IdempotencyKey = request.IdempotencyKey
        });

        var position = await portfolios.GetUserPositionsAsync(request.CarteiraId);
        var created = position.FirstOrDefault(item => item.AtivoId == transaction.AtivoId);
        return created is null
            ? StatusCode(StatusCodes.Status500InternalServerError, new RespostaApi<object>(null!, false, "The investment could not be loaded after saving."))
            : CreatedAtAction(nameof(GetById), new { id = created.Id }, new RespostaApi<PosicaoInvestimentoDto>(created));
    }

    [HttpPost("variable-income")]
    public async Task<ActionResult<RespostaApi<PosicaoInvestimentoDto>>> CreateVariableIncome([FromBody] CriarRendaVariavelDto request)
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
            ? StatusCode(StatusCodes.Status500InternalServerError, new RespostaApi<object>(null!, false, "The investment could not be loaded after saving."))
            : CreatedAtAction(nameof(GetById), new { id = created.Id }, new RespostaApi<PosicaoInvestimentoDto>(created));
    }

    [HttpPost("{id:guid}/valuations")]
    public async Task<ActionResult<RespostaApi<PosicaoInvestimentoDto>>> AddStatementValuation(Guid id, [FromBody] RegistrarAvaliacaoDto request)
    {
        var position = await portfolios.UpdatePositionValuationAsync(id, request.TotalValue, request.Date);
        return position is null
            ? NotFound(new RespostaApi<PosicaoInvestimentoDto>(null!, false, "Investment not found."))
            : Ok(new RespostaApi<PosicaoInvestimentoDto>(position));
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<RespostaApi<IReadOnlyList<PrecoHistoricoDto>>>> GetHistory(
        Guid id,
        [FromQuery(Name = "dataDe")] DateTime? fromDate)
    {
        var position = await portfolios.GetPositionByIdAsync(id);
        if (position is null)
            return NotFound(new RespostaApi<IReadOnlyList<PrecoHistoricoDto>>(null!, false, "Investment not found."));

        return Ok(new RespostaApi<IReadOnlyList<PrecoHistoricoDto>>(
            await portfolios.GetPriceHistoryAsync(id, fromDate)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<RespostaApi<bool>>> Delete(Guid id)
    {
        var deleted = await portfolios.DeleteInvestmentAsync(id);
        return deleted
            ? Ok(new RespostaApi<bool>(true))
            : NotFound(new RespostaApi<bool>(false, false, "Investment not found."));
    }

    private async Task<List<PosicaoInvestimentoDto>> FilterPositionsAsync(
        Guid? portfolioId,
        Guid? grupoId,
        string? type,
        string? subtype,
        string? issuer,
        string? sector,
        string? status,
        string? search)
    {
        var positions = await portfolios.GetUserPositionsAsync(portfolioId, grupoId);
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
            "nome" => position => position.Name,
            "ticker" => position => position.Ticker,
            "totalinvestido" => position => position.TotalInvested,
            "valoratual" => position => position.CurrentValue,
            "percentualganho" => position => position.GainPercentage,
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

}
