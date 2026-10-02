using InvestDashboard.Application.Interfaces;

namespace InvestDashboard.IntegrationTests.Fakes;

public sealed class FakeInvitationEmailSender : IInvitationEmailSender
{
    public Task<bool> SendInvitationAsync(string email, Guid groupId, Guid invitationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(!string.Equals(email, FakeAuthProvider.SecondTestEmail, StringComparison.OrdinalIgnoreCase));
}
