namespace InvestDashboard.Application.DTOs.Auth;

/// <summary>Upstream Supabase session. This type must never be returned by an API controller.</summary>
public sealed class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UsuarioAutenticadoDto User { get; set; } = new();
    public bool RequiresEmailConfirmation { get; set; }
}
