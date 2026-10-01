using InvestDashboard.Domain.Aggregates.Portfolio;

namespace InvestDashboard.Domain.Repository;

public interface IGrupoCarteirasRepository
{
    Task AddGroupAsync(GrupoCarteiras group, CancellationToken cancellationToken = default);
    Task AddMemberAsync(MembroGrupo member, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(GrupoCarteiras Group, MembroGrupo Membership)>> GetGroupsForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MembroGrupo>> GetMembersAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<MembroGrupo?> GetMemberAsync(Guid groupId, string userId, CancellationToken cancellationToken = default);
    Task<MembroGrupo?> GetMemberByIdAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken = default);
    Task<ConviteGrupo?> GetPendingInvitationAsync(Guid groupId, string email, DateTime utcNow, CancellationToken cancellationToken = default);
    Task AddInvitationAsync(ConviteGrupo invitation, CancellationToken cancellationToken = default);
    Task<ConviteGrupo?> GetInvitationAsync(Guid groupId, Guid invitationId, CancellationToken cancellationToken = default);
    Task<int> CountActiveAdminsAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(ConviteGrupo Invitation, GrupoCarteiras Group)>> GetPendingInvitationsForEmailAsync(string email, DateTime utcNow, CancellationToken cancellationToken = default);
}
