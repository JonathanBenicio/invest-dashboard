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

public sealed class GroupMembershipValidationTests
{
    [Theory]
    [InlineData("Investidor", false)]
    [InlineData("Consulta", false)]
    [InlineData("Investidor", true)]
    [InlineData("Consulta", true)]
    public async Task InvitationAcceptance_DowngradesAdminOnlyWhenAnotherActiveAdminExists(string role, bool anotherAdmin)
    {
        using var factory = new CustomWebApplicationFactory();

        await AssertInvitationAcceptanceAsync(factory, role, anotherAdmin);
    }

    [Theory]
    [InlineData("999")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task RoleUpdate_RejectsUndefinedNumericRolesWithoutChangingMembership(string role)
    {
        using var factory = new CustomWebApplicationFactory();

        await AssertInvalidRoleRejectedAsync(factory, role);
    }

    [RequiresPostgresFact]
    public async Task InvalidMembershipChanges_LeaveRolesAndInvitationsUnchangedInPostgres()
    {
        await using var database = await IsolatedPostgresDatabase.CreateAsync(
            Environment.GetEnvironmentVariable("INVEST_TEST_POSTGRES_CONNECTION")!);
        using var factory = CustomWebApplicationFactory.CreatePostgres(database.ConnectionString);

        await AssertInvitationAcceptanceAsync(factory, "Investidor", false);
        await AssertInvitationAcceptanceAsync(factory, "Consulta", false);
        await AssertInvalidRoleRejectedAsync(factory, "999");
    }

    private static async Task AssertInvitationAcceptanceAsync(CustomWebApplicationFactory factory, string role, bool anotherAdmin)
    {
        using var admin = CreateAdminClient(factory);
        var groupId = await CreateGroupAsync(admin);
        if (anotherAdmin)
            await AddSecondMemberAsync(factory, groupId, PapelGrupo.Admin);

        using var invitationResponse = await admin.PostAsJsonAsync($"/api/v1/grupos-carteiras/{groupId}/convites",
            new { email = FakeAuthProvider.TestEmail, papel = role });
        invitationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var pendingResponse = await admin.GetAsync("/api/v1/grupos-carteiras/convites-pendentes");
        using var pendingJson = JsonDocument.Parse(await pendingResponse.Content.ReadAsStringAsync());
        var invitationId = pendingJson.RootElement.GetProperty("dados").EnumerateArray()
            .Single(item => item.GetProperty("grupoId").GetGuid() == groupId).GetProperty("id").GetGuid();

        using var acceptanceResponse = await admin.PostAsync(
            $"/api/v1/grupos-carteiras/{groupId}/convites/{invitationId}/aceitar", null);

        acceptanceResponse.StatusCode.Should().Be(anotherAdmin ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
        var member = await db.GroupMembers.AsNoTracking().SingleAsync(item =>
            item.GrupoId == groupId && item.UsuarioId == FakeAuthProvider.TestUserId.ToString());
        member.Papel.Should().Be(anotherAdmin ? Enum.Parse<PapelGrupo>(role) : PapelGrupo.Admin);
        member.Ativo.Should().BeTrue();
        (await db.GroupMembers.CountAsync(item => item.GrupoId == groupId && item.Ativo && item.Papel == PapelGrupo.Admin))
            .Should().Be(1);
        var invitation = await db.GroupInvitations.AsNoTracking().SingleAsync(item => item.Id == invitationId);
        invitation.AceitoEmUtc.HasValue.Should().Be(anotherAdmin);
        if (!anotherAdmin)
            (await admin.GetAsync($"/api/v1/grupos-carteiras/{groupId}/membros")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task AssertInvalidRoleRejectedAsync(CustomWebApplicationFactory factory, string role)
    {
        using var admin = CreateAdminClient(factory);
        var groupId = await CreateGroupAsync(admin);
        var memberId = await AddSecondMemberAsync(factory, groupId, PapelGrupo.Consulta);

        using var response = await admin.PutAsJsonAsync($"/api/v1/grupos-carteiras/{groupId}/membros/{memberId}/papel",
            new { papel = role });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
        var member = await db.GroupMembers.AsNoTracking().SingleAsync(item => item.Id == memberId);
        member.Papel.Should().Be(PapelGrupo.Consulta);
        member.Ativo.Should().BeTrue();
    }

    private static async Task<Guid> AddSecondMemberAsync(CustomWebApplicationFactory factory, Guid groupId, PapelGrupo role)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestDbContext>();
        var member = new MembroGrupo(Guid.NewGuid(), groupId, FakeAuthProvider.SecondTestUserId.ToString(),
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName, role, DateTime.UtcNow);
        db.GroupMembers.Add(member);
        await db.SaveChangesAsync();
        return member.Id;
    }

    private static async Task<Guid> CreateGroupAsync(HttpClient admin)
    {
        using var response = await admin.PostAsJsonAsync("/api/v1/grupos-carteiras", new { nome = "Validação de membros" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("id").GetGuid();
    }

    private static HttpClient CreateAdminClient(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
                FakeAuthProvider.TestEmail, FakeAuthProvider.TestName));
        return client;
    }
}
