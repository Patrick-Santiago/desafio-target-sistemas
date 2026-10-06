using System.Globalization;
using TargetSistemas.Comissoes;

namespace TargetSistemas.Tests;

public sealed class CalculadoraComissaoTests
{
    [Theory]
    [InlineData("99.99", "0")]
    [InlineData("100.00", "1.00")]
    [InlineData("499.99", "4.9999")]
    [InlineData("500.00", "25.00")]
    public void CalcularComissao_respeita_os_limites_das_faixas(string valorVenda, string comissaoEsperada)
    {
        var comissao = CalculadoraComissao.CalcularComissao(decimal.Parse(valorVenda, CultureInfo.InvariantCulture));

        Assert.Equal(decimal.Parse(comissaoEsperada, CultureInfo.InvariantCulture), comissao);
    }

    [Theory]
    [InlineData("João Silva", 10, "10754.70", "495.68")]
    [InlineData("Maria Souza", 9, "9874.30", "465.95")]
    [InlineData("Carlos Oliveira", 8, "7928.35", "379.37")]
    [InlineData("Ana Lima", 9, "8763.95", "404.98")]
    public void CalcularPorVendedor_com_vendas_do_arquivo(
        string vendedor, int quantidadeVendas, string totalVendido, string comissao)
    {
        var resultado = CalcularVendasDoArquivo().Single(c => c.Vendedor == vendedor);

        Assert.Equal(quantidadeVendas, resultado.QuantidadeVendas);
        Assert.Equal(decimal.Parse(totalVendido, CultureInfo.InvariantCulture), resultado.TotalVendido);
        Assert.Equal(decimal.Parse(comissao, CultureInfo.InvariantCulture), Arredondar(resultado.Comissao));
    }

    [Fact]
    public void CalcularPorVendedor_total_geral_de_comissoes()
    {
        var total = CalcularVendasDoArquivo().Sum(c => c.Comissao);

        Assert.Equal(1745.98m, Arredondar(total));
    }

    private static IReadOnlyList<ComissaoVendedor> CalcularVendasDoArquivo() =>
        CalculadoraComissao.CalcularPorVendedor(
            LeitorVendas.Ler(Path.Combine(AppContext.BaseDirectory, "vendas.json")));

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
