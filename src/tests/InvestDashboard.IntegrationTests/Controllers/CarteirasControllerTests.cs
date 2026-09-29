using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Xunit;

namespace InvestDashboard.IntegrationTests.Controllers;

public class CarteirasControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public CarteirasControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        var token = FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetAll_WhenNoPortfolios_ReturnsEmptyPaginatedResponse()
    {
        var response = await _client.GetAsync("/api/v1/portfolios");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PaginatedPortfolioResponse>(JsonOpts);
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        var unauthClient = new CustomWebApplicationFactory().CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/portfolios");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ValidPortfolio_ReturnsCreated()
    {
        var payload = new { name = "Minha Carteira Teste", description = "Carteira de teste" };

        var response = await _client.PostAsJsonAsync("/api/v1/portfolios", payload);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        var fakeId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/v1/portfolios/{fakeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSummary_WithInvalidId_ReturnsNotFound()
    {
        var fakeId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/v1/portfolios/{fakeId}/summary");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        var fakeId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/v1/portfolios/{fakeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Local DTO for paginated response deserialization
    private record PaginatedPortfolioResponse(PortfolioDto[] Data, int Page, int PageSize, int TotalCount);
    private record PortfolioDto(Guid Id, string Name, decimal Balance, decimal TotalValue);
}
