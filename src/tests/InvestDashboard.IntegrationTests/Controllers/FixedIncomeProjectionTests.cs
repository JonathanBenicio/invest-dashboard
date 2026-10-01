using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using InvestDashboard.Application.Services;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class FixedIncomeProjectionTests
{
    [Fact]
    public void FinancialBusinessCalendar_SkipsBrazilianNationalAndMarketHolidays()
    {
        CalendarioFinanceiroBrasileiro.ContarDiasUteis(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 9)).Should().Be(5);
        CalendarioFinanceiroBrasileiro.ContarDiasUteis(new DateOnly(2026, 2, 13), new DateOnly(2026, 2, 20)).Should().Be(3);
        CalendarioFinanceiroBrasileiro.ContarDiasUteis(new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 10)).Should().Be(5);
        CalendarioFinanceiroBrasileiro.ContarDiasUteis(new DateOnly(2026, 4, 2), new DateOnly(2026, 4, 7)).Should().Be(2);
        CalendarioFinanceiroBrasileiro.ContarDiasUteis(new DateOnly(2026, 11, 17), new DateOnly(2026, 11, 25)).Should().Be(5);
    }
    [Theory]
    [InlineData("Diaria", 7, true)]
    [InlineData("Diaria", 10, false)]
    [InlineData("Mensal", 45, true)]
    [InlineData("Mensal", 46, false)]
    [InlineData("Anual", 400, true)]
    [InlineData("Anual", 401, false)]
    [InlineData("Pontual", 0, false)]
    public async Task Projection_BlocksExpiredPremiseAndRetainsObservedValues(string periodicity, int age, bool valid)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        using var walletResponse = await client.PostAsJsonAsync("/api/v1/portfolios", new { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),  nome = "Premissas" });
        walletResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var walletJson = JsonDocument.Parse(await walletResponse.Content.ReadAsStringAsync());
        var wallet = walletJson.RootElement.GetProperty("dados");
        var groupId = wallet.GetProperty("grupoId").GetGuid();
        using var rateResponse = await client.PostAsJsonAsync($"/api/v1/taxes?grupoId={groupId}", new {
            nome = "CDI", simbolo = "CDI", valorAtual = 10m, valorAnterior = 10m,
            descricao = "Premissa manual", origem = "Manual", unidade = "Percentual", periodicidade = periodicity,
            dataReferencia = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).DateTime).AddDays(-age)
        });
        rateResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var positionResponse = await client.PostAsJsonAsync("/api/v1/investments/fixed-income", new {
            carteiraId = wallet.GetProperty("id").GetGuid(), nome = "CDB CDI", subtipo = "CDB", emissor = "Banco",
            valorPrincipal = 1000m, valorExtrato = 1100m, taxaJuros = 100m, indexador = "CDI",
            dataCompra = DateTime.UtcNow.AddDays(-1), dataVencimento = DateTime.UtcNow.AddYears(1),
            chaveIdempotencia = Guid.NewGuid(), convencao = "365 dias corridos"
        });
        positionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var positionJson = JsonDocument.Parse(await positionResponse.Content.ReadAsStringAsync());
        var id = positionJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        using var projectionResponse = await client.GetAsync($"/api/v1/investments/{id}/projecao-renda-fixa");
        projectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var projectionJson = JsonDocument.Parse(await projectionResponse.Content.ReadAsStringAsync());
        var projection = projectionJson.RootElement.GetProperty("dados");
        projection.GetProperty("valorObservado").GetDecimal().Should().Be(1100m);
        projection.GetProperty("taxaValorObservado").GetDecimal().Should().Be(10m);
        (projection.GetProperty("valorProjetadoBruto").ValueKind != JsonValueKind.Null).Should().Be(valid);
        if (!valid) projection.GetProperty("motivo").GetString().Should().Contain("CDI");
    }

    [Fact]
    public async Task Projection_UsesLastStatementAndShowsUnavailableWhenContractPremiseIsMissing()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        var portfolioResponse = await client.PostAsJsonAsync("/api/v1/portfolios", new { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),  nome = "Renda fixa" });
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var portfolioJson = JsonDocument.Parse(await portfolioResponse.Content.ReadAsStringAsync());
        var portfolioId = portfolioJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        var positionResponse = await client.PostAsJsonAsync("/api/v1/investments/fixed-income", new
        {
            carteiraId = portfolioId,
            nome = "CDB prefixado",
            subtipo = "CDB",
            emissor = "Banco teste",
            valorPrincipal = 1_000m,
            valorExtrato = 1_000m,
            taxaJuros = 10m,
            indexador = "PREFIXADO",
            dataCompra = DateTime.UtcNow.AddDays(-1),
            dataVencimento = DateTime.UtcNow.AddDays(364),
            chaveIdempotencia = Guid.NewGuid(),
            liquidez = "Diária após carência",
            convencao = "365 dias corridos"
        });
        positionResponse.StatusCode.Should().Be(HttpStatusCode.Created, await positionResponse.Content.ReadAsStringAsync());
        using var positionJson = JsonDocument.Parse(await positionResponse.Content.ReadAsStringAsync());
        var positionId = positionJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        positionJson.RootElement.GetProperty("dados").GetProperty("liquidez").GetString().Should().Be("Diária após carência");

        using var projectionJson = JsonDocument.Parse(await (await client.GetAsync("/api/v1/portfolios/projecao-renda-fixa")).Content.ReadAsStringAsync());
        var consolidated = projectionJson.RootElement.GetProperty("dados");
        consolidated.GetProperty("estaCompleta").GetBoolean().Should().BeTrue();
        consolidated.GetProperty("valorProjetadoBruto").GetDecimal().Should().BeGreaterThan(1_000m);

        var indexedResponse = await client.PostAsJsonAsync("/api/v1/investments/fixed-income", new
        {
            carteiraId = portfolioId,
            nome = "CDB CDI",
            subtipo = "CDB",
            emissor = "Banco teste",
            valorPrincipal = 2_000m,
            valorExtrato = 2_000m,
            taxaJuros = 110m,
            indexador = "CDI",
            dataCompra = DateTime.UtcNow.AddDays(-1),
            dataVencimento = DateTime.UtcNow.AddDays(364),
            chaveIdempotencia = Guid.NewGuid()
        });
        indexedResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var indexedJson = JsonDocument.Parse(await indexedResponse.Content.ReadAsStringAsync());
        var indexedPositionId = indexedJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        using var incompleteJson = JsonDocument.Parse(await (await client.GetAsync("/api/v1/portfolios/projecao-renda-fixa")).Content.ReadAsStringAsync());
        incompleteJson.RootElement.GetProperty("dados").GetProperty("estaCompleta").GetBoolean().Should().BeFalse();
        incompleteJson.RootElement.GetProperty("dados").GetProperty("valorProjetadoBruto").ValueKind.Should().Be(JsonValueKind.Null);
        using var detailProjection = JsonDocument.Parse(await (await client.GetAsync($"/api/v1/investments/{indexedPositionId}/projecao-renda-fixa")).Content.ReadAsStringAsync());
        detailProjection.RootElement.GetProperty("dados").GetProperty("valorProjetadoBruto").ValueKind.Should().Be(JsonValueKind.Null);
        detailProjection.RootElement.GetProperty("dados").GetProperty("estadoProjecao").GetString().Should().Be("sem_premissa");
    }
}
