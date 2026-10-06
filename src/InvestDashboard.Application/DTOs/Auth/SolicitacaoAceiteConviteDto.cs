namespace InvestDashboard.Application.DTOs.Auth;

public sealed record SolicitacaoAceiteConviteDto(Guid GrupoId, Guid ConviteId, string? TokenHash, string? TokenSupabase);
