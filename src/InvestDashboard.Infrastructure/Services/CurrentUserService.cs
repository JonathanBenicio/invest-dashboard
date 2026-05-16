using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using InvestDashboard.Application.Interfaces;

namespace InvestDashboard.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) 
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

                if (Guid.TryParse(userIdString, out var userId))
                {
                    return userId;
                }

                return null;
            }
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}
