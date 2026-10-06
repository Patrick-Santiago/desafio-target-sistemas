using System.Diagnostics.CodeAnalysis;

namespace TargetSistemas.Terminal;

public readonly record struct Conversao<T>
{
    private Conversao(T valor, string? erro)
    {
        Valor = valor;
        Erro = erro;
    }

    public T Valor { get; }

    public string? Erro { get; }

    [MemberNotNullWhen(false, nameof(Erro))]
    public bool Sucesso => Erro is null;

    public static Conversao<T> Ok(T valor) => new(valor, null);

    public static Conversao<T> Falha(string erro) => new(default!, erro);
}
