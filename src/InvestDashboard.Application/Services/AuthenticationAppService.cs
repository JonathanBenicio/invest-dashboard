using System.Security.Cryptography;
using System.Text;
using InvestDashboard.Application.DTOs.Auth;
using InvestDashboard.Application.Interfaces;
using InvestDashboard.Application.Exceptions;
using InvestDashboard.Domain.Aggregates.Authentication;
using System.Security.Authentication;

namespace InvestDashboard.Application.Services;

public sealed class AuthenticationAppService(
    IAuthProvider authProvider,
    IAccessTokenIssuer accessTokenIssuer,
    IRefreshTokenSessionRepository sessions,
    IAuthRoleProvider authRoleProvider,
    IGrupoCarteirasAppService groupService,
    TimeProvider timeProvider) : IAuthenticationAppService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

    public async Task<SessaoAutenticacaoDto> LoginAsync(SolicitacaoLoginDto request, CancellationToken cancellationToken = default)
    {
        var identity = await AuthenticateAsync(
            () => authProvider.LoginAsync(request.Email, request.Password, cancellationToken), cancellationToken);

        return await CreateSessionAsync(identity, cancellationToken);
    }

    public async Task<SessaoAutenticacaoDto> RegisterAsync(SolicitacaoCadastroDto request, CancellationToken cancellationToken = default)
    {
        var identity = await AuthenticateAsync(
            () => authProvider.RegisterAsync(request.Name.Trim(), request.Email.Trim(), request.Password, cancellationToken), cancellationToken);

        if (identity.RequiresEmailConfirmation || string.IsNullOrWhiteSpace(identity.AccessToken))
        {
            return new SessaoAutenticacaoDto
            {
                User = new UsuarioAutenticadoDto
                {
                    Id = identity.User.Id,
                    Email = identity.User.Email,
                    Name = identity.User.Name,
                    Role = "user"
                },
                RequiresEmailConfirmation = true
            };
        }

        return await CreateSessionAsync(identity, cancellationToken);
    }

    public async Task<SessaoAutenticacaoDto> AceitarConviteAsync(Guid groupId, Guid invitationId, string tokenHash, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenHash) && string.IsNullOrWhiteSpace(accessToken))
            throw new AuthenticationException("Convite inválido ou expirado.");
        var identity = await AuthenticateAsync(
            () => string.IsNullOrWhiteSpace(tokenHash)
                ? authProvider.ValidateAccessTokenAsync(accessToken!, cancellationToken)
                : authProvider.VerifyInvitationAsync(tokenHash, cancellationToken),
            cancellationToken);
        if (!Guid.TryParse(identity.User.Id, out _) || string.IsNullOrWhiteSpace(identity.User.Email))
            throw new AuthenticationException("A identidade do convite é inválida.");
        var accepted = await groupService.AceitarConviteAsync(groupId, invitationId, identity.User.Id,
            identity.User.Email, identity.User.Name, cancellationToken);
        if (accepted != true) throw new AuthenticationException("Convite inválido, expirado ou destinado a outro e-mail.");
        return await CreateSessionAsync(identity, cancellationToken);
    }

    public async Task<SessaoAutenticacaoDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (!TryReadSessionId(refreshToken, out var sessionId))
            throw new AuthenticationException("Sessão inválida ou expirada.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var session = await sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null || !session.IsActive(now))
            throw new AuthenticationException("Sessão inválida ou expirada.");

        var nextRefreshToken = CreateRefreshToken(session.Id);
        var rotated = await sessions.TryRotateAsync(
            session.Id,
            Hash(refreshToken),
            Hash(nextRefreshToken),
            now,
            cancellationToken);

        if (!rotated)
        {
            await sessions.RevokeAsync(session.Id, now, cancellationToken);
            throw new AuthenticationException("Sessão inválida ou expirada.");
        }

        return CreateResponse(session.Id, session.UserId, session.Email, session.Name, session.Role, session.ExpiresAtUtc, nextRefreshToken, now);
    }

    public async Task LogoutAsync(Guid? sessionId, string? refreshToken, CancellationToken cancellationToken = default)
    {
        var id = sessionId;
        if (id is null && TryReadSessionId(refreshToken, out var cookieSessionId))
            id = cookieSessionId;

        if (id.HasValue)
            await sessions.RevokeAsync(id.Value, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }

    private async Task<AuthResponseDto> AuthenticateAsync(
        Func<Task<AuthResponseDto>> authenticate,
        CancellationToken cancellationToken)
    {
        try
        {
            return await authenticate();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (AuthenticationException)
        {
            throw;
        }
        catch (RegistrationConflictException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new IdentityProviderUnavailableException();
        }
    }

    private async Task<SessaoAutenticacaoDto> CreateSessionAsync(AuthResponseDto identity, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(identity.User.Id, out var userId))
            throw new AuthenticationException("O provedor de identidade retornou um usuário inválido.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.Add(SessionLifetime);
        var sessionId = Guid.NewGuid();
        var refreshToken = CreateRefreshToken(sessionId);
        var role = authRoleProvider.GetRole(userId);

        var session = new RefreshTokenSession(
            sessionId,
            userId,
            identity.User.Email,
            identity.User.Name,
            role,
            Hash(refreshToken),
            now,
            expiresAt);

        await sessions.AddAsync(session, cancellationToken);
        await sessions.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(identity.AccessToken))
        {
            try
            {
                await authProvider.RevokeAsync(identity.AccessToken, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // The upstream token is discarded immediately after identity verification.
            }
        }

        return CreateResponse(sessionId, userId, identity.User.Email, identity.User.Name, role, expiresAt, refreshToken, now);
    }

    private SessaoAutenticacaoDto CreateResponse(
        Guid sessionId,
        Guid userId,
        string email,
        string name,
        string role,
        DateTime sessionExpiresAt,
        string refreshToken,
        DateTime now)
    {
        var accessExpiresAt = now.Add(AccessTokenLifetime);
        var token = accessTokenIssuer.Issue(userId, sessionId, email, name, role, accessExpiresAt);

        return new SessaoAutenticacaoDto
        {
            AccessToken = token,
            ExpiresIn = (int)AccessTokenLifetime.TotalSeconds,
            User = new UsuarioAutenticadoDto { Id = userId.ToString(), Email = email, Name = name, Role = role },
            RequiresEmailConfirmation = false,
            RefreshToken = refreshToken,
            SessionExpiresAtUtc = sessionExpiresAt
        };
    }

    private static string CreateRefreshToken(Guid sessionId) =>
        $"{sessionId:N}.{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool TryReadSessionId(string? token, out Guid sessionId)
    {
        sessionId = Guid.Empty;
        return token is { Length: > 33 } && token[32] == '.' && Guid.TryParseExact(token[..32], "N", out sessionId);
    }
}
