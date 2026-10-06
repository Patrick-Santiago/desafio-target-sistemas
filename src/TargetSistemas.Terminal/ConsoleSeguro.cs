namespace TargetSistemas.Terminal;

internal static class ConsoleSeguro
{
    // Console.CursorVisible e Console.KeyAvailable lançam exceção em alguns terminais e quando não
    // há console real; nesses casos o programa segue sem esconder o cursor nem detectar teclas.
    public static void DefinirCursorVisivel(bool visivel)
    {
        try
        {
            Console.CursorVisible = visivel;
        }
        catch (Exception e) when (e is IOException or PlatformNotSupportedException or InvalidOperationException)
        {
        }
    }

    public static bool ConsumirTeclas()
    {
        try
        {
            if (!Console.KeyAvailable)
            {
                return false;
            }
            while (Console.KeyAvailable)
            {
                Console.ReadKey(intercept: true);
            }
            return true;
        }
        catch (Exception e) when (e is IOException or InvalidOperationException)
        {
            return false;
        }
    }
}
