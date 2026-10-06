using Microsoft.Extensions.Time.Testing;
using TargetSistemas.Juros;
using TargetSistemas.Terminal;

namespace TargetSistemas.Tests;

public sealed class AplicativoJurosTests
{
    private static (int Codigo, string Saida) Executar(string[] linhas, params string[] args)
    {
        var saida = new StringWriter();
        var sessao = new Sessao(new StringReader(string.Concat(linhas.Select(l => l + '\n'))), saida);
        var relogio = new FakeTimeProvider(new DateTimeOffset(2026, 10, 6, 10, 0, 0, TimeSpan.Zero));
        var codigo = new AplicativoJuros(sessao, relogio).Executar(args);
        return (codigo, saida.ToString());
    }

    [Fact]
    public void Sessao_interativa_repete_entradas_invalidas_e_mostra_o_valor_interpretado()
    {
        var (codigo, saida) = Executar(["abc", "1,500.50", "1500.50", "06/10/26", "31/02/2026", "26/09/2026"]);

        Assert.Equal(Execucao.CodigoSucesso, codigo);
        Assert.Contains("Valor inválido: digite um número", saida, StringComparison.Ordinal);
        Assert.Contains("use vírgula para os centavos", saida, StringComparison.Ordinal);
        Assert.Contains($"Valor interpretado: {Formatacao.Moeda(1500.50m)}", saida, StringComparison.Ordinal);
        Assert.Contains("use o ano com 4 dígitos", saida, StringComparison.Ordinal);
        Assert.Contains("Data inexistente: 31/02/2026", saida, StringComparison.Ordinal);
        Assert.Contains(Formatacao.Moeda(375.13m), saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Resultado_mostra_a_memoria_de_calculo()
    {
        var (_, saida) = Executar([], "200", "03/10/2026");

        Assert.Contains($"{Formatacao.Moeda(200m)} x 2,5% x 3 dias = {Formatacao.Moeda(15m)}", saida, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("06/10/2026", "Vence hoje: sem juros.")]
    [InlineData("07/10/2026", "Ainda não venceu: falta 1 dia.")]
    [InlineData("11/10/2026", "Ainda não venceu: faltam 5 dias.")]
    public void Resultado_sem_atraso_explica_por_que_nao_ha_juros(string vencimento, string mensagem)
    {
        var (_, saida) = Executar([], "200", vencimento);

        Assert.Contains(mensagem, saida, StringComparison.Ordinal);
        Assert.DoesNotContain("x 2,5%", saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Sessao_interativa_pergunta_se_deseja_calcular_outro_valor()
    {
        var (codigo, saida) = Executar(["200", "hoje", "talvez", "s", "300", "hoje", "n"]);

        Assert.Equal(Execucao.CodigoSucesso, codigo);
        Assert.Contains($"Valor interpretado: {Formatacao.Moeda(300m)}", saida, StringComparison.Ordinal);
        Assert.Contains("Resposta inválida", saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Modo_por_argumentos_calcula_e_encerra()
    {
        var (codigo, saida) = Executar([], "R$ 1.000,00", "26/09/2026");

        Assert.Equal(Execucao.CodigoSucesso, codigo);
        Assert.Contains($"Valor interpretado: {Formatacao.Moeda(1000m)}", saida, StringComparison.Ordinal);
        Assert.Contains(Formatacao.Moeda(1250m), saida, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("abc", "26/09/2026", "Valor inválido:")]
    [InlineData("1000", "26/09/26", "Data inválida:")]
    [InlineData("-5", "hoje", "o valor precisa ser maior que zero")]
    public void Modo_por_argumentos_invalidos_falha_e_mostra_o_uso(string valor, string data, string trecho)
    {
        var (codigo, saida) = Executar([], valor, data);

        Assert.Equal(Execucao.CodigoFalha, codigo);
        Assert.Contains(trecho, saida, StringComparison.Ordinal);
        Assert.Contains("Uso:", saida, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1000", "Foi informado 1 argumento: informe o valor e a data de vencimento, nesta ordem.")]
    [InlineData("1000|hoje|extra", "Foram informados 3 argumentos: informe o valor e a data de vencimento, nesta ordem.")]
    public void Modo_por_argumentos_com_quantidade_errada_falha(string argumentos, string mensagem)
    {
        var (codigo, saida) = Executar([], argumentos.Split('|'));

        Assert.Equal(Execucao.CodigoFalha, codigo);
        Assert.StartsWith(mensagem, saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Ajuda_mostra_o_uso()
    {
        var (codigo, saida) = Executar([], "--help");

        Assert.Equal(Execucao.CodigoSucesso, codigo);
        Assert.StartsWith("Uso:", saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Fim_da_entrada_antes_da_data_lanca_EntradaEncerradaException()
    {
        Assert.Throws<EntradaEncerradaException>(() => Executar(["200"]));
    }
}
