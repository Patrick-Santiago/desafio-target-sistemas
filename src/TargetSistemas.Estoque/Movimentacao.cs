namespace TargetSistemas.Estoque;

public sealed record Movimentacao(
    int Id,
    string Descricao,
    TipoMovimentacao Tipo,
    int CodigoProduto,
    int Quantidade,
    int EstoqueFinal,
    DateTimeOffset DataHora);
