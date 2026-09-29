namespace InvestDashboard.Application.Interfaces;

public interface IAuthRoleProvider
{
    string GetRole(Guid userId);
}
