namespace TargetSistemas.Juros;

public sealed class CalculadoraJuros(TimeProvider relogio)
{
    public const decimal TaxaDiaria = 0.025m;

    public ResultadoJuros Calcular(decimal valor, DateOnly vencimento)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);

        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);
        var diasAtraso = Math.Max(0, hoje.DayNumber - vencimento.DayNumber);
        var juros = valor * TaxaDiaria * diasAtraso;
        return new ResultadoJuros(hoje, diasAtraso, juros, valor + juros);
    }
}
