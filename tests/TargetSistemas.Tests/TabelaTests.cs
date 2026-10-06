using TargetSistemas.Terminal;

namespace TargetSistemas.Tests;

public sealed class TabelaTests
{
    [Fact]
    public void Formatar_ajusta_largura_ao_conteudo_e_alinha_numeros_a_direita()
    {
        var linhas = Tabela.Formatar(
            [new Coluna("Nome"), new Coluna("Valor", AlinharDireita: true)],
            [["Ana", "5"], ["Maria Aparecida", "1.250"]],
            ["Total", "1.255"]);

        Assert.Equal(
            [
                "Nome             Valor",
                "---------------  -----",
                "Ana                  5",
                "Maria Aparecida  1.250",
                "---------------  -----",
                "Total            1.255",
            ],
            linhas);
    }

    [Fact]
    public void Formatar_corta_texto_acima_da_largura_maxima()
    {
        var linhas = Tabela.Formatar(
            [new Coluna("Nome", LarguraMaxima: 10), new Coluna("Qtde", AlinharDireita: true)],
            [["Maria Aparecida dos Santos", "1"]]);

        Assert.Equal("Maria A...     1", linhas[2]);
    }

    [Fact]
    public void Formatar_nao_deixa_espacos_no_fim_da_linha()
    {
        var linhas = Tabela.Formatar([new Coluna("Descrição")], [["a"]]);

        Assert.All(linhas, l => Assert.Equal(l.TrimEnd(), l));
    }
}
