using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace InvestDashboard.Infrastructure.Services;

/// <summary>Uma observação do arquivo público de preços, sem vínculo com posições de clientes.</summary>
public sealed record CotacaoTesouroTransparente(
    string TipoTitulo,
    DateOnly DataVencimento,
    DateOnly DataBase,
    decimal TaxaCompraManha,
    decimal TaxaVendaManha,
    decimal PuCompraManha,
    decimal PuVendaManha,
    decimal PuBaseManha);

/// <summary>
/// Lê o recurso CSV de taxas do Tesouro Transparente. A data de coleta deve ser registrada
/// pelo consumidor separadamente da data base contida em cada observação.
/// </summary>
public static partial class TesouroTransparenteCsvReader
{
    public const string Fonte = "Tesouro Transparente — Taxas dos Títulos Ofertados pelo Tesouro Direto";
    public const string UrlFonte = "https://www.tesourotransparente.gov.br/ckan/dataset/taxas-dos-titulos-ofertados-pelo-tesouro-direto";

    private static readonly string[] Cabecalho =
    [
        "Tipo Titulo", "Data Vencimento", "Data Base", "Taxa Compra Manha",
        "Taxa Venda Manha", "PU Compra Manha", "PU Venda Manha", "PU Base Manha"
    ];

    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");
    private const int MaximoCaracteresPorLinha = 32768;
    private const int MaximoRegistros = 500_000;

    /// <remarks>
    /// O leitor deve ser enumerado até o fim antes de confirmar uma carga: uma linha
    /// posterior inválida ou duplicada invalida todo o arquivo. O TextReader permanece aberto.
    /// O formato observado contém um registro por linha; campos multilinha são rejeitados.
    /// </remarks>
    public static async IAsyncEnumerable<CotacaoTesouroTransparente> LerAsync(
        TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var leitorLimitado = new LeitorLinhasLimitado(reader);
        var primeiraLinha = await leitorLimitado.LerAsync(cancellationToken);
        if (primeiraLinha is null)
            throw new InvalidDataException("Arquivo de cotações vazio.");

        var colunas = SepararLinha(primeiraLinha.TrimStart('\uFEFF'), 1);
        if (!colunas.SequenceEqual(Cabecalho, StringComparer.Ordinal))
            throw new InvalidDataException("Cabeçalho do arquivo de cotações não reconhecido.");

        // A verificação de duplicidade cobre toda a execução, inclusive registros distantes.
        var chaves = new HashSet<(string TipoTitulo, DateOnly DataVencimento, DateOnly DataBase)>();
        var numeroLinha = 1;
        var possuiDados = false;
        while (await leitorLimitado.LerAsync(cancellationToken) is { } linha)
        {
            numeroLinha++;
            if (numeroLinha > MaximoRegistros + 1)
                throw new InvalidDataException("O arquivo de cotações excede o limite de registros.");
            if (string.IsNullOrWhiteSpace(linha))
                throw new InvalidDataException($"Linha {numeroLinha}: registro vazio.");

            var campos = SepararLinha(linha, numeroLinha);
            if (campos.Length != Cabecalho.Length)
                throw new InvalidDataException($"Linha {numeroLinha}: quantidade de campos inválida.");

            var tipo = campos[0].Trim();
            if (tipo.Length == 0)
                throw new InvalidDataException($"Linha {numeroLinha}: tipo de título ausente.");

            var vencimento = LerData(campos[1], numeroLinha, Cabecalho[1]);
            var dataBase = LerData(campos[2], numeroLinha, Cabecalho[2]);
            var cotacao = new CotacaoTesouroTransparente(
                tipo, vencimento, dataBase,
                LerDecimal(campos[3], numeroLinha, Cabecalho[3]),
                LerDecimal(campos[4], numeroLinha, Cabecalho[4]),
                LerDecimal(campos[5], numeroLinha, Cabecalho[5]),
                LerDecimal(campos[6], numeroLinha, Cabecalho[6]),
                LerDecimal(campos[7], numeroLinha, Cabecalho[7]));

            if (!chaves.Add((tipo, vencimento, dataBase)))
                throw new InvalidDataException($"Linha {numeroLinha}: cotação duplicada para título, vencimento e data base.");
            possuiDados = true;
            yield return cotacao;
        }

        if (!possuiDados)
            throw new InvalidDataException("Arquivo de cotações não contém registros.");
    }

    private static DateOnly LerData(string valor, int linha, string coluna)
    {
        if (!DateOnly.TryParseExact(valor.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var resultado))
            throw new InvalidDataException($"Linha {linha}: data inválida ou ausente na coluna {coluna}.");
        return resultado;
    }

    private static decimal LerDecimal(string valor, int linha, string coluna)
    {
        valor = valor.Trim();
        if (!NumeroBrasileiro().IsMatch(valor) ||
            !decimal.TryParse(valor, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint |
                NumberStyles.AllowThousands, CulturaBrasileira, out var resultado))
            throw new InvalidDataException($"Linha {linha}: número inválido ou ausente na coluna {coluna}.");
        return resultado;
    }

    [GeneratedRegex(@"^[+-]?(?:\d+|\d{1,3}(?:\.\d{3})+)(?:,\d+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex NumeroBrasileiro();

    private static string[] SepararLinha(string linha, int numeroLinha)
    {
        if (linha.Length > MaximoCaracteresPorLinha)
            throw new InvalidDataException($"Linha {numeroLinha}: tamanho excedido.");

        var campos = new List<string>();
        var campo = new StringBuilder();
        var emAspas = false;
        var terminouAspas = false;
        for (var i = 0; i < linha.Length; i++)
        {
            var c = linha[i];
            if (emAspas)
            {
                if (c != '"') campo.Append(c);
                else if (i + 1 < linha.Length && linha[i + 1] == '"') { campo.Append('"'); i++; }
                else { emAspas = false; terminouAspas = true; }
            }
            else if (c == ';')
            {
                campos.Add(campo.ToString());
                campo.Clear();
                terminouAspas = false;
            }
            else if (c == '"' && campo.Length == 0 && !terminouAspas) emAspas = true;
            else if (c == '"' || terminouAspas)
                throw new InvalidDataException($"Linha {numeroLinha}: aspas inválidas no CSV.");
            else campo.Append(c);
        }

        if (emAspas) throw new InvalidDataException($"Linha {numeroLinha}: campo com aspas não encerradas ou multilinha.");
        campos.Add(campo.ToString());
        return campos.ToArray();
    }

    // Impõe o limite durante a leitura, antes de alocar uma linha arbitrariamente grande.
    private sealed class LeitorLinhasLimitado(TextReader reader)
    {
        private readonly char[] buffer = new char[4096];
        private int posicao;
        private int disponiveis;
        private bool ignorarQuebraDeLinha;

        public async ValueTask<string?> LerAsync(CancellationToken cancellationToken)
        {
            var linha = new StringBuilder();
            while (true)
            {
                if (posicao == disponiveis)
                {
                    disponiveis = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
                    posicao = 0;
                    if (disponiveis == 0)
                        return linha.Length == 0 ? null : linha.ToString();
                }

                var caractere = buffer[posicao++];
                if (ignorarQuebraDeLinha)
                {
                    ignorarQuebraDeLinha = false;
                    if (caractere == '\n') continue;
                }
                if (caractere is '\r' or '\n')
                {
                    ignorarQuebraDeLinha = caractere == '\r';
                    return linha.ToString();
                }
                if (linha.Length == MaximoCaracteresPorLinha)
                    throw new InvalidDataException("Uma linha do arquivo de cotações excede o tamanho permitido.");
                linha.Append(caractere);
            }
        }
    }
}
