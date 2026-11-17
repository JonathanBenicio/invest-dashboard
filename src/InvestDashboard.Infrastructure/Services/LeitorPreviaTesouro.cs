using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
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
                extrato.Posicoes.Select(p => new PosicaoPreviaTesouroDto(p.NumeroLinha, p.CamposBrutos, InterpretarPosicao(p))).ToArray(), extrato.Movimentacoes.Select(m => new MovimentoPreviaTesouroDto(
                    m.Campos.Select(c => new CampoPreviaTesouroDto(c.Rotulo, c.Valor)).ToArray(), InterpretarMovimento(m))).ToArray(), [],
                ["A leitura pode não identificar todo o histórico do arquivo; revise os dados antes de usá-los.",
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

    // O leitor já verificou a ordem e os rótulos dos sete campos. A interpretação
    // é auxiliar: não atribui identidade ao evento nem determina a sua inclusão.
    private static DadosMovimentoTesouroDto InterpretarMovimento(TesouroDiretoHtmlMovimentacao movimento)
    {
        var campos = movimento.Campos;
        var falhas = new List<string>();
        DateOnly? data = DateOnly.TryParseExact(campos[0].Valor, "dd/MM/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var dataLida) ? dataLida : null;
        if (data is null) falhas.Add("data");
        string? natureza = campos[1].Rotulo.Trim().TrimEnd(':').Trim().Equals("Natureza", StringComparison.OrdinalIgnoreCase)
            ? ReconhecerNatureza(campos[1].Valor) : null;
        if (natureza is null) falhas.Add("natureza");
        var tipo = Reconhecer(campos[2].Valor, "Crédito", "Débito");
        if (tipo is null) falhas.Add("tipo");
        var naoAplicaveis = new List<string>();
        decimal? LerNumero(int indice, string nome, bool moeda = false, bool percentual = false, bool positivo = false)
        {
            // O portal mostra um traço nestes três campos em cartões de taxas.
            // Não converter a ausência em zero nem aplicá-la a outras naturezas.
            if (natureza == "Taxas" && campos[indice].Valor.Trim() is "-" or "--" or "—" or "–")
            {
                naoAplicaveis.Add(nome);
                return null;
            }
            return Numero(campos[indice].Valor, nome, falhas, moeda, percentual, positivo);
        }
        var quantidade = LerNumero(3, "quantidade", positivo: true);
        var preco = LerNumero(4, "preco", moeda: true, positivo: true);
        var taxa = LerNumero(5, "taxaAnual", percentual: true);
        var valor = Numero(campos[6].Valor, "valor", falhas, moeda: true);
        return new DadosMovimentoTesouroDto(data, natureza, tipo, quantidade, preco, taxa, valor, falhas, naoAplicaveis);
    }

    private static string? Reconhecer(string valor, params string[] opcoes) =>
        opcoes.FirstOrDefault(opcao => opcao.Equals(valor.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? ReconhecerNatureza(string valor)
    {
        // Uma marca de ajuda "(?)" é incluída no texto de alguns cartões do portal.
        // Aceitar apenas essa forma conhecida, preservando o texto original na prévia.
        var texto = valor.Trim();
        if (Regex.IsMatch(texto, @"^Taxas\s*\(\?\)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            return "Taxas";
        return Reconhecer(texto, "Compra", "Venda", "Taxas");
    }

    private static DadosPosicaoTesouroDto InterpretarPosicao(TesouroDiretoHtmlPosicao posicao)
    {
        var campos = posicao.CamposBrutos;
        var falhas = new List<string>();
        DateOnly? data = DateOnly.TryParseExact(campos["data_aplicacao"], "dd/MM/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var dataLida) ? dataLida : null;
        if (data is null) falhas.Add("dataAplicacao");
        var quantidade = Numero(campos["quantidade_titulos"], "quantidadeTitulos", falhas, positivo: true);
        var preco = Numero(campos["preco_aplicacao"], "precoAplicacao", falhas, moeda: true, positivo: true);
        var investido = Numero(campos["valor_investido"], "valorInvestido", falhas, moeda: true);
        var anualizada = Numero(campos["rentabilidade_bruta_anualizada"], "rentabilidadeBrutaAnualizada", falhas, percentual: true);
        var acumulada = Numero(campos["rentabilidade_bruta_acumulada"], "rentabilidadeBrutaAcumulada", falhas, percentual: true);
        var bruto = Numero(campos["valor_bruto"], "valorBruto", falhas, moeda: true);
        var diasTexto = campos["dias_corridos"].Trim();
        int? dias = int.TryParse(diasTexto, NumberStyles.None, CultureInfo.InvariantCulture, out var diasLidos) ? diasLidos : null;
        if (dias is null) falhas.Add("diasCorridos");
        var aliquota = Numero(campos["aliquota_ir"], "aliquotaIr", falhas, percentual: true);
        var ir = Numero(campos["imposto_previsto_ir"], "impostoPrevistoIr", falhas, moeda: true);
        var iof = Numero(campos["imposto_previsto_iof"], "impostoPrevistoIof", falhas, moeda: true);
        var b3 = Numero(campos["taxa_devida_b3"], "taxaDevidaB3", falhas, moeda: true);
        var instituicao = Numero(campos["taxa_devida_instituicao"], "taxaDevidaInstituicao", falhas, moeda: true);
        var liquido = Numero(campos["valor_liquido"], "valorLiquido", falhas, moeda: true);
        return new DadosPosicaoTesouroDto(data, quantidade, preco, investido, campos["rentabilidade_contratada"],
            anualizada, acumulada, bruto, dias, aliquota, ir, iof, b3, instituicao, liquido, falhas);
    }

    private static decimal? Numero(string bruto, string campo, List<string> falhas,
        bool moeda = false, bool percentual = false, bool positivo = false)
    {
        var texto = bruto.Trim();
        if (moeda && texto.StartsWith("R$", StringComparison.Ordinal)) texto = texto[2..].Trim();
        if (percentual && texto.EndsWith('%')) texto = texto[..^1].Trim();
        // Agrupamento brasileiro explícito; não aceitar "1.23" como 123.
        if (!Regex.IsMatch(texto, @"^-?(?:\d+|\d{1,3}(?:\.\d{3})+)(?:,\d+)?$", RegexOptions.CultureInvariant) ||
            !decimal.TryParse(texto, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
                CultureInfo.GetCultureInfo("pt-BR"), out var numero) ||
            (positivo ? numero <= 0 : !percentual && numero < 0))
        {
            falhas.Add(campo);
            return null;
        }
        return numero;
    }
}
