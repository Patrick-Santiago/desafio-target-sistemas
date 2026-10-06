# TargetSistemas

Solução em C#/.NET para os três exercícios do desafio técnico: comissões de vendas, movimentação de estoque e juros por atraso.

## Requisitos

.NET SDK 10.0 (desenvolvido e testado com a versão 10.0.401). Não há dependências externas nos programas. O projeto de testes usa xUnit e `Microsoft.Extensions.TimeProvider.Testing`.

## Como executar

Todos os comandos partem da raiz do repositório.

```
dotnet run --project src/TargetSistemas.Comissoes
dotnet run --project src/TargetSistemas.Estoque
dotnet run --project src/TargetSistemas.Juros
```

Os três programas aceitam `--help`, que mostra o uso com exemplos. Argumentos do programa vão depois de `--`, por exemplo `dotnet run --project src/TargetSistemas.Juros -- --help`.

### Animações

No terminal, os programas usam animações curtas: o cabeçalho aparece linha a linha, os valores contam até o resultado e as barras crescem até o tamanho final. Somadas, não passam de cerca de 1 segundo por tela, e qualquer tecla pula direto para o resultado.

Para desligar as animações, use está forma:

```
dotnet run --project src/TargetSistemas.Juros -- --sem-animacao
```

## Como rodar os testes

```
dotnet test
```

## Estrutura do repositório

```
TargetSistemas.slnx
Directory.Build.props        configurações comuns de compilação
src/
  TargetSistemas.Comissoes/  desafio 1, lê vendas.json
  TargetSistemas.Estoque/    desafio 2, lê estoque.json
  TargetSistemas.Juros/      desafio 3
  TargetSistemas.Terminal/   leitura de entrada, conversões, tabelas, cores e animações
tests/
  TargetSistemas.Tests/      testes xUnit dos quatro projetos
```

## Validações de entrada

Toda mensagem de erro diz o que está errado e dá um exemplo do formato certo. O programa pede o dado de novo, sem encerrar. Espaços antes e depois do texto são ignorados em todos os campos.

Valor (juros):

- aceita `200`, `200,5`, `1.500,00`, `R$ 1.500,00` e `1500.50`;
- o valor interpretado é sempre exibido antes do resultado.

Data de vencimento (juros):

- aceita `06/10/2026`, `6/10/2026`, `06-10-2026`, `06.10.2026`, `06102026` e `hoje`;

Estoque:

- menu: aceita apenas 1, 2, 3 ou 0;
- código: aceita só os códigos da lista, e a mensagem de erro mostra os códigos disponíveis;
- tipo: aceita `E`, `S`, `entrada`, `saida` ou `saída`, sem diferenciar maiúsculas de minúsculas;
- descrição: de 1 a 60 caracteres;
- confirmação: aceita `S`, `sim`, `N` ou `não`.

Comissões: valida a estrutura do JSON e cada venda, como descrito no desafio 1. Argumentos desconhecidos mostram o uso e encerram.
