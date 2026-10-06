using System.Globalization;
using TargetSistemas.Juros;

namespace TargetSistemas.Tests;

public sealed class EntradaJurosTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 6);

    [Theory]
    [InlineData("1500", "1500")]
    [InlineData("1500,5", "1500.5")]
    [InlineData("1.500,50", "1500.50")]
    [InlineData(" 1.234.567,89 ", "1234567.89")]
    [InlineData("1500.50", "1500.50")]
    [InlineData("R$ 1.500,00", "1500")]
    public void ConverterValor_aceita_formato_pt_BR_e_ponto_decimal(string texto, string esperado)
    {
        var conversao = EntradaJuros.ConverterValor(texto);

        Assert.True(conversao.Sucesso);
        Assert.Equal(decimal.Parse(esperado, CultureInfo.InvariantCulture), conversao.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-10")]
    [InlineData("1,500.50")]
    [InlineData("10,555")]
    [InlineData("1000000000,01")]
    [InlineData("79228162514264337593543950335")]
    public void ConverterValor_rejeita_entrada_invalida(string texto)
    {
        Assert.False(EntradaJuros.ConverterValor(texto).Sucesso);
    }

    [Fact]
    public void ConverterData_aceita_dd_MM_yyyy()
    {
        var conversao = EntradaJuros.ConverterData("05/10/2026", Hoje);

        Assert.True(conversao.Sucesso);
        Assert.Equal(new DateOnly(2026, 10, 5), conversao.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-10-05")]
    [InlineData("10/05/26")]
    [InlineData("31/02/2026")]
    [InlineData("01/01/1800")]
    [InlineData("31/12/9999")]
    public void ConverterData_rejeita_entrada_invalida(string texto)
    {
        Assert.False(EntradaJuros.ConverterData(texto, Hoje).Sucesso);
    }
}
