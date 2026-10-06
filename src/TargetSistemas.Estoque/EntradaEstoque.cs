using System.Globalization;
using TargetSistemas.Terminal;

namespace TargetSistemas.Estoque;

public static class EntradaEstoque
{
    public static Conversao<int> ConverterCodigo(string texto, IReadOnlyList<int> codigosExistentes)
    {
        var lista = string.Join(", ", codigosExistentes);
        if (!int.TryParse(texto.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var codigo))
        {
            return Conversao<int>.Falha($"Código inválido: digite um dos códigos da lista ({lista}), como {codigosExistentes[0]}.");
        }
        return codigosExistentes.Contains(codigo)
            ? Conversao<int>.Ok(codigo)
            : Conversao<int>.Falha($"Produto {codigo} não encontrado: digite um dos códigos da lista ({lista}).");
    }

    public static Conversao<TipoMovimentacao> ConverterTipo(string texto) => Conversores.Normalizar(texto) switch
    {
        "E" or "ENTRADA" => Conversao<TipoMovimentacao>.Ok(TipoMovimentacao.Entrada),
        "S" or "SAIDA" => Conversao<TipoMovimentacao>.Ok(TipoMovimentacao.Saida),
        _ => Conversao<TipoMovimentacao>.Falha("Tipo inválido: digite E para entrada ou S para saída."),
    };

    public static Conversao<int> ConverterQuantidade(string texto, TipoMovimentacao tipo, int saldoAtual)
    {
        var conversao = Conversores.Inteiro(texto, "Quantidade inválida", "25", 1, Deposito.QuantidadeMaxima);
        if (!conversao.Sucesso)
        {
            return conversao;
        }

        var quantidade = conversao.Valor;
        if (tipo == TipoMovimentacao.Saida && quantidade > saldoAtual)
        {
            return Conversao<int>.Falha(
                $"Saldo insuficiente: há {Formatacao.Inteiro(saldoAtual)} un. disponíveis; digite uma quantidade até {Formatacao.Inteiro(saldoAtual)}.");
        }
        if (tipo == TipoMovimentacao.Entrada && (long)saldoAtual + quantidade > int.MaxValue)
        {
            return Conversao<int>.Falha(
                $"Quantidade inválida: o estoque não pode passar de {Formatacao.Inteiro(int.MaxValue)} un.");
        }
        return conversao;
    }

    public static Conversao<string> ConverterDescricao(string texto)
    {
        var descricao = texto.Trim();
        if (descricao.Length == 0)
        {
            return Conversao<string>.Falha("Descrição obrigatória: descreva a movimentação, como Compra de fornecedor.");
        }
        return descricao.Length > Deposito.TamanhoMaximoDescricao
            ? Conversao<string>.Falha(
                $"Descrição muito longa ({descricao.Length} caracteres): use até {Deposito.TamanhoMaximoDescricao}.")
            : Conversao<string>.Ok(descricao);
    }
}
