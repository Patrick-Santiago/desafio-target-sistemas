using System.Text;

namespace TargetSistemas.Terminal;

public sealed class Animacao
{
    private const int IntervaloQuadroMs = 16;

    private readonly TextWriter _saida;
    private readonly Func<bool> _teclaPressionada;
    private readonly Action<int> _esperar;
    private readonly Action<bool> _definirCursorVisivel;
    private bool _pulada;

    public Animacao(
        TextWriter saida, bool ativa, Func<bool> teclaPressionada, Action<int> esperar, Action<bool> definirCursorVisivel)
    {
        _saida = saida;
        Ativa = ativa;
        _teclaPressionada = teclaPressionada;
        _esperar = esperar;
        _definirCursorVisivel = definirCursorVisivel;
        Bloco = saida.Encoding.CodePage == Encoding.UTF8.CodePage ? '█' : '#';
    }

    public bool Ativa { get; }

    public char Bloco { get; }

    public static Animacao Desligada(TextWriter saida) => new(saida, false, () => false, _ => { }, _ => { });

    public static Animacao ParaConsole(bool ativa) =>
        new(Console.Out, ativa, ConsoleSeguro.ConsumirTeclas, Thread.Sleep, ConsoleSeguro.DefinirCursorVisivel);

    public void NovaTela() => _pulada = false;

    public void Segurar(int milissegundos) => ComCursorOculto(() => Pausar(milissegundos));

    public void RevelarLinhas(IReadOnlyList<string> linhas, int duracaoTotalMs)
    {
        var intervalo = linhas.Count > 1 ? duracaoTotalMs / (linhas.Count - 1) : 0;
        ComCursorOculto(() =>
        {
            for (var i = 0; i < linhas.Count; i++)
            {
                if (i > 0)
                {
                    Pausar(intervalo);
                }
                _saida.WriteLine(linhas[i]);
            }
        });
    }

    public void AnimarLinha(Func<double, string> quadro, int duracaoMs)
    {
        if (!Ativa || _pulada)
        {
            _saida.WriteLine(quadro(1));
            return;
        }

        ComCursorOculto(() =>
        {
            var quadros = Math.Max(2, duracaoMs / IntervaloQuadroMs);
            var larguraMaxima = 0;
            for (var i = 1; i < quadros && !_pulada; i++)
            {
                var texto = quadro((double)i / quadros);
                larguraMaxima = Math.Max(larguraMaxima, texto.Length);
                _saida.Write('\r' + texto.PadRight(larguraMaxima));
                Pausar(IntervaloQuadroMs);
            }
            _saida.WriteLine('\r' + quadro(1).PadRight(larguraMaxima));
        });
    }

    public void ContarValor(string rotulo, decimal inicio, decimal fim, Func<decimal, string> formatar, int duracaoMs) =>
        AnimarLinha(t => rotulo + formatar(t >= 1 ? fim : inicio + ((fim - inicio) * (decimal)Suavizar(t))), duracaoMs);

    public void CrescerBarra(string prefixo, int tamanhoFinal, string sufixo, int duracaoMs) =>
        AnimarLinha(t => prefixo + new string(Bloco, (int)Math.Round(tamanhoFinal * Suavizar(t))) + sufixo, duracaoMs);

    public string Barra(int preenchidos, int largura) =>
        new string(Bloco, preenchidos) + new string('.', largura - preenchidos);

    public static double Suavizar(double t) => 1 - Math.Pow(1 - t, 3);

    private void Pausar(int milissegundos)
    {
        var restante = milissegundos;
        while (Ativa && !_pulada && restante > 0)
        {
            if (_teclaPressionada())
            {
                _pulada = true;
                return;
            }
            var passo = Math.Min(IntervaloQuadroMs, restante);
            _esperar(passo);
            restante -= passo;
        }
    }

    private void ComCursorOculto(Action acao)
    {
        if (!Ativa)
        {
            acao();
            return;
        }

        _definirCursorVisivel(false);
        try
        {
            acao();
        }
        finally
        {
            _definirCursorVisivel(true);
        }
    }
}
