namespace InvestDashboard.Application.DTOs.Importacao;

/// <summary>Metadados de um lote, sem o conteúdo privado da prévia.</summary>
public sealed record LoteTesouroResumoDto(Guid Id, Guid CarteiraId, DateOnly DataReferencia, string Formato,
    string Estado, DateTime CriadoEmUtc, DateTime? ConfirmadoEmUtc);
