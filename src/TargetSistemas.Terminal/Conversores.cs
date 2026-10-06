using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TargetSistemas.Terminal;

public static partial class Conversores
{
    [GeneratedRegex(@"^-?(\d+|\d{1,3}(\.\d{3})+)$")]
    private static partial Regex InteiroComMilhar();

    [GeneratedRegex(@"^-?\d+[.,]\d+$")]
    private static partial Regex NumeroComCasasDecimais();

    [GeneratedRegex(@"^(?<inteiro>\d+|\d{1,3}(\.\d{3})+)(,(?<centavos>\d+))?$")]
    private static partial Regex MoedaComVirgula();

    [GeneratedRegex(@"^(?<inteiro>\d+)\.(?<centavos>\d{1,2})$")]
    private static partial Regex MoedaComPonto();

    [GeneratedRegex(@"^(?<dia>\d{1,2})(?<sep>[/.-])(?<mes>\d{1,2})\k<sep>(?<ano>\d+)$")]
    private static partial Regex DataComSeparador();

    [GeneratedRegex(@"^(?<dia>\d{2})(?<mes>\d{2})(?<ano>\d{4})$")]
    private static partial Regex DataSemSeparador();

    public static string Normalizar(string texto)
    {
        var decomposto = texto.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                resultado.Append(c);
            }
        }
        return resultado.ToString();
    }

    public static Conversao<int> Inteiro(string texto, string prefixoErro, string exemplo, int minimo, int maximo)
    {
        texto = texto.Trim();
        if (!InteiroComMilhar().IsMatch(texto))
        {
            var instrucao = NumeroComCasasDecimais().IsMatch(texto)
                ? "use um número inteiro, sem casas decimais"
                : "use um número inteiro";
            return Conversao<int>.Falha($"{prefixoErro}: {instrucao}, como {exemplo}.");
        }

        var semMilhar = texto.Replace(".", "", StringComparison.Ordinal);
        if (!long.TryParse(semMilhar, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var valor))
        {
            valor = texto.StartsWith('-') ? long.MinValue : long.MaxValue;
        }
        if (valor < minimo)
        {
            var limite = minimo == 1 ? "maior que zero" : $"a partir de {Formatacao.Inteiro(minimo)}";
            return Conversao<int>.Falha($"{prefixoErro}: use um número inteiro {limite}, como {exemplo}.");
        }
        if (valor > maximo)
        {
            return Conversao<int>.Falha($"{prefixoErro}: use no máximo {Formatacao.Inteiro(maximo)}, como {exemplo}.");
        }
        return Conversao<int>.Ok((int)valor);
    }

    // Regra sem ambiguidade para valores digitados em pt-BR ou com ponto decimal:
    // vírgula é sempre decimal; sem vírgula, um único ponto seguido de 1 ou 2 dígitos no fim é decimal;
    // qualquer outro ponto precisa separar grupos de milhar de 3 dígitos.
    public static Conversao<decimal> Moeda(string texto, decimal maximo)
    {
        texto = texto.Trim();
        if (texto.StartsWith("R$", StringComparison.OrdinalIgnoreCase))
        {
            texto = texto[2..].Trim();
        }

        if (texto.StartsWith('-'))
        {
            return Conversao<decimal>.Falha("Valor inválido: o valor precisa ser maior que zero, como 200,00.");
        }
        var virgula = texto.IndexOf(',', StringComparison.Ordinal);
        if (virgula >= 0 && virgula < texto.LastIndexOf('.'))
        {
            return Conversao<decimal>.Falha("Valor inválido: use vírgula para os centavos, como 1.500,50.");
        }

        var partes = MoedaComPonto().Match(texto);
        if (!partes.Success)
        {
            partes = MoedaComVirgula().Match(texto);
        }
        if (!partes.Success)
        {
            return Conversao<decimal>.Falha("Valor inválido: digite um número, como 1500 ou 1.500,50.");
        }

        var centavos = partes.Groups["centavos"].Value;
        if (centavos.Length > 2)
        {
            return Conversao<decimal>.Falha("Valor inválido: use no máximo 2 casas decimais, como 1.500,50.");
        }

        var inteiro = partes.Groups["inteiro"].Value.Replace(".", "", StringComparison.Ordinal);
        var normalizado = centavos.Length > 0 ? $"{inteiro}.{centavos}" : inteiro;
        if (!decimal.TryParse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor)
            || valor > maximo)
        {
            return Conversao<decimal>.Falha($"Valor acima do limite: use até {Formatacao.Moeda(maximo)}.");
        }
        if (valor == 0m)
        {
            return Conversao<decimal>.Falha("Valor inválido: o valor precisa ser maior que zero, como 200,00.");
        }
        return Conversao<decimal>.Ok(valor);
    }

    public static Conversao<DateOnly> Data(string texto, DateOnly hoje, DateOnly minima, DateOnly maxima)
    {
        texto = texto.Trim();
        if (Normalizar(texto) == "HOJE")
        {
            return DentroDoIntervalo(hoje, minima, maxima);
        }

        var exemplo = Formatacao.Data(hoje);
        var partes = DataSemSeparador().Match(texto);
        if (!partes.Success)
        {
            partes = DataComSeparador().Match(texto);
        }
        if (!partes.Success)
        {
            return Conversao<DateOnly>.Falha($"Data inválida: use o formato dd/mm/aaaa, como {exemplo}.");
        }

        var textoAno = partes.Groups["ano"].Value;
        if (textoAno.Length != 4)
        {
            return Conversao<DateOnly>.Falha($"Data inválida: use o ano com 4 dígitos, como {exemplo}.");
        }

        var dia = int.Parse(partes.Groups["dia"].Value, CultureInfo.InvariantCulture);
        var mes = int.Parse(partes.Groups["mes"].Value, CultureInfo.InvariantCulture);
        var ano = int.Parse(textoAno, CultureInfo.InvariantCulture);
        if (ano < 1 || mes is < 1 or > 12 || dia < 1 || dia > DateTime.DaysInMonth(ano, mes))
        {
            return Conversao<DateOnly>.Falha($"Data inexistente: {dia:00}/{mes:00}/{textoAno} não existe no calendário.");
        }
        return DentroDoIntervalo(new DateOnly(ano, mes, dia), minima, maxima);
    }

    public static Conversao<bool> SimNao(string texto) => Normalizar(texto) switch
    {
        "S" or "SIM" => Conversao<bool>.Ok(true),
        "N" or "NAO" => Conversao<bool>.Ok(false),
        _ => Conversao<bool>.Falha("Resposta inválida: digite S para sim ou N para não."),
    };

    private static Conversao<DateOnly> DentroDoIntervalo(DateOnly data, DateOnly minima, DateOnly maxima) =>
        data < minima || data > maxima
            ? Conversao<DateOnly>.Falha(
                $"Data fora do intervalo: use uma data entre {Formatacao.Data(minima)} e {Formatacao.Data(maxima)}.")
            : Conversao<DateOnly>.Ok(data);
}
