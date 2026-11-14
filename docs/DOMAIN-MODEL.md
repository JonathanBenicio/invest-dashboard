# Modelo de domínio — Invest Dashboard

Revisão estática em 13/11/2026, no checkout `f85ae79` com alterações locais. Este resumo descreve o código encontrado, não comprova execução nem correção de regras fiscais. Os requisitos e planos históricos estão no [índice](INDEX.md).

## Carteiras, posições e operações

- [`Carteira`](../src/InvestDashboard.Domain/Aggregates/Portfolio/Carteira.cs) é uma raiz de agregado que mantém usuário, grupo e titular, instituição, visibilidade, saldo, resultados realizados e posições. Os valores são `decimal`; nomes de propriedades do código ainda misturam português e inglês.
- [`PosicaoInvestimento`](../src/InvestDashboard.Domain/Aggregates/Portfolio/PosicaoInvestimento.cs) associa carteira e ativo e registra ticker, tipo, quantidade, custo médio, custo total, preço e datas. Para renda fixa, a avaliação usa o preço da posição; para outros ativos, usa o preço do ativo quando disponível. [`ValuacaoPosicao`](../src/InvestDashboard.Domain/Aggregates/Portfolio/ValuacaoPosicao.cs) registra preço e quantidade observados por posição.
- [`Transacao`](../src/InvestDashboard.Domain/Aggregates/Trading/Transacao.cs) registra autor, carteira, ativo, quantidade, preço, taxa e data. O enum [`TipoTransacao`](../src/InvestDashboard.Domain/Aggregates/Trading/TipoTransacao.cs) contém `Buy`, `Sell`, `Deposit` e `Withdrawal`; vencimento é marcado na posição, não é um tipo desse enum. A API de registro valida `Buy` e `Sell` no [DTO](../src/InvestDashboard.Application/DTOs/Trading/RegistrarTransacaoDto.cs).
- Na compra, o agregado adiciona quantidade e recalcula custo médio com taxas. Na venda, verifica quantidade disponível e registra custo e ganho realizados. Associações e autorização de acesso por grupo são tratadas também na camada de aplicação.

## Fiscal e simulação

- [`CalculoImpostoService`](../src/InvestDashboard.Domain/Services/CalculoImpostoService.cs) recebe valores e prejuízos anteriores como parâmetros e calcula resultados para swing trade, FII e day trade. A aplicação expõe uma estimativa mensal de escopo limitado; consulte [US-TAX-002](USER-STORIES.md#us-tax-002--imposto-de-renda) para exclusões e pendências. Não se deve inferir emissão de DARF ou persistência de um `TaxProfile` desse serviço. A regra exige revisão especializada antes de uso fiscal.
- [`EstrategiaDeterministica`](../src/InvestDashboard.Domain/Services/EstrategiaDeterministica.cs) usa taxa anual e aportes informados para calcular pontos mensais. [`EstrategiaMonteCarlo`](../src/InvestDashboard.Domain/Services/EstrategiaMonteCarlo.cs) usa volatilidade informada ou padrão, número de simulações e semente fixa; a série retornada usa a mediana. O código calcula P5 e P95 localmente, mas não os inclui no resultado. Não foi identificada leitura de volatilidade histórica nessas estratégias.

## Dados externos

O código inclui provedor de cotações e consulta de CDI; a [análise de posições](../frontend/src/pages/tools/Analysis.tsx) utiliza o benchmark CDI. A simulação recebe sua taxa nos parâmetros, sem ligação automática às taxas persistidas. Disponibilidade e homologação de integrações externas não são demonstradas por esta revisão estática; consulte os [planos e relatórios](INDEX.md).
