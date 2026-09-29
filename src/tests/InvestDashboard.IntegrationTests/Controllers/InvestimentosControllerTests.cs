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

public class InvestimentosControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public InvestimentosControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        var token = FakeAuthProvider.GenerateJwt(FakeAuthProvider.TestEmail);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetAll_WhenNoInvestments_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/v1/investments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse>(JsonOpts);
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        var unauthClient = new CustomWebApplicationFactory().CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/investments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_WhenEmpty_ReturnsZeroValues()
    {
        var response = await _client.GetAsync("/api/v1/investments/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"totalInvested\"");
    }

    [Fact]
    public async Task GetDividends_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/v1/investments/dividends");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse>(JsonOpts);
        body.Should().NotBeNull();
        body!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        var fakeId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/v1/investments/{fakeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransactions_WithInvalidId_ReturnsNotFound()
    {
        var fakeId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/v1/investments/{fakeId}/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        var fakeId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/v1/investments/{fakeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUsingLegacyRoute_ReturnsMethodNotAllowed()
    {
        var fakeId = Guid.NewGuid();
        var payload = new { quantity = 10 };

        var response = await _client.PatchAsJsonAsync($"/api/v1/investments/{fakeId}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    // Local DTO for deserialization
    private record PaginatedResponse(JsonElement[] Data, int Page, int PageSize, int TotalCount);
}
