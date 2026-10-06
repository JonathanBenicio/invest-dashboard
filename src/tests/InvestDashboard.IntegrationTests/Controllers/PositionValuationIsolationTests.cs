using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Domain.Aggregates.MarketData;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Aggregates.Trading;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class PositionValuationIsolationTests
{
    [RequiresPostgresFact]
    public async Task Migration_ImportsOnlyUnambiguouslyPositionScopedStatements()
    {
        var connectionString = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connectionString);
        var options = new DbContextOptionsBuilder<InvestDbContext>()
            .UseNpgsql(database.ConnectionString, provider => provider.MigrationsAssembly("InvestDashboard.Infrastructure"))
            .Options;
        var legacyDate = DateTime.UtcNow.AddDays(-3);
        Guid uniquePositionId;
        Guid ambiguousFirstPositionId;
        Guid ambiguousSecondPositionId;
        Guid ambiguousAssetId;
        var ambiguousStatementIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        await using (var context = new InvestDbContext(options))
        {
            await context.Database.MigrateAsync("20260930165723_PreserveRateHistoryMetadata");
            var firstPortfolio = new Carteira(Guid.NewGuid(), "legacy-owner-a", "Legacy A");
            var secondPortfolio = new Carteira(Guid.NewGuid(), "legacy-owner-b", "Legacy B");
            context.Portfolios.AddRange(firstPortfolio, secondPortfolio);

            var uniqueAsset = CreateLegacyAsset("RFUNIQUE44", legacyDate);
            uniquePositionId = AddLegacyPosition(context, uniqueAsset, firstPortfolio, legacyDate, 1.25m, out var uniqueStatementId);

            var ambiguousAsset = CreateLegacyAsset("RFAMBIG44", legacyDate);
            ambiguousAssetId = ambiguousAsset.Id;
            ambiguousFirstPositionId = AddLegacyPosition(context, ambiguousAsset, firstPortfolio, legacyDate,
                1.30m, out ambiguousStatementIds[0]);
            ambiguousSecondPositionId = AddLegacyPosition(context, ambiguousAsset, secondPortfolio, legacyDate,
                1.40m, out ambiguousStatementIds[1]);
            await context.SaveChangesAsync();

            context.HistoricalPrices.Add(new PrecoHistorico(uniqueStatementId, uniqueAsset.Id, 1.25m, legacyDate, "statement"));
            context.HistoricalPrices.Add(new PrecoHistorico(ambiguousStatementIds[0], ambiguousAsset.Id, 1.30m, legacyDate, "statement"));
            context.HistoricalPrices.Add(new PrecoHistorico(ambiguousStatementIds[1], ambiguousAsset.Id, 1.40m, legacyDate.AddMinutes(1), "statement"));
            await context.SaveChangesAsync();
        }

        await using (var context = new InvestDbContext(options))
        {
            await context.Database.MigrateAsync();
            var imported = await context.PositionValuations.AsNoTracking().ToListAsync();
            imported.Should().ContainSingle(item => item.PosicaoId == uniquePositionId && item.PrecoUnitario == 1.25m);
            imported.Should().NotContain(item => item.PosicaoId == ambiguousFirstPositionId || item.PosicaoId == ambiguousSecondPositionId);

            var ambiguousPositions = await context.AssetPositions.AsNoTracking()
                .Where(position => position.AtivoId == ambiguousAssetId).ToListAsync();
            ambiguousPositions.Should().OnlyContain(position => position.CurrentPrice == position.AverageCost);

            (await context.HistoricalPrices.CountAsync(price => price.Source == "statement" && price.AtivoId == ambiguousAssetId))
                .Should().Be(2);
        }
    }

    [Fact]
    public async Task FixedIncomeStatements_AreScopedToTheOwningPosition_InMemory()
    {
        using var factory = new CustomWebApplicationFactory();
        await AssertStatementsAreScopedAsync(factory);
    }

    [RequiresPostgresFact]
    public async Task FixedIncomeStatements_AreScopedToTheOwningPosition_Postgres()
    {
        var connectionString = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connectionString);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        await AssertStatementsAreScopedAsync(factory);
    }

    [Fact]
    public async Task BackdatedStatement_DoesNotReplaceTheLatestObservedValue_InMemory()
    {
        using var factory = new CustomWebApplicationFactory();
        await AssertBackdatedStatementAsync(factory);
    }

    [RequiresPostgresFact]
    public async Task BackdatedStatement_DoesNotReplaceTheLatestObservedValue_Postgres()
    {
        var connectionString = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connectionString);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        await AssertBackdatedStatementAsync(factory);
    }

    private static async Task AssertStatementsAreScopedAsync(CustomWebApplicationFactory factory)
    {
        using var owner = CreateClient(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        using var other = CreateClient(factory, FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName);
        var ownerPortfolio = await CreatePortfolioAsync(owner, "Owner fixed income");
        var otherPortfolio = await CreatePortfolioAsync(other, "Other fixed income");
        var sharedIdempotencyKey = Guid.NewGuid();
        var ownerPosition = await CreateFixedIncomeAsync(owner, ownerPortfolio, sharedIdempotencyKey, 1_000m);
        var otherPosition = await CreateFixedIncomeAsync(other, otherPortfolio, sharedIdempotencyKey, 1_400m);
        ownerPosition.Should().NotBe(otherPosition);

        (await GetDecimalAsync(owner, $"/api/v1/investments/{ownerPosition}", "valorAtual")).Should().Be(1_000m);
        (await GetDecimalAsync(other, $"/api/v1/investments/{otherPosition}", "valorAtual")).Should().Be(1_400m);
        (await GetDecimalAsync(owner, $"/api/v1/investments/{ownerPosition}/projecao-renda-fixa", "valorObservado"))
            .Should().Be(1_000m);
        (await GetDecimalAsync(other, $"/api/v1/investments/{otherPosition}/projecao-renda-fixa", "valorObservado"))
            .Should().Be(1_400m);

        using var ownerHistoryResponse = await owner.GetAsync($"/api/v1/investments/{ownerPosition}/history");
        ownerHistoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var ownerHistory = JsonDocument.Parse(await ownerHistoryResponse.Content.ReadAsStringAsync());
        var ownerStatements = ownerHistory.RootElement.GetProperty("dados").EnumerateArray()
            .Where(item => item.GetProperty("origem").GetString() == "statement").ToArray();
        ownerStatements.Should().ContainSingle();
        ownerStatements[0].GetProperty("preco").GetDecimal().Should().Be(1m);

        using var forbiddenHistory = await other.GetAsync($"/api/v1/investments/{ownerPosition}/history");
        forbiddenHistory.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task AssertBackdatedStatementAsync(CustomWebApplicationFactory factory)
    {
        using var client = CreateClient(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        var portfolioId = await CreatePortfolioAsync(client, "Backdated statement");
        var positionId = await CreateFixedIncomeAsync(client, portfolioId, Guid.NewGuid(), 1_000m);
        var observedAt = DateTime.UtcNow.AddDays(-1);
        var recent = await client.PostAsJsonAsync($"/api/v1/investments/{positionId}/valuations",
            new { valorTotal = 1_200m, data = observedAt });
        recent.StatusCode.Should().Be(HttpStatusCode.OK);
        var sameDateRevision = await client.PostAsJsonAsync($"/api/v1/investments/{positionId}/valuations",
            new { valorTotal = 1_250m, data = observedAt });
        sameDateRevision.StatusCode.Should().Be(HttpStatusCode.OK);
        var backdated = await client.PostAsJsonAsync($"/api/v1/investments/{positionId}/valuations",
            new { valorTotal = 1_100m, data = observedAt.AddDays(-10) });
        backdated.StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetDecimalAsync(client, $"/api/v1/investments/{positionId}", "valorAtual")).Should().Be(1_250m);
        (await GetDecimalAsync(client, $"/api/v1/investments/{positionId}/projecao-renda-fixa", "valorObservado"))
            .Should().Be(1_250m);
        using var historyResponse = await client.GetAsync($"/api/v1/investments/{positionId}/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var history = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
        history.RootElement.GetProperty("dados").EnumerateArray()
            .Where(item => item.GetProperty("origem").GetString() == "statement")
            .Select(item => item.GetProperty("preco").GetDecimal())
            .Should().Contain(new[] { 1m, 1.2m, 1.25m, 1.1m });
    }

    private static HttpClient CreateClient(CustomWebApplicationFactory factory, Guid userId, Guid sessionId,
        string email, string name)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(userId, sessionId, email, name));
        return client;
    }

    private static async Task<Guid> CreatePortfolioAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/portfolios", new
        {
            nome = name,
            instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001")
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateFixedIncomeAsync(HttpClient client, Guid portfolioId,
        Guid idempotencyKey, decimal statementValue)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/investments/fixed-income", new
        {
            carteiraId = portfolioId,
            nome = "CDB compartilhado por ticker",
            subtipo = "CDB",
            emissor = "Banco de teste",
            valorPrincipal = 1_000m,
            valorExtrato = statementValue,
            taxaJuros = 10m,
            indexador = "PREFIXADO",
            dataCompra = DateTime.UtcNow.AddDays(-2),
            dataVencimento = DateTime.UtcNow.AddYears(1),
            convencao = "365 dias corridos",
            chaveIdempotencia = idempotencyKey
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task<decimal> GetDecimalAsync(HttpClient client, string path, string property)
    {
        using var response = await client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("dados").GetProperty(property).GetDecimal();
    }

    private static RendaFixa CreateLegacyAsset(string ticker, DateTime date) =>
        new(Guid.NewGuid(), ticker, ticker, 9m, date, "PREFIXADO", 10m, date.AddYears(1), "Banco legado", "CDB", convention: "365 dias corridos");

    private static Guid AddLegacyPosition(InvestDbContext context, RendaFixa asset, Carteira portfolio,
        DateTime date, decimal statementPrice, out Guid statementId)
    {
        context.Assets.Add(asset);
        var position = new PosicaoInvestimento(Guid.NewGuid(), portfolio.Id, asset.Id, asset.Ticker,
            TipoAtivo.RendaFixa, 9m);
        position.AddShares(1_000m, 1m, 0m, date);
        context.AssetPositions.Add(position);
        context.Transactions.Add(new Transacao(Guid.NewGuid(), portfolio.UserId, portfolio.Id, asset.Id,
            asset.Ticker, TipoTransacao.Buy, 1_000m, 1m, 0m, date));
        statementId = Guid.NewGuid();
        return position.Id;
    }
}
