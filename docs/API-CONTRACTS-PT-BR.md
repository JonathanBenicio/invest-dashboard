# Contratos da API — português brasileiro

A API própria publica JSON e parâmetros de consulta em português brasileiro em `/api/v1`. A migração foi coordenada com o frontend porque ainda não há clientes reais. Os caminhos de recursos continuam em inglês; não há aliases de payload em inglês.

## Regras gerais

- Propriedades e query parameters usam `camelCase` em português.
- `id`, `ticker`, siglas, moeda e valores de enum usados como códigos continuam estáveis. Exemplos: `Buy`, `Sell`, `fixed_income`, `variable_income`, `open`, `closed`, `deterministic`, `montecarlo`, `user`, `admin`.
- Envelopes e paginação fazem parte do contrato da aplicação:

```json
{
  "dados": [],
  "sucesso": true,
  "mensagem": null,
  "paginacao": {
    "pagina": 1,
    "itensPorPagina": 10,
    "totalItens": 0,
    "totalPaginas": 0,
    "temProximaPagina": false,
    "temPaginaAnterior": false
  }
}
```

- Requests inválidos continuam usando `ProblemDetails` conforme o protocolo HTTP; seus nomes padronizados não são traduzidos.
- O schema gerado em `/openapi/v1.json` usa os mesmos nomes JSON portugueses da API.
- DTOs que representam respostas de terceiros mantêm o formato do provedor. Claims e cookies JWT também mantêm nomes de protocolo.

## Recursos e filtros

| Recurso | Rotas | Contrato relevante |
|---|---|---|
| Carteiras | `/portfolios?grupoId=...` | `grupoId` é opcional e restringe lista/paginação a um grupo do qual o usuário é membro ativo; sem ele, retorna todos os grupos acessíveis. Inclui `nome`, `descricao`, `posicoes`, `valorTotal`, `totalInvestido`, `ganhoTotal`, `percentualGanho` |
| Resumo geral das carteiras | `/portfolios/resumo-geral?grupoId=...` | `grupoId` opcional mantém o mesmo escopo da lista; resposta agrega todas as carteiras acessíveis no escopo com `valorTotal`, `totalInvestido`, `ganhoTotal`, `percentualGanho` e `quantidadeCarteiras` |
| Projeção consolidada de renda fixa | `/portfolios/projecao-renda-fixa?grupoId=...` | `grupoId` opcional; sem ele projeta todas as posições acessíveis, com ele somente as posições do grupo após validar a associação do usuário |
| Resumo da carteira | `/portfolios/{id}/summary` | `alocacaoAtivos`, `categoria`, `valor`, `percentual`, `cor`; use `/portfolios/{id}/history` para séries históricas |
| Histórico da carteira | `/portfolios/{id}/history` | `dataDe`, `dataAte`; resposta `data`, `valorTotal`, `estaCompleto`, `tickersAusentes` |
| Posições | `/investments`, `/portfolios/{portfolioId}/investments` | filtros `carteiraId`, `grupoId`, `tipo`, `subtipo`, `emissor`, `setor`, `situacao`, `busca`, `ordenarPor`, `ordem`, `pagina`, `itensPorPagina`; `grupoId` exige associação ativa |
| Renda fixa | `/investments/fixed-income` | `carteiraId`, `nome`, `subtipo`, `emissor`, `valorPrincipal`, `valorExtrato`, `taxaJuros`, `indexador`, `dataCompra`, `dataVencimento`, `chaveIdempotencia` |
| Renda variável | `/investments/variable-income` | `carteiraId`, `ticker`, `subtipo`, `nome`, `setor`, `quantidade`, `precoUnitario`, `taxas`, `dataTransacao`, `chaveIdempotencia` |
| Avaliação da posição | `/investments/{id}/valuations` | `valorTotal`, `data` |
| Transações | `/transactions`, `/transactions/portfolio/{portfolioId}` | `carteiraId`, `ativoId`, `tipo`, `quantidade`, `precoUnitario`, `taxas`, `dataTransacao`, `chaveIdempotencia`, `observacoes` |
| Taxas | `/taxes` | `nome`, `simbolo`, `valorAtual`, `valorAnterior`, `variacao`, `descricao`, `origem`, `atualizadoEm` |
| Simulação | `/simulation`, `/simulation/strategies` | `valorInicial`, `aporteMensal`, `anos`, `taxaJurosAnual`, `estrategia`, `volatilidade`, `numeroSimulacoes`; estratégias usam `id`, `nome`, `descricao` |
| Mercado | `/market-data/quotes`, `/search`, `/history` | consultas `simbolos`, `consulta`, `dataInicio`, `dataFim`; respostas próprias usam `simbolo`, `nome`, `preco`, `observadoEmUtc`, `origem` |
| Autenticação | `/auth/login`, `/register`, `/refresh`, `/logout`, `/me` | requests usam `email`, `senha`, `nome`; sessão usa `tokenAcesso`, `expiraEmSegundos`, `usuario`, `requerConfirmacaoEmail` |

O evento SignalR existente `OnPriceUpdate` preserva o nome técnico do evento e envia `ticker`, `preco`, `observadoEmUtc` e `origem`.

## Exemplos

Criar carteira:

```json
{"nome":"Longo prazo","descricao":"Carteira principal"}
```

Registrar compra:

```json
{
  "carteiraId":"00000000-0000-0000-0000-000000000001",
  "ticker":"PETR4",
  "tipo":"Buy",
  "classeAtivo":"ACAO",
  "quantidade":10,
  "precoUnitario":35.5,
  "taxas":1,
  "dataTransacao":"2026-09-29T12:00:00Z",
  "chaveIdempotencia":"00000000-0000-0000-0000-000000000002"
}
```

Simular investimento:

```json
{
  "valorInicial":1000,
  "aporteMensal":500,
  "anos":5,
  "taxaJurosAnual":10,
  "estrategia":"deterministic"
}
```

## Fronteiras e trabalho futuro

Os dados financeiros persistidos e as regras de domínio não mudaram nesta migração. Fluxo de caixa e proventos continuam adiados. Coleta diária do Tesouro Transparente e suas tabelas/gráficos comparativos continuam planejadas para uma etapa futura. Integração sustentada com Brapi, Supabase e outros serviços externos permanece reservada à fase final.
