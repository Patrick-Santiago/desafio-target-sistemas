namespace TargetSistemas.Comissoes;

public static class CalculadoraComissao
{
    public static decimal CalcularComissao(decimal valorVenda) => valorVenda switch
    {
        < 100m => 0m,
        < 500m => valorVenda * 0.01m,
        _ => valorVenda * 0.05m,
    };

    public static IReadOnlyList<ComissaoVendedor> CalcularPorVendedor(IEnumerable<Venda> vendas) =>
        vendas
            .GroupBy(v => v.Vendedor)
            .Select(g => new ComissaoVendedor(
                g.Key,
                g.Count(),
                g.Sum(v => v.Valor),
                g.Sum(v => CalcularComissao(v.Valor))))
            .ToList();
}
