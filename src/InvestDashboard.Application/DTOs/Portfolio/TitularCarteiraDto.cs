using System.ComponentModel.DataAnnotations;

namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed record TitularCarteiraDto(Guid Id, Guid GrupoId, string Nome, string? Parentesco, string? UsuarioId);

public sealed class SalvarTitularCarteiraDto
{
    [Required, StringLength(160, MinimumLength = 1)]
    public string Nome { get; set; } = string.Empty;
    [StringLength(80)]
    public string? Parentesco { get; set; }
    [StringLength(100)]
    public string? UsuarioId { get; set; }
}
