using System.Text.Json;

namespace TargetSistemas.Estoque;

public static class LeitorEstoque
{
    private sealed record ArquivoEstoque(IReadOnlyList<Produto>? Estoque);

    public static IReadOnlyList<Produto> Ler(string caminho)
    {
        if (!File.Exists(caminho))
        {
            throw new FileNotFoundException($"Arquivo de estoque não encontrado: {caminho}.", caminho);
        }

        ArquivoEstoque? conteudo;
        try
        {
            conteudo = JsonSerializer.Deserialize<ArquivoEstoque>(File.ReadAllText(caminho), JsonSerializerOptions.Web);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException(
                $"JSON inválido em {caminho}: linha {e.LineNumber + 1}, posição {e.BytePositionInLine + 1}.", e);
        }

        if (conteudo?.Estoque is not { Count: > 0 } produtos)
        {
            throw new InvalidDataException($"O arquivo {caminho} não contém a lista \"estoque\" com produtos.");
        }
        return produtos;
    }
}
