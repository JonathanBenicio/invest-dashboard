using System;

namespace InvestDashboard.Application.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        bool IsAuthenticated { get; }
    }
}
