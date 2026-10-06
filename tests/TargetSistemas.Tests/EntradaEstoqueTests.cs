using TargetSistemas.Estoque;

namespace TargetSistemas.Tests;

public sealed class EntradaEstoqueTests
{
    private static readonly int[] Codigos = [101, 102, 103];

    [Theory]
    [InlineData("101", 101)]
    [InlineData(" 102 ", 102)]
    public void ConverterCodigo_aceita_codigo_existente(string texto, int esperado)
    {
        var conversao = EntradaEstoque.ConverterCodigo(texto, Codigos);

        Assert.True(conversao.Sucesso);
        Assert.Equal(esperado, conversao.Valor);
    }

    [Theory]
    [InlineData("abc", "Código inválido: digite um dos códigos da lista (101, 102, 103), como 101.")]
    [InlineData("-5", "Código inválido: digite um dos códigos da lista (101, 102, 103), como 101.")]
    [InlineData("10.1", "Código inválido: digite um dos códigos da lista (101, 102, 103), como 101.")]
    [InlineData("99999999999", "Código inválido: digite um dos códigos da lista (101, 102, 103), como 101.")]
    [InlineData("999", "Produto 999 não encontrado: digite um dos códigos da lista (101, 102, 103).")]
    public void ConverterCodigo_rejeita_com_a_lista_de_codigos(string texto, string mensagem)
    {
        Assert.Equal(mensagem, EntradaEstoque.ConverterCodigo(texto, Codigos).Erro);
    }

    [Theory]
    [InlineData("E", TipoMovimentacao.Entrada)]
    [InlineData("e", TipoMovimentacao.Entrada)]
    [InlineData("entrada", TipoMovimentacao.Entrada)]
    [InlineData(" ENTRADA ", TipoMovimentacao.Entrada)]
    [InlineData("S", TipoMovimentacao.Saida)]
    [InlineData("s", TipoMovimentacao.Saida)]
    [InlineData("saida", TipoMovimentacao.Saida)]
    [InlineData("saída", TipoMovimentacao.Saida)]
    [InlineData("SAÍDA", TipoMovimentacao.Saida)]
    public void ConverterTipo_aceita_letra_ou_palavra_sem_diferenciar_caixa_e_acento(string texto, TipoMovimentacao esperado)
    {
        var conversao = EntradaEstoque.ConverterTipo(texto);

        Assert.True(conversao.Sucesso);
        Assert.Equal(esperado, conversao.Valor);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("1")]
    [InlineData("entradas")]
    public void ConverterTipo_rejeita_outros_textos(string texto)
    {
        Assert.Equal("Tipo inválido: digite E para entrada ou S para saída.", EntradaEstoque.ConverterTipo(texto).Erro);
    }

    [Theory]
    [InlineData("25", TipoMovimentacao.Entrada, 75, 25)]
    [InlineData("1.000", TipoMovimentacao.Entrada, 75, 1000)]
    [InlineData("75", TipoMovimentacao.Saida, 75, 75)]
    public void ConverterQuantidade_aceita_quantidade_valida(string texto, TipoMovimentacao tipo, int saldo, int esperado)
    {
        var conversao = EntradaEstoque.ConverterQuantidade(texto, tipo, saldo);

        Assert.True(conversao.Sucesso);
        Assert.Equal(esperado, conversao.Valor);
    }

    [Theory]
    [InlineData("0", TipoMovimentacao.Entrada, 75, "Quantidade inválida: use um número inteiro maior que zero, como 25.")]
    [InlineData("-3", TipoMovimentacao.Saida, 75, "Quantidade inválida: use um número inteiro maior que zero, como 25.")]
    [InlineData("10,5", TipoMovimentacao.Entrada, 75, "Quantidade inválida: use um número inteiro, sem casas decimais, como 25.")]
    [InlineData("2147483647", TipoMovimentacao.Entrada, 75, "Quantidade inválida: use no máximo 1.000.000, como 25.")]
    [InlineData("76", TipoMovimentacao.Saida, 75, "Saldo insuficiente: há 75 un. disponíveis; digite uma quantidade até 75.")]
    [InlineData("1000000", TipoMovimentacao.Entrada, 2147483000, "Quantidade inválida: o estoque não pode passar de 2.147.483.647 un.")]
    public void ConverterQuantidade_rejeita_com_mensagem_e_exemplo(string texto, TipoMovimentacao tipo, int saldo, string mensagem)
    {
        Assert.Equal(mensagem, EntradaEstoque.ConverterQuantidade(texto, tipo, saldo).Erro);
    }

    [Fact]
    public void ConverterDescricao_remove_espacos_das_pontas()
    {
        Assert.Equal("Compra de fornecedor", EntradaEstoque.ConverterDescricao("  Compra de fornecedor  ").Valor);
    }

    [Fact]
    public void ConverterDescricao_aceita_ate_o_limite()
    {
        Assert.True(EntradaEstoque.ConverterDescricao(new string('a', Deposito.TamanhoMaximoDescricao)).Sucesso);
    }

    [Fact]
    public void ConverterDescricao_rejeita_texto_acima_do_limite()
    {
        Assert.Equal(
            "Descrição muito longa (150 caracteres): use até 60.",
            EntradaEstoque.ConverterDescricao(new string('a', 150)).Erro);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConverterDescricao_rejeita_texto_vazio(string texto)
    {
        Assert.False(EntradaEstoque.ConverterDescricao(texto).Sucesso);
    }
}
