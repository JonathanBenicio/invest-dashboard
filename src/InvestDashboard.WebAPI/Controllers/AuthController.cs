using System.Security.Claims;
using System.Security.Authentication;
using InvestDashboard.Application.DTOs.Auth;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvestDashboard.WebAPI.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthenticationAppService authentication) : ControllerBase
{
    private const string RefreshTokenCookieName = "refresh_token";
    private static readonly TimeSpan RefreshCookieLifetime = TimeSpan.FromDays(7);

    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [HttpPost("login")]
    public async Task<ActionResult<RespostaApi<SessaoAutenticacaoDto>>> Login(
        [FromBody] SolicitacaoLoginDto request,
        CancellationToken cancellationToken)
    {
        var result = await authentication.LoginAsync(request, cancellationToken);
        SetRefreshTokenCookie(result);
        return Ok(new RespostaApi<SessaoAutenticacaoDto>(result));
    }

    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [HttpPost("register")]
    public async Task<ActionResult<RespostaApi<SessaoAutenticacaoDto>>> Register(
        [FromBody] SolicitacaoCadastroDto request,
        CancellationToken cancellationToken)
    {
        var result = await authentication.RegisterAsync(request, cancellationToken);
        if (result.RequiresEmailConfirmation)
            return Accepted(new RespostaApi<SessaoAutenticacaoDto>(result, true, "Confirme seu e-mail para entrar."));

        SetRefreshTokenCookie(result);
        return Ok(new RespostaApi<SessaoAutenticacaoDto>(result));
    }

    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [HttpPost("convites/aceitar")]
    public async Task<ActionResult<RespostaApi<SessaoAutenticacaoDto>>> AceitarConvite(
        [FromBody] SolicitacaoAceiteConviteDto request, CancellationToken cancellationToken)
    {
        var result = await authentication.AceitarConviteAsync(request.GrupoId, request.ConviteId,
            request.TokenHash ?? string.Empty, request.TokenSupabase, cancellationToken);
        SetRefreshTokenCookie(result);
        return Ok(new RespostaApi<SessaoAutenticacaoDto>(result));
    }

    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    [HttpPost("refresh")]
    public async Task<ActionResult<RespostaApi<SessaoAutenticacaoDto>>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new AuthenticationException("Sessão ausente ou expirada.");

        var result = await authentication.RefreshAsync(refreshToken, cancellationToken);
        SetRefreshTokenCookie(result);
        return Ok(new RespostaApi<SessaoAutenticacaoDto>(result));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<ActionResult<RespostaApi<bool>>> Logout(CancellationToken cancellationToken)
    {
        var sessionClaim = User.FindFirstValue("sid");
        Guid? sessionId = Guid.TryParseExact(sessionClaim, "N", out var parsedSessionId) ? parsedSessionId : null;
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        await authentication.LogoutAsync(sessionId, refreshToken, cancellationToken);
        DeleteRefreshTokenCookie();
        return Ok(new RespostaApi<bool>(true));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<RespostaApi<UsuarioAutenticadoDto>> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email))
            throw new AuthenticationException("Sessão inválida.");

        var user = new UsuarioAutenticadoDto
        {
            Id = userId,
            Email = email,
            Name = User.FindFirstValue("name") ?? string.Empty,
            Role = User.FindFirstValue("role") ?? "user"
        };

        return Ok(new RespostaApi<UsuarioAutenticadoDto>(user));
    }

    private void SetRefreshTokenCookie(SessaoAutenticacaoDto session)
    {
        Response.Cookies.Append(RefreshTokenCookieName, session.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = IsProductionRequest(),
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = session.SessionExpiresAtUtc,
            MaxAge = RefreshCookieLifetime,
            IsEssential = true
        });
    }

    private void DeleteRefreshTokenCookie() => Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
    {
        HttpOnly = true,
        Secure = IsProductionRequest(),
        SameSite = SameSiteMode.Lax,
        Path = "/api/v1/auth",
        IsEssential = true
    });

    private bool IsProductionRequest()
    {
        var environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        return !environment.IsDevelopment() && !environment.IsEnvironment("Testing");
    }
}
