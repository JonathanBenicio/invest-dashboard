# Inventário de operações HTTP e schema OpenAPI

Gerado em 13/11/2026 a partir de `/openapi/v1.json` com SDK 10.0.401, binários preexistentes, host temporário em Development, EF InMemory e rede de container desativada. Workers de mercado e vencimento foram removidos do host. O schema reflete esses binários, não comprova que correspondam byte a byte à árvore atual.

A tabela usa parâmetros e corpos do OpenAPI, não garante validação real, autorização ou efeitos. Os controllers, DTOs e serviços são as fontes para conferência; [relatório de auditoria](auditoria-aprofundada.md) registra discrepâncias e limites. Não foram chamados endpoints de negócio durante a extração.

## Autorização, respostas e limitações gerais

- Os controllers exigem autenticação exceto `auth/login`, `auth/register`, `auth/convites/aceitar`, `auth/refresh` e `auth/logout`; `auth/me` exige autenticação. A autenticação não garante acesso a determinado grupo ou carteira: serviços e repositórios verificam associação, papel, titularidade e visibilidade conforme a operação.
- Nas taxas, leitura exige membro ativo e escrita exige Admin; nos grupos, listagem de membros e mudanças de papel são administrativas. Os testes de isolamento e autorização são evidências somente para os cenários que exercitam.
- Os controllers podem retornar envelopes ou respostas diretas; o [middleware de exceções](../../src/InvestDashboard.WebAPI/Middleware/ExceptionHandlingMiddleware.cs) converte exceções reconhecidas em `ProblemDetails` com 400, 401, 403, 404, 409 ou 503, além de 500 para erro não mapeado. A ocorrência efetiva depende do caminho.
- O schema obtido não declara `securitySchemes` ou requisitos `security` por operação. Ele lista somente `200` para as operações, embora algumas actions retornem `201` ou `202` e outras respostas de erro. Não interpretar a coluna de respostas como lista completa de status reais.
- Este inventário extraído não rastreia todos os efeitos, papéis, regras por campo, clientes e testes por operação. Para confirmação é necessário seguir cada referência ao controller, DTO e serviço; a cobertura desses itens permanece pendente.

## Operações

| Método e rota | Parâmetros no schema | Corpo no schema | Resposta no schema | Fonte atual |
|---|---|---|---|---|
| `POST /api/v1/auth/login` | — | SolicitacaoLoginDto | 200 RespostaApiOfSessaoAutenticacaoDto | [AuthController.cs:21](../../src/InvestDashboard.WebAPI/Controllers/AuthController.cs#L21) |
| `POST /api/v1/auth/register` | — | SolicitacaoCadastroDto | 200 RespostaApiOfSessaoAutenticacaoDto | [AuthController.cs:33](../../src/InvestDashboard.WebAPI/Controllers/AuthController.cs#L33) |
| `POST /api/v1/auth/convites/aceitar` | — | SolicitacaoAceiteConviteDto | 200 RespostaApiOfSessaoAutenticacaoDto | [AuthController.cs:48](../../src/InvestDashboard.WebAPI/Controllers/AuthController.cs#L48) |
| `POST /api/v1/auth/refresh` | — | — | 200 RespostaApiOfSessaoAutenticacaoDto | [AuthController.cs:60](../../src/InvestDashboard.WebAPI/Controllers/AuthController.cs#L60) |
| `POST /api/v1/auth/logout` | — | — | 200 RespostaApiOfboolean | [AuthController.cs:73](../../src/InvestDashboard.WebAPI/Controllers/AuthController.cs#L73) |
| `GET /api/v1/auth/me` | — | — | 200 RespostaApiOfUsuarioAutenticadoDto | [AuthController.cs:86](../../src/InvestDashboard.WebAPI/Controllers/AuthController.cs#L86) |
| `GET /api/v1/benchmarks/cdi` | dataDe (query, string); dataAte (query, string) | — | 200 RespostaApiOfSerieBenchmarkCdiDto | [BenchmarksController.cs:14](../../src/InvestDashboard.WebAPI/Controllers/BenchmarksController.cs#L14) |
| `GET /api/v1/portfolios` | pagina (query, integer/string, padrão 1); itensPorPagina (query, integer/string, padrão 10); grupoId (query, string) | — | 200 RespostaPaginadaOfCarteiraDto | [CarteirasController.cs:14](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs#L14) |
| `POST /api/v1/portfolios` | — | CriarCarteiraDto | 200 RespostaApiOfCarteiraDto | [CarteirasController.cs:98](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs#L98) |
| `GET /api/v1/portfolios/resumo-geral` | grupoId (query, string) | — | 200 RespostaApiOfResumoCarteirasDto | [CarteirasController.cs:24](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs#L24) |
| `GET /api/v1/portfolios/projecao-renda-fixa` | grupoId (query, string) | — | 200 RespostaApiOfProjecaoRendaFixaConsolidadaDto | [CarteirasController.cs:29](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs#L29) |
| `GET /api/v1/portfolios/{id}` | id (path, string, obrigatório) | — | 200 RespostaApiOfCarteiraDto | [CarteirasController.cs:34](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs#L34) |
| `PATCH /api/v1/portfolios/{id}` | id (path, string, obrigatório) | AtualizarCarteiraDto | 200 RespostaApiOfCarteiraDto | [CarteirasController.cs:105](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs#L105) |
| `DELETE /api/v1/portfolios/{id}` | id (path, string, obrigatório) | — | 200 RespostaApiOfboolean | [CarteirasController.cs:114](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs#L114) |
| `GET /api/v1/portfolios/{id}/summary` | id (path, string, obrigatório) | — | 200 RespostaApiOfResumoCarteiraDto | [CarteirasController.cs:43](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs#L43) |
| `GET /api/v1/portfolios/{id}/history` | id (path, string, obrigatório); dataDe (query, string); dataAte (query, string) | — | 200 RespostaApiOfIReadOnlyListOfPontoHistoricoCarteiraDto | [CarteirasController.cs:86](../../src/InvestDashboard.WebAPI/Controllers/CarteirasController.cs#L86) |
| `GET /api/v1/grupos-carteiras` | — | — | 200 RespostaApiOfIReadOnlyListOfGrupoCarteirasDto | [GruposCarteirasController.cs:15](../../src/InvestDashboard.WebAPI/Controllers/GruposCarteirasController.cs#L15) |
| `POST /api/v1/grupos-carteiras` | — | CriarGrupoCarteirasDto | 200 RespostaApiOfGrupoCarteirasDto | [GruposCarteirasController.cs:22](../../src/InvestDashboard.WebAPI/Controllers/GruposCarteirasController.cs#L22) |
| `GET /api/v1/grupos-carteiras/{grupoId}/membros` | grupoId (path, string, obrigatório) | — | 200 RespostaApiOfIReadOnlyListOfMembroGrupoDto | [GruposCarteirasController.cs:32](../../src/InvestDashboard.WebAPI/Controllers/GruposCarteirasController.cs#L32) |
| `PUT /api/v1/grupos-carteiras/{grupoId}/membros/{membroId}/papel` | grupoId (path, string, obrigatório); membroId (path, string, obrigatório) | AlterarPapelGrupoDto | 200 RespostaApiOfboolean | [GruposCarteirasController.cs:39](../../src/InvestDashboard.WebAPI/Controllers/GruposCarteirasController.cs#L39) |
| `DELETE /api/v1/grupos-carteiras/{grupoId}/membros/{membroId}` | grupoId (path, string, obrigatório); membroId (path, string, obrigatório) | — | 200 RespostaApiOfboolean | [GruposCarteirasController.cs:46](../../src/InvestDashboard.WebAPI/Controllers/GruposCarteirasController.cs#L46) |
| `POST /api/v1/grupos-carteiras/{grupoId}/convites` | grupoId (path, string, obrigatório) | ConvidarMembroGrupoDto | 200 RespostaApiOfResultadoConviteGrupoDto | [GruposCarteirasController.cs:53](../../src/InvestDashboard.WebAPI/Controllers/GruposCarteirasController.cs#L53) |
| `GET /api/v1/grupos-carteiras/convites-pendentes` | — | — | 200 RespostaApiOfIReadOnlyListOfConvitePendenteGrupoDto | [GruposCarteirasController.cs:62](../../src/InvestDashboard.WebAPI/Controllers/GruposCarteirasController.cs#L62) |
| `POST /api/v1/grupos-carteiras/{grupoId}/convites/{conviteId}/aceitar` | grupoId (path, string, obrigatório); conviteId (path, string, obrigatório) | — | 200 RespostaApiOfboolean | [GruposCarteirasController.cs:70](../../src/InvestDashboard.WebAPI/Controllers/GruposCarteirasController.cs#L70) |
| `GET /api/v1/instituicoes-financeiras` | grupoId (query, string) | — | 200 RespostaApiOfIReadOnlyListOfInstituicaoFinanceiraDto | [InstituicoesFinanceirasController.cs:17](../../src/InvestDashboard.WebAPI/Controllers/InstituicoesFinanceirasController.cs#L17) |
| `POST /api/v1/instituicoes-financeiras` | grupoId (query, string, obrigatório) | CriarInstituicaoPersonalizadaDto | 200 RespostaApiOfInstituicaoFinanceiraDto | [InstituicoesFinanceirasController.cs:25](../../src/InvestDashboard.WebAPI/Controllers/InstituicoesFinanceirasController.cs#L25) |
| `GET /api/v1/investments/{positionId}/projecao-renda-fixa` | positionId (path, string, obrigatório) | — | 200 RespostaApiOfProjecaoRendaFixaDto | [InvestimentosController.cs:20](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L20) |
| `GET /api/v1/investments` | carteiraId (query, string); grupoId (query, string); tipo (query, string); subtipo (query, string); emissor (query, string); setor (query, string); situacao (query, string); busca (query, string); ordenarPor (query, string); ordem (query, string); pagina (query, integer/string, padrão 1); itensPorPagina (query, integer/string, padrão 10) | — | 200 RespostaPaginadaOfPosicaoInvestimentoDto | [InvestimentosController.cs:29](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L29) |
| `GET /api/v1/portfolios/{portfolioId}/investments` | portfolioId (path, string, obrigatório); tipo (query, string); subtipo (query, string); emissor (query, string); setor (query, string); situacao (query, string); busca (query, string); ordenarPor (query, string); ordem (query, string); pagina (query, integer/string, padrão 1); itensPorPagina (query, integer/string, padrão 10) | — | 200 RespostaPaginadaOfPosicaoInvestimentoDto | [InvestimentosController.cs:54](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L54) |
| `GET /api/v1/investments/summary` | carteiraId (query, string) | — | 200 RespostaApiOfResumoInvestimentoDto | [InvestimentosController.cs:71](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L71) |
| `GET /api/v1/investments/dividends` | pagina (query, integer/string, padrão 1); itensPorPagina (query, integer/string, padrão 10) | — | 200 RespostaPaginadaOfObject | [InvestimentosController.cs:119](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L119) |
| `GET /api/v1/investments/{id}/transactions` | id (path, string, obrigatório); pagina (query, integer/string, padrão 1); itensPorPagina (query, integer/string, padrão 10) | — | 200 RespostaPaginadaOfTransacaoDto | [InvestimentosController.cs:128](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L128) |
| `GET /api/v1/investments/{id}` | id (path, string, obrigatório) | — | 200 RespostaApiOfPosicaoInvestimentoDto | [InvestimentosController.cs:145](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L145) |
| `DELETE /api/v1/investments/{id}` | id (path, string, obrigatório) | — | 200 RespostaApiOfboolean | [InvestimentosController.cs:233](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L233) |
| `POST /api/v1/investments/fixed-income` | — | CriarRendaFixaDto | 200 RespostaApiOfPosicaoInvestimentoDto | [InvestimentosController.cs:154](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L154) |
| `POST /api/v1/investments/variable-income` | — | CriarRendaVariavelDto | 200 RespostaApiOfPosicaoInvestimentoDto | [InvestimentosController.cs:186](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L186) |
| `POST /api/v1/investments/{id}/valuations` | id (path, string, obrigatório) | RegistrarAvaliacaoDto | 200 RespostaApiOfPosicaoInvestimentoDto | [InvestimentosController.cs:211](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L211) |
| `GET /api/v1/investments/{id}/history` | id (path, string, obrigatório); dataDe (query, string) | — | 200 RespostaApiOfIReadOnlyListOfPrecoHistoricoDto | [InvestimentosController.cs:220](../../src/InvestDashboard.WebAPI/Controllers/InvestimentosController.cs#L220) |
| `GET /api/v1/market-data/quotes` | simbolos (query, string, obrigatório) | — | 200 RespostaApiOfIReadOnlyListOfCotacaoMercadoDto | [MarketDataController.cs:15](../../src/InvestDashboard.WebAPI/Controllers/MarketDataController.cs#L15) |
| `GET /api/v1/market-data/search` | consulta (query, string, obrigatório) | — | 200 RespostaApiOfIReadOnlyListOfResultadoBuscaMercadoDto | [MarketDataController.cs:28](../../src/InvestDashboard.WebAPI/Controllers/MarketDataController.cs#L28) |
| `GET /api/v1/market-data/history` | simbolos (query, string, obrigatório); dataInicio (query, string, obrigatório); dataFim (query, string, obrigatório) | — | 200 RespostaApiOfIReadOnlyListOfPontoHistoricoMercadoDto | [MarketDataController.cs:37](../../src/InvestDashboard.WebAPI/Controllers/MarketDataController.cs#L37) |
| `POST /api/v1/simulation` | — | SimulacaoRequestDto | 200 RespostaApiOfSimulacaoResponseDto | [SimulacaoController.cs:23](../../src/InvestDashboard.WebAPI/Controllers/SimulacaoController.cs#L23) |
| `GET /api/v1/simulation/strategies` | — | — | 200 RespostaApiOfObject | [SimulacaoController.cs:62](../../src/InvestDashboard.WebAPI/Controllers/SimulacaoController.cs#L62) |
| `GET /api/v1/taxes/estimativa-mensal` | ano (query, integer/string); mes (query, integer/string); grupoId (query, string, obrigatório); titular (query, string); titularId (query, string) | — | 200 RespostaApiOfEstimativaImpostoMensalDto | [TaxasController.cs:27](../../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs#L27) |
| `GET /api/v1/taxes` | grupoId (query, string, obrigatório) | — | 200 RespostaApiOfListOfTaxaEconomicaDto | [TaxasController.cs:39](../../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs#L39) |
| `POST /api/v1/taxes` | grupoId (query, string, obrigatório) | CriarTaxaEconomicaDto | 200 RespostaApiOfTaxaEconomicaDto | [TaxasController.cs:66](../../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs#L66) |
| `GET /api/v1/taxes/{id}` | id (path, string, obrigatório); grupoId (query, string, obrigatório) | — | 200 RespostaApiOfTaxaEconomicaDto | [TaxasController.cs:46](../../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs#L46) |
| `PUT /api/v1/taxes/{id}` | id (path, string, obrigatório); grupoId (query, string, obrigatório) | AtualizarTaxaEconomicaDto | 200 RespostaApiOfTaxaEconomicaDto | [TaxasController.cs:74](../../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs#L74) |
| `DELETE /api/v1/taxes/{id}` | id (path, string, obrigatório); grupoId (query, string, obrigatório) | — | 200 RespostaApiOfObject | [TaxasController.cs:85](../../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs#L85) |
| `GET /api/v1/taxes/{id}/historico` | id (path, string, obrigatório); grupoId (query, string, obrigatório) | — | 200 RespostaApiOfIReadOnlyListOfTaxaEconomicaHistoricoDto | [TaxasController.cs:56](../../src/InvestDashboard.WebAPI/Controllers/TaxasController.cs#L56) |
| `GET /api/v1/grupos-carteiras/{grupoId}/titulares` | grupoId (path, string, obrigatório) | — | 200 RespostaApiOfIReadOnlyListOfTitularCarteiraDto | [TitularesController.cs:15](../../src/InvestDashboard.WebAPI/Controllers/TitularesController.cs#L15) |
| `POST /api/v1/grupos-carteiras/{grupoId}/titulares` | grupoId (path, string, obrigatório) | SalvarTitularCarteiraDto | 200 RespostaApiOfTitularCarteiraDto | [TitularesController.cs:19](../../src/InvestDashboard.WebAPI/Controllers/TitularesController.cs#L19) |
| `PUT /api/v1/grupos-carteiras/{grupoId}/titulares/{id}` | grupoId (path, string, obrigatório); id (path, string, obrigatório) | SalvarTitularCarteiraDto | 200 RespostaApiOfTitularCarteiraDto | [TitularesController.cs:23](../../src/InvestDashboard.WebAPI/Controllers/TitularesController.cs#L23) |
| `POST /api/v1/transactions` | — | RegistrarTransacaoDto | 200 RespostaApiOfTransacaoDto | [TransacoesController.cs:29](../../src/InvestDashboard.WebAPI/Controllers/TransacoesController.cs#L29) |
| `GET /api/v1/transactions/portfolio/{portfolioId}` | portfolioId (path, string, obrigatório) | — | 200 RespostaApiOfListOfTransacaoDto | [TransacoesController.cs:41](../../src/InvestDashboard.WebAPI/Controllers/TransacoesController.cs#L41) |
| `PATCH /api/v1/transactions/{id}` | id (path, string, obrigatório) | AtualizarTransacaoDto | 200 RespostaApiOfTransacaoDto | [TransacoesController.cs:48](../../src/InvestDashboard.WebAPI/Controllers/TransacoesController.cs#L48) |
| `DELETE /api/v1/transactions/{id}` | id (path, string, obrigatório) | — | 200 RespostaApiOfboolean | [TransacoesController.cs:57](../../src/InvestDashboard.WebAPI/Controllers/TransacoesController.cs#L57) |
| `POST /api/v1/transactions/upload-note` | — | application/x-www-form-urlencoded | 200 | [TransacoesController.cs:69](../../src/InvestDashboard.WebAPI/Controllers/TransacoesController.cs#L69) |

## Schemas de corpo de requisição

Campos, tipos e restrições exibidos pelo OpenAPI; `required` é a lista explícita do schema e pode não refletir as validações da aplicação. Campos com `format: uuid` possuem formato UUID. Defaults e faixas não presentes no schema exigem consulta ao DTO e serviço.

### AlterarPapelGrupoDto

`papel` (string; obrigatório no schema)

### AtualizarCarteiraDto

`nome` (null/string ; minLength=1, maxLength=100); `descricao` (null/string ; minLength=0, maxLength=500); `titular` (null/string ; minLength=1, maxLength=160); `titularId` (null/string ; format=uuid); `titularUsuarioId` (null/string); `desvincularTitular` (boolean); `parentesco` (null/string ; minLength=0, maxLength=80); `instituicaoFinanceira` (null/string ; minLength=0, maxLength=120); `instituicaoFinanceiraId` (null/string ; format=uuid); `tipoInstituicao` (null/string ; minLength=0, maxLength=20); `visibilidade` (null/string)

### AtualizarTaxaEconomicaDto

`nome` (string); `simbolo` (string); `valorAtual` (number/string ; format=double, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `valorAnterior` (number/string ; format=double, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `descricao` (string); `origem` (string); `unidade` (string); `periodicidade` (string); `dataReferencia` (string ; format=date)

### AtualizarTransacaoDto

`tipo` (string; obrigatório no schema ; pattern=^([Bb][Uu][Yy]|[Ss][Ee][Ll][Ll])$); `quantidade` (number/string ; format=double, minimum=1e-08, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `precoUnitario` (number/string ; format=double, minimum=1e-08, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `taxas` (number/string ; format=double, minimum=0, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `modalidadeFiscal` (null/string ; minLength=0, maxLength=20); `dataTransacao` (string ; format=date-time); `observacoes` (null/string ; minLength=0, maxLength=1000)

### ConvidarMembroGrupoDto

`email` (string; obrigatório no schema); `papel` (string; obrigatório no schema)

### CriarCarteiraDto

`nome` (string; obrigatório no schema ; minLength=1, maxLength=100); `descricao` (null/string ; minLength=0, maxLength=500); `grupoId` (null/string ; format=uuid); `titular` (null/string ; minLength=1, maxLength=160); `titularId` (null/string ; format=uuid); `titularUsuarioId` (null/string); `parentesco` (null/string ; minLength=0, maxLength=80); `instituicaoFinanceira` (null/string ; minLength=0, maxLength=120); `instituicaoFinanceiraId` (null/string ; format=uuid); `tipoInstituicao` (null/string ; minLength=0, maxLength=20); `visibilidade` (string)

### CriarGrupoCarteirasDto

`nome` (string; obrigatório no schema)

### CriarInstituicaoPersonalizadaDto

`nome` (string)

### CriarRendaFixaDto

`carteiraId` (string; obrigatório no schema ; format=uuid); `nome` (string; obrigatório no schema ; minLength=0, maxLength=200); `subtipo` (string; obrigatório no schema ; minLength=0, maxLength=20); `emissor` (string; obrigatório no schema ; minLength=0, maxLength=200); `valorPrincipal` (number/string ; format=double, minimum=1e-08, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `valorExtrato` (number/string ; format=double, minimum=0, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `taxaJuros` (number/string ; format=double, minimum=0, maximum=10000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `indexador` (string; obrigatório no schema ; minLength=0, maxLength=20); `dataVencimento` (string ; format=date-time); `dataCompra` (string ; format=date-time); `liquidez` (null/string ; minLength=0, maxLength=80); `convencao` (null/string ; minLength=0, maxLength=80); `chaveIdempotencia` (string; obrigatório no schema ; format=uuid)

### CriarRendaVariavelDto

`carteiraId` (string; obrigatório no schema ; format=uuid); `ticker` (string; obrigatório no schema ; minLength=0, maxLength=20); `subtipo` (string; obrigatório no schema ; pattern=^(ACAO|FII|ETF|BDR|CRYPTO)$); `nome` (null/string ; minLength=0, maxLength=200); `setor` (null/string ; minLength=0, maxLength=100); `quantidade` (number/string ; format=double, minimum=1e-08, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `precoUnitario` (number/string ; format=double, minimum=1e-08, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `taxas` (number/string ; format=double, minimum=0, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `dataTransacao` (string ; format=date-time); `chaveIdempotencia` (string; obrigatório no schema ; format=uuid)

### CriarTaxaEconomicaDto

`nome` (string); `simbolo` (string); `valorAtual` (number/string ; format=double, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `valorAnterior` (number/string ; format=double, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `descricao` (string); `origem` (string); `unidade` (string); `periodicidade` (string); `dataReferencia` (string ; format=date)

### RegistrarAvaliacaoDto

`valorTotal` (number/string ; format=double, minimum=0, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `data` (string ; format=date-time)

### RegistrarTransacaoDto

`carteiraId` (string; obrigatório no schema ; format=uuid); `ativoId` (null/string ; format=uuid); `ticker` (null/string ; minLength=0, maxLength=20); `tipo` (string; obrigatório no schema ; pattern=^([Bb][Uu][Yy]|[Ss][Ee][Ll][Ll])$); `classeAtivo` (null/string ; minLength=0, maxLength=20); `nome` (null/string ; minLength=0, maxLength=200); `setor` (null/string ; minLength=0, maxLength=100); `emissor` (null/string ; minLength=0, maxLength=200); `subtipo` (null/string ; minLength=0, maxLength=20); `indexador` (null/string ; minLength=0, maxLength=50); `taxaJuros` (null/number/string ; format=double, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `dataVencimento` (null/string ; format=date-time); `liquidez` (null/string ; minLength=0, maxLength=80); `convencao` (null/string ; minLength=0, maxLength=80); `chaveIdempotencia` (string; obrigatório no schema ; format=uuid); `valorInicialExtrato` (null/number/string ; format=double, minimum=0, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `quantidade` (number/string ; format=double, minimum=1e-08, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `precoUnitario` (number/string ; format=double, minimum=1e-08, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `taxas` (number/string ; format=double, minimum=0, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `modalidadeFiscal` (null/string ; minLength=0, maxLength=20); `dataTransacao` (string ; format=date-time); `observacoes` (null/string)

### SalvarTitularCarteiraDto

`nome` (string; obrigatório no schema ; minLength=1, maxLength=160); `parentesco` (null/string ; minLength=0, maxLength=80); `usuarioId` (null/string ; minLength=0, maxLength=100)

### SimulacaoRequestDto

`valorInicial` (number/string ; format=double, minimum=0, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `aporteMensal` (number/string ; format=double, minimum=0, maximum=100000000000000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `anos` (integer/string ; format=int32, minimum=1, maximum=50, pattern=^-?(?:0|[1-9]\d*)$); `taxaJurosAnual` (number/string ; format=double, minimum=0, maximum=1000, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `estrategia` (string ; pattern=^(deterministic|montecarlo)$); `volatilidade` (null/number/string ; format=double, minimum=1, maximum=100, pattern=^-?(?:0|[1-9]\d*)(?:\.\d+)?$); `numeroSimulacoes` (null/integer/string ; format=int32, minimum=1, maximum=10000, pattern=^-?(?:0|[1-9]\d*)$)

### SolicitacaoAceiteConviteDto

`grupoId` (string; obrigatório no schema ; format=uuid); `conviteId` (string; obrigatório no schema ; format=uuid); `tokenHash` (null/string; obrigatório no schema); `tokenSupabase` (null/string; obrigatório no schema)

### SolicitacaoCadastroDto

`nome` (string; obrigatório no schema ; minLength=2, maxLength=120); `email` (string; obrigatório no schema ; minLength=0, maxLength=254); `senha` (string; obrigatório no schema ; minLength=0, maxLength=128)

### SolicitacaoLoginDto

`email` (string; obrigatório no schema ; minLength=0, maxLength=254); `senha` (string; obrigatório no schema ; minLength=0, maxLength=128)
