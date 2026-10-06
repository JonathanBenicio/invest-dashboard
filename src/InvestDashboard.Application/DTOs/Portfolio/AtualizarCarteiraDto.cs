using System.ComponentModel.DataAnnotations;

namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed class AtualizarCarteiraDto
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(160, MinimumLength = 1)]
    public string? Titular { get; set; }

    public Guid? TitularId { get; set; }

    public string? TitularUsuarioId { get; set; }

    public bool DesvincularTitular { get; set; }

    [StringLength(80)]
    public string? Parentesco { get; set; }

    [StringLength(120)]
    public string? InstituicaoFinanceira { get; set; }

    public Guid? InstituicaoFinanceiraId { get; set; }

    [StringLength(20)]
    public string? TipoInstituicao { get; set; }

    public string? Visibilidade { get; set; }
}
