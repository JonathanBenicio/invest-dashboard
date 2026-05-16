using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Trading;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services
{
    public class TransactionAppService : ITransactionAppService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public TransactionAppService(
            ITransactionRepository transactionRepository,
            IPortfolioRepository portfolioRepository,
            IAssetRepository assetRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _transactionRepository = transactionRepository;
            _portfolioRepository = portfolioRepository;
            _assetRepository = assetRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<TransactionDto> RegisterTransactionAsync(RegisterTransactionDto dto)
        {
            var userId = _currentUserService.UserId?.ToString() 
                ?? throw new UnauthorizedAccessException("User is not authenticated");

            // Load and validate portfolio ownership
            var portfolio = await _portfolioRepository.GetByIdAsync(dto.PortfolioId)
                ?? throw new KeyNotFoundException($"Portfolio {dto.PortfolioId} not found");

            if (portfolio.UserId != userId)
            {
                throw new UnauthorizedAccessException("You do not own this portfolio");
            }

            TransactionType txType;
            if (!Enum.TryParse(dto.Type, true, out txType))
            {
                throw new ArgumentException($"Invalid transaction type '{dto.Type}'. Allowed: Deposit, Withdrawal, Buy, Sell");
            }

            Asset? asset = null;
            if (txType == TransactionType.Buy || txType == TransactionType.Sell)
            {
                if (string.IsNullOrWhiteSpace(dto.Ticker))
                {
                    throw new ArgumentException("Ticker is required for Buy/Sell transactions");
                }

                var cleanTicker = dto.Ticker.Trim().ToUpperInvariant();
                asset = await _assetRepository.GetByTickerAsync(cleanTicker);

                // Self-healing asset tracking: create the asset if not tracked in local database
                if (asset == null)
                {
                    asset = CreateConcreteAssetByTicker(cleanTicker, dto.UnitPrice);
                    await _assetRepository.AddAsync(asset);
                    await _unitOfWork.SaveChangesAsync(); // Commit the new asset so it can be linked
                }
            }

            // Instantiate Transaction aggregate
            var tx = new Transaction(
                Guid.NewGuid(),
                userId,
                dto.PortfolioId,
                asset?.Id,
                asset?.Ticker,
                txType,
                dto.Quantity,
                txType == TransactionType.Deposit || txType == TransactionType.Withdrawal ? 1.0m : dto.UnitPrice,
                dto.BrokerageFee,
                dto.TransactionDate,
                dto.Notes
            );

            // Execute synchronous state update of the portfolio aggregate (in-memory math)
            portfolio.ProcessTransaction(
                tx, 
                asset?.CurrentPrice ?? 1.0m, 
                asset?.AssetType ?? AssetType.Stock
            );

            // Persist transaction and update portfolio state in DB
            await _transactionRepository.AddAsync(tx);
            _portfolioRepository.Update(portfolio);

            await _unitOfWork.SaveChangesAsync();

            return MapToDto(tx);
        }

        public async Task<List<TransactionDto>> GetTransactionsByPortfolioIdAsync(Guid portfolioId)
        {
            var userId = _currentUserService.UserId?.ToString()
                ?? throw new UnauthorizedAccessException("User is not authenticated");

            var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId);
            if (portfolio == null || portfolio.UserId != userId)
            {
                return new List<TransactionDto>();
            }

            var allTransactions = await _transactionRepository.GetByUserIdAsync(userId);
            return allTransactions
                .Where(t => t.PortfolioId == portfolioId)
                .Select(MapToDto)
                .ToList();
        }

        private static TransactionDto MapToDto(Transaction tx)
        {
            return new TransactionDto
            {
                Id = tx.Id,
                PortfolioId = tx.PortfolioId,
                AssetId = tx.AssetId,
                Ticker = tx.Ticker,
                Type = tx.Type.ToString(),
                Quantity = tx.Quantity,
                UnitPrice = tx.UnitPrice,
                BrokerageFee = tx.BrokerageFee,
                TotalAmount = tx.TotalAmount,
                TransactionDate = tx.TransactionDate,
                Notes = tx.Notes
            };
        }

        private static Asset CreateConcreteAssetByTicker(string ticker, decimal price)
        {
            var cleanTicker = ticker.Trim().ToUpperInvariant();
            
            // Smart heuristics to detect asset category for proper EF TPH inheritance instantiation
            if ((cleanTicker.Length == 6 || cleanTicker.Length == 5) && cleanTicker.EndsWith("11"))
            {
                return new FiiAsset(Guid.NewGuid(), cleanTicker, $"{cleanTicker} Real Estate Fund", price, DateTime.UtcNow, "Outros");
            }
            else if (cleanTicker.EndsWith("3") || cleanTicker.EndsWith("4") || cleanTicker.EndsWith("5") || cleanTicker.EndsWith("6"))
            {
                return new StockAsset(Guid.NewGuid(), cleanTicker, $"{cleanTicker} Equity", price, DateTime.UtcNow, "Outros");
            }
            else if (cleanTicker.Length >= 3 && cleanTicker.Length <= 4 && !cleanTicker.Any(char.IsDigit))
            {
                return new CryptoAsset(Guid.NewGuid(), cleanTicker, $"{cleanTicker} Crypto Token", price, DateTime.UtcNow, "Mainnet");
            }

            return new StockAsset(Guid.NewGuid(), cleanTicker, $"{cleanTicker} Asset", price, DateTime.UtcNow, "Outros");
        }
    }
}
