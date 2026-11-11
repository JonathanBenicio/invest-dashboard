using System.Net;
using System.Text.Json;
using FluentAssertions;
using InvestDashboard.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace InvestDashboard.UnitTests.Infrastructure;

public sealed class SupabaseInvitationEmailSenderTests
{
    private const string ProjectUrl = "https://project.supabase.co";
    private const string SecretKey = "sb_secret_fake_unit_test_key";
    private const string RedirectUrl = "https://app.example.com/auth/callback?source=admin&next=%2Fgroups%3Ftab%3Dmembers";
    private static readonly Guid GroupId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001");
    private static readonly Guid InvitationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002");

    [Fact]
    public async Task SendInvitationAsync_SendsInviteWithSecretKeyAndPreservesRedirectQuery()
    {
        string? submittedBody = null;
        using var httpClient = CreateHttpClient(async (request, cancellationToken) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/auth/v1/invite");
            request.Headers.GetValues("apikey").Should().ContainSingle().Which.Should().Be(SecretKey);
            request.Headers.Authorization.Should().BeNull();
            request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
            submittedBody = await request.Content.ReadAsStringAsync(cancellationToken);

            var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
            var redirectUri = new Uri(query["redirect_to"]!);
            var redirectQuery = System.Web.HttpUtility.ParseQueryString(redirectUri.Query);
            redirectUri.GetLeftPart(UriPartial.Path).Should().Be("https://app.example.com/auth/callback");
            redirectQuery["source"].Should().Be("admin");
            redirectQuery["next"].Should().Be("/groups?tab=members");
            redirectQuery["grupoId"].Should().Be(GroupId.ToString());
            redirectQuery["conviteId"].Should().Be(InvitationId.ToString());

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sender = CreateSender(httpClient);

        var result = await sender.SendInvitationAsync("invitee@example.com", GroupId, InvitationId);

        result.Should().BeTrue();
        using var payload = JsonDocument.Parse(submittedBody!);
        payload.RootElement.EnumerateObject().Select(property => property.Name).Should().Equal("email");
        payload.RootElement.GetProperty("email").GetString().Should().Be("invitee@example.com");
    }

    [Fact]
    public async Task SendInvitationAsync_ReturnsFalseWhenSupabaseReportsUnprocessableEntity()
    {
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)));
        var sender = CreateSender(httpClient);

        var result = await sender.SendInvitationAsync("invitee@example.com", GroupId, InvitationId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendInvitationAsync_ThrowsWhenSupabaseReturnsAnotherFailure()
    {
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var sender = CreateSender(httpClient);

        var action = () => sender.SendInvitationAsync("invitee@example.com", GroupId, InvitationId);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Supabase could not send the group invitation.");
    }

    [Fact]
    public async Task SendInvitationAsync_RejectsMissingSecretKeyBeforeSending()
    {
        var sendCount = 0;
        using var httpClient = CreateHttpClient((_, _) =>
        {
            sendCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var sender = CreateSender(httpClient, includeSecretKey: false);

        var action = () => sender.SendInvitationAsync("invitee@example.com", GroupId, InvitationId);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Supabase invitation configuration is incomplete.");
        sendCount.Should().Be(0);
    }

    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) =>
        new(new StubHttpMessageHandler(sendAsync));

    private static SupabaseInvitationEmailSender CreateSender(HttpClient httpClient, bool includeSecretKey = true)
    {
        var configuration = new ConfigurationManager
        {
            ["Storage:SupabaseUrl"] = ProjectUrl,
            ["Storage:SupabaseInviteRedirectUrl"] = RedirectUrl
        };
        if (includeSecretKey)
            configuration["Storage:SupabaseSecretKey"] = SecretKey;

        return new SupabaseInvitationEmailSender(httpClient, configuration);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => sendAsync(request, cancellationToken);
    }
}
