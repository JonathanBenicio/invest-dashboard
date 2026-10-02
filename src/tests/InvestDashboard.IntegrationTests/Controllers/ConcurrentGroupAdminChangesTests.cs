using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class ConcurrentGroupAdminChangesTests
{
    [RequiresPostgresFact]
    public async Task ConcurrentAdminDowngrades_LeaveAtLeastOneActiveAdmin()
    {
        await using var database = await IsolatedPostgresDatabase.CreateAsync(
            Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var firstAdmin = CreateClient(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        using var secondAdmin = CreateClient(factory, FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName);
        var groupId = await CreateGroupAsync(firstAdmin);
        var secondMemberId = await AddSecondAdminAsync(factory, groupId);
        var firstMemberId = await GetMemberIdAsync(factory, groupId, FakeAuthProvider.TestUserId.ToString());

        var firstChange = firstAdmin.PutAsJsonAsync(
            $"/api/v1/grupos-carteiras/{groupId}/membros/{secondMemberId}/papel", new { papel = "Investidor" });
        var secondChange = secondAdmin.PutAsJsonAsync(
            $"/api/v1/grupos-carteiras/{groupId}/membros/{firstMemberId}/papel", new { papel = "Investidor" });
        using var firstResponse = await firstChange;
        using var secondResponse = await secondChange;

        new[] { firstResponse.StatusCode, secondResponse.StatusCode }
            .Should().Contain(HttpStatusCode.OK);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
        (await db.GroupMembers.CountAsync(member => member.GrupoId == groupId && member.Ativo && member.Papel == PapelGrupo.Admin))
            .Should().BeGreaterThanOrEqualTo(1);
    }

    [RequiresPostgresFact]
    public async Task ConcurrentInvitationAcceptanceAndRoleUpdate_LeaveAtLeastOneActiveAdmin()
    {
        await using var database = await IsolatedPostgresDatabase.CreateAsync(
            Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);
        using var firstAdmin = CreateClient(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        using var secondAdmin = CreateClient(factory, FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName);
        var groupId = await CreateGroupAsync(firstAdmin);
        var secondMemberId = await AddSecondAdminAsync(factory, groupId);
        var firstMemberId = await GetMemberIdAsync(factory, groupId, FakeAuthProvider.TestUserId.ToString());
        using var inviteResponse = await firstAdmin.PostAsJsonAsync($"/api/v1/grupos-carteiras/{groupId}/convites",
            new { email = FakeAuthProvider.SecondTestEmail, papel = "Investidor" });
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var pendingResponse = await secondAdmin.GetAsync("/api/v1/grupos-carteiras/convites-pendentes");
        using var pendingJson = JsonDocument.Parse(await pendingResponse.Content.ReadAsStringAsync());
        var invitationId = pendingJson.RootElement.GetProperty("dados").EnumerateArray()
            .Single(invitation => invitation.GetProperty("grupoId").GetGuid() == groupId)
            .GetProperty("id").GetGuid();

        var roleChange = firstAdmin.PutAsJsonAsync(
            $"/api/v1/grupos-carteiras/{groupId}/membros/{firstMemberId}/papel", new { papel = "Investidor" });
        var acceptance = secondAdmin.PostAsync(
            $"/api/v1/grupos-carteiras/{groupId}/convites/{invitationId}/aceitar", null);
        using var roleResponse = await roleChange;
        using var acceptanceResponse = await acceptance;

        new[] { roleResponse.StatusCode, acceptanceResponse.StatusCode }
            .Should().Contain(HttpStatusCode.OK);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
        (await db.GroupMembers.CountAsync(member => member.GrupoId == groupId && member.Ativo && member.Papel == PapelGrupo.Admin))
            .Should().BeGreaterThanOrEqualTo(1);
        (await db.GroupMembers.AsNoTracking().SingleAsync(member => member.Id == secondMemberId)).Ativo.Should().BeTrue();
    }

    private static async Task<Guid> CreateGroupAsync(HttpClient admin)
    {
        using var response = await admin.PostAsJsonAsync("/api/v1/grupos-carteiras", new { nome = "Admin concorrente" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static async Task<Guid> AddSecondAdminAsync(CustomWebApplicationFactory factory, Guid groupId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
        var member = new MembroGrupo(Guid.NewGuid(), groupId, FakeAuthProvider.SecondTestUserId.ToString(),
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName, PapelGrupo.Admin, DateTime.UtcNow);
        db.GroupMembers.Add(member);
        await db.SaveChangesAsync();
        return member.Id;
    }

    private static async Task<Guid> GetMemberIdAsync(CustomWebApplicationFactory factory, Guid groupId, string userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<InvestDbContext>().GroupMembers.AsNoTracking()
            .Where(member => member.GrupoId == groupId && member.UsuarioId == userId)
            .Select(member => member.Id)
            .SingleAsync();
    }

    private static HttpClient CreateClient(CustomWebApplicationFactory factory, Guid userId, Guid sessionId, string email, string name)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(userId, sessionId, email, name));
        return client;
    }
}
