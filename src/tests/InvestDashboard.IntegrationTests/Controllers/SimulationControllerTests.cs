using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class SimulationControllerTests
{
    [Fact]
    public async Task Simulate_Deterministic_UsesPortugueseRequestAndResponseContract()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/simulation", new
        {
            valorInicial = 1000m,
            aporteMensal = 500m,
            anos = 1,
            taxaJurosAnual = 10m,
            estrategia = "deterministic"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = body.RootElement.GetProperty("dados");
        data.GetProperty("nomeEstrategia").GetString().Should().Contain("Determinístico");
        data.GetProperty("totalInvestido").GetDecimal().Should().Be(7000m);
        data.GetProperty("totalJuros").GetDecimal().Should().BeGreaterThan(0);

        var points = data.GetProperty("pontos");
        points.GetArrayLength().Should().Be(13);
        points[0].GetProperty("mes").GetInt32().Should().Be(0);
        points[0].GetProperty("investido").GetDecimal().Should().Be(1000m);
        points[12].GetProperty("mes").GetInt32().Should().Be(12);
        points[12].GetProperty("investido").GetDecimal().Should().Be(7000m);
        points[12].GetProperty("total").GetDecimal().Should().Be(data.GetProperty("valorFinal").GetDecimal());
    }

    [Fact]
    public async Task Simulate_MonteCarlo_UsesRequestedStrategy()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/simulation", new
        {
            valorInicial = 1000m,
            aporteMensal = 500m,
            anos = 1,
            taxaJurosAnual = 10m,
            estrategia = "montecarlo",
            volatilidade = 15m,
            numeroSimulacoes = 1000
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = body.RootElement.GetProperty("dados");
        data.GetProperty("nomeEstrategia").GetString().Should().Contain("Monte Carlo");
        data.GetProperty("pontos").GetArrayLength().Should().Be(13);
    }

    [Theory]
    [InlineData(-1, "deterministic")]
    [InlineData(1000, "unknown")]
    public async Task Simulate_RejectsInvalidRequestParameters(decimal initialAmount, string strategy)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync("/api/v1/simulation", new
        {
            valorInicial = initialAmount,
            aporteMensal = 500m,
            anos = 1,
            taxaJurosAnual = 10m,
            estrategia = strategy
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetStrategies_ReturnsPortugueseContract()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync("/api/v1/simulation/strategies");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var strategies = body.RootElement.GetProperty("dados");
        strategies.GetArrayLength().Should().Be(2);
        foreach (var strategy in strategies.EnumerateArray())
        {
            strategy.TryGetProperty("id", out _).Should().BeTrue();
            strategy.TryGetProperty("nome", out _).Should().BeTrue();
            strategy.TryGetProperty("descricao", out _).Should().BeTrue();
            strategy.TryGetProperty("name", out _).Should().BeFalse();
        }
    }

    private static HttpClient CreateAuthenticatedClient(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        return client;
    }
}
