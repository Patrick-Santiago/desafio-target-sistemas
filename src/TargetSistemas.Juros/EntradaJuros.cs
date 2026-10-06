using TargetSistemas.Terminal;

namespace TargetSistemas.Juros;

public static class EntradaJuros
{
    public const decimal ValorMaximo = 1_000_000_000m;
    public static readonly DateOnly DataMinima = new(1900, 1, 1);
    public static readonly DateOnly DataMaxima = new(2100, 12, 31);

    public static Conversao<decimal> ConverterValor(string texto) => Conversores.Moeda(texto, ValorMaximo);

    public static Conversao<DateOnly> ConverterData(string texto, DateOnly hoje) =>
        Conversores.Data(texto, hoje, DataMinima, DataMaxima);
}
