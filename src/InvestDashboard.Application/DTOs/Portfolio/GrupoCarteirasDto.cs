namespace InvestDashboard.Application.DTOs.Portfolio;

public sealed record GrupoCarteirasDto(Guid Id, string Nome, string Papel);
public sealed record MembroGrupoDto(Guid Id, string UsuarioId, string Email, string Nome, string Papel, bool Ativo);
public sealed record CriarGrupoCarteirasDto(string Nome);
public sealed record AlterarPapelGrupoDto(string Papel);
public sealed record ConvidarMembroGrupoDto(string Email, string Papel);
public sealed record ResultadoConviteGrupoDto(bool EmailEnviado, string Mensagem);
public sealed record ConvitePendenteGrupoDto(Guid Id, Guid GrupoId, string GrupoNome, string Papel, DateTime ExpiraEmUtc);
