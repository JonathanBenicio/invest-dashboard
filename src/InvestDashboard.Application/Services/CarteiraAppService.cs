using System;
using System.Globalization;
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
        private readonly IGrupoCarteirasRepository _grupoRepository;
        private readonly ITaxaEconomicaRepository _taxaRepository;
        private readonly ITitularCarteiraRepository _titularRepository;
        private readonly IInstituicaoFinanceiraRepository _instituicaoRepository;
        private readonly TimeProvider _timeProvider;

        public CarteiraAppService(
            ICarteiraRepository carteiraRepository,
            ITransacaoRepository transacaoRepository,
            IPrecoHistoricoRepository precoHistoricoRepository,
            IUnitOfWork unitOfWork,
            IUsuarioAtualService usuarioAtualService,
            IGrupoCarteirasRepository grupoRepository,
            ITaxaEconomicaRepository taxaRepository,
            ITitularCarteiraRepository titularRepository,
            IInstituicaoFinanceiraRepository instituicaoRepository,
            TimeProvider timeProvider)
        {
            _carteiraRepository = carteiraRepository;
            _transacaoRepository = transacaoRepository;
            _precoHistoricoRepository = precoHistoricoRepository;
            _unitOfWork = unitOfWork;
            _usuarioAtualService = usuarioAtualService;
            _grupoRepository = grupoRepository;
            _taxaRepository = taxaRepository;
            _titularRepository = titularRepository;
            _instituicaoRepository = instituicaoRepository;
            _timeProvider = timeProvider;
        }

        public async Task<CarteiraDto> CreatePortfolioAsync(CriarCarteiraDto dto)
        {
            var userId = _usuarioAtualService.UserId?.ToString()
                ?? throw new UnauthorizedAccessException("User is not authenticated");

            var groupId = dto.GrupoId;
            if (!groupId.HasValue)
            {
                var existingGroups = await _grupoRepository.GetGroupsForUserAsync(userId);
                if (existingGroups.Count > 0) groupId = existingGroups[0].Group.Id;
                else
                {
                    if (string.IsNullOrWhiteSpace(_usuarioAtualService.Email))
                        throw new UnauthorizedAccessException("A conta precisa ter um e-mail confirmado para criar o grupo inicial.");
                    var group = new GrupoCarteiras(Guid.NewGuid(), "Meu grupo", userId, DateTime.UtcNow);
                    await _grupoRepository.AddGroupAsync(group);
                    await _grupoRepository.AddMemberAsync(new MembroGrupo(Guid.NewGuid(), group.Id, userId,
                        _usuarioAtualService.Email, _usuarioAtualService.Name ?? string.Empty, PapelGrupo.Admin, DateTime.UtcNow));
                    await _unitOfWork.SaveChangesAsync();
                    groupId = group.Id;
                }
            }

            var membership = await _grupoRepository.GetMemberAsync(groupId.Value, userId);
            if (membership is null || !membership.Ativo || membership.Papel == PapelGrupo.Consulta)
                throw new UnauthorizedAccessException("Você não pode criar carteiras neste grupo.");
            if (!Enum.TryParse<VisibilidadeCarteira>(dto.Visibilidade, true, out var visibility) || !Enum.IsDefined(visibility))
                throw new ArgumentException("Visibilidade deve ser Particular ou PublicaDoGrupo.");
            var holder = await ResolveTitularAsync(groupId.Value, dto.Titular, dto.TitularId, dto.TitularUsuarioId,
                dto.Parentesco, userId, membership, visibility);
            var institution = await ResolveInstitutionAsync(groupId.Value, dto.InstituicaoFinanceiraId,
                dto.InstituicaoFinanceira, dto.TipoInstituicao);
            if (institution is null) throw new ArgumentException("Selecione uma instituição financeira para a carteira.");

            var carteira = new Carteira(Guid.NewGuid(), userId, dto.Name, description: dto.Description,
                grupoId: groupId.Value, titular: holder.Nome, instituicaoFinanceira: institution?.Nome,
                tipoInstituicao: institution?.Categoria,
                visibilidade: visibility, titularId: holder.Id, instituicaoFinanceiraId: institution?.Id);
            carteira.AssociarTitular(holder);
            carteira.AssociarInstituicao(institution);
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

        public async Task<RespostaPaginada<CarteiraDto>> GetUserPortfoliosAsync(int page = 1, int pageSize = 10, Guid? grupoId = null)
        {
            if (page < 1 || pageSize is < 1 or > 100)
                throw new ArgumentException("Page must be positive and page size must be between 1 and 100.");

            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
                throw new AuthenticationException("Authentication is required.");

            await RequireActiveGroupMembershipAsync(grupoId, userId);
            var totalCount = await _carteiraRepository.CountByUserIdAsync(userId, grupoId);
            var portfolios = await _carteiraRepository.GetByUserIdPageAsync(userId, (page - 1) * pageSize, pageSize, grupoId);

            return new RespostaPaginada<CarteiraDto>(
                portfolios.Select(MapToDto).ToList(),
                page,
                pageSize,
                totalCount);
        }

        public async Task<ResumoCarteirasDto> GetUserPortfoliosSummaryAsync(Guid? grupoId = null)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId)) throw new AuthenticationException("Authentication is required.");
            await RequireActiveGroupMembershipAsync(grupoId, userId);
            var count = await _carteiraRepository.CountByUserIdAsync(userId, grupoId);
            decimal value = 0, invested = 0, gain = 0;
            for (var skip = 0; skip < count; skip += 100)
            {
                var portfolios = await _carteiraRepository.GetByUserIdPageAsync(userId, skip, 100, grupoId);
                value += portfolios.Sum(portfolio => portfolio.TotalAssetsValue);
                invested += portfolios.Sum(portfolio => portfolio.TotalAssetsCost + portfolio.RealizedCostBasis);
                gain += portfolios.Sum(portfolio => portfolio.TotalReturnAmount + portfolio.RealizedGain);
            }
            return new ResumoCarteirasDto(value, invested, gain, invested > 0 ? gain / invested * 100 : 0, count);
        }

        public async Task<ProjecaoRendaFixaConsolidadaDto> GetFixedIncomeProjectionAsync(Guid? grupoId = null)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId)) throw new AuthenticationException("Authentication is required.");
            await RequireActiveGroupMembershipAsync(grupoId, userId);
            var count = await _carteiraRepository.CountByUserIdAsync(userId, grupoId);
            var positions = new List<(Guid? GroupId, PosicaoInvestimento Position)>();
            for (var skip = 0; skip < count; skip += 100)
                positions.AddRange((await _carteiraRepository.GetByUserIdPageAsync(userId, skip, 100, grupoId))
                    .SelectMany(portfolio => portfolio.Positions
                        .Where(position => position.TipoAtivo == TipoAtivo.RendaFixa && position.Quantity > 0)
                        .Select(position => (portfolio.GrupoId, position))));

            var estimates = new List<ProjecaoRendaFixaDto>();
            foreach (var item in positions)
            {
                var estimate = await CalculateFixedIncomeProjectionAsync(item.Position, item.GroupId);
                estimates.Add(estimate);
            }
            var missing = estimates.Where(item => item.ValorProjetadoBruto is null)
                .Select(item => $"{item.Ticker}: {item.Motivo ?? "premissa ausente"}")
                .ToList();
            var complete = missing.Count == 0;
            return new ProjecaoRendaFixaConsolidadaDto(
                estimates.Sum(item => item.ValorObservado),
                complete ? estimates.Sum(item => item.ValorProjetadoBruto!.Value) : null,
                estimates.Count,
                complete,
                missing);
        }

        public async Task<ProjecaoRendaFixaDto?> GetFixedIncomeProjectionAsync(Guid positionId)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId)) throw new AuthenticationException("Authentication is required.");
            var position = await _carteiraRepository.GetPositionByIdForUserAsync(positionId, userId);
            if (!position.HasValue || position.Value.Position.TipoAtivo != TipoAtivo.RendaFixa) return null;
            return await CalculateFixedIncomeProjectionAsync(position.Value.Position, position.Value.Portfolio.GrupoId);
        }

        private async Task<ProjecaoRendaFixaDto> CalculateFixedIncomeProjectionAsync(PosicaoInvestimento position, Guid? groupId)
        {
            var asset = position.Ativo as RendaFixa;
            var priceHistory = await _precoHistoricoRepository.GetByAtivoIdAsync(position.AtivoId);
            var latestStatement = priceHistory.Where(price => price.Source == "statement").OrderByDescending(price => price.Date).FirstOrDefault();
            var observedValue = latestStatement is null ? position.CurrentValue : latestStatement.Price * position.Quantity;
            var maturityDate = asset?.MaturityDate ?? DateTime.MinValue;
            if (asset is null || latestStatement is null)
                return new ProjecaoRendaFixaDto(position.Id, position.Ticker, observedValue, latestStatement?.Date, null, maturityDate, "sem_base_observada", "Registre um valor de extrato antes de estimar.");
            if (!string.Equals(asset.Convention?.Trim(), "365 dias corridos", StringComparison.OrdinalIgnoreCase))
                return new ProjecaoRendaFixaDto(position.Id, position.Ticker, observedValue, latestStatement.Date, null, maturityDate, "sem_premissa", "Informe a taxa contratual e a convenção '365 dias corridos'.");
            if (asset.InterestRate <= 0)
                return new ProjecaoRendaFixaDto(position.Id, position.Ticker, observedValue, latestStatement.Date, null, maturityDate, "sem_premissa", "Informe uma taxa contratual positiva.");

            var observationDate = DateOnly.FromDateTime(latestStatement.Date);
            var maturityDay = DateOnly.FromDateTime(maturityDate);
            if (maturityDay <= observationDate)
                return new ProjecaoRendaFixaDto(position.Id, position.Ticker, observedValue, latestStatement.Date, observedValue, maturityDate, "vencido", "A estimativa preserva o valor observado e não gera saldo em caixa.");
            var days = maturityDay.DayNumber - observationDate.DayNumber;
            decimal annualRate;
            TaxaEconomica? observedRate = null;
            int? rateAge = null;
            int? rateValidity = null;
            string? rateValidityUnit = null;
            if (string.Equals(asset.Indexer, "PREFIXADO", StringComparison.OrdinalIgnoreCase))
                annualRate = asset.InterestRate / 100m;
            else
            {
                if (!groupId.HasValue)
                    return new ProjecaoRendaFixaDto(position.Id, position.Ticker, observedValue, latestStatement.Date, null, maturityDate, "sem_premissa", "A taxa do indexador precisa estar cadastrada no grupo.");
                observedRate = (await _taxaRepository.GetAllInGroupAsync(groupId.Value))
                    .Where(rate => string.Equals(rate.Symbol, asset.Indexer, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(rate.Unit, "Percentual", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(rate => rate.ReferenceDate).FirstOrDefault();
                if (observedRate is null)
                    return new ProjecaoRendaFixaDto(position.Id, position.Ticker, observedValue, latestStatement.Date, null, maturityDate, "sem_premissa", $"Cadastre uma taxa {asset.Indexer} periódica e datada no grupo.");
                if (string.Equals(observedRate.Periodicity, "Pontual", StringComparison.OrdinalIgnoreCase))
                    return MapProjection(position, observedValue, latestStatement.Date, null, maturityDate, "sem_premissa",
                        $"A taxa {observedRate.Symbol} observada em {observedRate.ReferenceDate:dd/MM/yyyy} é pontual e não pode ser usada na projeção.",
                        observedRate, null, null, null);

                var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
                    _timeProvider.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).DateTime);
                var freshness = GetRateFreshness(observedRate, localToday);
                rateAge = freshness.Age;
                rateValidity = freshness.Validity;
                rateValidityUnit = freshness.Unit;
                if (freshness.IsStale)
                    return MapProjection(position, observedValue, latestStatement.Date, null, maturityDate, "taxa_desatualizada",
                        $"Atualize {observedRate.Symbol}: o valor observado ({observedRate.CurrentValue.ToString("0.####", CultureInfo.GetCultureInfo("pt-BR"))} {observedRate.Unit}) é de {observedRate.ReferenceDate:dd/MM/yyyy} e ultrapassou a validade de {freshness.Validity} {freshness.Unit}.",
                        observedRate, rateAge, rateValidity, rateValidityUnit);

                var indexRate = ToAnnualRate(observedRate.CurrentValue, observedRate.Periodicity);
                annualRate = string.Equals(asset.Indexer, "IPCA", StringComparison.OrdinalIgnoreCase)
                    ? (1m + indexRate) * (1m + asset.InterestRate / 100m) - 1m
                    : indexRate * asset.InterestRate / 100m;
            }
            var projected = observedValue * (decimal)Math.Pow((double)(1 + annualRate), days / 365d);
            return MapProjection(position, observedValue, latestStatement.Date,
                decimal.Round(projected, 2, MidpointRounding.AwayFromZero), maturityDate, "estimativa_bruta",
                "Estimativa bruta com taxa do contrato e premissa vigente do grupo; não inclui impostos, aportes ou reinvestimento.",
                observedRate, rateAge, rateValidity, rateValidityUnit);
        }

        private static ProjecaoRendaFixaDto MapProjection(
            PosicaoInvestimento position,
            decimal observedValue,
            DateTime? observedAt,
            decimal? projectedValue,
            DateTime maturityDate,
            string state,
            string? reason,
            TaxaEconomica? rate,
            int? rateAge,
            int? rateValidity,
            string? rateValidityUnit) =>
            new(position.Id, position.Ticker, observedValue, observedAt, projectedValue, maturityDate, state, reason,
                rate?.Symbol, rate?.CurrentValue, rate?.Unit, rate?.Periodicity, rate?.ReferenceDate, rate?.Source,
                rateAge, rateValidity, rateValidityUnit);

        private static RateFreshness GetRateFreshness(TaxaEconomica rate, DateOnly today)
        {
            var ageDays = today.DayNumber - rate.ReferenceDate.DayNumber;
            if (ageDays < 0) return new(true, 0, 0, "dias corridos");

            return rate.Periodicity.ToUpperInvariant() switch
            {
                "DIARIA" => new(CalendarioFinanceiroBrasileiro.ContarDiasUteis(rate.ReferenceDate, today) > 5,
                    CalendarioFinanceiroBrasileiro.ContarDiasUteis(rate.ReferenceDate, today), 5, "dias úteis"),
                "MENSAL" => new(ageDays > 45, ageDays, 45, "dias corridos"),
                "ANUAL" => new(ageDays > 400, ageDays, 400, "dias corridos"),
                _ => new(true, ageDays, 0, "dias corridos")
            };
        }

        private sealed record RateFreshness(bool IsStale, int Age, int Validity, string Unit);

        private static decimal ToAnnualRate(decimal value, string periodicity) => periodicity.ToUpperInvariant() switch
        {
            "ANUAL" => value / 100m,
            "MENSAL" => (decimal)Math.Pow((double)(1 + value / 100m), 12) - 1m,
            "DIARIA" => (decimal)Math.Pow((double)(1 + value / 100m), 365) - 1m,
            _ => throw new ArgumentException("A periodicidade da taxa não pode ser projetada.")
        };

        public async Task<IReadOnlyList<PosicaoInvestimentoDto>> GetUserPositionsAsync(Guid? portfolioId = null, Guid? grupoId = null)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
                throw new AuthenticationException("Authentication is required.");

            await RequireActiveGroupMembershipAsync(grupoId, userId);

            if (portfolioId.HasValue)
            {
                var ownedPortfolio = await _carteiraRepository.GetByIdForUserAsync(portfolioId.Value, userId);
                return ownedPortfolio is not null && (!grupoId.HasValue || ownedPortfolio.GrupoId == grupoId)
                    ? ownedPortfolio.Positions.Select(position => MapToPositionDto(position, ownedPortfolio)).ToList()
                    : [];
            }

            var total = await _carteiraRepository.CountByUserIdAsync(userId, grupoId);
            var positions = new List<PosicaoInvestimentoDto>();
            for (var skip = 0; skip < total; skip += 100)
            {
                var portfolios = await _carteiraRepository.GetByUserIdPageAsync(userId, skip, 100, grupoId);
                positions.AddRange(portfolios.SelectMany(portfolio => portfolio.Positions
                    .Select(position => MapToPositionDto(position, portfolio))));
            }

            return positions;
        }

        private async Task RequireActiveGroupMembershipAsync(Guid? grupoId, string userId)
        {
            if (!grupoId.HasValue) return;
            var member = await _grupoRepository.GetMemberAsync(grupoId.Value, userId);
            if (member is null || !member.Ativo)
                throw new UnauthorizedAccessException("Active membership in this group is required.");
        }

        public async Task<PosicaoInvestimentoDto?> GetPositionByIdAsync(Guid positionId)
        {
            var userId = _usuarioAtualService.UserId?.ToString();
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var result = await _carteiraRepository.GetPositionByIdForUserAsync(positionId, userId);
            return result.HasValue ? MapToPositionDto(result.Value.Position, result.Value.Portfolio) : null;
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
            if (!await _carteiraRepository.CanManageAsync(result.Value.Portfolio.Id, userId)) return null;

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
            return MapToPositionDto(position, result.Value.Portfolio);
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
            if (!await _carteiraRepository.CanManageAsync(result.Value.Portfolio.Id, userId)) return false;

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
            if (!await _carteiraRepository.CanManageAsync(portfolioId, userId)) return null;

            var visibility = carteira.Visibilidade;
            if (dto.Visibilidade is not null &&
                (!Enum.TryParse(dto.Visibilidade, true, out visibility) || !Enum.IsDefined(visibility)))
                throw new ArgumentException("Visibilidade deve ser Particular ou PublicaDoGrupo.");

            MembroGrupo? membership = null;
            if (carteira.GrupoId.HasValue)
            {
                membership = await _grupoRepository.GetMemberAsync(carteira.GrupoId.Value, userId);
                if (membership is null || !membership.Ativo) return null;
                if (visibility == VisibilidadeCarteira.Particular && membership.Papel != PapelGrupo.Admin &&
                    carteira.TitularProfile?.UsuarioId != userId)
                    throw new UnauthorizedAccessException("Somente o titular vinculado ou um Admin pode tornar a carteira particular.");
            }

            var currentHolderName = carteira.TitularProfile?.Nome ?? carteira.Titular;
            var holderChanged = dto.TitularId.HasValue && dto.TitularId != carteira.TitularId ||
                dto.Titular is not null && !string.Equals(dto.Titular.Trim(), currentHolderName, StringComparison.Ordinal) ||
                dto.TitularUsuarioId is not null && dto.TitularUsuarioId != carteira.TitularProfile?.UsuarioId ||
                dto.Parentesco is not null && dto.Parentesco != carteira.TitularProfile?.Parentesco ||
                dto.DesvincularTitular && carteira.TitularProfile?.UsuarioId is not null;

            if (holderChanged && carteira.GrupoId.HasValue)
            {
                var isLinkedHolder = carteira.TitularProfile?.UsuarioId == userId;
                if (membership!.Papel != PapelGrupo.Admin && !isLinkedHolder)
                    throw new UnauthorizedAccessException("Somente o titular vinculado ou um Admin pode alterar o perfil do titular.");

                var requestedHolderId = dto.TitularId ?? carteira.TitularId;
                var profile = await ResolveTitularAsync(
                    carteira.GrupoId.Value,
                    dto.Titular ?? (requestedHolderId.HasValue ? null : currentHolderName),
                    requestedHolderId,
                    dto.DesvincularTitular ? null : dto.TitularUsuarioId,
                    dto.Parentesco ?? carteira.TitularProfile?.Parentesco,
                    userId,
                    membership,
                    visibility,
                    dto.DesvincularTitular,
                    true);
                carteira.AssociarTitular(profile);
            }
            else if (holderChanged)
            {
                carteira.UpdateOwnershipDetails(dto.Titular, null, null, visibility);
            }

            var institutionWasSpecified = dto.InstituicaoFinanceiraId.HasValue ||
                dto.InstituicaoFinanceira is not null || dto.TipoInstituicao is not null;
            if (institutionWasSpecified)
            {
                var currentInstitutionName = carteira.InstituicaoFinanceiraProfile?.Nome ?? carteira.InstituicaoFinanceira;
                Guid? requestedInstitutionId = dto.InstituicaoFinanceiraId;
                if (!requestedInstitutionId.HasValue && dto.InstituicaoFinanceira is not null &&
                    string.Equals(dto.InstituicaoFinanceira.Trim(), currentInstitutionName, StringComparison.OrdinalIgnoreCase) &&
                    (dto.TipoInstituicao is null || string.Equals(dto.TipoInstituicao, carteira.TipoInstituicao?.ToString(), StringComparison.OrdinalIgnoreCase)))
                    requestedInstitutionId = carteira.InstituicaoFinanceiraId;
                var institution = await ResolveInstitutionAsync(
                    carteira.GrupoId ?? Guid.Empty,
                    requestedInstitutionId,
                    dto.InstituicaoFinanceira,
                    dto.TipoInstituicao);
                if (institution is null)
                    throw new ArgumentException("Toda carteira precisa estar associada a uma instituição financeira.");
                carteira.AssociarInstituicao(institution);
            }

            carteira.UpdateDetails(dto.Name, dto.Description);
            carteira.UpdateOwnershipDetails(null, null, null, visibility);

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
            if (!await _carteiraRepository.CanManageAsync(portfolioId, userId)) return false;

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
                GrupoId = carteira.GrupoId,
                TitularId = carteira.TitularId,
                TitularVinculado = carteira.TitularProfile?.UsuarioId is not null,
                Titular = carteira.TitularProfile?.Nome ?? carteira.Titular,
                Parentesco = carteira.TitularProfile?.Parentesco,
                InstituicaoFinanceiraId = carteira.InstituicaoFinanceiraId,
                InstituicaoFinanceira = carteira.InstituicaoFinanceiraProfile?.Nome ?? carteira.InstituicaoFinanceira,
                TipoInstituicao = (carteira.InstituicaoFinanceiraProfile?.Categoria ?? carteira.TipoInstituicao)?.ToString(),
                Visibilidade = carteira.Visibilidade.ToString(),
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
                Positions = carteira.Positions.Select(position => MapToPositionDto(position, carteira)).ToList()
            };
        }

        private async Task<TitularCarteira> ResolveTitularAsync(
            Guid grupoId,
            string? nome,
            Guid? titularId,
            string? titularUsuarioId,
            string? parentesco,
            string usuarioId,
            MembroGrupo solicitante,
            VisibilidadeCarteira visibilidade,
            bool desvincularTitular = false,
            bool updateExistingProfile = false)
        {
            var vinculadoUsuarioId = string.IsNullOrWhiteSpace(titularUsuarioId) ? null : titularUsuarioId.Trim();
            var nomeTitular = nome?.Trim();
            if (!titularId.HasValue && string.IsNullOrWhiteSpace(nomeTitular) && vinculadoUsuarioId is null)
            {
                vinculadoUsuarioId = usuarioId;
                nomeTitular = solicitante.Nome;
            }

            if (visibilidade == VisibilidadeCarteira.Particular && solicitante.Papel != PapelGrupo.Admin)
            {
                if (desvincularTitular)
                    throw new UnauthorizedAccessException("Somente um Admin pode desvincular o titular de uma carteira particular.");
                if (vinculadoUsuarioId is not null && vinculadoUsuarioId != usuarioId)
                    throw new UnauthorizedAccessException("Uma carteira particular só pode ser vinculada à conta do próprio titular.");
                if (titularId.HasValue)
                {
                    var selectedPrivateHolder = await _titularRepository.GetByIdInGroupAsync(titularId.Value, grupoId)
                        ?? throw new KeyNotFoundException("Titular não encontrado neste grupo.");
                    if (selectedPrivateHolder.UsuarioId != usuarioId)
                        throw new UnauthorizedAccessException("Uma carteira particular só pode usar o perfil do titular da sessão.");
                    return selectedPrivateHolder;
                }
                vinculadoUsuarioId = usuarioId;
                nomeTitular = solicitante.Nome;
            }

            if (titularId.HasValue)
            {
                var selected = await _titularRepository.GetByIdInGroupAsync(titularId.Value, grupoId)
                    ?? throw new KeyNotFoundException("Titular não encontrado neste grupo.");
                var userToLink = desvincularTitular ? null : vinculadoUsuarioId ?? selected.UsuarioId;
                if (userToLink is not null)
                {
                    if (userToLink != usuarioId && solicitante.Papel != PapelGrupo.Admin)
                        throw new UnauthorizedAccessException("Somente um Admin pode vincular outro membro ao perfil de titular.");
                    var linkedMember = await _grupoRepository.GetMemberAsync(grupoId, userToLink);
                    if (linkedMember is null || !linkedMember.Ativo)
                        throw new ArgumentException("O titular vinculado precisa ser membro ativo do grupo.");
                }
                if (updateExistingProfile && userToLink is not null)
                {
                    var otherProfile = await _titularRepository.GetByUserIdAsync(grupoId, userToLink);
                    if (otherProfile is not null && otherProfile.Id != selected.Id)
                        throw new ArgumentException("Este membro já está vinculado a outro titular.");
                }
                var nextName = string.IsNullOrWhiteSpace(nomeTitular) ? selected.Nome : nomeTitular;
                var nextRelationship = parentesco ?? selected.Parentesco;
                var profileChanged = selected.UsuarioId != userToLink || selected.Nome != nextName || selected.Parentesco != nextRelationship;
                var canUpdateProfile = solicitante.Papel == PapelGrupo.Admin || selected.UsuarioId == usuarioId;
                if (profileChanged && canUpdateProfile && updateExistingProfile)
                {
                    selected.AtualizarDados(nextName, userToLink, nextRelationship, _timeProvider.GetUtcNow().UtcDateTime);
                    _titularRepository.Update(selected);
                }
                return selected;
            }

            MembroGrupo? titularMembro = null;
            if (vinculadoUsuarioId is not null)
            {
                if (vinculadoUsuarioId != usuarioId && solicitante.Papel != PapelGrupo.Admin)
                    throw new UnauthorizedAccessException("Somente um Admin pode vincular uma carteira a outro membro.");
                titularMembro = await _grupoRepository.GetMemberAsync(grupoId, vinculadoUsuarioId);
                if (titularMembro is null || !titularMembro.Ativo)
                    throw new ArgumentException("O titular vinculado precisa ser membro ativo do grupo.");
                if (titularMembro.Papel == PapelGrupo.Consulta && visibilidade == VisibilidadeCarteira.Particular && solicitante.Papel != PapelGrupo.Admin)
                    throw new UnauthorizedAccessException("A carteira particular só pode ser criada para o próprio titular por um usuário Consulta.");

                var linkedProfile = await _titularRepository.GetByUserIdAsync(grupoId, vinculadoUsuarioId);
                if (linkedProfile is not null)
                {
                    if (!string.IsNullOrWhiteSpace(parentesco) && linkedProfile.Parentesco != parentesco.Trim())
                    {
                        if (solicitante.Papel != PapelGrupo.Admin && vinculadoUsuarioId != usuarioId)
                            throw new UnauthorizedAccessException("Somente um Admin pode alterar o parentesco do titular.");
                        linkedProfile.AtualizarDados(linkedProfile.Nome, vinculadoUsuarioId, parentesco, _timeProvider.GetUtcNow().UtcDateTime);
                        _titularRepository.Update(linkedProfile);
                    }
                    return linkedProfile;
                }

                nomeTitular = titularMembro.Nome;
            }

            if (string.IsNullOrWhiteSpace(nomeTitular))
                throw new ArgumentException("Informe o nome do titular ou vincule um membro ativo.");

            if (vinculadoUsuarioId is null)
            {
                var normalizedName = TitularCarteira.NormalizeName(nomeTitular);
                var existingProfile = await _titularRepository.GetByNormalizedNameAsync(grupoId, normalizedName);
                if (existingProfile is not null)
                {
                    if (!string.IsNullOrWhiteSpace(parentesco) && existingProfile.Parentesco != parentesco.Trim())
                    {
                        if (solicitante.Papel == PapelGrupo.Admin)
                        {
                            existingProfile.AtualizarDados(existingProfile.Nome, existingProfile.UsuarioId, parentesco, _timeProvider.GetUtcNow().UtcDateTime);
                            _titularRepository.Update(existingProfile);
                        }
                    }
                    return existingProfile;
                }
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var created = new TitularCarteira(Guid.NewGuid(), grupoId, nomeTitular, vinculadoUsuarioId, parentesco, now);
            await _titularRepository.AddAsync(created);
            return created;
        }

        private async Task<InstituicaoFinanceira?> ResolveInstitutionAsync(
            Guid grupoId,
            Guid? instituicaoId,
            string? nome,
            string? categoria)
        {
            CategoriaInstituicaoFinanceira? expectedCategory = null;
            if (categoria is not null)
            {
                if (!Enum.TryParse<CategoriaInstituicaoFinanceira>(categoria, true, out var parsed) || !Enum.IsDefined(parsed))
                    throw new ArgumentException("Categoria de instituição inválida.");
                expectedCategory = parsed;
            }

            if (instituicaoId.HasValue)
            {
                var selected = await _instituicaoRepository.GetByIdAsync(instituicaoId.Value)
                    ?? throw new KeyNotFoundException("Instituição financeira não encontrada.");
                if (selected.GrupoId.HasValue && selected.GrupoId != grupoId)
                    throw new UnauthorizedAccessException("A instituição personalizada pertence a outro grupo.");
                if (expectedCategory.HasValue && expectedCategory.Value != selected.Categoria)
                    throw new ArgumentException("A categoria enviada não corresponde à instituição selecionada.");
                return selected;
            }

            if (string.IsNullOrWhiteSpace(nome)) return null;
            var normalizedName = InstituicaoFinanceira.NormalizeName(nome);
            var existing = await _instituicaoRepository.GetByNameAsync(grupoId, normalizedName);
            if (existing is not null)
            {
                if (expectedCategory.HasValue && expectedCategory.Value != existing.Categoria)
                    throw new ArgumentException("A categoria enviada não corresponde à instituição do catálogo.");
                return existing;
            }

            if (expectedCategory != CategoriaInstituicaoFinanceira.Outra)
                throw new ArgumentException("Selecione uma instituição conhecida ou escolha Outra para cadastrar uma personalizada.");

            if (grupoId == Guid.Empty)
                throw new ArgumentException("Instituições personalizadas exigem um grupo de carteiras.");

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var custom = new InstituicaoFinanceira(Guid.NewGuid(), nome, CategoriaInstituicaoFinanceira.Outra, grupoId, now);
            await _instituicaoRepository.AddAsync(custom);
            return custom;
        }

        private static CategoriaInstituicaoFinanceira? ParseInstitutionType(string? type, string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            if (!Enum.TryParse<CategoriaInstituicaoFinanceira>(type, true, out var parsed) || !Enum.IsDefined(parsed))
                throw new ArgumentException("Selecione uma categoria de instituição financeira.");
            return parsed;
        }

        private static PosicaoInvestimentoDto MapToPositionDto(PosicaoInvestimento pos, Carteira? carteira = null)
        {
            return new PosicaoInvestimentoDto
            {
                Id = pos.Id,
                CarteiraId = pos.CarteiraId,
                CarteiraNome = carteira?.Name,
                GrupoId = carteira?.GrupoId,
                Titular = carteira?.TitularProfile?.Nome ?? carteira?.Titular,
                InstituicaoFinanceira = carteira?.InstituicaoFinanceiraProfile?.Nome ?? carteira?.InstituicaoFinanceira,
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
                Status = pos.Quantity <= 0 ? "closed" : pos.MaturedAtUtc.HasValue ? "matured" : "open",
                PurchaseDate = pos.PurchaseDateUtc,
                Issuer = (pos.Ativo as RendaFixa)?.Issuer,
                InterestRate = (pos.Ativo as RendaFixa)?.InterestRate,
                Indexer = (pos.Ativo as RendaFixa)?.Indexer,
                MaturityDate = (pos.Ativo as RendaFixa)?.MaturityDate,
                Liquidity = (pos.Ativo as RendaFixa)?.Liquidity,
                Convention = (pos.Ativo as RendaFixa)?.Convention,
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
