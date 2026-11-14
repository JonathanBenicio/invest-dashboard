using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Globalization;

namespace InvestDashboard.Infrastructure.Services;

/// <summary>
/// Lê os cartões de movimentação de uma página salva do Tesouro Direto.
/// O resultado contém somente os dados presentes no arquivo e não afirma que
/// o histórico está completo ou que os cartões foram conciliados. A leitura não
/// interpreta regras CSS nem carrega folhas de estilo. O consumidor deve exigir
/// revisão pelo usuário e não apresentar os cartões como visíveis ou confirmados.
/// </summary>
public sealed class TesouroDiretoHtmlReader
{
    public const int MaximoCaracteres = 5_000_000;
    private static readonly string[] RotulosConhecidos =
        ["Data", "Tipo", "Quantidade", "Preço (R$)", "Taxa (% a.a.)", "Valor (R$)"];
    private static readonly string[] CamposPosicao =
    ["data_aplicacao", "quantidade_titulos", "preco_aplicacao", "valor_investido",
     "rentabilidade_contratada", "rentabilidade_bruta_anualizada", "rentabilidade_bruta_acumulada",
     "valor_bruto", "dias_corridos", "aliquota_ir", "imposto_previsto_ir", "imposto_previsto_iof",
     "taxa_devida_b3", "taxa_devida_instituicao", "valor_liquido"];

    public TesouroDiretoHtmlExtrato Ler(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        if (html.Length == 0 || html.Length > MaximoCaracteres)
            throw new InvalidDataException("O arquivo HTML está vazio ou excede o limite de tamanho.");

        // HtmlParser, sem contexto de navegação, carregador de recursos ou motor
        // de scripts, constrói somente um DOM a partir dos bytes já recebidos.
        using var document = new HtmlParser().ParseDocument(html);
        var cartoes = document.QuerySelectorAll("div.td-posicao-detalhada__info");
        if (cartoes.Length == 0)
            throw new InvalidDataException("Estrutura de movimentações não reconhecida no arquivo HTML.");

        var containers = document.QuerySelectorAll("div.td-custom-meus-investimentos-titulo");
        if (containers.Length != 1 || EstaOculto(containers[0]) ||
            containers[0].QuerySelectorAll("div.td-posicao-detalhada__info").Length != cartoes.Length)
            throw new InvalidDataException("Contexto de investimento ausente ou ambíguo no arquivo HTML.");

        var titulos = containers[0].QuerySelectorAll("h1.td-meus-investimentos-detalhe__titulo_card");
        var vencimentos = containers[0].QuerySelectorAll("tr.saldo-table-vencimento strong");
        if (titulos.Length != 1 || vencimentos.Length != 1 || TemConteudoOculto(titulos[0]) ||
            TemConteudoOculto(vencimentos[0]))
            throw new InvalidDataException("Título ou vencimento ausente ou ambíguo no arquivo HTML.");

        var titulo = titulos[0].TextContent.Trim();
        var rotuloVencimento = vencimentos[0].TextContent.Trim();
        const string prefixoVencimento = "VENCIMENTO:";
        if (string.IsNullOrWhiteSpace(titulo) ||
            !rotuloVencimento.StartsWith(prefixoVencimento, StringComparison.OrdinalIgnoreCase) ||
            !DateOnly.TryParseExact(rotuloVencimento[prefixoVencimento.Length..].Trim(), "dd/MM/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var vencimento))
            throw new InvalidDataException("Título ou vencimento inválido no arquivo HTML.");

        var movimentacoes = new List<TesouroDiretoHtmlMovimentacao>(cartoes.Length);
        foreach (var cartao in cartoes)
        {
            if (EstaOculto(cartao))
                throw new InvalidDataException("O arquivo contém movimentações ocultas que precisam de revisão.");

            var campos = cartao.Children
                .Where(elemento => elemento.LocalName == "p" &&
                    elemento.ClassList.Contains("td-posicao-detalhada__info__valor"))
                .Select(LerCampo)
                .ToArray();

            // As amostras possuem um campo adicional entre data e tipo, cujo
            // significado ainda não foi estabelecido. Preservá-lo sem inferência.
            if (campos.Length != 7 ||
                !RotuloIgual(campos[0].Rotulo, RotulosConhecidos[0]) ||
                !RotuloIgual(campos[2].Rotulo, RotulosConhecidos[1]) ||
                !RotuloIgual(campos[3].Rotulo, RotulosConhecidos[2]) ||
                !RotuloIgual(campos[4].Rotulo, RotulosConhecidos[3]) ||
                !RotuloIgual(campos[5].Rotulo, RotulosConhecidos[4]) ||
                !RotuloIgual(campos[6].Rotulo, RotulosConhecidos[5]))
                throw new InvalidDataException("Estrutura de cartão de movimentação não reconhecida.");

            movimentacoes.Add(new TesouroDiretoHtmlMovimentacao(campos));
        }

        return new TesouroDiretoHtmlExtrato(titulo, vencimento, movimentacoes, LerPosicoes(containers[0]));
    }

    private static IReadOnlyList<TesouroDiretoHtmlPosicao> LerPosicoes(IElement container)
    {
        var linhas = container.QuerySelectorAll("tr.saldo-table-data-values");
        if (linhas.Length == 0) return [];
        var cabecalhos = container.QuerySelectorAll("tr.saldo-table-data-names");
        if (cabecalhos.Length != 3 || cabecalhos.Any(TemConteudoOculto) ||
            !ValidarCabecalho(cabecalhos[0], ["", "DATA DA APLICAÇÃO", "QUANTIDADE DE TÍTULOS", "PREÇO DO TÍTULO NA APLICAÇÃO", "VALOR INVESTIDO", "RENTABILIDADE CONTRATADA"], [1, 1, 1, 1, 1, 1]) ||
            !ValidarCabecalho(cabecalhos[1], ["RENTABILIDADE CONTRATADA", "VALOR BRUTO", "TEMPO DA APLICAÇÃO", "ALÍQUOTA", "IMPOSTO PREVISTO", "TAXA DEVIDA", "VALOR LÍQUIDO"], [2, 1, 1, 1, 2, 2, 1]) ||
            !ValidarCabecalho(cabecalhos[2], ["ANUALIZADA", "ACUMULADA", "I.R.", "IOF", "B3", "INSTITUIÇÃO FINANCEIRA"], [1, 1, 1, 1, 1, 1]))
            throw new InvalidDataException("Cabeçalho de posições não reconhecido.");

        var resultado = new List<TesouroDiretoHtmlPosicao>(linhas.Length);
        for (var linha = 0; linha < linhas.Length; linha++)
        {
            var celulas = linhas[linha].Children.Where(elemento => elemento.LocalName == "td").ToArray();
            if (EstaOculto(linhas[linha]) || celulas.Length != CamposPosicao.Length + 1 ||
                !string.IsNullOrWhiteSpace(celulas[0].TextContent) ||
                celulas.Skip(1).Any(TemConteudoOculto))
                throw new InvalidDataException("Estrutura de posição HTML não reconhecida.");
            var campos = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var indice = 0; indice < CamposPosicao.Length; indice++)
            {
                var valor = celulas[indice + 1].TextContent.Trim();
                if (string.IsNullOrWhiteSpace(valor))
                    throw new InvalidDataException("Campo de posição HTML ausente.");
                campos.Add(CamposPosicao[indice], valor);
            }
            if (!DateOnly.TryParseExact(campos["data_aplicacao"], "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _))
                throw new InvalidDataException("Data de posição HTML inválida.");
            resultado.Add(new TesouroDiretoHtmlPosicao(linha + 1, campos));
        }
        return resultado;
    }

    private static bool ValidarCabecalho(IElement linha, string[] rotulos, int[] spans)
    {
        var celulas = linha.Children.Where(elemento => elemento.LocalName == "td").ToArray();
        if (celulas.Length != rotulos.Length) return false;
        for (var i = 0; i < celulas.Length; i++)
        {
            var span = celulas[i].GetAttribute("colspan");
            if (span is not null && (!int.TryParse(span, NumberStyles.None, CultureInfo.InvariantCulture, out var numero) || numero != spans[i])) return false;
            if (span is null && spans[i] != 1) return false;
            if (!celulas[i].TextContent.Contains(rotulos[i], StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    private static TesouroDiretoHtmlCampo LerCampo(IElement elemento)
    {
        var valores = elemento.Children.Where(filho =>
            filho.LocalName == "span" && filho.ClassList.Contains("td-posicao-detalhada__info__data")).ToArray();
        if (valores.Length != 1 || TemConteudoOculto(valores[0]))
            throw new InvalidDataException("Campo de movimentação ausente, ambíguo ou oculto.");

        var rotulo = string.Concat(elemento.ChildNodes
            .Where(no => no.NodeType == NodeType.Text)
            .Select(no => no.TextContent)).Trim();
        var valor = valores[0].TextContent.Trim();
        if (string.IsNullOrWhiteSpace(rotulo) || string.IsNullOrWhiteSpace(valor))
            throw new InvalidDataException("Campo de movimentação sem rótulo ou valor.");

        return new TesouroDiretoHtmlCampo(rotulo, valor);
    }

    private static bool RotuloIgual(string obtido, string esperado) =>
        string.Equals(obtido.Trim().TrimEnd(':').Trim(), esperado, StringComparison.OrdinalIgnoreCase);

    private static bool TemConteudoOculto(IElement elemento) =>
        EstaOculto(elemento) || elemento.QuerySelectorAll("*").Any(filho => EstaOculto(filho) ||
            filho.LocalName is "script" or "style" or "template" or "noscript");

    private static bool EstaOculto(IElement elemento)
    {
        for (IElement? atual = elemento; atual is not null; atual = atual.ParentElement)
        {
            if (atual.HasAttribute("hidden") ||
                string.Equals(atual.GetAttribute("aria-hidden"), "true", StringComparison.OrdinalIgnoreCase))
                return true;

            var estilo = atual.GetAttribute("style");
            if (estilo is null)
                continue;
            foreach (var regra in estilo.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var partes = regra.Split(':', 2, StringSplitOptions.TrimEntries);
                if (partes.Length != 2)
                    continue;
                var valor = partes[1].Replace("!important", "", StringComparison.OrdinalIgnoreCase).Trim();
                if ((partes[0].Equals("display", StringComparison.OrdinalIgnoreCase) && valor.Equals("none", StringComparison.OrdinalIgnoreCase)) ||
                    (partes[0].Equals("visibility", StringComparison.OrdinalIgnoreCase) && valor.Equals("hidden", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
        }
        return false;
    }
}

public sealed record TesouroDiretoHtmlCampo(string Rotulo, string Valor);
public sealed record TesouroDiretoHtmlMovimentacao(IReadOnlyList<TesouroDiretoHtmlCampo> Campos);
public sealed record TesouroDiretoHtmlPosicao(int NumeroLinha, IReadOnlyDictionary<string, string> CamposBrutos);
/// <summary>
/// Resultado preliminar para revisão, sem garantia de completude ou visibilidade
/// dos elementos segundo CSS. Não usar como confirmação de movimentações.
/// </summary>
public sealed record TesouroDiretoHtmlExtrato(
    string Titulo, DateOnly Vencimento, IReadOnlyList<TesouroDiretoHtmlMovimentacao> Movimentacoes,
    IReadOnlyList<TesouroDiretoHtmlPosicao> Posicoes)
{
    public bool VisibilidadeCssVerificada => false;
    public bool ExigeRevisao => true;
}
