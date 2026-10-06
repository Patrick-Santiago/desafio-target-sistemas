using Microsoft.Extensions.Time.Testing;
using TargetSistemas.Estoque;
using TargetSistemas.Terminal;

namespace TargetSistemas.Tests;

public sealed class AplicativoEstoqueTests
{
    private static readonly string CaminhoEstoque = Path.Combine(AppContext.BaseDirectory, "estoque.json");

    private static (int Codigo, string Saida) Executar(string[] linhas, params string[] args)
    {
        var saida = new StringWriter();
        var sessao = new Sessao(new StringReader(string.Concat(linhas.Select(l => l + '\n'))), saida);
        var relogio = new FakeTimeProvider(new DateTimeOffset(2026, 10, 6, 14, 30, 0, TimeSpan.Zero));
        var codigo = new AplicativoEstoque(sessao, CaminhoEstoque, relogio).Executar(args);
        return (codigo, saida.ToString());
    }

    [Fact]
    public void Sessao_com_entradas_invalidas_registra_a_movimentacao_e_mostra_o_saldo_final()
    {
        var (codigo, saida) = Executar(
            ["x", "2", "abc", "999", "104", "talvez", "saída", "10,5", "321", "100", new string('a', 61), "Venda balcão", "talvez", "s", "3", "0"]);

        Assert.Equal(Execucao.CodigoSucesso, codigo);
        Assert.Contains("Opção \"x\" inválida: digite 1, 2, 3 ou 0.", saida, StringComparison.Ordinal);
        Assert.Contains("Código inválido:", saida, StringComparison.Ordinal);
        Assert.Contains("Produto 999 não encontrado", saida, StringComparison.Ordinal);
        Assert.Contains("Tipo inválido:", saida, StringComparison.Ordinal);
        Assert.Contains("sem casas decimais", saida, StringComparison.Ordinal);
        Assert.Contains("Saldo insuficiente: há 320 un. disponíveis", saida, StringComparison.Ordinal);
        Assert.Contains("Descrição muito longa (61 caracteres)", saida, StringComparison.Ordinal);
        Assert.Contains("Saída de 100 un. de Lápis Preto HB (320 -> 220). Confirmar? (S/N): ", saida, StringComparison.Ordinal);
        Assert.Contains("Resposta inválida: digite S para sim ou N para não.", saida, StringComparison.Ordinal);
        Assert.Contains("Saldo de Lápis Preto HB: 320 -> 220  [##############......]", saida, StringComparison.Ordinal);
        Assert.Contains("Movimentação 1 registrada.", saida, StringComparison.Ordinal);
        Assert.Contains("104 Lápis Preto HB  Saída  -100          220  Venda balcão", saida, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("2|")]
    [InlineData("2|101|  ")]
    [InlineData("2|101|E|")]
    [InlineData("2|101|E|10|")]
    [InlineData("2|101|E|10|Compra|n")]
    public void Resposta_em_branco_em_qualquer_pergunta_cancela_o_lancamento(string respostas)
    {
        var (codigo, saida) = Executar([.. respostas.Split('|'), "3", "0"]);

        Assert.Equal(Execucao.CodigoSucesso, codigo);
        Assert.Contains("Lançamento cancelado.", saida, StringComparison.Ordinal);
        Assert.Contains("Nenhuma movimentação lançada ainda.", saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Saida_de_produto_sem_estoque_volta_ao_menu()
    {
        var (_, saida) = Executar(["2", "102", "S", "75", "Zerar", "s", "2", "102", "S", "0"]);

        Assert.Contains("Produto sem estoque disponível para saída.", saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Fim_da_entrada_no_menu_encerra_normalmente()
    {
        var (codigo, _) = Executar(["1"]);

        Assert.Equal(Execucao.CodigoSucesso, codigo);
    }

    [Fact]
    public void Fim_da_entrada_no_meio_do_lancamento_lanca_EntradaEncerradaException()
    {
        Assert.Throws<EntradaEncerradaException>(() => Executar(["2", "101"]));
    }

    [Fact]
    public void Menu_com_opcao_vazia_explica_as_opcoes()
    {
        var (_, saida) = Executar(["", "0"]);

        Assert.Contains("Nenhuma opção digitada: digite 1, 2, 3 ou 0.", saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Argumento_desconhecido_falha_e_mostra_o_uso()
    {
        var (codigo, saida) = Executar([], "--foo");

        Assert.Equal(Execucao.CodigoFalha, codigo);
        Assert.StartsWith("Argumento desconhecido: --foo.", saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Ajuda_mostra_o_uso()
    {
        var (codigo, saida) = Executar([], "--help");

        Assert.Equal(Execucao.CodigoSucesso, codigo);
        Assert.StartsWith("Uso:", saida, StringComparison.Ordinal);
    }
}
