using System;
using System.Linq;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Application.DTOs.Common;
using System.Security.Authentication;

namespace InvestDashboard.Application.Services
{
    public class CarteiraAppService : ICarteiraAppService
    {
        private readonly ICarteiraRepository _carteiraRepository;
        private readonly ITransacaoRepository _transacaoRepository;
        private readonly IPrecoHistoricoRepository _precoHistoricoRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUsuarioAtualService _usuarioAtualService;

        public CarteiraAppService(
            ICarteiraRepository carteiraRepository,
            ITransacaoRepository transacaoRepository,
            IPrecoHistoricoRepository precoHistoricoRepository,
            IUnitOfWork unitOfWork,
            IUsuarioAtualService usuarioAtualService)
        {
            _carteiraRepository = carteiraRepository;
            _transacaoRepository = transacaoRepository;
            _precoHistoricoRepository = precoHistoricoRepository;
            _unitOfWork = unitOfWork;
            _usuarioAtualService = usuarioAtualService;
        }

        public async Task<CarteiraDto> CreatePortfolioAsync(CriarCarteiraDto dto)
        {
            var userId = _usuarioAtualService.UserId?.ToString()
                ?? throw new UnauthorizedAccessException("User is not authenticated");

            var carteira = new Carteira(Guid.NewGuid(), userId, dto.Name, description: dto.Description);
            await _carteiraRepository.AddAsync(carteira);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(carteira);
        }

        public async Task<CarteiraDto?> GetUserPortfolioAsync()
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrEmpty(userId)) return null;

            var carteira = await _carteiraRepository.GetByUserIdAsync(userId);
            return carteira != null ? MapToDto(carteira) : null;
        }

        public async Task<CarteiraDto?> GetPortfolioByIdAsync(Guid id)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var carteira = await _carteiraRepository.GetByIdForUserAsync(id, userId);
            return carteira != null ? MapToDto(carteira) : null;
        }

        public async Task<PaginatedResponse<CarteiraDto>> GetUserPortfoliosAsync(int page = 1, int pageSize = 10)
        {
            if (page < 1 || pageSize is < 1 or > 100)
                throw new ArgumentException("Page must be positive and page size must be between 1 and 100.");

            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
                throw new AuthenticationException("Authentication is required.");

            var totalCount = await _carteiraRepository.CountByUserIdAsync(userId);
            var portfolios = await _carteiraRepository.GetByUserIdPageAsync(userId, (page - 1) * pageSize, pageSize);

            return new PaginatedResponse<CarteiraDto>(
                portfolios.Select(MapToDto).ToList(),
                page,
                pageSize,
                totalCount);
        }

        public async Task<IReadOnlyList<PosicaoInvestimentoDto>> GetUserPositionsAsync(Guid? portfolioId = null)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
                throw new AuthenticationException("Authentication is required.");

            if (portfolioId.HasValue)
            {
                var ownedPortfolio = await _carteiraRepository.GetByIdForUserAsync(portfolioId.Value, userId);
                return ownedPortfolio?.Positions.Select(MapToPositionDto).ToList() ?? [];
            }

            var total = await _carteiraRepository.CountByUserIdAsync(userId);
            var positions = new List<PosicaoInvestimentoDto>();
            for (var skip = 0; skip < total; skip += 100)
            {
                var portfolios = await _carteiraRepository.GetByUserIdPageAsync(userId, skip, 100);
                positions.AddRange(portfolios.SelectMany(portfolio => portfolio.Positions).Select(MapToPositionDto));
            }

            return positions;
        }

        public async Task<PosicaoInvestimentoDto?> GetPositionByIdAsync(Guid positionId)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var result = await _carteiraRepository.GetPositionByIdForUserAsync(positionId, userId);
            return result.HasValue ? MapToPositionDto(result.Value.Position) : null;
        }

        public async Task<PosicaoInvestimentoDto?> UpdatePositionValuationAsync(Guid positionId, decimal totalValue, DateTime observedAtUtc)
        {
            if (totalValue < 0)
                throw new ArgumentOutOfRangeException(nameof(totalValue), "Statement value cannot be negative.");

            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
                throw new AuthenticationException("Authentication is required.");

            var result = await _carteiraRepository.GetPositionByIdForUserAsync(positionId, userId);
            if (!result.HasValue) return null;

            var position = result.Value.Position;
            if (position.Quantity <= 0)
                throw new InvalidOperationException("A closed position cannot be valued.");

            var price = totalValue / position.Quantity;
            position.UpdateCurrentPrice(price);
            await _precoHistoricoRepository.AddAsync(new PrecoHistorico(
                Guid.NewGuid(),
                position.AtivoId,
                price,
                observedAtUtc,
                "statement"));

            _carteiraRepository.Update(result.Value.Portfolio);
            await _unitOfWork.SaveChangesAsync();
            return MapToPositionDto(position);
        }

        public async Task<IReadOnlyList<PrecoHistoricoDto>> GetPriceHistoryAsync(Guid positionId, DateTime? fromDate = null)
        {
            var position = await GetPositionByIdAsync(positionId);
            if (position is null) return [];

            var prices = await _precoHistoricoRepository.GetByAtivoIdAsync(position.AtivoId, fromDate);
            return prices.Select(price => new PrecoHistoricoDto
            {
                Date = price.Date,
                Price = price.Price,
                Source = price.Source,
                IsAdjusted = price.IsAdjusted
            }).ToList();
        }

        public async Task<IReadOnlyList<PontoHistoricoCarteiraDto>?> GetPortfolioHistoryAsync(
            Guid portfolioId,
            DateOnly? fromDate,
            DateOnly? toDate)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
                throw new AuthenticationException("Authentication is required.");

            var portfolio = await _carteiraRepository.GetByIdForUserAsync(portfolioId, userId);
            if (portfolio is null) return null;

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = fromDate ?? today.AddYears(-1);
            var end = toDate ?? today;
            if (start > end || end > today || end.DayNumber - start.DayNumber > 3650)
                throw new ArgumentException("The requested history range is invalid or exceeds ten years.");

            var operations = await _transacaoRepository.GetByPortfolioIdAsync(portfolioId);
            var transactions = operations.Where(transaction => transaction.AtivoId.HasValue).ToList();
            var assetIds = transactions.Select(transaction => transaction.AtivoId!.Value).Distinct().ToArray();
            var prices = await _precoHistoricoRepository.GetByAtivoIdsAsync(assetIds);
            var dailyPrices = prices
                .Where(price => !price.IsAdjusted)
                .GroupBy(price => price.AtivoId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(price => price.Date).ToArray());

            var dates = prices.Select(price => DateOnly.FromDateTime(price.Date))
                .Concat(transactions.Select(transaction => DateOnly.FromDateTime(transaction.TransactionDate)))
                .Where(date => date >= start && date <= end)
                .Distinct()
                .OrderBy(date => date)
                .ToArray();

            var result = new List<PontoHistoricoCarteiraDto>(dates.Length);
            foreach (var date in dates)
            {
                var dayEnd = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                var quantities = transactions
                    .Where(transaction => transaction.TransactionDate <= dayEnd)
                    .GroupBy(transaction => transaction.AtivoId!.Value)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Sum(transaction => transaction.Type == TipoTransacao.Buy
                            ? transaction.Quantity
                            : transaction.Type == TipoTransacao.Sell ? -transaction.Quantity : 0));

                decimal value = 0;
                var missingTickers = new List<string>();
                foreach (var (assetId, quantity) in quantities.Where(entry => entry.Value > 0))
                {
                    if (!dailyPrices.TryGetValue(assetId, out var series))
                    {
                        missingTickers.Add(transactions.First(transaction => transaction.AtivoId == assetId).Ticker ?? assetId.ToString());
                        continue;
                    }

                    var latest = series.LastOrDefault(price => DateOnly.FromDateTime(price.Date) <= date);
                    if (latest is null)
                    {
                        missingTickers.Add(transactions.First(transaction => transaction.AtivoId == assetId).Ticker ?? assetId.ToString());
                        continue;
                    }

                    value += quantity * latest.Price;
                    if (latest.Source == "statement" && DateOnly.FromDateTime(latest.Date) < date)
                        missingTickers.Add(transactions.First(transaction => transaction.AtivoId == assetId).Ticker ?? assetId.ToString());
                }

                result.Add(new PontoHistoricoCarteiraDto
                {
                    Date = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    TotalValue = missingTickers.Count == 0 ? value : null,
                    IsComplete = missingTickers.Count == 0,
                    MissingTickers = missingTickers.Distinct().Order().ToList()
                });
            }

            return result;
        }

        public async Task<bool> DeleteInvestmentAsync(Guid positionId)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var result = await _carteiraRepository.GetPositionByIdForUserAsync(positionId, userId);
            if (!result.HasValue) return false;

            var (carteira, position) = result.Value;
            var investmentTransactions = await _transacaoRepository.GetByPortfolioIdAsync(carteira.Id);
            foreach (var transaction in investmentTransactions.Where(transaction => transaction.AtivoId == position.AtivoId))
                _transacaoRepository.Delete(transaction);

            carteira.RemovePosition(position.Id);
            _carteiraRepository.Update(carteira);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<CarteiraDto?> UpdatePortfolioAsync(Guid portfolioId, AtualizarCarteiraDto dto)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrEmpty(userId)) return null;

            var carteira = await _carteiraRepository.GetByIdForUserAsync(portfolioId, userId);
            if (carteira == null) return null;

            carteira.UpdateDetails(dto.Name, dto.Description);

            _carteiraRepository.Update(carteira);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(carteira);
        }

        public async Task<bool> DeletePortfolioAsync(Guid portfolioId)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrEmpty(userId)) return false;

            var carteira = await _carteiraRepository.GetByIdForUserAsync(portfolioId, userId);
            if (carteira == null) return false;

            _carteiraRepository.Delete(carteira);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private static CarteiraDto MapToDto(Carteira carteira)
        {
            return new CarteiraDto
            {
                Id = carteira.Id,
                Name = carteira.Name,
                Description = carteira.Description,
                TotalValue = carteira.TotalAssetsValue,
                TotalInvested = carteira.TotalAssetsCost + carteira.RealizedCostBasis,
                TotalGain = carteira.RealizedGain + carteira.TotalReturnAmount,
                UnrealizedGain = carteira.TotalReturnAmount,
                RealizedGain = carteira.RealizedGain,
                GainPercentage = carteira.TotalAssetsCost + carteira.RealizedCostBasis > 0
                    ? (carteira.RealizedGain + carteira.TotalReturnAmount) / (carteira.TotalAssetsCost + carteira.RealizedCostBasis) * 100
                    : 0,
                Currency = "BRL",
                AssetsCount = carteira.Positions.Count(position => position.Quantity > 0),
                Positions = carteira.Positions.Select(MapToPositionDto).ToList()
            };
        }

        private static PosicaoInvestimentoDto MapToPositionDto(PosicaoInvestimento pos)
        {
            return new PosicaoInvestimentoDto
            {
                Id = pos.Id,
                CarteiraId = pos.CarteiraId,
                AtivoId = pos.AtivoId,
                Name = pos.Ativo?.Name ?? pos.Ticker,
                Ticker = pos.Ticker,
                Type = pos.TipoAtivo == TipoAtivo.RendaFixa ? "fixed_income" : "variable_income",
                Subtype = pos.Ativo?.Subtype ?? pos.TipoAtivo.ToString(),
                Sector = pos.Ativo switch
                {
                    Acao stock => stock.Sector,
                    FundoImobiliario fund => fund.Segment,
                    _ => null
                },
                Status = pos.Quantity > 0 ? "open" : "closed",
                PurchaseDate = pos.PurchaseDateUtc,
                Issuer = (pos.Ativo as RendaFixa)?.Issuer,
                InterestRate = (pos.Ativo as RendaFixa)?.InterestRate,
                Indexer = (pos.Ativo as RendaFixa)?.Indexer,
                MaturityDate = (pos.Ativo as RendaFixa)?.MaturityDate,
                Quantity = pos.Quantity,
                AveragePrice = pos.AverageCost,
                CurrentPrice = pos.ValuationPrice,
                TotalInvested = pos.TotalCost,
                CurrentValue = pos.CurrentValue,
                Gain = pos.TotalReturnAmount,
                GainPercentage = pos.TotalReturnPercentage,
                Currency = "BRL"
            };
        }
    }
}
