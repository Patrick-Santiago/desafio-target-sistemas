using System.Text;

namespace TargetSistemas.Terminal;

public static class Execucao
{
    public const int CodigoSucesso = 0;
    public const int CodigoFalha = 1;
    public const int CodigoInterrompido = 130;
    public const string OpcaoSemAnimacao = "--sem-animacao";

    public static int Executar(string[] args, Func<Sessao, IReadOnlyList<string>, int> programa)
    {
        Console.OutputEncoding = Encoding.UTF8;
        // No teclado o console entrega Unicode; já a entrada redirecionada chega em bytes e seria
        // decodificada no code page OEM (850), estragando os acentos.
        if (Console.IsInputRedirected)
        {
            Console.InputEncoding = Encoding.UTF8;
        }
        Console.CancelKeyPress += AoInterromper;

        var argumentos = args.Where(a => a != OpcaoSemAnimacao).ToList();
        var animacoes = argumentos.Count == args.Length && AnimacoesPermitidas();
        var cores = !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null;
        var sessao = new Sessao(Console.In, Console.Out, cores, Animacao.ParaConsole(animacoes));
        try
        {
            return programa(sessao, argumentos);
        }
        catch (EntradaEncerradaException e)
        {
            sessao.Erro(e.Message);
            return CodigoFalha;
        }
    }

    public static bool PediuAjuda(IReadOnlyList<string> args) =>
        args.Any(a => a is "--help" or "-h" or "/?");

    // Sem console de verdade dos dois lados (saída capturada, entrada vinda de pipe) ou em CI não há
    // quem veja a animação nem tecla para pulá-la.
    private static bool AnimacoesPermitidas() =>
        !Console.IsOutputRedirected
        && !Console.IsInputRedirected
        && Environment.GetEnvironmentVariable("TARGET_SEM_ANIMACAO") != "1"
        && Environment.GetEnvironmentVariable("CI") is null
        && Environment.GetEnvironmentVariable("TF_BUILD") is null;

    private static void AoInterromper(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        ConsoleSeguro.DefinirCursorVisivel(true);
        Console.ResetColor();
        Console.Out.WriteLine();
        Console.Out.WriteLine("Programa interrompido.");
        Environment.Exit(CodigoInterrompido);
    }
}
