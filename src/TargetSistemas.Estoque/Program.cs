using TargetSistemas.Estoque;
using TargetSistemas.Terminal;

return Execucao.Executar(args, (sessao, argumentos) =>
    new AplicativoEstoque(sessao, Path.Combine(AppContext.BaseDirectory, "estoque.json"), TimeProvider.System)
        .Executar(argumentos));
