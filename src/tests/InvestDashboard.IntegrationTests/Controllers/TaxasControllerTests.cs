using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class TaxasControllerTests
{
    [Fact]
    public async Task TaxEstimate_AttributesGroupOperationsToPortfolioHolderInsteadOfEntryUser()
    {
        using var factory = new CustomWebApplicationFactory();
        using var admin = CreateClient(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        using var investor = CreateClient(factory, FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName);

        using var groupResponse = await admin.PostAsJsonAsync("/api/v1/grupos-carteiras", new { nome = "Família fiscal" });
        using var groupJson = JsonDocument.Parse(await groupResponse.Content.ReadAsStringAsync());
        var groupId = groupJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        using var portfolioResponse = await admin.PostAsJsonAsync("/api/v1/portfolios", new
        { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),
            nome = "Carteira de Maria",
            grupoId = groupId,
            titular = "Maria Titular",
            visibilidade = "PublicaDoGrupo"
        });
        using var portfolioJson = JsonDocument.Parse(await portfolioResponse.Content.ReadAsStringAsync());
        var portfolioId = portfolioJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.GroupMembers.Add(new MembroGrupo(Guid.NewGuid(), groupId, FakeAuthProvider.SecondTestUserId.ToString(),
                FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName, PapelGrupo.Investidor, DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        var buy = await investor.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId = portfolioId, ticker = "WEGE3", tipo = "Buy", classeAtivo = "ACAO", nome = "WEG",
            quantidade = 2200m, precoUnitario = 10m, taxas = 0m, dataTransacao = DateTime.UtcNow.AddDays(-1),
            chaveIdempotencia = Guid.NewGuid()
        });
        buy.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = await investor.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId = portfolioId, ticker = "WEGE3", tipo = "Sell", classeAtivo = "ACAO",
            quantidade = 2200m, precoUnitario = 11m, taxas = 0m, modalidadeFiscal = "Comum",
            dataTransacao = DateTime.UtcNow, chaveIdempotencia = Guid.NewGuid()
        });
        sale.StatusCode.Should().Be(HttpStatusCode.Created);

        var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));
        using var estimateResponse = await admin.GetAsync(
            $"/api/v1/taxes/estimativa-mensal?ano={today.Year}&mes={today.Month}&grupoId={groupId}&titular=Maria%20Titular");
        estimateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var estimateJson = JsonDocument.Parse(await estimateResponse.Content.ReadAsStringAsync());
        estimateJson.RootElement.GetProperty("dados").GetProperty("vendasAcoesComuns").GetDecimal().Should().Be(24_200m);
        estimateJson.RootElement.GetProperty("dados").GetProperty("estimativaIr").GetDecimal().Should().Be(330m);
    }

    [Fact]
    public async Task TaxEstimate_AggregatesMonthlyStockSalesAndAppliesVersionedRules()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));

        using var portfolioResponse = await client.PostAsJsonAsync("/api/v1/portfolios", new { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),  nome = "Carteira fiscal" });
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var portfolioJson = JsonDocument.Parse(await portfolioResponse.Content.ReadAsStringAsync());
        var portfolio = portfolioJson.RootElement.GetProperty("dados");
        var portfolioId = portfolio.GetProperty("id").GetGuid();
        var groupId = portfolio.GetProperty("grupoId").GetGuid();
        var titular = portfolio.GetProperty("titular").GetString()!;

        var buy = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId = portfolioId, ticker = "WEGE3", tipo = "Buy", classeAtivo = "ACAO", nome = "WEG",
            quantidade = 2200m, precoUnitario = 10m, taxas = 0m, dataTransacao = DateTime.UtcNow.AddMonths(-1),
            chaveIdempotencia = Guid.NewGuid()
        });
        buy.StatusCode.Should().Be(HttpStatusCode.Created);

        foreach (var _ in Enumerable.Range(0, 2))
        {
            var sale = await client.PostAsJsonAsync("/api/v1/transactions", new
            {
                carteiraId = portfolioId, ticker = "WEGE3", tipo = "Sell", classeAtivo = "ACAO",
                quantidade = 1100m, precoUnitario = 11m, taxas = 0m, modalidadeFiscal = "Comum",
                dataTransacao = DateTime.UtcNow, chaveIdempotencia = Guid.NewGuid()
            });
            sale.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));
        using var response = await client.GetAsync($"/api/v1/taxes/estimativa-mensal?ano={today.Year}&mes={today.Month}&grupoId={groupId}&titular={Uri.EscapeDataString(titular)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var estimate = body.RootElement.GetProperty("dados");
        estimate.GetProperty("versaoRegras").GetString().Should().Be("BR-RV-2026.09.1");
        estimate.GetProperty("vendasAcoesComuns").GetDecimal().Should().Be(24200m);
        estimate.GetProperty("estimativaIr").GetDecimal().Should().Be(330m);
        estimate.GetProperty("estimativaIncompleta").GetBoolean().Should().BeFalse();
        estimate.GetProperty("categorias")[0].GetProperty("isento").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Taxa_CreatesReadsUpdatesAndDeletesPersistedValues()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));

        var groupResponse = await client.PostAsJsonAsync("/api/v1/grupos-carteiras", new { nome = "Taxas" });
        using var groupBody = JsonDocument.Parse(await groupResponse.Content.ReadAsStringAsync());
        var groupId = groupBody.RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        var emptyResponse = await client.GetAsync($"/api/v1/taxes?grupoId={groupId}");
        emptyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var emptyBody = JsonDocument.Parse(await emptyResponse.Content.ReadAsStringAsync());
        emptyBody.RootElement.GetProperty("dados").GetArrayLength().Should().Be(0);

        var createResponse = await client.PostAsJsonAsync($"/api/v1/taxes?grupoId={groupId}", new
        {
            nome = "Taxa teste",
            simbolo = "TST",
            valorAtual = 5.25m,
            valorAnterior = 5m,
            descricao = "Indicador manual",
            origem = "Usuário"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdBody = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var createdRate = createdBody.RootElement.GetProperty("dados");
        var rateId = createdRate.GetProperty("id").GetGuid();
        createdRate.GetProperty("nome").GetString().Should().Be("Taxa teste");
        createdRate.GetProperty("simbolo").GetString().Should().Be("TST");
        createResponse.Headers.Location.Should().NotBeNull();
        createResponse.Headers.Location!.ToString().Should().Contain($"grupoId={groupId}");
        using var locationResponse = await client.GetAsync(createResponse.Headers.Location);
        locationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createdRate.GetProperty("origem").GetString().Should().Be("Usuário");

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/taxes/{rateId}?grupoId={groupId}", new
        {
            nome = "Taxa revisada",
            simbolo = "tst2",
            valorAtual = 6.75m,
            valorAnterior = 6.5m,
            descricao = "Descrição atualizada",
            origem = "Revisão manual"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var updatedBody = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        var updatedRate = updatedBody.RootElement.GetProperty("dados");
        updatedRate.GetProperty("nome").GetString().Should().Be("Taxa revisada");
        updatedRate.GetProperty("simbolo").GetString().Should().Be("TST2");
        updatedRate.GetProperty("valorAtual").GetDecimal().Should().Be(6.75m);
        updatedRate.GetProperty("valorAnterior").GetDecimal().Should().Be(6.5m);
        updatedRate.GetProperty("variacao").GetDecimal().Should().Be(0.25m);
        updatedRate.GetProperty("descricao").GetString().Should().Be("Descrição atualizada");
        updatedRate.GetProperty("origem").GetString().Should().Be("Revisão manual");
        DateOnly.TryParse(updatedRate.GetProperty("atualizadoEm").GetString(), out _).Should().BeTrue();

        using var historyBody = JsonDocument.Parse(await (await client.GetAsync($"/api/v1/taxes/{rateId}/historico?grupoId={groupId}")).Content.ReadAsStringAsync());
        historyBody.RootElement.GetProperty("dados").GetArrayLength().Should().Be(2);

        var deleteResponse = await client.DeleteAsync($"/api/v1/taxes/{rateId}?grupoId={groupId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var readAfterDelete = await client.GetAsync($"/api/v1/taxes/{rateId}?grupoId={groupId}");
        readAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static HttpClient CreateClient(CustomWebApplicationFactory factory, Guid userId, Guid sessionId, string email, string name)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwtForUser(userId, sessionId, email, name));
        return client;
    }
}
