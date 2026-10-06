namespace TargetSistemas.Estoque;

public sealed class Deposito
{
    public const int QuantidadeMaxima = 1_000_000;
    public const int TamanhoMaximoDescricao = 60;

    private readonly Dictionary<int, Produto> _produtos;
    private readonly List<Movimentacao> _historico = [];
    private readonly TimeProvider _relogio;
    private int _ultimoId;

    public Deposito(IEnumerable<Produto> produtos, TimeProvider relogio)
    {
        _produtos = produtos.ToDictionary(p => p.CodigoProduto);
        _relogio = relogio;
    }

    public IReadOnlyList<Produto> Produtos => _produtos.Values.OrderBy(p => p.CodigoProduto).ToList();

    public IReadOnlyList<Movimentacao> Historico => _historico;

    public Produto? BuscarProduto(int codigoProduto) => _produtos.GetValueOrDefault(codigoProduto);

    public Movimentacao Lancar(int codigoProduto, TipoMovimentacao tipo, int quantidade, string descricao)
    {
        var produto = BuscarProduto(codigoProduto)
            ?? throw new ArgumentException($"Produto {codigoProduto} não encontrado.", nameof(codigoProduto));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantidade, QuantidadeMaxima);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        descricao = descricao.Trim();
        if (descricao.Length > TamanhoMaximoDescricao)
        {
            throw new ArgumentException(
                $"A descrição tem {descricao.Length} caracteres; o limite é {TamanhoMaximoDescricao}.", nameof(descricao));
        }

        var estoqueFinal = tipo == TipoMovimentacao.Entrada
            ? checked(produto.Estoque + quantidade)
            : produto.Estoque - quantidade;
        if (estoqueFinal < 0)
        {
            throw new InvalidOperationException(
                $"Saída de {quantidade} deixaria o produto {codigoProduto} com estoque negativo (disponível: {produto.Estoque}).");
        }

        _produtos[codigoProduto] = produto with { Estoque = estoqueFinal };
        var movimentacao = new Movimentacao(
            ++_ultimoId, descricao, tipo, codigoProduto, quantidade, estoqueFinal, _relogio.GetLocalNow());
        _historico.Add(movimentacao);
        return movimentacao;
    }
}
