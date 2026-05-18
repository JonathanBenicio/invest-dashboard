namespace InvestDashboard.Domain.Services;

public class CalculoImpostoService : ICalculoImpostoService
{
    private const decimal SwingTradeRate = 0.15m;
    private const decimal FiiRate = 0.20m;
    private const decimal DayTradeRate = 0.20m;
    private const decimal MonthlyExemptionThreshold = 20000m;

    public ResultadoCalculoImposto CalcularImpostoSwingTrade(
        decimal valorVendaTotal,
        decimal valorCustoTotal,
        decimal prejuizosAnteriores,
        out decimal prejuizosAtualizados)
    {
        var lucroBruto = valorVendaTotal - valorCustoTotal;

        if (lucroBruto <= 0)
        {
            prejuizosAtualizados = prejuizosAnteriores + Math.Abs(lucroBruto);
            return new ResultadoCalculoImposto
            {
                LucroBruto = lucroBruto,
                Aliquota = SwingTradeRate,
                ValorImposto = 0,
                LucroLiquido = lucroBruto,
                Isento = false,
                PrejuizoRestante = prejuizosAtualizados
            };
        }

        var isento = valorVendaTotal <= MonthlyExemptionThreshold;
        string? motivoIsencao = null;

        if (isento)
        {
            motivoIsencao = $"Vendas no mês (R$ {valorVendaTotal:N2}) abaixo do limite de R$ {MonthlyExemptionThreshold:N2}";
            prejuizosAtualizados = prejuizosAnteriores;
            return new ResultadoCalculoImposto
            {
                LucroBruto = lucroBruto,
                Aliquota = SwingTradeRate,
                ValorImposto = 0,
                LucroLiquido = lucroBruto,
                Isento = true,
                MotivoIsencao = motivoIsencao,
                PrejuizoRestante = prejuizosAnteriores
            };
        }

        var lucroTributavel = lucroBruto;
        if (prejuizosAnteriores > 0)
        {
            var compensacaoPrejuizo = Math.Min(prejuizosAnteriores, lucroTributavel);
            lucroTributavel -= compensacaoPrejuizo;
            prejuizosAtualizados = prejuizosAnteriores - compensacaoPrejuizo;
        }
        else
        {
            prejuizosAtualizados = 0;
        }

        var valorImposto = lucroTributavel > 0 ? lucroTributavel * SwingTradeRate : 0;
        var lucroLiquido = lucroBruto - valorImposto;

        return new ResultadoCalculoImposto
        {
            LucroBruto = lucroBruto,
            Aliquota = SwingTradeRate,
            ValorImposto = valorImposto,
            LucroLiquido = lucroLiquido,
            Isento = false,
            PrejuizoRestante = prejuizosAtualizados
        };
    }

    public ResultadoCalculoImposto CalcularImpostoFii(
        decimal lucro,
        decimal prejuizosAnteriores,
        out decimal prejuizosAtualizados)
    {
        if (lucro <= 0)
        {
            prejuizosAtualizados = prejuizosAnteriores + Math.Abs(lucro);
            return new ResultadoCalculoImposto
            {
                LucroBruto = lucro,
                Aliquota = FiiRate,
                ValorImposto = 0,
                LucroLiquido = lucro,
                Isento = false,
                PrejuizoRestante = prejuizosAtualizados
            };
        }

        var lucroTributavel = lucro;
        if (prejuizosAnteriores > 0)
        {
            var compensacaoPrejuizo = Math.Min(prejuizosAnteriores, lucroTributavel);
            lucroTributavel -= compensacaoPrejuizo;
            prejuizosAtualizados = prejuizosAnteriores - compensacaoPrejuizo;
        }
        else
        {
            prejuizosAtualizados = 0;
        }

        var valorImposto = lucroTributavel > 0 ? lucroTributavel * FiiRate : 0;
        var lucroLiquido = lucro - valorImposto;

        return new ResultadoCalculoImposto
        {
            LucroBruto = lucro,
            Aliquota = FiiRate,
            ValorImposto = valorImposto,
            LucroLiquido = lucroLiquido,
            Isento = false,
            PrejuizoRestante = prejuizosAtualizados
        };
    }

    public ResultadoCalculoImposto CalcularImpostoDayTrade(
        decimal lucro,
        decimal prejuizosAnteriores,
        out decimal prejuizosAtualizados)
    {
        if (lucro <= 0)
        {
            prejuizosAtualizados = prejuizosAnteriores + Math.Abs(lucro);
            return new ResultadoCalculoImposto
            {
                LucroBruto = lucro,
                Aliquota = DayTradeRate,
                ValorImposto = 0,
                LucroLiquido = lucro,
                Isento = false,
                PrejuizoRestante = prejuizosAtualizados
            };
        }

        var lucroTributavel = lucro;
        if (prejuizosAnteriores > 0)
        {
            var compensacaoPrejuizo = Math.Min(prejuizosAnteriores, lucroTributavel);
            lucroTributavel -= compensacaoPrejuizo;
            prejuizosAtualizados = prejuizosAnteriores - compensacaoPrejuizo;
        }
        else
        {
            prejuizosAtualizados = 0;
        }

        var valorImposto = lucroTributavel > 0 ? lucroTributavel * DayTradeRate : 0;
        var lucroLiquido = lucro - valorImposto;

        return new ResultadoCalculoImposto
        {
            LucroBruto = lucro,
            Aliquota = DayTradeRate,
            ValorImposto = valorImposto,
            LucroLiquido = lucroLiquido,
            Isento = false,
            PrejuizoRestante = prejuizosAtualizados
        };
    }
}
