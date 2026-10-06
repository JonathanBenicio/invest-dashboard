using System.Security.Authentication;
using InvestDashboard.Application.DTOs.Trading;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services;

public sealed class TransacaoAppService(
    ITransacaoRepository transactions,
    ICarteiraRepository portfolios,
    IAtivoRepository assets,
    IValuacaoPosicaoRepository valuations,
    IUnitOfWork unitOfWork,
    IUsuarioAtualService currentUser) : ITransacaoAppService
{
    private const int TransactionLimit = 100;

    public async Task<TransacaoDto> RegisterTransactionAsync(RegistrarTransacaoDto dto)
    {
        var userId = currentUser.UserId?.ToString()
            ?? throw new AuthenticationException("Authentication is required.");

        if (dto.IdempotencyKey == Guid.Empty)
            throw new ArgumentException("An idempotency key is required.");
        if (!Enum.TryParse<TipoTransacao>(dto.Type, true, out var type) ||
            type is not (TipoTransacao.Buy or TipoTransacao.Sell))
            throw new ArgumentException("Only buy and sell transactions are supported; cash transactions are not enabled.");
        if (!Enum.TryParse<ModalidadeFiscal>(dto.ModalidadeFiscal, true, out var modalidadeFiscal))
        {
            if (type == TipoTransacao.Sell && !string.IsNullOrWhiteSpace(dto.ModalidadeFiscal))
                throw new ArgumentException("Fiscal modality must be Comum or DayTrade.");
            modalidadeFiscal = ModalidadeFiscal.NaoInformada;
        }
        if (!Enum.IsDefined(modalidadeFiscal))
            throw new ArgumentException("Fiscal modality must be a supported value.");
        if (string.IsNullOrWhiteSpace(dto.Ticker))
            throw new ArgumentException("Ticker is required for buy and sell transactions.");

        var portfolio = await portfolios.GetByIdForUserAsync(dto.CarteiraId, userId)
            ?? throw new KeyNotFoundException("Portfolio not found.");
        if (!await portfolios.CanManageAsync(portfolio.Id, userId))
            throw new UnauthorizedAccessException("You cannot change this portfolio.");

        var priorRequest = await transactions.GetByIdempotencyKeyAsync(userId, dto.IdempotencyKey);
        if (priorRequest is not null)
        {
            if (priorRequest.CarteiraId != portfolio.Id)
                throw new TransactionLedgerConflictException();
            return MapToDto(priorRequest);
        }

        var ticker = dto.Ticker.Trim().ToUpperInvariant();
        var asset = dto.AtivoId.HasValue
            ? await assets.GetByIdAsync(dto.AtivoId.Value)
            : await assets.GetByTickerAsync(ticker);

        if (asset is not null && !string.Equals(asset.Ticker, ticker, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The ticker and asset ID do not match.");

        if (asset is not null && !string.IsNullOrWhiteSpace(dto.AssetClass) &&
            !string.Equals(GetAssetClass(asset), NormalizeAssetClass(dto.AssetClass), StringComparison.OrdinalIgnoreCase))
            throw new TransactionLedgerConflictException();

        if (asset is null)
        {
            if (type != TipoTransacao.Buy)
                throw new KeyNotFoundException("Asset not found.");
            asset = CreateAsset(dto, ticker);
            await assets.AddAsync(asset);
        }
        else if (!string.IsNullOrWhiteSpace(dto.AssetClass) &&
                 !string.Equals(GetAssetClass(asset), NormalizeAssetClass(dto.AssetClass), StringComparison.OrdinalIgnoreCase))
        {
            throw new TransactionLedgerConflictException();
        }

        if (type == TipoTransacao.Sell && RequiresFiscalModality(asset) &&
            modalidadeFiscal == ModalidadeFiscal.NaoInformada)
            throw new ArgumentException("Fiscal modality is required for stock and real estate fund sales.");

        var transaction = new Transacao(
            Guid.NewGuid(),
            userId,
            portfolio.Id,
            asset.Id,
            asset.Ticker,
            type,
            dto.Quantity,
            dto.UnitPrice,
            dto.BrokerageFee,
            dto.TransactionDate,
            dto.Notes,
            dto.IdempotencyKey,
            modalidadeFiscal,
            portfolio.TitularId);

        var walletTransactions = await transactions.GetByPortfolioIdAsync(portfolio.Id);
        var history = walletTransactions.Append(transaction)
            .OrderBy(item => item.TransactionDate)
            .ThenBy(item => item.Id)
            .ToList();

        try
        {
            await RebuildPositionsAsync(portfolio, history, asset);
        }
        catch (InvalidOperationException)
        {
            throw new TransactionLedgerConflictException();
        }
        await transactions.AddAsync(transaction);

        if (asset is RendaFixa && dto.InitialStatementValue.HasValue)
        {
            var unitPrice = dto.InitialStatementValue.Value / dto.Quantity;
            var position = portfolio.Positions.Single(item => item.AtivoId == asset.Id);
            var valuation = new ValuacaoPosicao(Guid.NewGuid(), position.Id, dto.TransactionDate,
                unitPrice, position.Quantity, DateTime.UtcNow);
            var latestValuation = await valuations.GetLatestAsync(position.Id);
            await valuations.AddAsync(valuation);
            if (valuation.IsNewerThan(latestValuation)) position.UpdateCurrentPrice(unitPrice);
        }

        portfolios.Update(portfolio);
        await unitOfWork.SaveChangesAsync();
        return MapToDto(transaction);
    }

    public async Task<List<TransacaoDto>> GetTransactionsByPortfolioIdAsync(Guid portfolioId)
    {
        var userId = currentUser.UserId?.ToString()
            ?? throw new AuthenticationException("Authentication is required.");
        if (await portfolios.GetByIdForUserAsync(portfolioId, userId) is null)
            throw new KeyNotFoundException("Portfolio not found.");

        var records = await transactions.GetByPortfolioIdAsync(portfolioId);
        return records.Select(MapToDto).ToList();
    }

    public async Task<TransacaoDto> UpdateTransactionAsync(Guid id, AtualizarTransacaoDto dto)
    {
        var userId = currentUser.UserId?.ToString()
            ?? throw new AuthenticationException("Authentication is required.");
        var transaction = await transactions.GetByIdAsync(id);
        if (transaction is null)
            throw new KeyNotFoundException("Transaction not found.");
        if (!Enum.TryParse<TipoTransacao>(dto.Type, true, out var type) ||
            type is not (TipoTransacao.Buy or TipoTransacao.Sell))
            throw new ArgumentException("Transaction type must be Buy or Sell.");

        var portfolio = await portfolios.GetByIdForUserAsync(transaction.CarteiraId, userId)
            ?? throw new KeyNotFoundException("Portfolio not found.");
        if (!await portfolios.CanManageAsync(portfolio.Id, userId))
            throw new KeyNotFoundException("Transaction not found.");

        var modality = transaction.ModalidadeFiscal;
        if (!string.IsNullOrWhiteSpace(dto.ModalidadeFiscal))
        {
            if (!Enum.TryParse<ModalidadeFiscal>(dto.ModalidadeFiscal, true, out modality) || !Enum.IsDefined(modality))
                throw new ArgumentException("Fiscal modality must be Comum or DayTrade.");
        }
        else if (type != TipoTransacao.Sell || transaction.Type != TipoTransacao.Sell)
        {
            modality = ModalidadeFiscal.NaoInformada;
        }

        var asset = transaction.AtivoId.HasValue
            ? await assets.GetByIdAsync(transaction.AtivoId.Value)
            : null;
        if (type == TipoTransacao.Sell && asset is not null && RequiresFiscalModality(asset) &&
            modality == ModalidadeFiscal.NaoInformada)
            throw new ArgumentException("Fiscal modality is required for stock and real estate fund sales.");

        transaction.UpdateDetails(type, dto.Quantity, dto.UnitPrice, dto.BrokerageFee, dto.TransactionDate, dto.Notes, modality);

        var history = await transactions.GetByPortfolioIdAsync(portfolio.Id);
        try
        {
            await RebuildPositionsAsync(portfolio, history, null);
        }
        catch (InvalidOperationException)
        {
            throw new TransactionLedgerConflictException();
        }

        transactions.Update(transaction);
        portfolios.Update(portfolio);
        await unitOfWork.SaveChangesAsync();
        return MapToDto(transaction);
    }

    public async Task DeleteTransactionAsync(Guid id)
    {
        var userId = currentUser.UserId?.ToString()
            ?? throw new AuthenticationException("Authentication is required.");
        var transaction = await transactions.GetByIdAsync(id);
        if (transaction is null)
            throw new KeyNotFoundException("Transaction not found.");

        var portfolio = await portfolios.GetByIdForUserAsync(transaction.CarteiraId, userId)
            ?? throw new KeyNotFoundException("Portfolio not found.");
        if (!await portfolios.CanManageAsync(portfolio.Id, userId))
            throw new KeyNotFoundException("Transaction not found.");
        var history = (await transactions.GetByPortfolioIdAsync(portfolio.Id))
            .Where(item => item.Id != transaction.Id)
            .ToList();

        try
        {
            await RebuildPositionsAsync(portfolio, history, null);
        }
        catch (InvalidOperationException)
        {
            throw new TransactionLedgerConflictException();
        }

        transactions.Delete(transaction);
        portfolios.Update(portfolio);
        await unitOfWork.SaveChangesAsync();
    }

    private async Task RebuildPositionsAsync(Carteira portfolio, IReadOnlyList<Transacao> history, Ativo? pendingAsset)
    {
        var existingAssetIds = portfolio.Positions.Select(position => position.AtivoId).ToHashSet();
        var stablePositionIds = portfolio.Positions.ToDictionary(position => position.AtivoId, position => position.Id);
        var previousFixedIncomePrices = portfolio.Positions
            .Where(position => position.TipoAtivo == TipoAtivo.RendaFixa)
            .ToDictionary(position => position.AtivoId, position => position.CurrentPrice);
        var assetIds = history.Where(item => item.AtivoId.HasValue).Select(item => item.AtivoId!.Value).Distinct().ToArray();
        var assetMap = (await assets.GetByIdsAsync(assetIds)).ToDictionary(asset => asset.Id);
        if (pendingAsset is not null)
            assetMap[pendingAsset.Id] = pendingAsset;

        portfolio.PreparePositionsForRebuild(assetIds, previousFixedIncomePrices);
        foreach (var item in history)
        {
            if (item.Type is TipoTransacao.Deposit or TipoTransacao.Withdrawal)
                continue;

            if (!item.AtivoId.HasValue || !assetMap.TryGetValue(item.AtivoId.Value, out var asset))
                throw new InvalidOperationException("Transaction references a missing asset.");

            var marketPrice = asset.CurrentPrice;
            if (asset.TipoAtivo == TipoAtivo.RendaFixa)
            {
                marketPrice = previousFixedIncomePrices.TryGetValue(asset.Id, out var positionPrice)
                    ? positionPrice
                    : history.Where(candidate => candidate.AtivoId == asset.Id && candidate.Type == TipoTransacao.Buy)
                        .OrderByDescending(candidate => candidate.TransactionDate)
                        .ThenByDescending(candidate => candidate.Id)
                        .Select(candidate => candidate.UnitPrice)
                        .FirstOrDefault(asset.CurrentPrice);
            }

            portfolio.ProcessTransaction(item, marketPrice, asset.TipoAtivo, stablePositionIds);
        }

        foreach (var position in portfolio.Positions.Where(position => !existingAssetIds.Contains(position.AtivoId)))
            portfolios.AddPosition(position);
    }

    private static Ativo CreateAsset(RegistrarTransacaoDto dto, string ticker)
    {
        var assetClass = NormalizeAssetClass(dto.AssetClass);
        var name = string.IsNullOrWhiteSpace(dto.Name) ? ticker : dto.Name.Trim();
        var now = DateTime.UtcNow;

        return assetClass switch
        {
            "ACAO" => new Acao(Guid.NewGuid(), ticker, name, dto.UnitPrice, now, dto.Sector ?? "Outros", "ACAO"),
            "ETF" => new Acao(Guid.NewGuid(), ticker, name, dto.UnitPrice, now, dto.Sector ?? "ETF", "ETF"),
            "BDR" => new Acao(Guid.NewGuid(), ticker, name, dto.UnitPrice, now, dto.Sector ?? "BDR", "BDR"),
            "FII" => new FundoImobiliario(Guid.NewGuid(), ticker, name, dto.UnitPrice, now, dto.Sector ?? "Outros"),
            "CRYPTO" => new Criptoativo(Guid.NewGuid(), ticker, name, dto.UnitPrice, now, "Mainnet"),
            "RENDA_FIXA" => CreateFixedIncomeAsset(dto, ticker, name, now),
            _ => throw new ArgumentException("Select a supported asset class before registering a purchase.")
        };
    }

    private static RendaFixa CreateFixedIncomeAsset(RegistrarTransacaoDto dto, string ticker, string name, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(dto.Issuer)
            || string.IsNullOrWhiteSpace(dto.Subtype)
            || string.IsNullOrWhiteSpace(dto.Indexer)
            || !dto.MaturityDate.HasValue
            || !dto.InterestRate.HasValue)
            throw new ArgumentException("Issuer, product type, rate, indexer, and maturity are required for fixed income.");

        var currentUnitValue = dto.InitialStatementValue.HasValue
            ? dto.InitialStatementValue.Value / dto.Quantity
            : dto.UnitPrice;

        return new RendaFixa(
            Guid.NewGuid(),
            ticker,
            name,
            currentUnitValue,
            now,
            dto.Indexer,
            dto.InterestRate.Value,
            dto.MaturityDate.Value,
            dto.Issuer,
            dto.Subtype,
            dto.Liquidity,
            dto.Convention);
    }

    private static string NormalizeAssetClass(string? assetClass) => assetClass?.Trim().ToUpperInvariant() switch
    {
        "ACAO" or "STOCK" => "ACAO",
        "ETF" => "ETF",
        "BDR" => "BDR",
        "FII" => "FII",
        "CRYPTO" or "CRIPTOATIVO" => "CRYPTO",
        "RENDA_FIXA" or "FIXED_INCOME" => "RENDA_FIXA",
        _ => string.Empty
    };

    private static string GetAssetClass(Ativo asset) => asset.TipoAtivo switch
    {
        TipoAtivo.RendaFixa => "RENDA_FIXA",
        _ => asset.Subtype
    };

    private static bool RequiresFiscalModality(Ativo asset) =>
        asset.TipoAtivo == TipoAtivo.FundoImobiliario ||
        asset.TipoAtivo == TipoAtivo.Acao &&
        (string.Equals(asset.Subtype, "ACAO", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(asset.Subtype, "STOCK", StringComparison.OrdinalIgnoreCase));

    private static TransacaoDto MapToDto(Transacao transaction) => new()
    {
        Id = transaction.Id,
        CarteiraId = transaction.CarteiraId,
        TitularId = transaction.TitularId,
        AtivoId = transaction.AtivoId,
        Ticker = transaction.Ticker,
        Type = transaction.Type.ToString(),
        Quantity = transaction.Quantity,
        UnitPrice = transaction.UnitPrice,
        BrokerageFee = transaction.BrokerageFee,
        TotalAmount = transaction.TotalAmount,
        RealizedGain = transaction.RealizedGain,
        RealizedCostBasis = transaction.RealizedCostBasis,
        ModalidadeFiscal = transaction.ModalidadeFiscal.ToString(),
        TransactionDate = transaction.TransactionDate,
        Notes = transaction.Notes
    };
}
