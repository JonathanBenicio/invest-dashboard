using InvestDashboard.Domain.Aggregates.Portfolio;
using InvestDashboard.Domain.Repository;
using InvestDashboard.Infrastructure.Persistence.EFCore;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.Infrastructure.Persistence.RepositoryImpl;

public sealed class GrupoCarteirasRepository(InvestDbContext context) : IGrupoCarteirasRepository
{
    public async Task LockMembershipChangesAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsNpgsql()) return;

        await context.Database.SqlQuery<Guid>(
                $"SELECT id AS \"Value\" FROM portfolio_groups WHERE id = {groupId} FOR UPDATE")
            .ToListAsync(cancellationToken);
    }

    public async Task AddGroupAsync(GrupoCarteiras group, CancellationToken cancellationToken = default) =>
        await context.PortfolioGroups.AddAsync(group, cancellationToken);

    public async Task AddMemberAsync(MembroGrupo member, CancellationToken cancellationToken = default) =>
        await context.GroupMembers.AddAsync(member, cancellationToken);

    public async Task<IReadOnlyList<(GrupoCarteiras Group, MembroGrupo Membership)>> GetGroupsForUserAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var memberships = await context.GroupMembers.AsNoTracking()
            .Where(member => member.UsuarioId == userId && member.Ativo)
            .Join(context.PortfolioGroups.AsNoTracking(), member => member.GrupoId, group => group.Id,
                (member, group) => new { Group = group, Membership = member })
            .OrderBy(item => item.Group.Name)
            .ToListAsync(cancellationToken);
        return memberships.Select(item => (item.Group, item.Membership)).ToList();
    }

    public async Task<IReadOnlyList<MembroGrupo>> GetMembersAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        await context.GroupMembers.AsNoTracking().Where(member => member.GrupoId == groupId)
            .OrderBy(member => member.Nome).ThenBy(member => member.Email).ToListAsync(cancellationToken);

    public Task<MembroGrupo?> GetMemberAsync(Guid groupId, string userId, CancellationToken cancellationToken = default) =>
        context.GroupMembers.FirstOrDefaultAsync(member => member.GrupoId == groupId && member.UsuarioId == userId, cancellationToken);

    public Task ReloadMemberAsync(MembroGrupo member, CancellationToken cancellationToken = default) =>
        context.Entry(member).ReloadAsync(cancellationToken);

    public Task<MembroGrupo?> GetMemberByIdAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken = default) =>
        context.GroupMembers.FirstOrDefaultAsync(member => member.GrupoId == groupId && member.Id == memberId, cancellationToken);

    public Task<ConviteGrupo?> GetPendingInvitationAsync(Guid groupId, string email, DateTime utcNow, CancellationToken cancellationToken = default) =>
        context.GroupInvitations.FirstOrDefaultAsync(invite => invite.GrupoId == groupId && invite.Email == email.ToLower() &&
            invite.AceitoEmUtc == null && invite.ExpiraEmUtc > utcNow, cancellationToken);

    public async Task AddInvitationAsync(ConviteGrupo invitation, CancellationToken cancellationToken = default) =>
        await context.GroupInvitations.AddAsync(invitation, cancellationToken);

    public Task<ConviteGrupo?> GetInvitationAsync(Guid groupId, Guid invitationId, CancellationToken cancellationToken = default) =>
        context.GroupInvitations.FirstOrDefaultAsync(invite => invite.GrupoId == groupId && invite.Id == invitationId, cancellationToken);

    public Task ReloadInvitationAsync(ConviteGrupo invitation, CancellationToken cancellationToken = default) =>
        context.Entry(invitation).ReloadAsync(cancellationToken);

    public Task<int> CountActiveAdminsAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        context.GroupMembers.CountAsync(member => member.GrupoId == groupId && member.Ativo && member.Papel == PapelGrupo.Admin, cancellationToken);

    public async Task<IReadOnlyList<(ConviteGrupo Invitation, GrupoCarteiras Group)>> GetPendingInvitationsForEmailAsync(
        string email, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var invitations = await context.GroupInvitations.AsNoTracking()
            .Where(invite => invite.Email == email.ToLower() && invite.AceitoEmUtc == null && invite.ExpiraEmUtc > utcNow)
            .Join(context.PortfolioGroups.AsNoTracking(), invite => invite.GrupoId, group => group.Id,
                (invite, group) => new { Invitation = invite, Group = group })
            .OrderBy(item => item.Group.Name)
            .ToListAsync(cancellationToken);
        return invitations.Select(item => (item.Invitation, item.Group)).ToList();
    }
}
