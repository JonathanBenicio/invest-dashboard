namespace InvestDashboard.Application.Interfaces;

public interface IInvitationEmailSender
{
    Task<bool> SendInvitationAsync(string email, Guid groupId, Guid invitationId, CancellationToken cancellationToken = default);
}
