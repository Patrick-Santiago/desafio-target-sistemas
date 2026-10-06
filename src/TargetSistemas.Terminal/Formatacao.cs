using System.Globalization;

namespace TargetSistemas.Terminal;

public static class Formatacao
{
    public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Moeda(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero).ToString("C", PtBr);

    public static string Inteiro(long valor) => valor.ToString("N0", PtBr);

    public static string Data(DateOnly data) => data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string DataHora(DateTimeOffset dataHora) => dataHora.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
}
