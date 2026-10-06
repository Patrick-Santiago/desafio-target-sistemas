using System.Diagnostics.CodeAnalysis;

namespace TargetSistemas.Terminal;

public sealed class Sessao
{
    private const int DuracaoCabecalhoMs = 150;
    private const int DuracaoDestaqueMs = 250;

    private readonly TextReader _entrada;
    private readonly TextWriter _saida;
    private readonly bool _cores;

    public Sessao(TextReader entrada, TextWriter saida, bool cores = false, Animacao? animacao = null)
    {
        _entrada = entrada;
        _saida = saida;
        _cores = cores;
        Animacao = animacao ?? Animacao.Desligada(saida);
    }

    public Animacao Animacao { get; }

    public string? LerLinha(string rotulo)
    {
        _saida.Write(rotulo);
        var linha = _entrada.ReadLine();
        if (linha is null)
        {
            _saida.WriteLine();
        }
        Animacao.NovaTela();
        return linha;
    }

    public T Perguntar<T>(string rotulo, Func<string, Conversao<T>> converter)
    {
        while (true)
        {
            var conversao = converter(LerObrigatorio(rotulo));
            if (conversao.Sucesso)
            {
                return conversao.Valor;
            }
            Erro(conversao.Erro);
        }
    }

    public bool TentarPerguntar<T>(string rotulo, Func<string, Conversao<T>> converter, [MaybeNullWhen(false)] out T valor)
    {
        while (true)
        {
            var linha = LerObrigatorio(rotulo);
            if (string.IsNullOrWhiteSpace(linha))
            {
                valor = default;
                return false;
            }

            var conversao = converter(linha);
            if (conversao.Sucesso)
            {
                valor = conversao.Valor;
                return true;
            }
            Erro(conversao.Erro);
        }
    }

    public bool Confirmar(string pergunta) => Perguntar($"{pergunta} (S/N): ", Conversores.SimNao);

    public void Cabecalho(string titulo, IReadOnlyList<string> descricao)
    {
        var linhas = new List<string> { titulo };
        linhas.AddRange(descricao);
        linhas.Add(new string('=', linhas.Max(l => l.Length)));
        Animacao.RevelarLinhas(linhas, DuracaoCabecalhoMs);
    }

    public void Secao(string titulo)
    {
        _saida.WriteLine();
        _saida.WriteLine(titulo);
        _saida.WriteLine(new string('-', titulo.Length));
    }

    public void Escrever(string texto = "") => _saida.WriteLine(texto);

    public void Erro(string mensagem) => EscreverEmCor(mensagem, ConsoleColor.Red);

    public void Sucesso(string mensagem)
    {
        if (_cores && Animacao.Ativa)
        {
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.White;
            _saida.Write(mensagem);
            Console.ResetColor();
            Animacao.Segurar(DuracaoDestaqueMs);
            _saida.Write('\r');
        }
        EscreverEmCor(mensagem, ConsoleColor.Green);
    }

    public void Dica(string mensagem) => EscreverEmCor(mensagem, ConsoleColor.DarkGray);

    private void EscreverEmCor(string mensagem, ConsoleColor cor)
    {
        if (!_cores)
        {
            _saida.WriteLine(mensagem);
            return;
        }

        Console.ForegroundColor = cor;
        _saida.WriteLine(mensagem);
        Console.ResetColor();
    }

    private string LerObrigatorio(string rotulo) => LerLinha(rotulo) ?? throw new EntradaEncerradaException();
}
