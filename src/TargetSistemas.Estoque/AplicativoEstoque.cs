using TargetSistemas.Terminal;

namespace TargetSistemas.Estoque;

public sealed class AplicativoEstoque(Sessao sessao, string caminhoEstoque, TimeProvider relogio)
{
    private const int LarguraMaximaNome = 30;
    private const int LarguraMaximaHistorico = 24;
    private const int LarguraBarraNivel = 20;
    private const int DuracaoSaldoMs = 500;

    private const string Uso = """
        Uso: dotnet run --project src/TargetSistemas.Estoque -- [opções]

        Lança entradas e saídas dos produtos do estoque.json e mostra o saldo final de cada produto movimentado.
        O estoque fica em memória durante a execução; o arquivo original não é alterado.
        Durante um lançamento, deixe a resposta em branco e tecle Enter para cancelar.

        Opções:
          --sem-animacao    Mostra o resultado direto, sem animações.
                            O mesmo vale para a variável de ambiente TARGET_SEM_ANIMACAO=1.
          --help            Mostra esta ajuda.

        Exemplos:
          dotnet run --project src/TargetSistemas.Estoque
          dotnet run --project src/TargetSistemas.Estoque -- --sem-animacao
        """;

    private static readonly string[] Menu =
    [
        "------------------------------",
        " MENU",
        "------------------------------",
        " 1  Listar produtos",
        " 2  Lançar movimentação",
        " 3  Listar histórico",
        " 0  Sair",
        "------------------------------",
    ];

    public int Executar(IReadOnlyList<string> args)
    {
        if (Execucao.PediuAjuda(args))
        {
            sessao.Escrever(Uso);
            return Execucao.CodigoSucesso;
        }
        if (args.Count > 0)
        {
            sessao.Erro($"Argumento desconhecido: {args[0]}.");
            sessao.Escrever();
            sessao.Escrever(Uso);
            return Execucao.CodigoFalha;
        }

        sessao.Cabecalho("Movimentação de Estoque",
        [
            "Lança entradas e saídas e mostra o saldo final de cada produto movimentado.",
            "O estoque fica em memória; o arquivo estoque.json não é alterado.",
        ]);

        Deposito deposito;
        try
        {
            deposito = new Deposito(LeitorEstoque.Ler(caminhoEstoque), relogio);
        }
        catch (Exception e) when (e is FileNotFoundException or InvalidDataException)
        {
            sessao.Erro(e.Message);
            return Execucao.CodigoFalha;
        }

        while (true)
        {
            sessao.Escrever();
            foreach (var linha in Menu)
            {
                sessao.Escrever(linha);
            }

            var opcao = sessao.LerLinha("Opção: ")?.Trim();
            switch (opcao)
            {
                case null:
                case "0":
                    return Execucao.CodigoSucesso;
                case "":
                    sessao.Erro("Nenhuma opção digitada: digite 1, 2, 3 ou 0.");
                    break;
                case "1":
                    ListarProdutos(deposito);
                    break;
                case "2":
                    LancarMovimentacao(deposito);
                    break;
                case "3":
                    ListarHistorico(deposito);
                    break;
                default:
                    sessao.Erro($"Opção \"{opcao}\" inválida: digite 1, 2, 3 ou 0.");
                    break;
            }
        }
    }

    private void ListarProdutos(Deposito deposito)
    {
        sessao.Secao("Produtos");
        var linhas = Tabela.Formatar(
            [new Coluna("Código"), new Coluna("Descrição", LarguraMaxima: LarguraMaximaNome), new Coluna("Estoque", AlinharDireita: true)],
            deposito.Produtos.Select(p => (IReadOnlyList<string>)
                [p.CodigoProduto.ToString(Formatacao.PtBr), p.DescricaoProduto, Formatacao.Inteiro(p.Estoque)]));
        foreach (var linha in linhas)
        {
            sessao.Escrever(linha);
        }
    }

    private void LancarMovimentacao(Deposito deposito)
    {
        sessao.Secao("Nova movimentação");
        sessao.Dica("Para cancelar, deixe a resposta em branco e tecle Enter.");

        var codigos = deposito.Produtos.Select(p => p.CodigoProduto).ToList();
        if (!sessao.TentarPerguntar("Código do produto: ", t => EntradaEstoque.ConverterCodigo(t, codigos), out var codigo))
        {
            Cancelar();
            return;
        }
        var produto = deposito.BuscarProduto(codigo)!;
        sessao.Dica($"{produto.DescricaoProduto}: {Formatacao.Inteiro(produto.Estoque)} un. em estoque.");

        if (!sessao.TentarPerguntar("Tipo (E = entrada, S = saída): ", EntradaEstoque.ConverterTipo, out var tipo))
        {
            Cancelar();
            return;
        }
        if (tipo == TipoMovimentacao.Saida && produto.Estoque == 0)
        {
            sessao.Erro("Produto sem estoque disponível para saída. Lance uma entrada antes.");
            return;
        }

        if (!sessao.TentarPerguntar("Quantidade: ", t => EntradaEstoque.ConverterQuantidade(t, tipo, produto.Estoque), out var quantidade)
            || !sessao.TentarPerguntar("Descrição: ", EntradaEstoque.ConverterDescricao, out var descricao))
        {
            Cancelar();
            return;
        }

        var antes = produto.Estoque;
        var depois = tipo == TipoMovimentacao.Entrada ? antes + quantidade : antes - quantidade;
        sessao.Escrever();
        var resumo = $"{NomeTipo(tipo)} de {Formatacao.Inteiro(quantidade)} un. de {produto.DescricaoProduto} "
            + $"({Formatacao.Inteiro(antes)} -> {Formatacao.Inteiro(depois)}).";
        if (!sessao.Confirmar($"{resumo} Confirmar?"))
        {
            Cancelar();
            return;
        }

        var movimentacao = deposito.Lancar(codigo, tipo, quantidade, descricao);
        MostrarSaldo(produto.DescricaoProduto, antes, movimentacao.EstoqueFinal);
        sessao.Sucesso($"Movimentação {movimentacao.Id} registrada.");
    }

    private void MostrarSaldo(string produto, int antes, int depois)
    {
        var maximo = Math.Max(antes, depois);
        var largura = Math.Max(Formatacao.Inteiro(antes).Length, Formatacao.Inteiro(depois).Length);
        sessao.Animacao.AnimarLinha(t =>
        {
            var atual = t >= 1 ? depois : antes + (int)Math.Round((depois - antes) * Animacao.Suavizar(t));
            var preenchidos = (int)Math.Round(LarguraBarraNivel * (double)atual / maximo);
            return $"Saldo de {produto}: {Formatacao.Inteiro(antes)} -> {Formatacao.Inteiro(atual).PadLeft(largura)}  "
                + $"[{sessao.Animacao.Barra(preenchidos, LarguraBarraNivel)}]";
        }, DuracaoSaldoMs);
    }

    private void ListarHistorico(Deposito deposito)
    {
        sessao.Secao("Histórico de movimentações");
        if (deposito.Historico.Count == 0)
        {
            sessao.Dica("Nenhuma movimentação lançada ainda. Use a opção 2 para lançar.");
            return;
        }

        var linhas = Tabela.Formatar(
            [
                new Coluna("Id", AlinharDireita: true),
                new Coluna("Data/hora"),
                new Coluna("Produto", LarguraMaxima: LarguraMaximaHistorico),
                new Coluna("Tipo"),
                new Coluna("Qtde", AlinharDireita: true),
                new Coluna("Saldo final", AlinharDireita: true),
                new Coluna("Descrição", LarguraMaxima: LarguraMaximaHistorico),
            ],
            deposito.Historico.Select(m => (IReadOnlyList<string>)
            [
                m.Id.ToString(Formatacao.PtBr),
                Formatacao.DataHora(m.DataHora),
                $"{m.CodigoProduto} {deposito.BuscarProduto(m.CodigoProduto)!.DescricaoProduto}",
                NomeTipo(m.Tipo),
                (m.Tipo == TipoMovimentacao.Entrada ? "+" : "-") + Formatacao.Inteiro(m.Quantidade),
                Formatacao.Inteiro(m.EstoqueFinal),
                m.Descricao,
            ]));
        foreach (var linha in linhas)
        {
            sessao.Escrever(linha);
        }
    }

    private void Cancelar() => sessao.Dica("Lançamento cancelado.");

    private static string NomeTipo(TipoMovimentacao tipo) => tipo == TipoMovimentacao.Entrada ? "Entrada" : "Saída";
}
