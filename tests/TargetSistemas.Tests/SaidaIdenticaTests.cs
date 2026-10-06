using Microsoft.Extensions.Time.Testing;
using TargetSistemas.Comissoes;
using TargetSistemas.Estoque;
using TargetSistemas.Juros;
using TargetSistemas.Terminal;

namespace TargetSistemas.Tests;

public sealed class SaidaIdenticaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 6, 14, 30, 0, TimeSpan.Zero);

    private static (string Animada, string Estatica) ExecutarNosDoisModos(string[] linhas, Func<Sessao, int> programa)
    {
        string Executar(bool animada)
        {
            var saida = new StringWriter();
            var animacao = animada
                ? new Animacao(saida, ativa: true, teclaPressionada: () => false, esperar: _ => { }, definirCursorVisivel: _ => { })
                : Animacao.Desligada(saida);
            var entrada = new EntradaComEco(string.Concat(linhas.Select(l => l + '\n')), saida);
            programa(new Sessao(entrada, saida, animacao: animacao));
            return saida.ToString();
        }

        return (Executar(animada: true), Executar(animada: false));
    }

    // As animações só rodam com teclado de verdade, e nele o terminal ecoa o que foi digitado e o Enter;
    // sem esse eco o primeiro '\r' de uma animação sobrescreveria a linha do prompt na simulação.
    private sealed class EntradaComEco(string texto, TextWriter saida) : StringReader(texto)
    {
        public override string? ReadLine()
        {
            var linha = base.ReadLine();
            if (linha is not null)
            {
                saida.WriteLine(linha);
            }
            return linha;
        }
    }

    private static void AssertMesmaTelaFinal(string animada, string estatica)
    {
        Assert.Contains('\r', animada.Replace(Environment.NewLine, "", StringComparison.Ordinal));
        Assert.Equal(TerminalVirtual.Renderizar(estatica), TerminalVirtual.Renderizar(animada));
    }

    [Fact]
    public void Comissoes_animada_termina_igual_a_sem_animacao()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "vendas.json");

        var (animada, estatica) = ExecutarNosDoisModos([], s => new AplicativoComissoes(s, caminho).Executar([]));

        AssertMesmaTelaFinal(animada, estatica);
    }

    [Fact]
    public void Estoque_animado_termina_igual_a_sem_animacao()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "estoque.json");
        string[] linhas = ["2", "104", "S", "100", "Venda balcão", "s", "2", "102", "E", "1.000", "Compra", "s", "3", "0"];

        var (animada, estatica) = ExecutarNosDoisModos(
            linhas, s => new AplicativoEstoque(s, caminho, new FakeTimeProvider(Agora)).Executar([]));

        AssertMesmaTelaFinal(animada, estatica);
    }

    [Fact]
    public void Juros_animado_termina_igual_a_sem_animacao()
    {
        string[] linhas = ["1500.50", "26/09/2026", "s", "800", "hoje", "s", "200", "15/10/2026", "n"];

        var (animada, estatica) = ExecutarNosDoisModos(
            linhas, s => new AplicativoJuros(s, new FakeTimeProvider(Agora)).Executar([]));

        AssertMesmaTelaFinal(animada, estatica);
    }
}
