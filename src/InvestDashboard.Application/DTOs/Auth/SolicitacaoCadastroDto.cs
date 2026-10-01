using System.ComponentModel.DataAnnotations;

namespace InvestDashboard.Application.DTOs.Auth;

public sealed class SolicitacaoCadastroDto
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
