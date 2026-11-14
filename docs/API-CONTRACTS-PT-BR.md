# Contratos da API — português brasileiro

A API própria usa `/api/v1`. A migração de nomes foi coordenada com o frontend segundo o [plano histórico](plan/api-contracts-ptbr.md); a existência atual de clientes externos não foi verificada. Os caminhos incluem nomes em inglês e português. O código aplica a [política de nomes](../src/InvestDashboard.WebAPI/Serialization/PortugueseApiJsonNamingPolicy.cs) e atributos explícitos; nomes sem mapeamento usam camelCase, portanto não se deve presumir tradução de todos os campos.

## Regras gerais

- Muitos campos próprios usam `camelCase` em português; há nomes técnicos, de protocolo e propriedades sem tradução específica. Os query parameters são definidos em cada controller.
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

- Erros de validação podem usar `ProblemDetails`; alguns controllers retornam também respostas de erro próprias. Consulte o endpoint para a forma exata.
- O OpenAPI é mapeado em `/openapi/v1.json` no ambiente `Development`. A extração isolada de 13/11/2026 identificou lacunas nos status de resposta e na descrição de autenticação; consulte o [inventário e suas limitações](validation/contratos-api-inventario.md) e o [relatório](validation/auditoria-aprofundada.md).
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
| Grupos e titulares | `/grupos-carteiras`, `/grupos-carteiras/{grupoId}/titulares` | Grupos, membros, convites e perfis de titular; consulte os controllers para permissões e operações. |
| Instituições | `/instituicoes-financeiras` | Listagem e cadastro de instituição personalizada. |
| Benchmark CDI | `/benchmarks/cdi` | Série CDI; parâmetros e resposta estão no controller e DTOs correspondentes. |
| Indicadores BCB | `/indicadores/selic`, `/indicadores/ipca` | GET autenticado, com `dataDe` e `dataAte` obrigatórios (`AAAA-MM-DD`), sem datas futuras no horário de Brasília e com intervalo de até 3.650 dias. Retorna código SGS, unidade, periodicidade, origem, instante de consulta e pontos com data e valor; série vazia indica ausência de observações. Selic é percentual diário (SGS 11) e IPCA é variação mensal (SGS 433). A data dos pontos mensais é o primeiro dia do mês e pode anteceder `dataDe` dentro do mesmo mês. As séries são informativas e não alteram as taxas manuais. |
| Prévia de extrato Tesouro | `/portfolios/{carteiraId}/tesouro-direto/previa` | POST autenticado `multipart/form-data`, um campo `arquivo` HTML, HTM ou XLSX de até 10 MiB. Exige acesso à carteira. Retorna posições, movimentações e avisos extraídos, sem guardar o arquivo ou efetivar a importação; `podeConfirmar` e `coberturaCompleta` são `false`. O processamento ocorre na própria requisição e não cria tarefa recuperável. |
| Cotações Tesouro | `/tesouro-direto/titulos`, `/tesouro-direto/cotacoes` | GET autenticado; a lista informa títulos, vencimentos e data mais recente. A série exige `tipoTitulo`, `dataVencimento`, `dataDe` e `dataAte`, com intervalo de até 12.000 dias. Respostas incluem fonte e data de coleta; falha da fonte sem cache válido retorna 503. São cotações públicas sem vínculo com posições pessoais. |

O evento SignalR existente `OnPriceUpdate` preserva o nome técnico do evento e envia `ticker`, `preco`, `observadoEmUtc` e `origem`.

## Exemplos

Criar carteira (substitua o ID por uma instituição existente e acessível; o grupo e o titular dependem do contexto e das permissões do usuário):

```json
{"nome":"Longo prazo","descricao":"Carteira principal","instituicaoFinanceiraId":"00000000-0000-0000-0000-000000000001"}
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
