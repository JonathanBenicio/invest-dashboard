using System;
using System.Linq;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Portfolio;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services
{
    public class CarteiraAppService : ICarteiraAppService
    {
        private readonly ICarteiraRepository _carteiraRepository;
        private readonly ITransacaoRepository _transacaoRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUsuarioAtualService _usuarioAtualService;

        public CarteiraAppService(
            ICarteiraRepository carteiraRepository,
            ITransacaoRepository transacaoRepository,
            IUnitOfWork unitOfWork,
            IUsuarioAtualService usuarioAtualService)
        {
            _carteiraRepository = carteiraRepository;
            _transacaoRepository = transacaoRepository;
            _unitOfWork = unitOfWork;
            _usuarioAtualService = usuarioAtualService;
        }

        public async Task<CarteiraDto> CreatePortfolioAsync(CriarCarteiraDto dto)
        {
            var userId = _usuarioAtualService.UserId?.ToString()
                ?? throw new UnauthorizedAccessException("User is not authenticated");

            var carteira = new Carteira(Guid.NewGuid(), userId, dto.Name, dto.SaldoInicial);
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
            var carteira = await _carteiraRepository.GetByIdAsync(id);
            return carteira != null ? MapToDto(carteira) : null;
        }

        public async Task<bool> DeleteInvestmentAsync(Guid positionId)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrEmpty(userId)) return false;

            var carteira = await _carteiraRepository.GetByUserIdAsync(userId);
            if (carteira == null) return false;

            var position = carteira.Positions.FirstOrDefault(p => p.Id == positionId);
            if (position == null) return false;

            var ativoId = position.AtivoId;
            carteira.RemovePositionAndRevertTransactions(ativoId, Enumerable.Empty<Transacao>());

            _carteiraRepository.Update(carteira);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private static CarteiraDto MapToDto(Carteira carteira)
        {
            return new CarteiraDto
            {
                Id = carteira.Id,
                Name = carteira.Name,
                Balance = carteira.Balance,
                TotalValue = carteira.TotalValue,
                TotalInvested = carteira.TotalAssetsCost,
                TotalGain = carteira.TotalReturnAmount,
                GainPercentage = carteira.TotalReturnPercentage,
                Currency = "BRL",
                IsActive = true,
                UserId = carteira.UserId,
                AssetsCount = carteira.Positions.Count,
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
                Name = pos.Ticker,
                Ticker = pos.Ticker,
                Type = pos.TipoAtivo == TipoAtivo.RendaFixa ? "fixed_income" : "variable_income",
                Subtype = pos.TipoAtivo.ToString(),
                Quantity = pos.Quantity,
                AveragePrice = pos.AverageCost,
                CurrentPrice = pos.CurrentPrice,
                TotalInvested = pos.TotalCost,
                CurrentValue = pos.CurrentValue,
                Gain = pos.TotalReturnAmount,
                GainPercentage = pos.TotalReturnPercentage,
                Currency = "BRL"
            };
        }
    }
}
