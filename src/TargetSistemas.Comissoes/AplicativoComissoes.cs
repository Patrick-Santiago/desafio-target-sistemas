using TargetSistemas.Terminal;

namespace TargetSistemas.Comissoes;

public sealed class AplicativoComissoes(Sessao sessao, string caminhoPadrao)
{
    private const int LarguraMaximaNome = 30;
    private const int LarguraGrafico = 30;
    private const int DuracaoLinhaTabelaMs = 50;
    private const int DuracaoBarraMs = 100;

    private const string Uso = """
        Uso: dotnet run --project src/TargetSistemas.Comissoes -- [arquivo.json] [opções]

        Calcula a comissão de cada vendedor a partir de um arquivo JSON de vendas.
        Sem arquivo, usa o vendas.json que acompanha o programa.

        Opções:
          --sem-animacao    Mostra o resultado direto, sem animações.
                            O mesmo vale para a variável de ambiente TARGET_SEM_ANIMACAO=1.
          --help            Mostra esta ajuda.

        Exemplos:
          dotnet run --project src/TargetSistemas.Comissoes
          dotnet run --project src/TargetSistemas.Comissoes -- --sem-animacao
          dotnet run --project src/TargetSistemas.Comissoes -- C:\dados\vendas.json
        """;

    public int Executar(IReadOnlyList<string> args)
    {
        if (Execucao.PediuAjuda(args))
        {
            sessao.Escrever(Uso);
            return Execucao.CodigoSucesso;
        }
        if (args.FirstOrDefault(a => a.StartsWith('-')) is { } desconhecido)
        {
            return FalhaDeUso($"Argumento desconhecido: {desconhecido}.");
        }
        if (args.Count > 1)
        {
            return FalhaDeUso("Informe no máximo um arquivo de vendas.");
        }

        var caminho = args.Count == 1 ? args[0] : caminhoPadrao;
        string[] faixas =
        [
            $"abaixo de {Formatacao.Moeda(100m)}",
            $"de {Formatacao.Moeda(100m)} a {Formatacao.Moeda(499.99m)}",
            $"a partir de {Formatacao.Moeda(500m)}",
        ];
        string[] percentuais = ["sem comissão", "1%", "5%"];
        var larguraFaixa = faixas.Max(f => f.Length);
        sessao.Cabecalho("Comissões de Vendas",
        [
            "Calcula a comissão de cada vendedor a partir das vendas de um arquivo JSON.",
            "Regra por venda:",
            .. faixas.Select((faixa, i) => $"  {faixa.PadRight(larguraFaixa)}  {percentuais[i]}"),
        ]);
        sessao.Dica($"Arquivo: {Path.GetFileName(caminho)}");

        IReadOnlyList<Venda> vendas;
        try
        {
            vendas = LeitorVendas.Ler(caminho);
        }
        catch (Exception e) when (e is FileNotFoundException or InvalidDataException)
        {
            sessao.Erro(e.Message);
            return Execucao.CodigoFalha;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            sessao.Erro($"Não foi possível ler o arquivo {caminho}: {e.Message}");
            return Execucao.CodigoFalha;
        }

        if (vendas.Count == 0)
        {
            sessao.Dica("Nenhuma venda encontrada no arquivo.");
            return Execucao.CodigoSucesso;
        }

        var comissoes = CalculadoraComissao.CalcularPorVendedor(vendas);
        MostrarTabela(comissoes);
        MostrarGrafico(comissoes);
        return Execucao.CodigoSucesso;
    }

    private void MostrarTabela(IReadOnlyList<ComissaoVendedor> comissoes)
    {
        var linhas = Tabela.Formatar(
            [
                new Coluna("Vendedor", LarguraMaxima: LarguraMaximaNome),
                new Coluna("Vendas", AlinharDireita: true),
                new Coluna("Total vendido", AlinharDireita: true),
                new Coluna("Comissão", AlinharDireita: true),
            ],
            comissoes.Select(c => (IReadOnlyList<string>)
                [c.Vendedor, Formatacao.Inteiro(c.QuantidadeVendas), Formatacao.Moeda(c.TotalVendido), Formatacao.Moeda(c.Comissao)]),
            [
                "Total geral",
                Formatacao.Inteiro(comissoes.Sum(c => c.QuantidadeVendas)),
                Formatacao.Moeda(comissoes.Sum(c => c.TotalVendido)),
                Formatacao.Moeda(comissoes.Sum(c => c.Comissao)),
            ]);
        sessao.Escrever();
        sessao.Animacao.RevelarLinhas(linhas, DuracaoLinhaTabelaMs * (linhas.Count - 1));
    }

    private void MostrarGrafico(IReadOnlyList<ComissaoVendedor> comissoes)
    {
        sessao.Secao("Comissão por vendedor");
        var maior = comissoes.Max(c => c.Comissao);
        var larguraNome = comissoes.Max(c => Math.Min(c.Vendedor.Length, LarguraMaximaNome));
        foreach (var c in comissoes)
        {
            var tamanho = maior == 0 ? 0 : (int)Math.Round(LarguraGrafico * c.Comissao / maior);
            sessao.Animacao.CrescerBarra(
                Tabela.Cortar(c.Vendedor, LarguraMaximaNome).PadRight(larguraNome) + "  ",
                tamanho,
                " " + Formatacao.Moeda(c.Comissao),
                DuracaoBarraMs);
        }
    }

    private int FalhaDeUso(string mensagem)
    {
        sessao.Erro(mensagem);
        sessao.Escrever();
        sessao.Escrever(Uso);
        return Execucao.CodigoFalha;
    }
}
