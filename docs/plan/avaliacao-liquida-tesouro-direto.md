# Plano — avaliação atual de posições do Tesouro Direto

- Estado: em andamento em branch separada; fora do escopo do PR #62.
- Issue: [#64 — Tesouro Direto: avaliar posição com cotação pública e líquido estimado](https://github.com/JonathanBenicio/invest-dashboard/issues/64), relacionada à iniciativa [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: [0003 — avaliação de mercado do Tesouro Direto](../adrs/0003-tesouro-market-valuation.md), proposta e sujeita à revisão.
- Story: [US-PORT-003](../USER-STORIES.md#us-port-003--avaliação-atual-de-posições-do-tesouro-direto), proposta.
- Baseline: branch base `codex/tesouro-previa-e-cotacoes`, HEAD `8ff9471`; trabalho separado em `codex/tesouro-avaliacao-liquida`, no mesmo checkout e sem worktree adicional.
- Plano canônico: `docs/plan/avaliacao-liquida-tesouro-direto.md`.

## Objetivo, baseline e escopo

- Problema observado: a cotação pública e o extrato do investidor têm fontes e datas diferentes. O extrato informa posição, impostos e taxas na data do arquivo; o PU base público pode servir de referência bruta mais recente.
- Resultado esperado: usar a cotação oficial associada ao título para atualizar o valor bruto de referência da posição e da carteira, mantendo o ledger e o valor histórico do extrato separados. Apresentar líquido estimado somente quando a posição, os tributos e todos os custos atuais puderem ser calculados sem lacunas.
- Incluído: associação por tipo de título e vencimento; consulta do PU base histórico mais recente do Tesouro Transparente; valor bruto `PU × quantidade` para posição e agregados da carteira; indicação de fonte/data; exibição dos componentes de imposto/taxa do extrato com suas datas; cálculo de IR/IOF por lote quando os dados forem suficientes; motivo explícito quando líquido não estiver disponível.
- Excluído: tratar 22% como alíquota universal; usar dias úteis como prazo fiscal; descontar taxa antiga do extrato como se fosse custo atual; alterar o ledger ou sobrescrever o custo de aquisição; apresentar cotação como garantia de preço de venda; comparação de histórico com CDI/outros investimentos; importação XLSX.
- Dependências: identidade, quantidade, data e custo de cada lote; operações posteriores e vendas parciais; cupons e eventos fiscais; regras vigentes de IR, IOF e custódia; taxa da instituição em vigor; cache e data de atualização nos totais de carteira.

## Direção de produto registrada

- O usuário aceitou a proposta de mostrar PU base público multiplicado pela quantidade como valor bruto de referência atual e manter os impostos/taxas extraídos acompanhados da data de origem.
- Os valores do extrato não serão apresentados como encargos atuais. Se não houver dados contemporâneos e suficientes para recalcular todos os componentes, o líquido atual ficará indisponível, acompanhado do motivo.
- A alíquota `aliquotaIr` e `diasCorridos` do extrato são a evidência inicial da faixa fiscal na data do arquivo. Se o extrato já informa 15%, essa é a menor faixa e continua aplicável; não é necessário reconstituir o prazo desde a compra para escolher a alíquota. Para faixas maiores, atualizar a idade usando os dias corridos transcorridos desde a data do extrato até a cotação, pois a faixa pode cair com o tempo. Se faltarem alíquota, idade ou datas necessárias, não estimar IR.
- “Dias úteis × taxa de 22%” fica como hipótese original a validar, não como regra aprovada. As fontes oficiais consultadas indicam IR regressivo por prazo em dias corridos: 22,5% até 180 dias, 20% entre 181 e 360, 17,5% entre 361 e 720 e 15% acima de 720. O IOF pode incidir sobre o rendimento nos primeiros 30 dias. Revalidar regras e vigência antes de concluir o cálculo.
- A custódia B3 é descrita oficialmente como 0,2% a.a. provisionada diariamente desde D+1, com exceções por título. A taxa da instituição é acordada com o agente e pode variar. O denominador e as regras por tipo devem ser confirmados antes de codificar o valor; não presumir uma fórmula única.
- Títulos com cupons não receberão estimativa líquida enquanto cupons pagos e bases de tributação não estiverem reconciliados por lote.

## Etapas e verificações

| Etapa/entrega | Dependências | Verificação observável | Estado e evidência |
|---|---|---|---|
| [ ] Aprovar o modelo de cotação de mercado separado do ledger | Issue #64, ADR-0003 e avaliações por posição existentes | Revisão confirma uso de cotação como valor de mercado, com origem/data, sem gravar transação ou substituir custo/extrato | Em andamento; ADR proposta |
| [ ] Associar título e PU público à posição | Mapeamento de tipo/vencimento e CSV oficial | Teste de contrato cobre título correspondente, vencimento divergente, cotação ausente e posição sem extrato | Parcial em branch separada; teste pendente |
| [ ] Aplicar valor bruto a posição e carteira | Associação e semântica de agregação | Testes demonstram `PU × quantidade atual` no detalhe e nos agregados, após compra/venda, sem modificar o ledger | Pendente; branch atual cobre somente o detalhe |
| [ ] Refinar método fiscal e de custos | Regras oficiais e dados do HTML/ledger | Matriz de alíquota, calendário, base, custos, vigência, cupons e arredondamento por tipo de título | Pendente |
| [ ] Reconciliar lotes e operações | Parser HTML, base temporal, ledger | Quantidade, data e custo conferidos por lote; compras, vendas parciais e cupons considerados; qualquer lacuna bloqueia líquido | Pendente |
| [ ] Calcular IR/IOF por lote usando a faixa do extrato | `aliquotaIr`, `diasCorridos`, datas do extrato/aplicação e cotação | 15% permanece no piso; faixas maiores avançam somente por dias corridos; limites 180/181, 360/361, 720/721 e IOF até 30 dias | Planejado; cálculo pausado |
| [ ] Calcular líquido quando todos os custos forem atuais | Método fiscal aceito e dados completos | Testes sem ganho, custo desconhecido, taxa histórica, cupom e movimento posterior | Pendente |
| [ ] Integrar interface acessível e estados de erro/defasagem | Contrato de avaliação e consulta | Tela exibe bruto, PU, fonte/data, componentes do extrato com data e indisponibilidade do líquido sem chamar valor antigo de atual | Parcial em branch separada; revisão encontrou ajustes de data e cache pendentes |
| [ ] Validar autorização, carteira e regressões | Implementação completa | Teste multiusuário, atualização após movimento e verificação de posição/carteira contra PostgreSQL | Pendente |

## Critérios de aceite

- [ ] A cotação pública correspondente ao tipo e vencimento fornece PU base e valor bruto pela quantidade atual, mesmo sem novo extrato; evidência: teste HTTP e teste de interface.
- [ ] O valor bruto público é usado no detalhe da posição e no cálculo agregado da carteira sem alterar custo de aquisição, histórico do extrato ou transações; evidência: testes de consulta/persistência.
- [ ] Tributos e taxas extraídos são apresentados como valores do extrato e com a data de referência; evidência: teste de contrato e revisão da tela.
- [ ] IR/IOF por lote seguem as regras vigentes e usam prazo em dias corridos; evidência: matriz oficial e testes de limites.
- [ ] A faixa `aliquotaIr` do extrato é usada como referência da data do arquivo; 15% permanece no piso, e outras faixas só avançam com dias corridos entre extrato e cotação. Evidência: testes de transição e de dados ausentes.
- [ ] Líquido atual só é mostrado quando os custos aplicáveis são contemporâneos e calculáveis. Taxas de extrato antigas não são reutilizadas como taxas atuais; evidência: testes de dados incompletos e taxa ausente.
- [ ] Cupons não reconciliados, operação posterior à base, lote incompleto ou quantidade divergente bloqueiam o líquido com motivo específico; evidência: testes direcionados.
- [ ] Data da cotação civil não sofre deslocamento de fuso e a consulta é atualizada após compra/venda; evidência: testes frontend com data `DateOnly` e invalidação de query.
- [ ] A API não revela posições de usuário/carteira sem autorização; evidência: testes de isolamento.

## Validação

Ambiente e versões: WSL sem `dotnet` e Bun no host; SDK .NET 10.0.401 executado em contêiner. Fontes oficiais consultadas em 17/11/2026 e revalidadas nesta análise em 18/11/2026. O teste de criação HTML não cobre ainda o endpoint novo de cotação/avaliação.

| Verificação | Comando/método e ambiente | Estado | Evidência/limitação |
|---|---|---|---|
| Tributação de renda fixa | Consulta à tabela Receita 2026, IN RFB nº 1.585/2015 e Tesouro Direto | passou | Fontes oficiais confirmam faixas de IR, base após IOF e tributos/taxas do produto; o cálculo exato por lote permanece sujeito à revisão. |
| Revisão independente de API | Agente explorer, leitura somente da branch | passou | Identificou dupla tributação possível de cupons e uso indevido de taxa histórica como atual; os critérios acima incorporam os gaps. |
| Revisão independente de frontend | Agente explorer, leitura somente da branch | passou | Identificou data `DateOnly` deslocada por fuso, query de cotação desatualizada após movimento e falta de composição/data do IR/IOF no líquido. |
| Build backend | `docker run` com SDK 10.0.401 e `dotnet build src/InvestDashboard.slnx --no-restore` | passou | 0 warnings e 0 errors; compilação em contêiner com cache NuGet dedicado. |
| Criação inicial HTML | `dotnet test src/tests/InvestDashboard.IntegrationTests/InvestDashboard.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~HtmlCriaPosicaoInicial_ConfirmaSaldoBaseERegistraEventoOficial`, PostgreSQL 15 temporário isolado | passou | 1 teste passou, 0 falhou, 0 ignorados; cobre confirmação HTML, ativo, saldo-base, posição e evento oficial. Não valida a nova avaliação por cotação. |
| Build/frontend | Não executado | capacidade ausente | Bun não está instalado no host e não foi feita instalação. Revisar TypeScript/UI antes de concluir. |

## Riscos, gaps e pendências

| Item | Impacto | Mitigação/ação | Referência/estado |
|---|---|---|---|
| Prazo fiscal confundido com dias úteis de mercado | Alíquota errada | Separar prazo fiscal em dias corridos da contagem de dias úteis usada no apreçamento, quando aplicável | Método pendente |
| Taxas do extrato estão defasadas | Líquido atual enganoso | Mostrar como snapshot; só subtrair custos contemporâneos calculáveis | Código local requer correção |
| Cupom não aparece no ledger ou não está alocado ao lote | IR calculado duas vezes | Bloquear líquido até haver reconciliação dos cupons e ajuste da base | Gap registrado por revisão da API |
| Eventos posteriores podem alterar quantidade/base por lote | Posição fiscal incorreta | Detectar operações posteriores e indicar indisponibilidade até conciliar | Pendente |
| Data civil é formatada em fuso local como timestamp UTC | Data pode aparecer no dia anterior | Formatar `DateOnly` sem conversão de fuso | Gap registrado por revisão frontend |
| Cotação pode ficar em cache após compra/venda | Quantidade × PU pode usar quantidade antiga | Invalidar avaliação ao atualizar transações e recalcular com a quantidade atual | Gap registrado por revisão frontend |
| Cobrança e exceções de custódia mudam | Estimativa desatualizada | Registrar regra/fonte/vigência e bloquear onde a regra não está suportada | Pendente |
| PU público pode diferir do preço efetivo de recompra | Usuário interpreta valor bruto como liquidação garantida | Rotular como referência de mercado com origem/data | Pendente |

## Entrega e sincronização

- Implementação/PR/commits: implementação bruta e snapshot de extrato em commit parcial na branch `codex/tesouro-avaliacao-liquida`; PR separado ainda não criado. O PR #62 permanece fora deste escopo.
- Relatório/evidências: pendente de implementação e validação.
- Limitações restantes: regras/custos por título e dados de cupom ainda não permitem garantir líquido atual; os agregados de carteira ainda não usam a cotação pública.
- Índices: plano será ligado ao Issue #64, Story US-PORT-003, ADR-0003 e track #64; PR futuro separado de #62.

## Fontes oficiais consultadas

- [Receita Federal — Tributação de 2026, rendimentos de aplicações de renda fixa](https://www.gov.br/receitafederal/pt-br/assuntos/meu-imposto-de-renda/tabelas/2026).
- [Receita Federal — IN RFB nº 1.585/2015](https://normas.receita.fazenda.gov.br/sijut2consulta/link.action?idAto=67494).
- [Tesouro Direto — Impostos e taxas](https://www.tesourodireto.com.br/b/impostos-e-taxas-no-tesouro-direto).
- [Tesouro Direto — Regras e regulamento](https://www.tesourodireto.com.br/sobre-o-tesouro/regras-e-regulamento).
- [Tesouro Transparente — taxas e preços ofertados pelo Tesouro Direto](https://www.tesourotransparente.gov.br/ckan/dataset/taxas-dos-titulos-ofertados-pelo-tesouro-direto).
