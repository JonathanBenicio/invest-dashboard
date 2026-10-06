using System.ComponentModel.DataAnnotations;

namespace InvestDashboard.Application.DTOs.Auth;

public sealed class SolicitacaoLoginDto
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
