using System;

namespace InvestDashboard.Application.Interfaces
{
    public interface IUsuarioAtualService
    {
        Guid? UserId { get; }
        bool IsAuthenticated { get; }
        string? Email { get; }
        string? Name { get; }
    }
}
