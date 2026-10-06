using TargetSistemas.Terminal;

namespace TargetSistemas.Tests;

public sealed class AnimacaoTests
{
    private readonly StringWriter _saida = new();
    private readonly List<int> _esperas = [];
    private readonly List<bool> _cursor = [];
    private int _teclasRestantes = -1;

    private Animacao CriarAtiva() => new(
        _saida,
        ativa: true,
        teclaPressionada: () => _teclasRestantes-- == 0,
        esperar: _esperas.Add,
        definirCursorVisivel: _cursor.Add);

    [Fact]
    public void Desligada_escreve_so_o_estado_final_sem_esperar()
    {
        var animacao = Animacao.Desligada(_saida);

        animacao.ContarValor("Total: ", 0, 100, v => v.ToString("0", Formatacao.PtBr), 400);

        Assert.Equal("Total: 100" + Environment.NewLine, _saida.ToString());
    }

    [Fact]
    public void ContarValor_redesenha_a_linha_e_termina_no_valor_final()
    {
        CriarAtiva().ContarValor("Total: ", 0, 100, v => v.ToString("0", Formatacao.PtBr), 400);

        Assert.Contains('\r', _saida.ToString());
        Assert.Equal("Total: 100", TerminalVirtual.Renderizar(_saida.ToString()).TrimEnd('\n'));
        Assert.InRange(_esperas.Sum(), 300, 400);
    }

    [Fact]
    public void RevelarLinhas_respeita_a_duracao_total()
    {
        CriarAtiva().RevelarLinhas(["a", "b", "c", "d"], 150);

        Assert.Equal("a\nb\nc\nd\n", _saida.ToString().Replace(Environment.NewLine, "\n", StringComparison.Ordinal));
        Assert.Equal(150, _esperas.Sum());
    }

    [Fact]
    public void Tecla_pressionada_pula_para_o_estado_final_e_pula_o_resto_da_tela()
    {
        _teclasRestantes = 2;
        var animacao = CriarAtiva();

        animacao.ContarValor("Total: ", 0, 100, v => v.ToString("0", Formatacao.PtBr), 1000);
        var esperasAntesDaSegunda = _esperas.Count;
        animacao.RevelarLinhas(["x", "y"], 500);

        Assert.Equal(2, esperasAntesDaSegunda);
        Assert.Equal(esperasAntesDaSegunda, _esperas.Count);
        Assert.Equal("Total: 100\nx\ny\n", TerminalVirtual.Renderizar(_saida.ToString()));
    }

    [Fact]
    public void NovaTela_volta_a_animar_depois_de_uma_tecla()
    {
        _teclasRestantes = 0;
        var animacao = CriarAtiva();
        animacao.RevelarLinhas(["a", "b"], 100);

        animacao.NovaTela();
        _esperas.Clear();
        animacao.RevelarLinhas(["c", "d"], 100);

        Assert.Equal(100, _esperas.Sum());
    }

    [Fact]
    public void Cursor_fica_oculto_durante_a_animacao_e_volta_mesmo_com_excecao()
    {
        var animacao = CriarAtiva();

        Assert.Throws<InvalidOperationException>(
            () => animacao.AnimarLinha(_ => throw new InvalidOperationException(), 100));

        Assert.Equal([false, true], _cursor);
    }

    [Fact]
    public void Desligada_nao_mexe_no_cursor()
    {
        var animacao = new Animacao(_saida, ativa: false, () => false, _esperas.Add, _cursor.Add);

        animacao.RevelarLinhas(["a"], 100);
        animacao.Segurar(250);

        Assert.Empty(_cursor);
        Assert.Empty(_esperas);
    }

    [Fact]
    public void Barra_usa_bloco_na_saida_utf8_e_cerquilha_nas_demais()
    {
        var utf8 = new StreamWriter(Stream.Null, new System.Text.UTF8Encoding(false));

        Assert.Equal("███..", Animacao.Desligada(utf8).Barra(3, 5));
        Assert.Equal("###..", Animacao.Desligada(_saida).Barra(3, 5));
    }
}
