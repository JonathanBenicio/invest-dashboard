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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class PostgresGroupWorkspaceTests
{
    [RequiresPostgresFact]
    public async Task HolderMigration_PreservesLegacyWalletsAndResolvesInstitutionReferences()
    {
        var connectionString = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connectionString);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260929201209_AddFinancialInstitutionCategory");
        var groupId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO portfolio_groups (id,name,created_by_user_id,created_at_utc) VALUES ({groupId},'Legado','creator',CURRENT_TIMESTAMP)");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO portfolios (id,user_id,name,group_id,holder_name,visibility,balance,realized_gain,realized_cost_basis,version,financial_institution,financial_institution_type) VALUES ({walletId},'creator','Carteira legada',{groupId},'Maria','Particular',0,0,0,0,'Banco do Brasil','Banco')");
        await migrator.MigrateAsync();
        var wallet = await db.Portfolios.AsNoTracking().Include(item => item.TitularProfile).SingleAsync(item => item.Id == walletId);
        wallet.TitularProfile!.Nome.Should().Be("Maria");
        wallet.TitularProfile.UsuarioId.Should().BeNull();
        wallet.InstituicaoFinanceiraId.Should().Be(Guid.Parse("10000000-0000-4000-8000-000000000001"));
        wallet.UserId.Should().Be("creator");
        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [RequiresPostgresFact]
    public async Task GroupsInvitationsPrivatePortfoliosAndRateHistory_PersistInPostgres()
    {
        var connectionString = Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!;
        await using var database = await IsolatedPostgresDatabase.CreateAsync(connectionString);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var admin = CreateClient(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);

        var groupResponse = await admin.PostAsJsonAsync("/api/v1/grupos-carteiras", new { nome = "Grupo PostgreSQL" });
        groupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var groupJson = JsonDocument.Parse(await groupResponse.Content.ReadAsStringAsync());
        var groupId = groupJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        var privateId = await CreatePortfolioAsync(admin, groupId, "Privada", "Particular");
        var publicId = await CreatePortfolioAsync(admin, groupId, "Pública", "PublicaDoGrupo");

        var rateResponse = await admin.PostAsJsonAsync($"/api/v1/taxes?grupoId={groupId}", new
        {
            nome = "CDI manual",
            simbolo = "CDI",
            valorAtual = 10m,
            valorAnterior = 9.8m,
            descricao = "Premissa cadastrada pelo grupo",
            origem = "Manual",
            unidade = "Percentual",
            periodicidade = "Anual",
            dataReferencia = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        });
        rateResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var rateJson = JsonDocument.Parse(await rateResponse.Content.ReadAsStringAsync());
        var rateId = rateJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        using var historyJson = JsonDocument.Parse(await (await admin.GetAsync($"/api/v1/taxes/{rateId}/historico?grupoId={groupId}")).Content.ReadAsStringAsync());
        historyJson.RootElement.GetProperty("dados").GetArrayLength().Should().Be(1);

        var inviteResponse = await admin.PostAsJsonAsync($"/api/v1/grupos-carteiras/{groupId}/convites",
            new { email = FakeAuthProvider.SecondTestEmail, papel = "Investidor" });
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid invitationId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
            invitationId = db.GroupInvitations.Single(item => item.GrupoId == groupId).Id;
        }

        var acceptResponse = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/convites/aceitar", new
        {
            grupoId = groupId, conviteId = invitationId, tokenHash = "invite-second-user"
        });
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var acceptedJson = JsonDocument.Parse(await acceptResponse.Content.ReadAsStringAsync());
        using var member = factory.CreateClient();
        member.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", acceptedJson.RootElement.GetProperty("dados").GetProperty("tokenAcesso").GetString());

        var buy = await member.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId = publicId, ticker = "WEGE3", tipo = "Buy", classeAtivo = "ACAO", nome = "WEG",
            quantidade = 2200m, precoUnitario = 10m, taxas = 0m, dataTransacao = DateTime.UtcNow.AddDays(-1),
            chaveIdempotencia = Guid.NewGuid()
        });
        buy.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = await member.PostAsJsonAsync("/api/v1/transactions", new
        {
            carteiraId = publicId, ticker = "WEGE3", tipo = "Sell", classeAtivo = "ACAO",
            quantidade = 2200m, precoUnitario = 11m, taxas = 0m, modalidadeFiscal = "Comum",
            dataTransacao = DateTime.UtcNow, chaveIdempotencia = Guid.NewGuid()
        });
        sale.StatusCode.Should().Be(HttpStatusCode.Created);

        using var visibleJson = JsonDocument.Parse(await (await member.GetAsync("/api/v1/portfolios")).Content.ReadAsStringAsync());
        var visibleIds = visibleJson.RootElement.GetProperty("dados").EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid()).ToArray();
        visibleIds.Should().Contain(publicId).And.NotContain(privateId);
        (await member.GetAsync($"/api/v1/taxes?grupoId={groupId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        using var holderResponse = await admin.PostAsJsonAsync($"/api/v1/grupos-carteiras/{groupId}/titulares", new {
            nome = "Titular vinculado", parentesco = "Pai", usuarioId = FakeAuthProvider.SecondTestUserId.ToString()
        });
        holderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var holderJson = JsonDocument.Parse(await holderResponse.Content.ReadAsStringAsync());
        var holderId = holderJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        using var customInstitutionResponse = await admin.PostAsJsonAsync($"/api/v1/instituicoes-financeiras?grupoId={groupId}", new { nome = "Instituição PostgreSQL" });
        customInstitutionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var customInstitutionJson = JsonDocument.Parse(await customInstitutionResponse.Content.ReadAsStringAsync());
        var institutionId = customInstitutionJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        using var updateResponse = await admin.PatchAsJsonAsync($"/api/v1/portfolios/{privateId}", new { titularId = holderId, instituicaoFinanceiraId = institutionId });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());
        (await member.GetAsync($"/api/v1/portfolios/{privateId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        await using (var persistedScope = factory.Services.CreateAsyncScope())
        {
            var db = persistedScope.ServiceProvider.GetRequiredService<InvestDbContext>();
            db.Portfolios.Single(item => item.Id == privateId).TitularId.Should().Be(holderId);
            db.Portfolios.Single(item => item.Id == privateId).InstituicaoFinanceiraId.Should().Be(institutionId);
            db.Transactions.Where(item => item.CarteiraId == publicId).All(item => item.TitularId != null).Should().BeTrue();
        }

        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));
        using var taxResponse = await admin.GetAsync(
            $"/api/v1/taxes/estimativa-mensal?ano={now.Year}&mes={now.Month}&grupoId={groupId}&titular=Titular%20PostgreSQL");
        taxResponse.StatusCode.Should().Be(HttpStatusCode.OK, await taxResponse.Content.ReadAsStringAsync());
        using var holderTaxJson = JsonDocument.Parse(await taxResponse.Content.ReadAsStringAsync());
        holderTaxJson.RootElement.GetProperty("dados").GetProperty("vendasAcoesComuns").GetDecimal().Should().Be(24_200m);
        holderTaxJson.RootElement.GetProperty("dados").GetProperty("estimativaIr").GetDecimal().Should().Be(330m);

        using var membersJson = JsonDocument.Parse(await (await admin.GetAsync($"/api/v1/grupos-carteiras/{groupId}/membros")).Content.ReadAsStringAsync());
        var memberId = membersJson.RootElement.GetProperty("dados").EnumerateArray()
            .Single(item => item.GetProperty("usuarioId").GetString() == FakeAuthProvider.SecondTestUserId.ToString())
            .GetProperty("id").GetGuid();
        (await admin.DeleteAsync($"/api/v1/grupos-carteiras/{groupId}/membros/{memberId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        using var afterDeactivation = JsonDocument.Parse(await (await member.GetAsync("/api/v1/portfolios")).Content.ReadAsStringAsync());
        afterDeactivation.RootElement.GetProperty("dados").GetArrayLength().Should().Be(0);
    }

    private static async Task<Guid> CreatePortfolioAsync(HttpClient client, Guid groupId, string name, string visibility)
    {
        var response = await client.PostAsJsonAsync("/api/v1/portfolios", new
        { instituicaoFinanceiraId = Guid.Parse("10000000-0000-4000-8000-000000000001"),
            nome = name,
            grupoId = groupId,
            titular = "Titular PostgreSQL",
            visibilidade = visibility
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static HttpClient CreateClient(CustomWebApplicationFactory factory, Guid userId, Guid sessionId, string email, string name)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(userId, sessionId, email, name));
        return client;
    }
}
