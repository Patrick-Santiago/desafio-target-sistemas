using TargetSistemas.Comissoes;
using TargetSistemas.Terminal;

return Execucao.Executar(args, (sessao, argumentos) =>
    new AplicativoComissoes(sessao, Path.Combine(AppContext.BaseDirectory, "vendas.json")).Executar(argumentos));
