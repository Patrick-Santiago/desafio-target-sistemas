using TargetSistemas.Comissoes;
using TargetSistemas.Terminal;

namespace TargetSistemas.Tests;

public sealed class LeitorVendasTests : IDisposable
{
    private readonly string _pasta = Directory.CreateTempSubdirectory("TargetSistemas.Tests").FullName;

    public void Dispose() => Directory.Delete(_pasta, recursive: true);

    private string CriarArquivo(string conteudo)
    {
        var caminho = Path.Combine(_pasta, "vendas.json");
        File.WriteAllText(caminho, conteudo);
        return caminho;
    }

    [Fact]
    public void Ler_arquivo_inexistente_lanca_FileNotFoundException()
    {
        var caminho = Path.Combine(_pasta, "nao-existe.json");

        var erro = Assert.Throws<FileNotFoundException>(() => LeitorVendas.Ler(caminho));
        Assert.Equal($"Arquivo de vendas não encontrado: {caminho}.", erro.Message);
    }

    [Theory]
    [InlineData("", "está vazio")]
    [InlineData("   ", "está vazio")]
    [InlineData("{\"vendas\":[{\"vendedor\":\"A\",\"valor\":100", "JSON inválido em")]
    [InlineData("{}", "não contém a lista \"vendas\"")]
    [InlineData("null", "não contém a lista \"vendas\"")]
    [InlineData("{\"vendas\":[{\"vendedor\":\"Ana\",\"valor\":600},{\"vendedor\":\"Ana\"}]}", "Venda 2 (Ana): campo \"valor\" ausente.")]
    [InlineData("{\"vendas\":[{\"valor\":600}]}", "Venda 1: campo \"vendedor\" ausente ou vazio.")]
    [InlineData("{\"vendas\":[{\"vendedor\":\"  \",\"valor\":600}]}", "Venda 1: campo \"vendedor\" ausente ou vazio.")]
    [InlineData("{\"vendas\":[null]}", "Venda 1: campo \"vendedor\" ausente ou vazio.")]
    [InlineData("{\"vendas\":[{\"vendedor\":\"Ana\",\"valor\":\"600\"}]}", "Venda 1: o campo \"valor\" deve ser um número, como 1200.50.")]
    [InlineData("{\"vendas\":[{\"vendedor\":42,\"valor\":600}]}", "Venda 1: o campo \"vendedor\" deve ser um texto entre aspas, como \"Ana Lima\".")]
    public void Ler_conteudo_invalido_lanca_InvalidDataException_com_mensagem_clara(string conteudo, string trecho)
    {
        var caminho = CriarArquivo(conteudo);

        var erro = Assert.Throws<InvalidDataException>(() => LeitorVendas.Ler(caminho));
        Assert.Contains(trecho, erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Ler_valor_negativo_informa_a_venda_e_o_valor()
    {
        var caminho = CriarArquivo("{\"vendas\":[{\"vendedor\":\"Ana\",\"valor\":-1000}]}");

        var erro = Assert.Throws<InvalidDataException>(() => LeitorVendas.Ler(caminho));
        Assert.Equal($"Venda 1 (Ana): valor negativo ({Formatacao.Moeda(-1000m)}).", erro.Message);
    }

    [Fact]
    public void Ler_lista_vazia_devolve_nenhuma_venda()
    {
        Assert.Empty(LeitorVendas.Ler(CriarArquivo("{\"vendas\":[]}")));
    }

    [Fact]
    public void Ler_remove_espacos_do_nome_do_vendedor()
    {
        var vendas = LeitorVendas.Ler(CriarArquivo("{\"vendas\":[{\"vendedor\":\" Ana \",\"valor\":0}]}"));

        Assert.Equal(new Venda("Ana", 0m), Assert.Single(vendas));
    }
}
