using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class PortfolioGroupAuthorizationTests
{
    [Fact]
    public async Task InstitutionCatalog_ReusesGlobalIdsAndRestrictsCustomIdsToTheirGroup()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        var firstGroup = await CreateGroupAsync(client, "Instituições A");
        var secondGroup = await CreateGroupAsync(client, "Instituições B");
        using var catalogResponse = await client.GetAsync("/api/v1/instituicoes-financeiras");
        catalogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var catalog = JsonDocument.Parse(await catalogResponse.Content.ReadAsStringAsync());
        var bankId = catalog.RootElement.GetProperty("dados").EnumerateArray()
            .Single(item => item.GetProperty("nome").GetString() == "Banco do Brasil").GetProperty("id").GetGuid();
        bankId.Should().Be(Guid.Parse("10000000-0000-4000-8000-000000000001"));
        using var customResponse = await client.PostAsJsonAsync($"/api/v1/instituicoes-financeiras?grupoId={firstGroup}", new { nome = "Instituição da família" });
        customResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var custom = JsonDocument.Parse(await customResponse.Content.ReadAsStringAsync());
        var customId = custom.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        using var repeatResponse = await client.PostAsJsonAsync($"/api/v1/instituicoes-financeiras?grupoId={firstGroup}", new { nome = "Instituição da família" });
        using var repeat = JsonDocument.Parse(await repeatResponse.Content.ReadAsStringAsync());
        repeat.RootElement.GetProperty("dados").GetProperty("id").GetGuid().Should().Be(customId);
        using var otherCatalogResponse = await client.GetAsync($"/api/v1/instituicoes-financeiras?grupoId={secondGroup}");
        using var otherCatalog = JsonDocument.Parse(await otherCatalogResponse.Content.ReadAsStringAsync());
        otherCatalog.RootElement.GetProperty("dados").EnumerateArray().Select(item => item.GetProperty("id").GetGuid())
            .Should().Contain(bankId).And.NotContain(customId);
        using var forgedWalletResponse = await client.PostAsJsonAsync("/api/v1/portfolios", new {
            nome = "Instituição cruzada", grupoId = secondGroup, titular = "Pessoa", instituicaoFinanceiraId = customId
        });
        forgedWalletResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PrivateWallet_UsesLinkedHolderAndNeverRelationshipOrCreator()
    {
        using var factory = new CustomWebApplicationFactory();
        using var admin = CreateClient(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        using var member = CreateClient(factory, FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName);
        var groupId = await CreateGroupAsync(admin, "Titulares");
        Guid membershipId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            var membership = new MembroGrupo(Guid.NewGuid(), groupId, FakeAuthProvider.SecondTestUserId.ToString(),
                FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName, PapelGrupo.Consulta, DateTime.UtcNow);
            membershipId = membership.Id;
            db.GroupMembers.Add(membership);
            await db.SaveChangesAsync();
        }
        using var holderResponse = await admin.PostAsJsonAsync($"/api/v1/grupos-carteiras/{groupId}/titulares", new {
            nome = "Perfil familiar", parentesco = "Irmã", usuarioId = FakeAuthProvider.SecondTestUserId.ToString()
        });
        holderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var holderJson = JsonDocument.Parse(await holderResponse.Content.ReadAsStringAsync());
        var holderId = holderJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        using var response = await admin.PostAsJsonAsync("/api/v1/portfolios", new { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),
            nome = "Privada vinculada", grupoId = groupId, titularId = holderId, visibilidade = "Particular"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var walletId = json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        (await member.GetAsync($"/api/v1/portfolios/{walletId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await member.PatchAsJsonAsync($"/api/v1/portfolios/{walletId}", new { nome = "Consulta tentou editar" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        var unlinkedId = await CreatePortfolioAsync(admin, groupId, "Mesmo parentesco sem login", "Particular");
        (await member.GetAsync($"/api/v1/portfolios/{unlinkedId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.DeleteAsync($"/api/v1/grupos-carteiras/{groupId}/membros/{membershipId}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await member.GetAsync($"/api/v1/portfolios/{walletId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.GetAsync($"/api/v1/portfolios/{walletId}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GroupFilters_ScopeWalletsSummariesAndPositionsAndRequireMembership()
    {
        using var factory = new CustomWebApplicationFactory();
        using var admin = CreateClient(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        using var outsider = CreateClient(factory, FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName);

        var firstGroup = await CreateGroupAsync(admin, "Grupo A");
        var secondGroup = await CreateGroupAsync(admin, "Grupo B");
        var firstPortfolio = await CreatePortfolioAsync(admin, firstGroup, "Carteira A", "PublicaDoGrupo");
        var secondPortfolio = await CreatePortfolioAsync(admin, secondGroup, "Carteira B", "PublicaDoGrupo");
        await RegisterBuyAsync(admin, firstPortfolio, "WEGE3", 2m);
        await RegisterBuyAsync(admin, secondPortfolio, "BBAS3", 3m);
        await CreateFixedIncomeAsync(admin, firstPortfolio, 100m, 110m);
        await CreateFixedIncomeAsync(admin, secondPortfolio, 200m, 220m);

        using var walletsResponse = await admin.GetAsync($"/api/v1/portfolios?grupoId={firstGroup}&pagina=2&itensPorPagina=1");
        walletsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var walletsJson = JsonDocument.Parse(await walletsResponse.Content.ReadAsStringAsync());
        walletsJson.RootElement.GetProperty("dados").GetArrayLength().Should().Be(0);
        walletsJson.RootElement.GetProperty("paginacao").GetProperty("totalItens").GetInt32().Should().Be(1);

        using var summaryResponse = await admin.GetAsync($"/api/v1/portfolios/resumo-geral?grupoId={firstGroup}");
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var summaryJson = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        summaryJson.RootElement.GetProperty("dados").GetProperty("quantidadeCarteiras").GetInt32().Should().Be(1);
        summaryJson.RootElement.GetProperty("dados").GetProperty("totalInvestido").GetDecimal().Should().Be(120m);

        using var positionsResponse = await admin.GetAsync($"/api/v1/investments?grupoId={firstGroup}&tipo=variable_income");
        positionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var positionsJson = JsonDocument.Parse(await positionsResponse.Content.ReadAsStringAsync());
        positionsJson.RootElement.GetProperty("dados").EnumerateArray()
            .Select(position => position.GetProperty("ticker").GetString()).Should().ContainSingle().Which.Should().Be("WEGE3");

        using var projectionResponse = await admin.GetAsync($"/api/v1/portfolios/projecao-renda-fixa?grupoId={firstGroup}");
        projectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var projectionJson = JsonDocument.Parse(await projectionResponse.Content.ReadAsStringAsync());
        projectionJson.RootElement.GetProperty("dados").GetProperty("quantidadePosicoes").GetInt32().Should().Be(1);

        using var allProjectionResponse = await admin.GetAsync("/api/v1/portfolios/projecao-renda-fixa");
        using var allProjectionJson = JsonDocument.Parse(await allProjectionResponse.Content.ReadAsStringAsync());
        allProjectionJson.RootElement.GetProperty("dados").GetProperty("quantidadePosicoes").GetInt32().Should().Be(2);

        (await outsider.GetAsync($"/api/v1/portfolios?grupoId={firstGroup}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await outsider.GetAsync($"/api/v1/portfolios/resumo-geral?grupoId={firstGroup}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GroupRolesAndPortfolioVisibility_AreEnforcedOnEveryRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var admin = CreateClient(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        using var member = CreateClient(factory, FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName);

        var groupResponse = await admin.PostAsJsonAsync("/api/v1/grupos-carteiras", new { nome = "Família" });
        groupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var groupJson = JsonDocument.Parse(await groupResponse.Content.ReadAsStringAsync());
        var groupId = groupJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.GroupMembers.Add(new MembroGrupo(Guid.NewGuid(), groupId, FakeAuthProvider.SecondTestUserId.ToString(),
                FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName, PapelGrupo.Consulta, DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        var privateId = await CreatePortfolioAsync(admin, groupId, "Privada", "Particular");
        var publicId = await CreatePortfolioAsync(admin, groupId, "Compartilhada", "PublicaDoGrupo");
        var visibleResponse = await member.GetAsync("/api/v1/portfolios");
        visibleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var visibleJson = JsonDocument.Parse(await visibleResponse.Content.ReadAsStringAsync()))
        {
            var ids = visibleJson.RootElement.GetProperty("dados").EnumerateArray()
                .Select(item => item.GetProperty("id").GetGuid()).ToArray();
            ids.Should().Contain(publicId).And.NotContain(privateId);
        }
        (await member.GetAsync($"/api/v1/portfolios/{privateId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await member.PatchAsJsonAsync($"/api/v1/portfolios/{publicId}", new { nome = "Tentativa" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        var membersResponse = await admin.GetAsync($"/api/v1/grupos-carteiras/{groupId}/membros");
        using var membersJson = JsonDocument.Parse(await membersResponse.Content.ReadAsStringAsync());
        var memberId = membersJson.RootElement.GetProperty("dados").EnumerateArray()
            .Single(item => item.GetProperty("usuarioId").GetString() == FakeAuthProvider.SecondTestUserId.ToString())
            .GetProperty("id").GetGuid();
        (await admin.DeleteAsync($"/api/v1/grupos-carteiras/{groupId}/membros/{memberId}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        using var afterDeactivation = JsonDocument.Parse(await (await member.GetAsync("/api/v1/portfolios")).Content.ReadAsStringAsync());
        afterDeactivation.RootElement.GetProperty("dados").GetArrayLength().Should().Be(0);
    }

    private static async Task<Guid> CreatePortfolioAsync(HttpClient client, Guid groupId, string name, string visibility)
    {
        var response = await client.PostAsJsonAsync("/api/v1/portfolios", new
        {
            nome = name,
            grupoId = groupId,
            titular = "Titular sem conta",
            instituicaoFinanceira = "Corretora exemplo",
            tipoInstituicao = "Outra",
            visibilidade = visibility
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateGroupAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/grupos-carteiras", new { nome = name });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task RegisterBuyAsync(HttpClient client, Guid portfolioId, string ticker, decimal quantity)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId = portfolioId,
            ticker,
            tipo = "Buy",
            classeAtivo = "ACAO",
            nome = ticker,
            quantidade = quantity,
            precoUnitario = 10m,
            taxas = 0m,
            dataTransacao = DateTime.UtcNow,
            chaveIdempotencia = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    private static async Task CreateFixedIncomeAsync(HttpClient client, Guid portfolioId, decimal principal, decimal statementValue)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/investments/fixed-income", new
        {
            carteiraId = portfolioId,
            nome = "CDB",
            subtipo = "CDB",
            emissor = "Banco teste",
            valorPrincipal = principal,
            valorExtrato = statementValue,
            taxaJuros = 10m,
            indexador = "PREFIXADO",
            dataCompra = DateTime.UtcNow.AddDays(-1),
            dataVencimento = DateTime.UtcNow.AddYears(1),
            convencao = "365 dias corridos",
            chaveIdempotencia = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    private static HttpClient CreateClient(CustomWebApplicationFactory factory, Guid userId, Guid sessionId, string email, string name)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(userId, sessionId, email, name));
        return client;
    }
}
