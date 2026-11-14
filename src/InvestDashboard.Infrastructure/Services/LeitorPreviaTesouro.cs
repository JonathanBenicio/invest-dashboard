using System.Text;
using InvestDashboard.Application.DTOs.Importacao;
using InvestDashboard.Application.Interfaces;

namespace InvestDashboard.Infrastructure.Services;

public sealed class LeitorPreviaTesouro : ILeitorPreviaTesouro
{
    public PreviaTesouroDto Ler(Guid carteiraId, string formato, Stream arquivo)
    {
        if (formato == "html")
        {
            using var leitor = new StreamReader(arquivo, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var extrato = new TesouroDiretoHtmlReader().Ler(leitor.ReadToEnd());
            return new PreviaTesouroDto(carteiraId, formato, "portal_tesouro_html", extrato.Titulo,
                extrato.Vencimento.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), null,
                extrato.Posicoes.Select(p => new PosicaoPreviaTesouroDto(p.NumeroLinha, p.CamposBrutos)).ToArray(), extrato.Movimentacoes.Select(m => new MovimentoPreviaTesouroDto(
                    m.Campos.Select(c => new CampoPreviaTesouroDto(c.Rotulo, c.Valor)).ToArray())).ToArray(), [],
                ["O histórico pode estar incompleto; revise todos os dados antes de usá-los.",
                 "A visibilidade dos elementos por CSS não foi verificada.",
                 "Esta leitura não registra nem concilia movimentações."], true, false, false);
        }

        if (formato == "xlsx")
        {
            var extrato = new LeitorPosicoesTesouroXlsx().Ler(arquivo);
            return new PreviaTesouroDto(carteiraId, formato, extrato.Layout, extrato.CabecalhoOriginal,
                null, extrato.VencimentoOriginal, extrato.Linhas.Select(p => new PosicaoPreviaTesouroDto(p.NumeroLinha, p.CamposBrutos)).ToArray(),
                [], extrato.LinhasNaoInterpretadas,
                ["A leitura não garante que todas as posições do arquivo foram identificadas.",
                 "O arquivo não comprova o histórico completo de movimentações.",
                 "Esta leitura não registra nem concilia posições."], true, false, false);
        }

        throw new InvalidDataException("Formato de arquivo não suportado.");
    }
}
