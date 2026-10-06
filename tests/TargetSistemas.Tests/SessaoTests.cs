using TargetSistemas.Terminal;

namespace TargetSistemas.Tests;

public sealed class SessaoTests
{
    private readonly StringWriter _saida = new();

    private Sessao CriarSessao(params string[] linhas) =>
        new(new StringReader(string.Concat(linhas.Select(l => l + '\n'))), _saida);

    [Fact]
    public void Perguntar_repete_ate_receber_valor_valido_mostrando_cada_erro()
    {
        var sessao = CriarSessao("abc", "x", "s");

        var resposta = sessao.Perguntar("Confirma? ", Conversores.SimNao);

        Assert.True(resposta);
        var erros = _saida.ToString().Split(Environment.NewLine)
            .Count(l => l.Contains("Resposta inválida", StringComparison.Ordinal));
        Assert.Equal(2, erros);
    }

    [Fact]
    public void Perguntar_sem_mais_entrada_lanca_EntradaEncerradaException()
    {
        var sessao = CriarSessao("abc");

        Assert.Throws<EntradaEncerradaException>(() => sessao.Perguntar("Confirma? ", Conversores.SimNao));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TentarPerguntar_resposta_em_branco_cancela(string linha)
    {
        var sessao = CriarSessao(linha);

        var respondeu = sessao.TentarPerguntar("Confirma? ", Conversores.SimNao, out _);

        Assert.False(respondeu);
    }

    [Fact]
    public void TentarPerguntar_repete_ate_valor_valido()
    {
        var sessao = CriarSessao("talvez", "n");

        var respondeu = sessao.TentarPerguntar("Confirma? ", Conversores.SimNao, out var resposta);

        Assert.True(respondeu);
        Assert.False(resposta);
    }

    [Fact]
    public void TentarPerguntar_sem_mais_entrada_lanca_EntradaEncerradaException()
    {
        var sessao = CriarSessao();

        Assert.Throws<EntradaEncerradaException>(() => sessao.TentarPerguntar("Confirma? ", Conversores.SimNao, out _));
    }

    [Fact]
    public void LerLinha_devolve_null_no_fim_da_entrada()
    {
        var sessao = CriarSessao();

        Assert.Null(sessao.LerLinha("Opção: "));
    }

    [Fact]
    public void Confirmar_acrescenta_as_opcoes_ao_rotulo()
    {
        var sessao = CriarSessao("S");

        Assert.True(sessao.Confirmar("Registrar"));
        Assert.StartsWith("Registrar (S/N): ", _saida.ToString(), StringComparison.Ordinal);
    }
}
