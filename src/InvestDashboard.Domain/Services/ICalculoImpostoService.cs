namespace InvestDashboard.Domain.Services;

public enum TipoAtivoImposto
{
    Acao,
    FundoImobiliario,
    Criptoativo,
    RendaFixa
}

public class ResultadoCalculoImposto
{
    public decimal LucroBruto { get; set; }
    public decimal Aliquota { get; set; }
    public decimal ValorImposto { get; set; }
    public decimal LucroLiquido { get; set; }
    public bool Isento { get; set; }
    public string? MotivoIsencao { get; set; }
    public decimal PrejuizoRestante { get; set; }
}

public interface ICalculoImpostoService
{
    ResultadoCalculoImposto CalcularImpostoSwingTrade(
        decimal valorVendaTotal,
        decimal valorCustoTotal,
        decimal prejuizosAnteriores,
        out decimal prejuizosAtualizados);

    ResultadoCalculoImposto CalcularImpostoFii(
        decimal lucro,
        decimal prejuizosAnteriores,
        out decimal prejuizosAtualizados);

    ResultadoCalculoImposto CalcularImpostoDayTrade(
        decimal lucro,
        decimal prejuizosAnteriores,
        out decimal prejuizosAtualizados);
}
