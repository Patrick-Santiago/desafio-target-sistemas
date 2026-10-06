using Microsoft.Extensions.Time.Testing;
using TargetSistemas.Estoque;

namespace TargetSistemas.Tests;

public sealed class DepositoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 6, 14, 30, 0, TimeSpan.Zero);

    private static Deposito CriarDeposito() => new(
        [
            new Produto(101, "Caneta Azul", 150),
            new Produto(102, "Caderno Universitário", 75),
        ],
        new FakeTimeProvider(Agora));

    [Fact]
    public void Lancar_entrada_soma_ao_estoque()
    {
        var deposito = CriarDeposito();

        var movimentacao = deposito.Lancar(101, TipoMovimentacao.Entrada, 50, "Compra de fornecedor");

        Assert.Equal(200, movimentacao.EstoqueFinal);
        Assert.Equal(200, deposito.BuscarProduto(101)!.Estoque);
    }

    [Fact]
    public void Lancar_saida_subtrai_do_estoque()
    {
        var deposito = CriarDeposito();

        var movimentacao = deposito.Lancar(102, TipoMovimentacao.Saida, 25, "Venda balcão");

        Assert.Equal(50, movimentacao.EstoqueFinal);
        Assert.Equal(50, deposito.BuscarProduto(102)!.Estoque);
    }

    [Fact]
    public void Lancar_saida_de_todo_o_estoque_zera_o_produto()
    {
        var deposito = CriarDeposito();

        var movimentacao = deposito.Lancar(102, TipoMovimentacao.Saida, 75, "Inventário");

        Assert.Equal(0, movimentacao.EstoqueFinal);
    }

    [Fact]
    public void Lancar_registra_movimentacao_no_historico_com_ids_sequenciais()
    {
        var deposito = CriarDeposito();

        deposito.Lancar(101, TipoMovimentacao.Entrada, 10, "Compra");
        deposito.Lancar(102, TipoMovimentacao.Saida, 5, "Venda");
        deposito.Lancar(101, TipoMovimentacao.Saida, 3, "Avaria");

        Assert.Equal([1, 2, 3], deposito.Historico.Select(m => m.Id));
        var ultima = deposito.Historico[^1];
        Assert.Equal("Avaria", ultima.Descricao);
        Assert.Equal(TipoMovimentacao.Saida, ultima.Tipo);
        Assert.Equal(101, ultima.CodigoProduto);
        Assert.Equal(3, ultima.Quantidade);
        Assert.Equal(Agora, ultima.DataHora);
    }

    [Fact]
    public void Lancar_produto_inexistente_falha()
    {
        var deposito = CriarDeposito();

        Assert.Throws<ArgumentException>(() => deposito.Lancar(999, TipoMovimentacao.Entrada, 1, "Compra"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Lancar_quantidade_nao_positiva_falha(int quantidade)
    {
        var deposito = CriarDeposito();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => deposito.Lancar(101, TipoMovimentacao.Entrada, quantidade, "Compra"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Lancar_sem_descricao_falha(string descricao)
    {
        var deposito = CriarDeposito();

        Assert.Throws<ArgumentException>(() => deposito.Lancar(101, TipoMovimentacao.Entrada, 1, descricao));
    }

    [Fact]
    public void Lancar_saida_maior_que_o_estoque_falha_sem_alterar_nada()
    {
        var deposito = CriarDeposito();

        Assert.Throws<InvalidOperationException>(() => deposito.Lancar(102, TipoMovimentacao.Saida, 76, "Venda"));
        Assert.Equal(75, deposito.BuscarProduto(102)!.Estoque);
        Assert.Empty(deposito.Historico);
    }

    [Fact]
    public void Lancar_quantidade_acima_do_limite_falha()
    {
        var deposito = CriarDeposito();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => deposito.Lancar(101, TipoMovimentacao.Entrada, Deposito.QuantidadeMaxima + 1, "Compra"));
    }

    [Fact]
    public void Lancar_descricao_acima_do_limite_falha()
    {
        var deposito = CriarDeposito();

        Assert.Throws<ArgumentException>(
            () => deposito.Lancar(101, TipoMovimentacao.Entrada, 1, new string('a', Deposito.TamanhoMaximoDescricao + 1)));
    }

    [Fact]
    public void Lancar_entrada_que_estouraria_o_estoque_falha_sem_alterar_nada()
    {
        var deposito = new Deposito([new Produto(101, "Caneta Azul", int.MaxValue - 10)], new FakeTimeProvider(Agora));

        Assert.Throws<OverflowException>(() => deposito.Lancar(101, TipoMovimentacao.Entrada, 100, "Compra"));
        Assert.Equal(int.MaxValue - 10, deposito.BuscarProduto(101)!.Estoque);
        Assert.Empty(deposito.Historico);
    }

    [Fact]
    public void LeitorEstoque_carrega_os_produtos_do_arquivo()
    {
        var produtos = LeitorEstoque.Ler(Path.Combine(AppContext.BaseDirectory, "estoque.json"));

        Assert.Equal(5, produtos.Count);
        Assert.Contains(new Produto(104, "Lápis Preto HB", 320), produtos);
    }
}
