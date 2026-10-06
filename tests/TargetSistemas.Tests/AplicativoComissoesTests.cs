using TargetSistemas.Comissoes;
using TargetSistemas.Terminal;

namespace TargetSistemas.Tests;

public sealed class AplicativoComissoesTests
{
    private static readonly string CaminhoVendas = Path.Combine(AppContext.BaseDirectory, "vendas.json");

    private static (int Codigo, string Saida) Executar(params string[] args)
    {
        var saida = new StringWriter();
        var codigo = new AplicativoComissoes(new Sessao(new StringReader(""), saida), CaminhoVendas).Executar(args);
        return (codigo, saida.ToString());
    }

    [Fact]
    public void Executar_sem_argumentos_mostra_o_total_geral_de_comissoes()
    {
        var (codigo, saida) = Executar();

        Assert.Equal(Execucao.CodigoSucesso, codigo);
        Assert.Contains(Formatacao.Moeda(1745.98m), saida, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    public void Executar_com_ajuda_mostra_o_uso(string argumento)
    {
        var (codigo, saida) = Executar(argumento);

        Assert.Equal(Execucao.CodigoSucesso, codigo);
        Assert.StartsWith("Uso:", saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Executar_com_opcao_desconhecida_falha_e_mostra_o_uso()
    {
        var (codigo, saida) = Executar("--foo");

        Assert.Equal(Execucao.CodigoFalha, codigo);
        Assert.StartsWith("Argumento desconhecido: --foo.", saida, StringComparison.Ordinal);
        Assert.Contains("Uso:", saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Executar_com_dois_arquivos_falha()
    {
        var (codigo, saida) = Executar("a.json", "b.json");

        Assert.Equal(Execucao.CodigoFalha, codigo);
        Assert.StartsWith("Informe no máximo um arquivo de vendas.", saida, StringComparison.Ordinal);
    }

    [Fact]
    public void Executar_com_arquivo_inexistente_falha_sem_stack_trace()
    {
        var (codigo, saida) = Executar("nao-existe.json");

        Assert.Equal(Execucao.CodigoFalha, codigo);
        Assert.Contains("Arquivo de vendas não encontrado: nao-existe.json.", saida, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", saida, StringComparison.Ordinal);
    }
}
