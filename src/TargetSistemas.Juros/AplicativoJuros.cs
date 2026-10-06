using TargetSistemas.Terminal;

namespace TargetSistemas.Juros;

public sealed class AplicativoJuros(Sessao sessao, TimeProvider relogio)
{
    private const int LarguraRotulo = 21;
    private const int DuracaoDiasMs = 250;
    private const int DuracaoCalculoMs = 300;
    private const int DuracaoValorMs = 400;

    private const string Uso = """
        Uso:
          dotnet run --project src/TargetSistemas.Juros -- [opções]
          dotnet run --project src/TargetSistemas.Juros -- <valor> <vencimento> [opções]

        Calcula os juros de 2,5% ao dia sobre o valor original, contando os dias corridos
        de atraso até hoje. Sem valor e vencimento, pergunta os dois.

        Valor: 1500, 1500,50, 1.500,50, 1500.50 ou R$ 1.500,50.
        Vencimento: dd/mm/aaaa (também aceita d/m/aaaa, dd-mm-aaaa, dd.mm.aaaa, ddmmaaaa ou "hoje").

        Opções:
          --sem-animacao    Mostra o resultado direto, sem animações.
                            O mesmo vale para a variável de ambiente TARGET_SEM_ANIMACAO=1.
          --help            Mostra esta ajuda.

        Exemplos:
          dotnet run --project src/TargetSistemas.Juros
          dotnet run --project src/TargetSistemas.Juros -- 1500,50 26/09/2026
          dotnet run --project src/TargetSistemas.Juros -- "R$ 200,00" hoje --sem-animacao
        """;

    public int Executar(IReadOnlyList<string> args)
    {
        if (Execucao.PediuAjuda(args))
        {
            sessao.Escrever(Uso);
            return Execucao.CodigoSucesso;
        }
        if (args.Count is not (0 or 2))
        {
            var informados = args.Count == 1 ? "Foi informado 1 argumento" : $"Foram informados {args.Count} argumentos";
            return FalhaDeUso($"{informados}: informe o valor e a data de vencimento, nesta ordem.");
        }

        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);
        if (args.Count == 2)
        {
            var conversaoValor = EntradaJuros.ConverterValor(args[0]);
            var conversaoData = EntradaJuros.ConverterData(args[1], hoje);
            if (!conversaoValor.Sucesso || !conversaoData.Sucesso)
            {
                return FalhaDeUso(conversaoValor.Erro ?? conversaoData.Erro!);
            }
            sessao.Dica($"Valor interpretado: {Formatacao.Moeda(conversaoValor.Valor)}");
            MostrarResultado(conversaoValor.Valor, conversaoData.Valor);
            return Execucao.CodigoSucesso;
        }

        sessao.Cabecalho("Juros por Atraso",
        [
            "Juros simples de 2,5% ao dia sobre o valor original, por dia corrido de atraso.",
            $"Hoje é {Formatacao.Data(hoje)}. No vencimento, também é possível digitar \"hoje\".",
        ]);
        do
        {
            sessao.Escrever();
            var valor = sessao.Perguntar("Valor (ex.: 1.500,00): ", EntradaJuros.ConverterValor);
            sessao.Dica($"Valor interpretado: {Formatacao.Moeda(valor)}");
            var vencimento = sessao.Perguntar("Vencimento (dd/mm/aaaa): ", t => EntradaJuros.ConverterData(t, hoje));
            MostrarResultado(valor, vencimento);
        }
        while (CalcularOutro());
        return Execucao.CodigoSucesso;
    }

    private bool CalcularOutro()
    {
        sessao.Escrever();
        try
        {
            return sessao.Confirmar("Calcular outro valor?");
        }
        catch (EntradaEncerradaException)
        {
            return false;
        }
    }

    private void MostrarResultado(decimal valor, DateOnly vencimento)
    {
        var resultado = new CalculadoraJuros(relogio).Calcular(valor, vencimento);
        sessao.Secao("Resultado");
        sessao.Escrever(Rotulo("Data de referência") + Formatacao.Data(resultado.DataReferencia));
        sessao.Escrever(Rotulo("Vencimento") + Formatacao.Data(vencimento));
        sessao.Escrever(Rotulo("Valor original") + Formatacao.Moeda(valor));

        if (resultado.DiasAtraso > 0)
        {
            sessao.Animacao.ContarValor(
                Rotulo("Dias de atraso"), 0, resultado.DiasAtraso, d => Formatacao.Inteiro((long)Math.Round(d)), DuracaoDiasMs);
            string[] termos =
            [
                Formatacao.Moeda(valor),
                $" x {(CalculadoraJuros.TaxaDiaria * 100).ToString("0.#", Formatacao.PtBr)}%",
                $" x {Dias(resultado.DiasAtraso)}",
                $" = {Formatacao.Moeda(resultado.ValorJuros)}",
            ];
            sessao.Animacao.AnimarLinha(
                t => Rotulo("Juros") + string.Concat(termos.Take((int)Math.Ceiling(t * termos.Length))), DuracaoCalculoMs);
        }
        else
        {
            var faltam = vencimento.DayNumber - resultado.DataReferencia.DayNumber;
            var situacao = faltam == 0
                ? "Vence hoje: sem juros."
                : $"Ainda não venceu: {(faltam == 1 ? "falta" : "faltam")} {Dias(faltam)}.";
            sessao.Escrever(Rotulo("Situação") + situacao);
        }

        sessao.Animacao.ContarValor(Rotulo("Valor atualizado"), 0, resultado.ValorAtualizado, Formatacao.Moeda, DuracaoValorMs);
    }

    private static string Rotulo(string texto) => texto.PadRight(LarguraRotulo);

    private static string Dias(int dias) => dias == 1 ? "1 dia" : $"{Formatacao.Inteiro(dias)} dias";

    private int FalhaDeUso(string mensagem)
    {
        sessao.Erro(mensagem);
        sessao.Escrever();
        sessao.Escrever(Uso);
        return Execucao.CodigoFalha;
    }
}
