namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed record InstituicaoFinanceiraDto(Guid Id, string Nome, string Categoria, Guid? GrupoId, bool Personalizada);

public sealed class CriarInstituicaoPersonalizadaDto
{
    public string Nome { get; set; } = string.Empty;
}
