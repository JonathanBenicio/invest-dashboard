using System.Security.Authentication;
using InvestDashboard.Application.DTOs.Taxes;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Domain.Repository;

namespace InvestDashboard.Application.Services;

public sealed class EstimativaImpostoAppService(
    ITransacaoRepository transacoes,
    IAtivoRepository ativos,
    ICarteiraRepository carteiras,
    ITitularCarteiraRepository titulares,
    IGrupoCarteirasRepository grupos,
    IUsuarioAtualService usuarioAtual,
    TimeProvider timeProvider) : IEstimativaImpostoAppService
{
    private const string Versao = "BR-RV-2026.09.1";
    private const decimal LimiteIsencaoAcoes = 20_000m;
    private static readonly TimeZoneInfo FusoBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public async Task<EstimativaImpostoMensalDto> CalcularMensalAsync(
        int ano, int mes, Guid grupoId, string? titular, Guid? titularId = null)
    {
        if (mes is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(mes));

        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), FusoBrasil).DateTime);
        var inicioMes = new DateOnly(ano, mes, 1);
        if (inicioMes > new DateOnly(hoje.Year, hoje.Month, 1))
            throw new ArgumentException("Não é possível estimar um mês futuro.");

        var usuarioId = usuarioAtual.UserId?.ToString();
        if (string.IsNullOrWhiteSpace(usuarioId))
            throw new AuthenticationException("Authentication is required.");

        var membership = await grupos.GetMemberAsync(grupoId, usuarioId);
        if (membership is null || !membership.Ativo)
            throw new UnauthorizedAccessException("Active membership in this group is required.");
        if (titularId.HasValue)
        {
            if (await titulares.GetByIdInGroupAsync(titularId.Value, grupoId) is null)
                throw new KeyNotFoundException("Titular não encontrado neste grupo.");
        }
        else if (string.IsNullOrWhiteSpace(titular))
        {
            throw new ArgumentException("Informe titularId ou titular para atribuir o resultado fiscal.");
        }

        var portfolioCount = await carteiras.CountByUserIdAsync(usuarioId, grupoId);
        var accessiblePortfolios = new List<Carteira>();
        for (var skip = 0; skip < portfolioCount; skip += 100)
            accessiblePortfolios.AddRange(await carteiras.GetByUserIdPageAsync(usuarioId, skip, 100, grupoId));

        if (!titularId.HasValue)
        {
            var matchingIds = accessiblePortfolios
                .Where(portfolio => string.Equals(portfolio.TitularProfile?.Nome ?? portfolio.Titular,
                    titular!.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(portfolio => portfolio.TitularId).Where(id => id.HasValue).Distinct().ToArray();
            if (matchingIds.Length > 1)
                throw new ArgumentException("Há titulares com o mesmo nome. Informe titularId.");
            titularId = matchingIds.SingleOrDefault();
        }

        var transactions = new List<Transacao>();
        foreach (var portfolio in accessiblePortfolios)
        {
            var portfolioTransactions = await transacoes.GetByPortfolioIdAsync(portfolio.Id);
            transactions.AddRange(portfolioTransactions.Where(transaction => titularId.HasValue
                ? transaction.TitularId == titularId
                : transaction.TitularId is null && string.Equals(portfolio.Titular, titular!.Trim(), StringComparison.OrdinalIgnoreCase)));
        }

        var registros = transactions
            .Where(item => item.Type == TipoTransacao.Sell && DataLocal(item.TransactionDate) < inicioMes.AddMonths(1))
            .ToList();
        var idsAtivos = registros.Where(item => item.AtivoId.HasValue).Select(item => item.AtivoId!.Value).Distinct().ToArray();
        var mapaAtivos = (await ativos.GetByIdsAsync(idsAtivos)).ToDictionary(item => item.Id);
        var classificadas = registros.Select(item => Classificar(item, mapaAtivos)).ToArray();
        var semModalidade = classificadas.Count(item => item.Categoria is null && item.Transacao.ModalidadeFiscal == ModalidadeFiscal.NaoInformada);
        var foraEscopo = classificadas.Count(item => item.Categoria is null && item.Transacao.ModalidadeFiscal != ModalidadeFiscal.NaoInformada);

        var categorias = new[]
        {
            CalcularCategoria("Ações — operações comuns", "acoes", 0.15m, true, classificadas, inicioMes),
            CalcularCategoria("Ações — day trade", "daytrade", 0.20m, false, classificadas, inicioMes),
            CalcularCategoria("Fundos imobiliários", "fii", 0.20m, false, classificadas, inicioMes)
        };

        return new EstimativaImpostoMensalDto
        {
            Ano = ano,
            Mes = mes,
            VersaoRegras = Versao,
            VendasAcoesComuns = classificadas
                .Where(item => item.Categoria == "acoes" && DataLocal(item.Transacao.TransactionDate) >= inicioMes)
                .Sum(item => item.Transacao.Quantity * item.Transacao.UnitPrice),
            OperacoesSemModalidade = semModalidade,
            OperacoesForaEscopo = foraEscopo,
            EstimativaIncompleta = semModalidade > 0 || foraEscopo > 0,
            EstimativaIr = categorias.Sum(item => item.IrEstimado),
            Categorias = categorias,
            Limitacoes =
            [
                "Estimativa informativa, não substitui apuração fiscal nem orientação profissional.",
                "Usa apenas vendas de ações à vista e cotas de FII identificadas no cadastro; ETFs, BDRs, criptoativos, renda fixa, derivativos e operações fora da bolsa ficam fora do cálculo.",
                "Operações sem modalidade fiscal são excluídas e tornam o resultado incompleto.",
                "IRRF, emolumentos não registrados, eventos corporativos, operações em outras instituições e ajustes manuais não estão incluídos.",
                "A isenção de ações considera o total bruto mensal vendido pelo usuário; perdas são carregadas apenas dentro da categoria identificada."
            ],
            Fontes =
            [
                "Receita Federal — Bolsa de Valores: https://www.gov.br/receitafederal/pt-br/assuntos/meu-imposto-de-renda/pagamento/renda-variavel/bolsa-de-valores-1",
                "Receita Federal — Isenções: https://www.gov.br/receitafederal/pt-br/assuntos/meu-imposto-de-renda/pagamento/renda-variavel/bolsa-de-valores-1/isencoes",
                "Receita Federal — Compensações: https://www.gov.br/receitafederal/pt-br/assuntos/meu-imposto-de-renda/pagamento/renda-variavel/bolsa-de-valores-1/compensacoes"
            ]
        };
    }

    private static CategoriaImpostoEstimadoDto CalcularCategoria(
        string nome,
        string chave,
        decimal aliquota,
        bool possuiIsencao,
        IReadOnlyCollection<OperacaoClassificada> operacoes,
        DateOnly mesAlvo)
    {
        var mesAtual = operacoes.Where(item => item.Categoria == chave)
            .GroupBy(item => new DateOnly(DataLocal(item.Transacao.TransactionDate).Year, DataLocal(item.Transacao.TransactionDate).Month, 1))
            .OrderBy(group => group.Key);

        decimal prejuizo = 0;
        decimal ganhoAtual = 0;
        decimal compensadoAtual = 0;
        decimal baseAtual = 0;
        var isentoAtual = false;

        foreach (var grupo in mesAtual)
        {
            var resultado = grupo.Sum(item => item.Transacao.RealizedGain);
            var vendasBrutas = grupo.Sum(item => item.Transacao.Quantity * item.Transacao.UnitPrice);
            var isento = possuiIsencao && vendasBrutas <= LimiteIsencaoAcoes;
            if (grupo.Key < mesAlvo)
            {
                if (resultado < 0)
                    prejuizo += Math.Abs(resultado);
                else if (resultado > 0 && !isento)
                    prejuizo -= Math.Min(prejuizo, resultado);
                continue;
            }

            ganhoAtual = resultado;
            isentoAtual = isento;
            if (resultado < 0)
            {
                prejuizo += Math.Abs(resultado);
            }
            else if (resultado > 0 && !isento)
            {
                compensadoAtual = Math.Min(prejuizo, resultado);
                baseAtual = resultado - compensadoAtual;
                prejuizo -= compensadoAtual;
            }
        }

        var imposto = Math.Round(baseAtual * aliquota, 2, MidpointRounding.AwayFromZero);
        return new CategoriaImpostoEstimadoDto
        {
            Categoria = nome,
            GanhoLiquido = ganhoAtual,
            PrejuizoCompensado = compensadoAtual,
            PrejuizoAcumulado = prejuizo,
            BaseTributavel = baseAtual,
            Aliquota = aliquota,
            Isento = isentoAtual && ganhoAtual > 0,
            IrEstimado = imposto
        };
    }

    private static OperacaoClassificada Classificar(
        Transacao transacao,
        IReadOnlyDictionary<Guid, Ativo> mapaAtivos)
    {
        if (transacao.ModalidadeFiscal == ModalidadeFiscal.NaoInformada || !transacao.AtivoId.HasValue ||
            !mapaAtivos.TryGetValue(transacao.AtivoId.Value, out var ativo))
            return new OperacaoClassificada(transacao, null);

        if (ativo.TipoAtivo == TipoAtivo.FundoImobiliario)
            return new OperacaoClassificada(transacao, "fii");

        if (ativo.TipoAtivo != TipoAtivo.Acao || ativo.Subtype is not ("ACAO" or "STOCK"))
            return new OperacaoClassificada(transacao, null);

        return new OperacaoClassificada(transacao,
            transacao.ModalidadeFiscal == ModalidadeFiscal.DayTrade ? "daytrade" : "acoes");
    }

    private static DateOnly DataLocal(DateTime dataUtc)
    {
        var utc = dataUtc.Kind == DateTimeKind.Utc ? dataUtc : DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, FusoBrasil));
    }

    private sealed record OperacaoClassificada(Transacao Transacao, string? Categoria);
}
