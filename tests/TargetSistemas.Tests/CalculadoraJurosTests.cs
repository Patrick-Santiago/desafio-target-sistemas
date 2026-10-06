using Microsoft.Extensions.Time.Testing;
using TargetSistemas.Juros;

namespace TargetSistemas.Tests;

public sealed class CalculadoraJurosTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 6);

    private static CalculadoraJuros CriarCalculadora() =>
        new(new FakeTimeProvider(new DateTimeOffset(Hoje, new TimeOnly(10, 0), TimeSpan.Zero)));

    [Fact]
    public void Calcular_vencimento_hoje_nao_gera_juros()
    {
        var resultado = CriarCalculadora().Calcular(1000m, Hoje);

        Assert.Equal(0, resultado.DiasAtraso);
        Assert.Equal(0m, resultado.ValorJuros);
        Assert.Equal(1000m, resultado.ValorAtualizado);
    }

    [Fact]
    public void Calcular_um_dia_de_atraso()
    {
        var resultado = CriarCalculadora().Calcular(1000m, Hoje.AddDays(-1));

        Assert.Equal(1, resultado.DiasAtraso);
        Assert.Equal(25m, resultado.ValorJuros);
        Assert.Equal(1025m, resultado.ValorAtualizado);
    }

    [Fact]
    public void Calcular_dez_dias_de_atraso_usa_juros_simples()
    {
        var resultado = CriarCalculadora().Calcular(1000m, Hoje.AddDays(-10));

        Assert.Equal(10, resultado.DiasAtraso);
        Assert.Equal(250m, resultado.ValorJuros);
        Assert.Equal(1250m, resultado.ValorAtualizado);
    }

    [Fact]
    public void Calcular_vencimento_futuro_nao_gera_juros()
    {
        var resultado = CriarCalculadora().Calcular(1000m, Hoje.AddDays(15));

        Assert.Equal(0, resultado.DiasAtraso);
        Assert.Equal(0m, resultado.ValorJuros);
        Assert.Equal(1000m, resultado.ValorAtualizado);
    }

    [Fact]
    public void Calcular_informa_a_data_de_referencia()
    {
        var resultado = CriarCalculadora().Calcular(1000m, Hoje);

        Assert.Equal(Hoje, resultado.DataReferencia);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Calcular_valor_nao_positivo_falha(int valor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CriarCalculadora().Calcular(valor, Hoje));
    }
}
