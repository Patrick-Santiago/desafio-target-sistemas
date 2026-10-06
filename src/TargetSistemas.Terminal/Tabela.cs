namespace TargetSistemas.Terminal;

public sealed record Coluna(string Titulo, bool AlinharDireita = false, int LarguraMaxima = int.MaxValue);

public static class Tabela
{
    private const string Separador = "  ";

    public static IReadOnlyList<string> Formatar(
        IReadOnlyList<Coluna> colunas, IEnumerable<IReadOnlyList<string>> linhas, IReadOnlyList<string>? total = null)
    {
        var corpo = linhas.Select(l => Cortar(colunas, l)).ToList();
        var rodape = total is null ? null : Cortar(colunas, total);
        var larguras = colunas
            .Select((coluna, i) => corpo.Append(rodape ?? []).Where(l => l.Count > i)
                .Select(l => l[i].Length)
                .Append(coluna.Titulo.Length)
                .Max())
            .ToList();
        var tracos = larguras.Select(l => new string('-', l)).ToList();

        var resultado = new List<string> { Montar(colunas, larguras, colunas.Select(c => c.Titulo).ToList()), Montar(colunas, larguras, tracos) };
        resultado.AddRange(corpo.Select(l => Montar(colunas, larguras, l)));
        if (rodape is not null)
        {
            resultado.Add(Montar(colunas, larguras, tracos));
            resultado.Add(Montar(colunas, larguras, rodape));
        }
        return resultado;
    }

    public static string Cortar(string texto, int larguraMaxima) =>
        texto.Length <= larguraMaxima ? texto : string.Concat(texto.AsSpan(0, larguraMaxima - 3), "...");

    private static List<string> Cortar(IReadOnlyList<Coluna> colunas, IReadOnlyList<string> linha) =>
        linha.Select((celula, i) => Cortar(celula, colunas[i].LarguraMaxima)).ToList();

    private static string Montar(IReadOnlyList<Coluna> colunas, IReadOnlyList<int> larguras, IReadOnlyList<string> celulas) =>
        string.Join(Separador, celulas.Select((celula, i) =>
            colunas[i].AlinharDireita ? celula.PadLeft(larguras[i]) : celula.PadRight(larguras[i]))).TrimEnd();
}
