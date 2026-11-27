using InvestDashboard.Application.DTOs.Portfolio;

namespace InvestDashboard.Application.Services;

/// <summary>Deriva vistas consolidadas a partir de posições já autorizadas.</summary>
public static class ConsolidadorPosicoesInvestimento
{
    public static List<PosicaoInvestimentoConsolidadoDto> Agrupar(
        IReadOnlyList<PosicaoInvestimentoDto> posicoes,
        Guid? carteiraId,
        Guid? titularId)
    {
        var grupos = posicoes.GroupBy(posicao => Chave(posicao, carteiraId, titularId));
        return grupos.Select(grupo => CriarResumo(grupo.ToArray(), carteiraId, titularId))
            .OrderBy(item => item.Ticker, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.GrupoId)
            .ThenBy(item => item.TitularId)
            .ThenBy(item => item.PosicaoId)
            .ToList();
    }

    public static DetalheInvestimentoConsolidadoDto CriarDetalhe(
        IReadOnlyList<PosicaoInvestimentoDto> posicoes,
        Guid grupoId,
        Guid ativoId,
        Guid? titularId)
    {
        var resumo = CriarResumo(posicoes.ToArray(), null, titularId);
        var chave = ChaveConsolidacao(grupoId, ativoId, titularId);
        resumo = resumo with { Id = chave, ChaveConsolidacao = chave };
        var contribuicoes = posicoes
            .OrderBy(posicao => posicao.CarteiraNome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(posicao => posicao.CarteiraId)
            .Select(posicao => new ContribuicaoPosicaoInvestimentoDto(
                posicao.Id,
                posicao.CarteiraId,
                posicao.CarteiraNome ?? "Carteira",
                posicao.TitularId,
                string.IsNullOrWhiteSpace(posicao.Titular) ? "Sem titular" : posicao.Titular,
                posicao.InstituicaoFinanceiraId,
                posicao.InstituicaoFinanceira,
                posicao.Quantity,
                posicao.TotalInvested,
                posicao.CurrentValue,
                posicao.CurrentValue - posicao.TotalInvested,
                posicao.DataAvaliacao,
                posicao.Status))
            .ToArray();

        return new DetalheInvestimentoConsolidadoDto(resumo, contribuicoes);
    }

    private static object Chave(PosicaoInvestimentoDto posicao, Guid? carteiraId, Guid? titularId)
    {
        if (carteiraId.HasValue || !posicao.GrupoId.HasValue)
            return (posicao.GrupoId, posicao.CarteiraId, posicao.Id, (Guid?)null, posicao.Currency);

        return (posicao.GrupoId, (Guid?)null, posicao.AtivoId,
            titularId, posicao.Currency,
            posicao.MaturityDate?.Date.Ticks, posicao.Subtype);
    }

    private static PosicaoInvestimentoConsolidadoDto CriarResumo(
        IReadOnlyList<PosicaoInvestimentoDto> posicoes,
        Guid? carteiraId,
        Guid? titularFilterId)
    {
        var primeira = posicoes[0];
        var quantidade = posicoes.Sum(posicao => posicao.Quantity);
        var totalInvestido = posicoes.Sum(posicao => posicao.TotalInvested);
        var valorAtual = posicoes.Sum(posicao => posicao.CurrentValue);
        var ganho = valorAtual - totalInvestido;
        var titulares = posicoes.Select(posicao => posicao.TitularId).Distinct().ToArray();
        var titularId = titularFilterId ?? (titulares.Length == 1 ? titulares[0] : null);
        var nomesTitulares = posicoes.Select(posicao => posicao.Titular)
            .Where(nome => !string.IsNullOrWhiteSpace(nome)).Distinct(StringComparer.Ordinal).ToArray();
        var titularNome = titularFilterId.HasValue
            ? posicoes.FirstOrDefault(posicao => posicao.TitularId == titularFilterId)?.Titular
            : nomesTitulares.Length == 1 ? nomesTitulares[0] : null;
        var grupoId = primeira.GrupoId;
        var carteiraIndividual = carteiraId.HasValue || !grupoId.HasValue;
        var escopo = carteiraIndividual ? "carteira" : titularFilterId.HasValue ? "titular" : "grupo";
        var id = carteiraIndividual
            ? $"posicao:{primeira.Id:N}"
            : ChaveConsolidacao(grupoId!.Value, primeira.AtivoId, titularFilterId);
        var precosAtuais = posicoes.Select(posicao => posicao.CurrentPrice).Distinct().ToArray();
        var datasAvaliacao = posicoes
            .Where(posicao => posicao.DataAvaliacao.HasValue)
            .Select(posicao => posicao.DataAvaliacao!.Value)
            .Distinct()
            .Order()
            .ToArray();

        return new PosicaoInvestimentoConsolidadoDto(
            id,
            id,
            posicoes.Count == 1 ? primeira.Id : null,
            primeira.AtivoId,
            grupoId,
            primeira.GrupoNome,
            titularId,
            titularNome,
            escopo,
            posicoes.Count == 1 ? primeira.CarteiraId : null,
            posicoes.Count == 1 ? primeira.CarteiraNome : null,
            posicoes.Select(posicao => posicao.CarteiraId).Distinct().Count(),
            datasAvaliacao.Length > 0 ? datasAvaliacao[0] : null,
            datasAvaliacao.Length > 0 ? datasAvaliacao[^1] : null,
            datasAvaliacao.Length > 1,
            posicoes.All(posicao => posicao.DataAvaliacao.HasValue),
            primeira.Name,
            primeira.Ticker,
            primeira.Type,
            primeira.Subtype,
            primeira.Sector,
            posicoes.Select(posicao => posicao.Status).Distinct(StringComparer.Ordinal).Count() == 1
                ? primeira.Status
                : "mixed",
            quantidade,
            quantidade > 0 ? totalInvestido / quantidade : 0m,
            precosAtuais.Length == 1 ? precosAtuais[0] : null,
            totalInvestido,
            valorAtual,
            ganho,
            totalInvestido > 0 ? ganho / totalInvestido * 100m : 0m,
            primeira.Currency,
            primeira.Issuer,
            posicoes.Select(posicao => posicao.InterestRate).Distinct().Count() == 1 ? primeira.InterestRate : null,
            primeira.Indexer,
            primeira.MaturityDate,
            primeira.Liquidity,
            primeira.Convention);
    }

    private static string ChaveConsolidacao(Guid grupoId, Guid ativoId, Guid? titularId) => titularId.HasValue
        ? $"grupo:{grupoId:N}:titular:{titularId.Value:N}:ativo:{ativoId:N}"
        : $"grupo:{grupoId:N}:ativo:{ativoId:N}";
}
