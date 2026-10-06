using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.IntegrationTests.Fakes;
using InvestDashboard.IntegrationTests.Setup;

namespace InvestDashboard.IntegrationTests.Controllers;

public sealed class GroupInvitationTests
{
    [Fact]
    public async Task ExistingConfirmedUserCanAcceptPendingGroupInviteInApp()
    {
        using var factory = new CustomWebApplicationFactory();
        using var admin = CreateClient(factory, FakeAuthProvider.TestUserId, FakeAuthProvider.TestSessionId,
            FakeAuthProvider.TestEmail, FakeAuthProvider.TestName);
        var groupResponse = await admin.PostAsJsonAsync("/api/v1/grupos-carteiras", new { nome = "Família" });
        using var groupJson = JsonDocument.Parse(await groupResponse.Content.ReadAsStringAsync());
        var groupId = groupJson.RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        var invitationResponse = await admin.PostAsJsonAsync($"/api/v1/grupos-carteiras/{groupId}/convites",
            new { email = FakeAuthProvider.SecondTestEmail, papel = "Consulta" });
        invitationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var invitationJson = JsonDocument.Parse(await invitationResponse.Content.ReadAsStringAsync());
        invitationJson.RootElement.GetProperty("dados").GetProperty("emailEnviado").GetBoolean().Should().BeFalse();

        using var invitedUser = CreateClient(factory, FakeAuthProvider.SecondTestUserId, FakeAuthProvider.SecondTestSessionId,
            FakeAuthProvider.SecondTestEmail, FakeAuthProvider.SecondTestName);
        using var pendingJson = JsonDocument.Parse(await (await invitedUser.GetAsync("/api/v1/grupos-carteiras/convites-pendentes")).Content.ReadAsStringAsync());
        var pendingInvitation = pendingJson.RootElement.GetProperty("dados").EnumerateArray().Single();
        pendingInvitation.GetProperty("grupoId").GetGuid().Should().Be(groupId);

        var acceptResponse = await invitedUser.PostAsync(
            $"/api/v1/grupos-carteiras/{groupId}/convites/{pendingInvitation.GetProperty("id").GetGuid()}/aceitar", null);
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var groupsJson = JsonDocument.Parse(await (await invitedUser.GetAsync("/api/v1/grupos-carteiras")).Content.ReadAsStringAsync());
        groupsJson.RootElement.GetProperty("dados").GetArrayLength().Should().Be(1);
        groupsJson.RootElement.GetProperty("dados")[0].GetProperty("papel").GetString().Should().Be("Consulta");
        var membersResponse = await invitedUser.GetAsync($"/api/v1/grupos-carteiras/{groupId}/membros");
        membersResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static HttpClient CreateClient(CustomWebApplicationFactory factory, Guid userId, Guid sessionId, string email, string name)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            FakeAuthProvider.GenerateJwtForUser(userId, sessionId, email, name));
        return client;
    }
}
