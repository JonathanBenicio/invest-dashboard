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
    public class TransacaoAppService : ITransacaoAppService
    {
        private readonly ITransacaoRepository _transacaoRepository;
        private readonly ICarteiraRepository _carteiraRepository;
        private readonly IAtivoRepository _ativoRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUsuarioAtualService _usuarioAtualService;

        public TransacaoAppService(
            ITransacaoRepository transacaoRepository,
            ICarteiraRepository carteiraRepository,
            IAtivoRepository ativoRepository,
            IUnitOfWork unitOfWork,
            IUsuarioAtualService usuarioAtualService)
        {
            _transacaoRepository = transacaoRepository;
            _carteiraRepository = carteiraRepository;
            _ativoRepository = ativoRepository;
            _unitOfWork = unitOfWork;
            _usuarioAtualService = usuarioAtualService;
        }

        public async Task<TransacaoDto> RegisterTransactionAsync(RegistrarTransacaoDto dto)
        {
            var userId = _usuarioAtualService.UserId?.ToString() 
                ?? throw new UnauthorizedAccessException("User is not authenticated");

            var carteira = await _carteiraRepository.GetByIdAsync(dto.CarteiraId)
                ?? throw new KeyNotFoundException($"Carteira {dto.CarteiraId} not found");

            if (carteira.UserId != userId)
            {
                throw new UnauthorizedAccessException("You do not own this carteira");
            }

            TipoTransacao tipoTransacao;
            if (!Enum.TryParse(dto.Type, true, out tipoTransacao))
            {
                throw new ArgumentException($"Invalid transaction type '{dto.Type}'. Allowed: Deposit, Withdrawal, Buy, Sell");
            }

            Ativo? ativo = null;
            if (tipoTransacao == TipoTransacao.Buy || tipoTransacao == TipoTransacao.Sell)
            {
                if (string.IsNullOrWhiteSpace(dto.Ticker))
                {
                    throw new ArgumentException("Ticker is required for Buy/Sell transactions");
                }

                var cleanTicker = dto.Ticker.Trim().ToUpperInvariant();
                ativo = await _ativoRepository.GetByTickerAsync(cleanTicker);

                if (ativo == null)
                {
                    ativo = CriarAtivoPorTicker(cleanTicker, dto.UnitPrice);
                    await _ativoRepository.AddAsync(ativo);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            var transacao = new Transacao(
                Guid.NewGuid(),
                userId,
                dto.CarteiraId,
                ativo?.Id,
                ativo?.Ticker,
                tipoTransacao,
                dto.Quantity,
                tipoTransacao == TipoTransacao.Deposit || tipoTransacao == TipoTransacao.Withdrawal ? 1.0m : dto.UnitPrice,
                dto.BrokerageFee,
                dto.TransactionDate,
                dto.Notes
            );

            carteira.ProcessTransaction(
                transacao, 
                ativo?.CurrentPrice ?? 1.0m, 
                ativo?.TipoAtivo ?? TipoAtivo.Acao
            );

            await _transacaoRepository.AddAsync(transacao);
            _carteiraRepository.Update(carteira);

            await _unitOfWork.SaveChangesAsync();

            return MapearParaDto(transacao);
        }

        public async Task<List<TransacaoDto>> GetTransactionsByPortfolioIdAsync(Guid portfolioId)
        {
            var userId = _usuarioAtualService.UserId?.ToString()
                ?? throw new UnauthorizedAccessException("User is not authenticated");

            var carteira = await _carteiraRepository.GetByIdAsync(portfolioId);
            if (carteira == null || carteira.UserId != userId)
            {
                return new List<TransacaoDto>();
            }

            var allTransacoes = await _transacaoRepository.GetByUserIdAsync(userId);
            return allTransacoes
                .Where(t => t.CarteiraId == portfolioId)
                .Select(MapearParaDto)
                .ToList();
        }

        private static TransacaoDto MapearParaDto(Transacao tx)
        {
            return new TransacaoDto
            {
                Id = tx.Id,
                CarteiraId = tx.CarteiraId,
                AtivoId = tx.AtivoId,
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

        private static Ativo CriarAtivoPorTicker(string ticker, decimal price)
        {
            var cleanTicker = ticker.Trim().ToUpperInvariant();
            
            if ((cleanTicker.Length == 6 || cleanTicker.Length == 5) && cleanTicker.EndsWith("11"))
            {
                return new FundoImobiliario(Guid.NewGuid(), cleanTicker, $"{cleanTicker} Fundo Imobiliário", price, DateTime.UtcNow, "Outros");
            }
            else if (cleanTicker.EndsWith("3") || cleanTicker.EndsWith("4") || cleanTicker.EndsWith("5") || cleanTicker.EndsWith("6"))
            {
                return new Acao(Guid.NewGuid(), cleanTicker, $"{cleanTicker} Ação", price, DateTime.UtcNow, "Outros");
            }
            else if (cleanTicker.Length >= 3 && cleanTicker.Length <= 4 && !cleanTicker.Any(char.IsDigit))
            {
                return new Criptoativo(Guid.NewGuid(), cleanTicker, $"{cleanTicker} Criptoativo", price, DateTime.UtcNow, "Mainnet");
            }

            return new Acao(Guid.NewGuid(), cleanTicker, $"{cleanTicker} Ativo", price, DateTime.UtcNow, "Outros");
        }
    }
}
