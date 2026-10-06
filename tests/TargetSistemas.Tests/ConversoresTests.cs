using System.Globalization;
using TargetSistemas.Terminal;

namespace TargetSistemas.Tests;

public sealed class ConversoresTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 6);
    private static readonly DateOnly Minima = new(1900, 1, 1);
    private static readonly DateOnly Maxima = new(2100, 12, 31);

    private static Conversao<int> Quantidade(string texto) =>
        Conversores.Inteiro(texto, "Quantidade inválida", "25", 1, 1_000_000);

    [Theory]
    [InlineData("25", 25)]
    [InlineData("  25  ", 25)]
    [InlineData("1", 1)]
    [InlineData("1.000", 1000)]
    [InlineData("1.000.000", 1_000_000)]
    public void Inteiro_aceita_numeros_inteiros_com_ou_sem_milhar(string texto, int esperado)
    {
        var conversao = Quantidade(texto);

        Assert.True(conversao.Sucesso);
        Assert.Equal(esperado, conversao.Valor);
    }

    [Theory]
    [InlineData("", "Quantidade inválida: use um número inteiro, como 25.")]
    [InlineData("   ", "Quantidade inválida: use um número inteiro, como 25.")]
    [InlineData("abc", "Quantidade inválida: use um número inteiro, como 25.")]
    [InlineData("1 000", "Quantidade inválida: use um número inteiro, como 25.")]
    [InlineData("10,5", "Quantidade inválida: use um número inteiro, sem casas decimais, como 25.")]
    [InlineData("10.5", "Quantidade inválida: use um número inteiro, sem casas decimais, como 25.")]
    [InlineData("1.00", "Quantidade inválida: use um número inteiro, sem casas decimais, como 25.")]
    [InlineData("0", "Quantidade inválida: use um número inteiro maior que zero, como 25.")]
    [InlineData("-3", "Quantidade inválida: use um número inteiro maior que zero, como 25.")]
    [InlineData("-99999999999999999999", "Quantidade inválida: use um número inteiro maior que zero, como 25.")]
    [InlineData("1000001", "Quantidade inválida: use no máximo 1.000.000, como 25.")]
    [InlineData("2147483648", "Quantidade inválida: use no máximo 1.000.000, como 25.")]
    [InlineData("99999999999999999999", "Quantidade inválida: use no máximo 1.000.000, como 25.")]
    public void Inteiro_rejeita_com_mensagem_e_exemplo(string texto, string mensagem)
    {
        Assert.Equal(mensagem, Quantidade(texto).Erro);
    }

    [Theory]
    [InlineData("200", "200")]
    [InlineData("  200  ", "200")]
    [InlineData("200,5", "200.5")]
    [InlineData("1500,50", "1500.50")]
    [InlineData("1.500,00", "1500")]
    [InlineData("1.500", "1500")]
    [InlineData("1.234.567,89", "1234567.89")]
    [InlineData("R$ 1.500,00", "1500")]
    [InlineData("r$1500", "1500")]
    [InlineData("1500.50", "1500.50")]
    [InlineData("200.5", "200.5")]
    [InlineData("0,01", "0.01")]
    [InlineData("1.000.000.000", "1000000000")]
    public void Moeda_aceita_formatos_sem_ambiguidade(string texto, string esperado)
    {
        var conversao = Conversores.Moeda(texto, 1_000_000_000m);

        Assert.True(conversao.Sucesso);
        Assert.Equal(decimal.Parse(esperado, CultureInfo.InvariantCulture), conversao.Valor);
    }

    [Theory]
    [InlineData("", "Valor inválido: digite um número, como 1500 ou 1.500,50.")]
    [InlineData("   ", "Valor inválido: digite um número, como 1500 ou 1.500,50.")]
    [InlineData("abc", "Valor inválido: digite um número, como 1500 ou 1.500,50.")]
    [InlineData("R$", "Valor inválido: digite um número, como 1500 ou 1.500,50.")]
    [InlineData("1.50,00", "Valor inválido: digite um número, como 1500 ou 1.500,50.")]
    [InlineData("1.500.50", "Valor inválido: digite um número, como 1500 ou 1.500,50.")]
    [InlineData("1,500.50", "Valor inválido: use vírgula para os centavos, como 1.500,50.")]
    [InlineData("1500,555", "Valor inválido: use no máximo 2 casas decimais, como 1.500,50.")]
    [InlineData("0", "Valor inválido: o valor precisa ser maior que zero, como 200,00.")]
    [InlineData("0,00", "Valor inválido: o valor precisa ser maior que zero, como 200,00.")]
    [InlineData("-10", "Valor inválido: o valor precisa ser maior que zero, como 200,00.")]
    public void Moeda_rejeita_com_mensagem_e_exemplo(string texto, string mensagem)
    {
        Assert.Equal(mensagem, Conversores.Moeda(texto, 1_000_000_000m).Erro);
    }

    [Theory]
    [InlineData("1000000000,01")]
    [InlineData("99999999999999999999999999999999")]
    public void Moeda_acima_do_limite_informa_o_maximo(string texto)
    {
        Assert.Equal(
            $"Valor acima do limite: use até {Formatacao.Moeda(1_000_000_000m)}.",
            Conversores.Moeda(texto, 1_000_000_000m).Erro);
    }

    [Theory]
    [InlineData("06/10/2026", 2026, 10, 6)]
    [InlineData(" 26/09/2026 ", 2026, 9, 26)]
    [InlineData("6/10/2026", 2026, 10, 6)]
    [InlineData("06-10-2026", 2026, 10, 6)]
    [InlineData("06.10.2026", 2026, 10, 6)]
    [InlineData("06102026", 2026, 10, 6)]
    [InlineData("hoje", 2026, 10, 6)]
    [InlineData("HOJE", 2026, 10, 6)]
    [InlineData("29/02/2024", 2024, 2, 29)]
    [InlineData("01/01/1900", 1900, 1, 1)]
    [InlineData("31/12/2100", 2100, 12, 31)]
    public void Data_aceita_formatos_comuns(string texto, int ano, int mes, int dia)
    {
        var conversao = Conversores.Data(texto, Hoje, Minima, Maxima);

        Assert.True(conversao.Sucesso);
        Assert.Equal(new DateOnly(ano, mes, dia), conversao.Valor);
    }

    [Theory]
    [InlineData("", "Data inválida: use o formato dd/mm/aaaa, como 06/10/2026.")]
    [InlineData("abc", "Data inválida: use o formato dd/mm/aaaa, como 06/10/2026.")]
    [InlineData("2026-10-06", "Data inválida: use o formato dd/mm/aaaa, como 06/10/2026.")]
    [InlineData("06/10-2026", "Data inválida: use o formato dd/mm/aaaa, como 06/10/2026.")]
    [InlineData("0610202", "Data inválida: use o formato dd/mm/aaaa, como 06/10/2026.")]
    [InlineData("06/10/26", "Data inválida: use o ano com 4 dígitos, como 06/10/2026.")]
    [InlineData("06/10/202", "Data inválida: use o ano com 4 dígitos, como 06/10/2026.")]
    [InlineData("31/02/2026", "Data inexistente: 31/02/2026 não existe no calendário.")]
    [InlineData("29/02/2026", "Data inexistente: 29/02/2026 não existe no calendário.")]
    [InlineData("06/13/2026", "Data inexistente: 06/13/2026 não existe no calendário.")]
    [InlineData("00/10/2026", "Data inexistente: 00/10/2026 não existe no calendário.")]
    [InlineData("31/12/1899", "Data fora do intervalo: use uma data entre 01/01/1900 e 31/12/2100.")]
    [InlineData("01/01/2101", "Data fora do intervalo: use uma data entre 01/01/1900 e 31/12/2100.")]
    [InlineData("01/01/0001", "Data fora do intervalo: use uma data entre 01/01/1900 e 31/12/2100.")]
    public void Data_rejeita_com_mensagem_e_exemplo(string texto, string mensagem)
    {
        Assert.Equal(mensagem, Conversores.Data(texto, Hoje, Minima, Maxima).Erro);
    }

    [Theory]
    [InlineData("s", true)]
    [InlineData("S", true)]
    [InlineData(" Sim ", true)]
    [InlineData("n", false)]
    [InlineData("nao", false)]
    [InlineData("NÃO", false)]
    public void SimNao_aceita_variacoes(string texto, bool esperado)
    {
        var conversao = Conversores.SimNao(texto);

        Assert.True(conversao.Sucesso);
        Assert.Equal(esperado, conversao.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("talvez")]
    public void SimNao_rejeita_outras_respostas(string texto)
    {
        Assert.Equal("Resposta inválida: digite S para sim ou N para não.", Conversores.SimNao(texto).Erro);
    }

    [Theory]
    [InlineData("Saída", "SAIDA")]
    [InlineData("  não ", "NAO")]
    [InlineData("Lápis", "LAPIS")]
    public void Normalizar_remove_acentos_espacos_e_caixa(string texto, string esperado)
    {
        Assert.Equal(esperado, Conversores.Normalizar(texto));
    }
}
