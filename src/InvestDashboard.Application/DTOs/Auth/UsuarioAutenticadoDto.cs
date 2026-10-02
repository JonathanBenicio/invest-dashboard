namespace InvestDashboard.Application.DTOs.Auth;

public sealed class UsuarioAutenticadoDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
}
