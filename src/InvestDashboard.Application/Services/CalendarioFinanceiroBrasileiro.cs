namespace InvestDashboard.Application.Services;

/// <summary>
/// Calendar of business days for Brazilian financial-market rate freshness.
/// It follows the national holidays and additional non-business days defined by CMN Resolution 2,932.
/// </summary>
public static class CalendarioFinanceiroBrasileiro
{
    public static int ContarDiasUteis(DateOnly dataInicial, DateOnly dataFinal)
    {
        if (dataFinal <= dataInicial) return 0;

        var diasUteis = 0;
        for (var data = dataInicial.AddDays(1); data <= dataFinal; data = data.AddDays(1))
        {
            if (EhDiaUtilFinanceiro(data)) diasUteis++;
            if (data == dataFinal) break;
        }

        return diasUteis;
    }

    public static bool EhDiaUtilFinanceiro(DateOnly data)
    {
        if (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return false;
        if ((data.Month, data.Day) is (1, 1) or (4, 21) or (5, 1) or (9, 7) or (10, 12) or
            (11, 2) or (11, 15) or (11, 20) or (12, 25)) return false;

        var pascoa = DomingoDePascoa(data.Year);
        return data != pascoa.AddDays(-48) && // segunda-feira de Carnaval
               data != pascoa.AddDays(-47) && // terça-feira de Carnaval
               data != pascoa.AddDays(-2) &&  // Sexta-feira da Paixão
               data != pascoa.AddDays(60);   // Corpus Christi
    }

    private static DateOnly DomingoDePascoa(int ano)
    {
        var a = ano % 19;
        var b = ano / 100;
        var c = ano % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var mes = (h + l - 7 * m + 114) / 31;
        var dia = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(ano, mes, dia);
    }
}