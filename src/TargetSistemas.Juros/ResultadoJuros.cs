namespace TargetSistemas.Juros;

public sealed record ResultadoJuros(DateOnly DataReferencia, int DiasAtraso, decimal ValorJuros, decimal ValorAtualizado);
