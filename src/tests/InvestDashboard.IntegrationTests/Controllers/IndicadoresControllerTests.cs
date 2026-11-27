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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class IndicadoresControllerTests
{
    [Fact]
    public async Task Consulta_ExigeAutenticacao()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/indicadores/selic?dataDe=2025-01-01&dataAte=2025-01-31");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("selic", "SELIC", 11, "percentual ao dia")]
    [InlineData("ipca", "IPCA", 433, "variação percentual mensal")]
    public async Task ConsultaAutenticada_RetornaContratoComOrigemEPeriodo(string rota, string indicador, int codigo, string unidade)
    {
        using var factory = new CustomWebApplicationFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIndicadoresBcbProvider>();
            services.AddSingleton<IIndicadoresBcbProvider>(new FakeIndicadores());
        }));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));

        using var response = await client.GetAsync($"/api/v1/indicadores/{rota}?dataDe=2025-01-01&dataAte=2025-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var dados = json.RootElement.GetProperty("dados");
        dados.GetProperty("indicador").GetString().Should().Be(indicador);
        dados.GetProperty("codigoSgs").GetInt32().Should().Be(codigo);
        dados.GetProperty("unidade").GetString().Should().Be(unidade);
        dados.GetProperty("origem").GetString().Should().Contain("Banco Central");
        dados.GetProperty("pontos")[0].GetProperty("data").GetString().Should().Be("2025-01-01");
    }

    [Fact]
    public async Task ConsultaSemDatas_Retorna400AntesDeInvocarProvedor()
    {
        using var factory = new CustomWebApplicationFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIndicadoresBcbProvider>();
            services.AddSingleton<IIndicadoresBcbProvider>(new FakeIndicadores());
        }));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        using var response = await client.GetAsync("/api/v1/indicadores/ipca");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FonteIndisponivel_Retorna503()
    {
        using var factory = new CustomWebApplicationFactory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIndicadoresBcbProvider>();
            services.AddSingleton<IIndicadoresBcbProvider>(new FakeIndicadores { Falhar = true });
        }));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail));
        using var response = await client.GetAsync("/api/v1/indicadores/selic?dataDe=2025-01-01&dataAte=2025-01-31");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private sealed class FakeIndicadores : IIndicadoresBcbProvider
    {
        public bool Falhar { get; init; }

        public Task<SerieIndicadorBcbDto> GetSelicAsync(DateOnly de, DateOnly ate, CancellationToken cancellationToken = default) =>
            Criar("SELIC", 11, "percentual ao dia", "diária", de, ate);

        public Task<SerieIndicadorBcbDto> GetIpcaAsync(DateOnly de, DateOnly ate, CancellationToken cancellationToken = default) =>
            Criar("IPCA", 433, "variação percentual mensal", "mensal", de, ate);

        private Task<SerieIndicadorBcbDto> Criar(string nome, int codigo, string unidade, string periodicidade, DateOnly de, DateOnly ate)
        {
            if (Falhar) throw new MarketDataUnavailableException();
            return Task.FromResult(new SerieIndicadorBcbDto(nome, codigo, unidade, periodicidade, de, ate,
                "Banco Central do Brasil — SGS (teste)", DateTime.UtcNow, [new PontoIndicadorBcbDto(de, 0.1m)]));
        }
    }
}
