using System;
using System.Collections.Generic;

namespace InvestDashboard.Application.DTOs.Common
{
    public class MetadadosPaginacao
    {
        public int Pagina { get; set; }
        public int ItensPorPagina { get; set; }
        public int TotalItens { get; set; }
        public int TotalPaginas { get; set; }
        public bool TemProximaPagina { get; set; }
        public bool TemPaginaAnterior { get; set; }
    }

    public class RespostaPaginada<T>
    {
        public List<T> Dados { get; set; } = new();
        public bool Sucesso { get; set; }
        public string? Mensagem { get; set; }
        public MetadadosPaginacao Paginacao { get; set; } = new();

        public RespostaPaginada() { }

        public RespostaPaginada(List<T> dados, int pagina, int itensPorPagina, int totalItens, bool sucesso = true, string? mensagem = null)
        {
            Dados = dados;
            Sucesso = sucesso;
            Mensagem = mensagem;

            var totalPaginas = (int)Math.Ceiling(totalItens / (double)itensPorPagina);
            Paginacao = new MetadadosPaginacao
            {
                Pagina = pagina,
                ItensPorPagina = itensPorPagina,
                TotalItens = totalItens,
                TotalPaginas = totalPaginas,
                TemProximaPagina = pagina < totalPaginas,
                TemPaginaAnterior = pagina > 1
            };
        }
    }
}
