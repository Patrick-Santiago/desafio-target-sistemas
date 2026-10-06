using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TargetSistemas.Terminal;

namespace TargetSistemas.Comissoes;

public static partial class LeitorVendas
{
    private sealed record ArquivoVendas(IReadOnlyList<VendaArquivo?>? Vendas);

    private sealed record VendaArquivo(string? Vendedor, decimal? Valor);

    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.Strict,
    };

    [GeneratedRegex(@"^\$\.vendas\[(?<indice>\d+)\]\.(?<campo>\w+)$")]
    private static partial Regex CaminhoCampoVenda();

    public static IReadOnlyList<Venda> Ler(string caminho)
    {
        if (!File.Exists(caminho))
        {
            throw new FileNotFoundException($"Arquivo de vendas não encontrado: {caminho}.", caminho);
        }

        var json = File.ReadAllText(caminho);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException($"O arquivo {caminho} está vazio.");
        }

        ArquivoVendas? conteudo;
        try
        {
            conteudo = JsonSerializer.Deserialize<ArquivoVendas>(json, Opcoes);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException(DescreverErro(caminho, e), e);
        }

        if (conteudo?.Vendas is null)
        {
            throw new InvalidDataException($"O arquivo {caminho} não contém a lista \"vendas\".");
        }
        return conteudo.Vendas.Select((venda, indice) => Validar(venda, indice + 1)).ToList();
    }

    private static Venda Validar(VendaArquivo? venda, int numero)
    {
        if (string.IsNullOrWhiteSpace(venda?.Vendedor))
        {
            throw new InvalidDataException($"Venda {numero}: campo \"vendedor\" ausente ou vazio.");
        }
        if (venda.Valor is not { } valor)
        {
            throw new InvalidDataException($"Venda {numero} ({venda.Vendedor}): campo \"valor\" ausente.");
        }
        if (valor < 0)
        {
            throw new InvalidDataException($"Venda {numero} ({venda.Vendedor}): valor negativo ({Formatacao.Moeda(valor)}).");
        }
        return new Venda(venda.Vendedor.Trim(), valor);
    }

    private static string DescreverErro(string caminho, JsonException erro)
    {
        // Erro de sintaxe tem como causa a exceção do leitor JSON; erro de tipo de um campo tem
        // InvalidOperationException (ou nenhuma causa), e só nesse caso vale apontar o campo.
        var campo = CaminhoCampoVenda().Match(erro.Path ?? "");
        if (erro.InnerException is null or InvalidOperationException && campo.Success)
        {
            var numero = int.Parse(campo.Groups["indice"].Value, CultureInfo.InvariantCulture) + 1;
            var esperado = campo.Groups["campo"].Value == "valor"
                ? "um número, como 1200.50"
                : "um texto entre aspas, como \"Ana Lima\"";
            return $"Venda {numero}: o campo \"{campo.Groups["campo"].Value}\" deve ser {esperado}.";
        }
        return $"JSON inválido em {caminho}: linha {erro.LineNumber + 1}, posição {erro.BytePositionInLine + 1}.";
    }
}
