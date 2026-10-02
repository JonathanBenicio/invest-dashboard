using System.Text.Json;

namespace InvestDashboard.WebAPI.Serialization;

public sealed class PortugueseApiJsonNamingPolicy : JsonNamingPolicy
{
    private static readonly IReadOnlyDictionary<string, string> ContractNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Data"] = "dados", ["Success"] = "sucesso", ["Message"] = "mensagem",
        ["Pagination"] = "paginacao", ["Page"] = "pagina", ["PageSize"] = "itensPorPagina",
        ["TotalCount"] = "totalItens", ["TotalPages"] = "totalPaginas",
        ["HasNextPage"] = "temProximaPagina", ["HasPreviousPage"] = "temPaginaAnterior",
        ["CreatedAt"] = "criadoEm", ["UpdatedAt"] = "atualizadoEm", ["IsActive"] = "ativo",
        ["Name"] = "nome", ["Description"] = "descricao", ["Positions"] = "posicoes",
        ["TotalValue"] = "valorTotal", ["TotalInvested"] = "totalInvestido", ["TotalGain"] = "ganhoTotal",
        ["UnrealizedGain"] = "ganhoNaoRealizado", ["RealizedGain"] = "ganhoRealizado",
        ["GainPercentage"] = "percentualGanho", ["Currency"] = "moeda", ["AssetsCount"] = "quantidadeAtivos",
        ["AssetAllocation"] = "alocacaoAtivos",
        ["FixedIncomeTotal"] = "totalRendaFixa", ["VariableIncomeTotal"] = "totalRendaVariavel",
        ["TopPerformers"] = "melhoresPosicoes", ["WorstPerformers"] = "pioresPosicoes",
        ["Category"] = "categoria", ["Value"] = "valor", ["Percentage"] = "percentual", ["Color"] = "cor",
        ["Date"] = "data", ["PercentageChange"] = "variacaoPercentual", ["PortfolioId"] = "carteiraId",
        ["AssetId"] = "ativoId", ["PurchaseDate"] = "dataCompra", ["Type"] = "tipo", ["Subtype"] = "subtipo",
        ["Sector"] = "setor", ["Status"] = "situacao", ["Quantity"] = "quantidade",
        ["AveragePrice"] = "precoMedio", ["CurrentPrice"] = "precoAtual", ["CurrentPriceSource"] = "origemPrecoAtual",
        ["CurrentPriceObservedAtUtc"] = "precoObservadoEmUtc", ["CurrentValue"] = "valorAtual", ["Gain"] = "ganho",
        ["Issuer"] = "emissor", ["InterestRate"] = "taxaJuros", ["Indexer"] = "indexador",
        ["MaturityDate"] = "dataVencimento", ["MissingTickers"] = "tickersAusentes", ["IsComplete"] = "estaCompleto",
        ["Price"] = "preco", ["Source"] = "origem", ["IsAdjusted"] = "ajustado", ["ObservedAtUtc"] = "observadoEmUtc",
        ["Symbol"] = "simbolo", ["DateUtc"] = "dataUtc", ["TransactionDate"] = "dataTransacao",
        ["UnitPrice"] = "precoUnitario", ["BrokerageFee"] = "taxaCorretagem", ["Fees"] = "taxas",
        ["TotalAmount"] = "valorTotal", ["RealizedCostBasis"] = "custoBaseRealizado", ["Notes"] = "observacoes",
        ["AssetClass"] = "classeAtivo", ["IdempotencyKey"] = "chaveIdempotencia",
        ["InitialStatementValue"] = "valorInicialExtrato", ["Principal"] = "valorPrincipal",
        ["StatementValue"] = "valorExtrato", ["PreviousValue"] = "valorAnterior",
        ["Variation"] = "variacao", ["LastUpdate"] = "atualizadoEm", ["AccessToken"] = "tokenAcesso",
        ["ExpiresIn"] = "expiraEmSegundos", ["User"] = "usuario", ["RequiresEmailConfirmation"] = "requerConfirmacaoEmail",
        ["Password"] = "senha", ["Role"] = "perfil", ["IsEmailVerified"] = "emailVerificado",
        ["Query"] = "consulta", ["Search"] = "busca", ["SortBy"] = "ordenarPor",
        ["SortOrder"] = "ordem", ["Symbols"] = "simbolos", ["FromDate"] = "dataDe", ["ToDate"] = "dataAte",
        ["StartDate"] = "dataInicio", ["EndDate"] = "dataFim", ["Ticker"] = "ticker", ["Id"] = "id",
        ["Email"] = "email", ["Code"] = "codigo", ["Errors"] = "erros",
        ["GrupoId"] = "grupoId", ["Titular"] = "titular", ["InstituicaoFinanceira"] = "instituicaoFinanceira",
        ["Visibilidade"] = "visibilidade", ["UsuarioId"] = "usuarioId", ["Papel"] = "papel",
        ["Liquidity"] = "liquidez", ["Convention"] = "convencao",
        ["Unit"] = "unidade", ["Periodicity"] = "periodicidade", ["ReferenceDate"] = "dataReferencia",
        ["UpdatedByUserId"] = "atualizadoPorUserId"
    };

    public override string ConvertName(string name) =>
        ContractNames.TryGetValue(name, out var contractName)
            ? contractName
            : JsonNamingPolicy.CamelCase.ConvertName(name);
}
