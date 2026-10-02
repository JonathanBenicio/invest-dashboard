using System;

namespace InvestDashboard.Application.DTOs.Common
{
public class RespostaApi<T>
{
    public T Dados { get; set; } = default!;
    public bool Sucesso { get; set; }
    public string? Mensagem { get; set; }

    public RespostaApi() { }

    public RespostaApi(T dados, bool sucesso = true, string? mensagem = null)
    {
        Dados = dados;
        Sucesso = sucesso;
        Mensagem = mensagem;
    }
}
}
