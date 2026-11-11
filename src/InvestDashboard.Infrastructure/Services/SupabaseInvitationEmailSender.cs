using System.Net.Http.Json;
using InvestDashboard.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace InvestDashboard.Infrastructure.Services;

public sealed class SupabaseInvitationEmailSender(HttpClient httpClient, IConfiguration configuration) : IInvitationEmailSender
{
    public async Task<bool> SendInvitationAsync(string email, Guid groupId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Storage:SupabaseUrl"]?.TrimEnd('/');
        var secretKey = configuration["Storage:SupabaseSecretKey"];
        var redirectUrl = configuration["Storage:SupabaseInviteRedirectUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(secretKey) || !Uri.TryCreate(redirectUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException("Supabase invitation configuration is incomplete.");

        var redirect = new UriBuilder(redirectUrl);
        var query = System.Web.HttpUtility.ParseQueryString(redirect.Query);
        query["grupoId"] = groupId.ToString();
        query["conviteId"] = invitationId.ToString();
        redirect.Query = query.ToString();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/auth/v1/invite?redirect_to={Uri.EscapeDataString(redirect.ToString())}");
        request.Headers.Add("apikey", secretKey);
        request.Content = JsonContent.Create(new { email });
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if ((int)response.StatusCode == 422) return false;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Supabase could not send the group invitation.");
        return true;
    }
}
