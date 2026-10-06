using System.Text;

namespace TargetSistemas.Tests;

// Reproduz o que fica visível num terminal: '\r' volta ao início da linha e o texto seguinte
// sobrescreve o anterior. Espaços no fim da linha não aparecem na tela e são descartados.
internal static class TerminalVirtual
{
    public static string Renderizar(string saida)
    {
        var linhas = new List<StringBuilder> { new() };
        var coluna = 0;
        foreach (var c in saida.Replace("\r\n", "\n", StringComparison.Ordinal))
        {
            switch (c)
            {
                case '\r':
                    coluna = 0;
                    break;
                case '\n':
                    linhas.Add(new StringBuilder());
                    coluna = 0;
                    break;
                default:
                    var linha = linhas[^1];
                    if (coluna < linha.Length)
                    {
                        linha[coluna] = c;
                    }
                    else
                    {
                        linha.Append(c);
                    }
                    coluna++;
                    break;
            }
        }
        return string.Join('\n', linhas.Select(l => l.ToString().TrimEnd()));
    }
}
