using System;
using System.Linq;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services
{
    public class PortfolioAppService : IPortfolioAppService
    {
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public PortfolioAppService(
            IPortfolioRepository portfolioRepository,
            ITransactionRepository transactionRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _portfolioRepository = portfolioRepository;
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<PortfolioDto> CreatePortfolioAsync(CreatePortfolioDto dto)
        {
            var userId = _currentUserService.UserId?.ToString() 
                ?? throw new UnauthorizedAccessException("User is not authenticated");

            // Check if user already has a portfolio to maintain our single-portfolio design constraint
            var existing = await _portfolioRepository.GetByUserIdAsync(userId);
            if (existing != null)
            {
                throw new InvalidOperationException("User already has an active portfolio");
            }

            var portfolio = new Portfolio(
                Guid.NewGuid(),
                userId,
                dto.Name,
                dto.InitialBalance
            );

            await _portfolioRepository.AddAsync(portfolio);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(portfolio);
        }

        public async Task<PortfolioDto?> GetPortfolioByIdAsync(Guid portfolioId)
        {
            var userId = _currentUserService.UserId?.ToString() 
                ?? throw new UnauthorizedAccessException("User is not authenticated");

            var portfolio = await _portfolioRepository.GetByIdAsync(portfolioId);
            if (portfolio == null || portfolio.UserId != userId)
            {
                return null;
            }

            return MapToDto(portfolio);
        }

        public async Task<PortfolioDto?> GetUserPortfolioAsync()
        {
            var userId = _currentUserService.UserId?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var portfolio = await _portfolioRepository.GetByUserIdAsync(userId);
            if (portfolio == null)
            {
                return null;
            }

            return MapToDto(portfolio);
        }

        public async Task<bool> DeleteInvestmentAsync(Guid positionId)
        {
            var userId = _currentUserService.UserId?.ToString() 
                ?? throw new UnauthorizedAccessException("User is not authenticated");

            var portfolio = await _portfolioRepository.GetByUserIdAsync(userId);
            if (portfolio == null)
            {
                return false;
            }

            var position = portfolio.Positions.FirstOrDefault(p => p.Id == positionId);
            if (position == null)
            {
                // Let's also check if positionId actually matches AssetId directly (fallback)
                position = portfolio.Positions.FirstOrDefault(p => p.AssetId == positionId);
                if (position == null)
                {
                    return false;
                }
            }

            var assetId = position.AssetId;

            var allTransactions = await _transactionRepository.GetByUserIdAsync(userId);
            var assetTransactions = allTransactions
                .Where(t => t.PortfolioId == portfolio.Id && t.AssetId == assetId)
                .ToList();

            portfolio.RemovePositionAndRevertTransactions(assetId, assetTransactions);

            foreach (var tx in assetTransactions)
            {
                _transactionRepository.Delete(tx);
            }

            _portfolioRepository.Update(portfolio);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private static PortfolioDto MapToDto(Portfolio portfolio)
        {
            var dto = new PortfolioDto
            {
                Id = portfolio.Id,
                Name = portfolio.Name,
                Balance = portfolio.Balance,
                TotalValue = portfolio.TotalValue,
                TotalInvested = portfolio.TotalAssetsCost,
                TotalGain = portfolio.TotalReturnAmount,
                GainPercentage = portfolio.TotalReturnPercentage,
                Currency = "BRL",
                IsActive = true,
                UserId = portfolio.UserId
            };

            foreach (var pos in portfolio.Positions)
            {
                var typeStr = pos.AssetType == Domain.Aggregates.MarketData.AssetType.FixedIncome ? "fixed_income" : "variable_income";
                var subtypeStr = pos.AssetType switch
                {
                    Domain.Aggregates.MarketData.AssetType.FixedIncome => "CDB",
                    Domain.Aggregates.MarketData.AssetType.Stock => "ACAO",
                    Domain.Aggregates.MarketData.AssetType.Fii => "FII",
                    Domain.Aggregates.MarketData.AssetType.Crypto => "CRYPTO",
                    _ => "ACAO"
                };

                dto.Positions.Add(new AssetPositionDto
                {
                    Id = pos.Id,
                    PortfolioId = portfolio.Id,
                    AssetId = pos.AssetId,
                    Name = pos.Ticker,
                    Ticker = pos.Ticker,
                    Type = typeStr,
                    Subtype = subtypeStr,
                    Quantity = pos.Quantity,
                    AveragePrice = pos.AverageCost,
                    TotalInvested = pos.TotalCost,
                    CurrentPrice = pos.CurrentPrice,
                    CurrentValue = pos.CurrentValue,
                    Gain = pos.TotalReturnAmount,
                    GainPercentage = pos.TotalReturnPercentage,
                    Currency = "BRL"
                });
            }

            return dto;
        }
    }
}
