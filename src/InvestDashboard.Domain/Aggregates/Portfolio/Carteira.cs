using System;
using System.Collections.Generic;
using System.Linq;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Common;

namespace InvestDashboard.Domain.Aggregates.Portfolio;

public class Carteira : AggregateRoot<Guid>
{
    private readonly List<PosicaoInvestimento> _positions = new();

    public string UserId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public decimal Balance { get; private set; }
    public decimal RealizedGain { get; private set; }
    public decimal RealizedCostBasis { get; private set; }
    public int Version { get; private set; }

    public IReadOnlyCollection<PosicaoInvestimento> Positions => _positions.AsReadOnly();

    public decimal TotalAssetsValue => _positions.Sum(p => p.CurrentValue);
    public decimal TotalAssetsCost => _positions.Sum(p => p.TotalCost);
    public decimal TotalValue => TotalAssetsValue + Balance;
    public decimal TotalReturnAmount => TotalAssetsValue - TotalAssetsCost;
    public decimal TotalReturnPercentage => TotalAssetsCost > 0 ? (TotalReturnAmount / TotalAssetsCost) * 100 : 0;

    public Carteira(Guid id, string userId, string name, decimal initialBalance = 0, string? description = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User Id cannot be null or empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Carteira name cannot be null or empty", nameof(name));

        if (initialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative", nameof(initialBalance));

        UserId = userId;
        Name = name.Trim();
        Balance = initialBalance;
        Description = NormalizeDescription(description);
    }

    public void UpdateDetails(string? name, string? description)
    {
        var updatedName = Name;
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Carteira name cannot be null or empty", nameof(name));

            updatedName = name.Trim();
        }

        var updatedDescription = description is null ? Description : NormalizeDescription(description);
        if (updatedName == Name && updatedDescription == Description) return;
        Name = updatedName;
        Description = updatedDescription;
        Version++;
    }

    public void Update(string name, decimal balance)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Carteira name cannot be null or empty", nameof(name));

        if (balance < 0)
            throw new ArgumentException("Balance cannot be negative", nameof(balance));

        Name = name.Trim();
        Balance = balance;
        Version++;
    }

    // Required for EF Core / deserialization
#pragma warning disable CS8618
    private Carteira() { }
#pragma warning restore CS8618

    private static string? NormalizeDescription(string? description)
    {
        var normalized = description?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    public void ProcessTransaction(
        Transacao transaction,
        decimal currentAssetPrice,
        TipoAtivo assetType,
        IReadOnlyDictionary<Guid, Guid>? stablePositionIds = null)
    {
        if (transaction is null)
            throw new ArgumentNullException(nameof(transaction));

        if (transaction.UserId != UserId)
            throw new InvalidOperationException("Transaction does not belong to the owner of this carteira");

        Version++;

        switch (transaction.Type)
        {
            case TipoTransacao.Deposit:
                Balance += transaction.Quantity;
                break;

            case TipoTransacao.Withdrawal:
                if (Balance < transaction.Quantity)
                    throw new InvalidOperationException($"Insufficient cash balance. Required: {transaction.Quantity}, Available: {Balance}");

                Balance -= transaction.Quantity;
                break;

            case TipoTransacao.Buy:
                {
                    if (transaction.AtivoId is null)
                        throw new InvalidOperationException("Ativo ID is required for a buy transaction");

                    if (string.IsNullOrWhiteSpace(transaction.Ticker))
                        throw new InvalidOperationException("Ticker is required for a buy transaction");

                    var position = _positions.FirstOrDefault(p => p.AtivoId == transaction.AtivoId);
                    if (position is null)
                    {
                        var positionId = stablePositionIds is not null
                            && stablePositionIds.TryGetValue(transaction.AtivoId.Value, out var existingPositionId)
                            ? existingPositionId
                            : Guid.NewGuid();
                        position = new PosicaoInvestimento(
                            positionId,
                            Id,
                            transaction.AtivoId.Value,
                            transaction.Ticker,
                            assetType,
                            currentAssetPrice
                        );
                        _positions.Add(position);
                    }

                    position.AddShares(transaction.Quantity, transaction.UnitPrice, transaction.BrokerageFee, transaction.TransactionDate);
                    break;
                }

            case TipoTransacao.Sell:
                {
                    if (transaction.AtivoId is null)
                        throw new InvalidOperationException("Ativo ID is required for a sell transaction");

                    var position = _positions.FirstOrDefault(p => p.AtivoId == transaction.AtivoId);
                    if (position is null || position.Quantity < transaction.Quantity)
                        throw new InvalidOperationException($"Insufficient shares held to execute sell. Required: {transaction.Quantity}, Available: {(position is null ? 0 : position.Quantity)}");

                    var costBasis = transaction.Quantity * position.AverageCost;
                    position.RemoveShares(transaction.Quantity, transaction.UnitPrice, transaction.BrokerageFee, out var realizedGain);
                    transaction.SetRealizedResult(realizedGain, costBasis);
                    RealizedGain += realizedGain;
                    RealizedCostBasis += costBasis;

                    break;
                }

            default:
                throw new InvalidOperationException("Unknown transaction type");
        }
    }

    public void UpdateAssetPrice(Guid assetId, decimal newPrice)
    {
        if (assetId == Guid.Empty)
            throw new ArgumentException("Ativo Id cannot be empty", nameof(assetId));

        var position = _positions.FirstOrDefault(p => p.AtivoId == assetId);
        if (position is not null)
        {
            position.UpdateCurrentPrice(newPrice);
            Version++;
        }
    }

    public void PreparePositionsForRebuild(
        IReadOnlyCollection<Guid> assetIds,
        IReadOnlyDictionary<Guid, decimal> fixedIncomeStatementPrices)
    {
        foreach (var position in _positions.Where(position => assetIds.Contains(position.AtivoId)))
        {
            var currentPrice = position.TipoAtivo == TipoAtivo.RendaFixa
                ? fixedIncomeStatementPrices.GetValueOrDefault(position.AtivoId, position.CurrentPrice)
                : position.Ativo?.CurrentPrice ?? position.CurrentPrice;
            position.PrepareLedgerRebuild(currentPrice);
        }

        Version++;
        RealizedGain = 0;
        RealizedCostBasis = 0;
    }

    public bool RemovePosition(Guid positionId)
    {
        var position = _positions.FirstOrDefault(item => item.Id == positionId);
        if (position is null || !_positions.Remove(position)) return false;
        Version++;
        return true;
    }

    public void RemovePositionAndRevertTransactions(Guid assetId, IEnumerable<Transacao> transactions)
    {
        _ = transactions;
        var position = _positions.FirstOrDefault(p => p.AtivoId == assetId);
        if (position is null)
            return;
        _positions.Remove(position);
        Version++;
    }
}
