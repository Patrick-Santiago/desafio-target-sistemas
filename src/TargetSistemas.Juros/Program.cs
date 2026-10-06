using TargetSistemas.Juros;
using TargetSistemas.Terminal;

return Execucao.Executar(args, (sessao, argumentos) => new AplicativoJuros(sessao, TimeProvider.System).Executar(argumentos));
