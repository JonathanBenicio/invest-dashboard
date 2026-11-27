using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Application.DTOs.MarketData;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class TesouroDiretoCotacoesControllerTests
{
    [Fact]
    public async Task Rotas_ExigemAutenticacao()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/tesouro-direto/titulos");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CatalogoAutenticado_RetornaOrigemDataEIdentidade()
    {
        using var factory = CriarFactory();
        using var client = Autenticar(factory);
        using var response = await client.GetAsync("/api/v1/tesouro-direto/titulos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var dados = json.RootElement.GetProperty("dados");
        dados.GetProperty("origem").GetString().Should().Contain("Tesouro Transparente");
        dados.GetProperty("ultimaDataDisponivel").GetString().Should().Be("2025-01-03");
        dados.GetProperty("titulos")[0].GetProperty("dataVencimento").GetString().Should().Be("2032-01-01");
    }

    [Fact]
    public async Task ConsultaAutenticada_RetornaValoresEExigeTodosOsFiltros()
    {
        using var factory = CriarFactory();
        using var client = Autenticar(factory);
        using var incompleta = await client.GetAsync("/api/v1/tesouro-direto/cotacoes?tipoTitulo=Tesouro%20Prefixado");
        incompleta.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var response = await client.GetAsync("/api/v1/tesouro-direto/cotacoes?tipoTitulo=Tesouro%20Prefixado&dataVencimento=2032-01-01&dataDe=2025-01-01&dataAte=2025-01-03");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var dados = json.RootElement.GetProperty("dados");
        dados.GetProperty("cotacoes")[0].GetProperty("dataBase").GetString().Should().Be("2025-01-03");
        dados.GetProperty("cotacoes")[0].GetProperty("puBaseManha").GetDecimal().Should().Be(800m);
    }

    [Fact]
    public async Task FonteIndisponivel_Retorna503()
    {
        using var factory = CriarFactory(falhar: true);
        using var client = Autenticar(factory);
        using var response = await client.GetAsync("/api/v1/tesouro-direto/titulos");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private static WebApplicationFactory<Program> CriarFactory(bool falhar = false) =>
        new CustomWebApplicationFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITesouroTransparenteConsulta>();
            services.AddSingleton<ITesouroTransparenteConsulta>(new ConsultaFalsa(falhar));
        }));

    private static HttpClient Autenticar(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        return client;
    }

    private sealed class ConsultaFalsa(bool falhar) : ITesouroTransparenteConsulta
    {
        public Task<CatalogoTesouroDto> ListarTitulosAsync(CancellationToken cancellationToken = default)
        {
            if (falhar) throw new MarketDataUnavailableException();
            return Task.FromResult(new CatalogoTesouroDto("Tesouro Transparente — teste", "https://example.test",
                DateTimeOffset.UnixEpoch, new(2025, 1, 3), [new("Tesouro Prefixado", new(2032, 1, 1), new(2025, 1, 3))]));
        }

        public Task<SerieCotacoesTesouroDto> ConsultarAsync(string tipoTitulo, DateOnly vencimento, DateOnly de,
            DateOnly ate, CancellationToken cancellationToken = default)
            => Task.FromResult(new SerieCotacoesTesouroDto("Tesouro Transparente — teste", "https://example.test",
                DateTimeOffset.UnixEpoch, new(2025, 1, 3), tipoTitulo, vencimento, de, ate,
                [new(new(2025, 1, 3), 10, 11, 810, 800, 800)]));
    }
}
