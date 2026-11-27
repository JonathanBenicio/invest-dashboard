> Exportação histórica de [Renda fixa com classificação, alertas e projeções avaliadas com ML.NET](https://chatgpt.com/space/page_8cb984bcffc4819195829d28e3ecec9f). Para o escopo e as decisões vigentes, consulte o [épico e os subplanos](../epico.md).

**Planejado.** Baseline de 27/11/2026 em `codex/tesouro-previa-e-cotacoes`, commit `d683cfe`. Issue, ADR, histórias, PR e evidências de implementação pendentes. O checkout já tinha `.codex-attachments/` sem rastreamento. Destino futuro no repositório em `docs/plan/renda-fixa-classificacao-anomalias-projecoes.md`.

## Objetivo e escopo

Evoluir a renda fixa com classificação de títulos, alertas de inconsistência e cenários de vencimento. A tela de renda fixa e a projeção já existem. No checkout, a projeção tem condições específicas de taxa, convenção e base observada e informa que a projeção de Tesouro depende das taxas de cada aplicação. As novas capacidades publicam resultados determinísticos para o dashboard e, quando disponíveis, para consulta pelo assistente interno. [Projeção atual em C#](https://github.com/JonathanBenicio/invest-dashboard/blob/d683cfe/src/InvestDashboard.Application/Services/CarteiraAppService.cs#L161) · [Rascunho](https://chatgpt.com/space/page_85f156fd618481918d068ae937910f24)

## Implementação

1. **Normalizar e classificar.** Começar com regras auditáveis para Tesouro, CDB, LCI e LCA, cobrindo prefixado, percentual do CDI, CDI mais spread, Selic e IPCA mais taxa. Separar produto, indexador, taxa, convenção, vencimento e tratamento tributário. Quando a descrição não sustentar um campo, deixá-lo pendente de revisão. Publicar a sugestão com origem e justificativa para reuso no cadastro ou na importação. Comparar `SdcaMaximumEntropy` com as regras em conjunto rotulado por produto e indexador, com instituições fora do treino. Medir precisão por classe e a proporção de casos resolvidos ou encaminhados a revisão.

2. **Sinalizar inconsistências.** Usar primeiro regras para quantidade, escala, ausência de referência e incompatibilidade de fonte ou data. Separar posição observada de cotação. Comparar séries somente dentro de uma semântica equivalente e considerar aportes, resgates, cupons e marcação a mercado. Avaliar `DetectIidSpike` e mudança de patamar offline, com séries adequadas e erros rotulados. Medir erros encontrados e falsos alertas por classe. Mostrar aviso explicável e permitir revisão. Preservar o valor observado na auditoria. [Detecção de anomalias no ML.NET](https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/sales-anomaly-detection)

3. **Estimar cenários.** Evoluir o contrato atual de projeção com valor observado e sua data, tipo de valor, premissas datadas, vencimento, método, estimativa bruta e estimativa líquida opcional. Calcular em C# por produto e aplicação, com taxa contratual, convenção de dias e tratamento de eventos. Separar marcação a mercado de evolução contratual até o vencimento. Calcular IR/IOF somente quando os dados por lote e as regras efetivas permitirem. Quando faltar uma premissa, indicar qual dado falta. Validar regras, convenções e exemplos de referência oficiais aplicáveis antes da implementação financeira. Usar séries oficiais com código, frequência, unidade e data de referência registrados. [Série Selic do SGS](https://dadosabertos.bcb.gov.br/dataset/11-taxa-de-juros---selic)

4. **Avaliar séries temporais.** Dependendo do volume e da qualidade do histórico, testar `ForecastBySsa` para cenários de indexadores e, em estudo separado, para proventos. Separar treino e avaliação por tempo e medir erro e cobertura dos intervalos contra referências simples. Converter premissas de cenário em valores pela calculadora C#. Liberar modelos em modo piloto somente quando melhorarem a referência nas métricas combinadas. Manter os cenários determinísticos disponíveis. [Transformações de séries no ML.NET](https://learn.microsoft.com/en-us/dotnet/machine-learning/resources/transforms#time-series-data-transformations)

## Critérios de aceite e validação

- [ ] Regras recuperam corretamente os campos de exemplos rotulados e preservam a revisão de nomenclaturas ambíguas. Usar fixtures sintéticas, com variantes de escrita e casos em que taxa, produto ou vencimento faltam.

- [ ] Alertas detectam erros sintéticos de escala e quantidade e tratam separadamente mudanças explicadas por movimentações e marcação a mercado. Métricas de modelos são comparadas com as regras antes de habilitá-los.

- [ ] Casos de referência cobrem carência, cupom, vencimento, taxa desatualizada, dias e arredondamento. Testar projeções por produto e indisponibilidade dos dados exigidos para o líquido. Conferir contrato HTTP e regressão da tela de renda fixa com valores observados e projetados lado a lado.

- [ ] Registrar dados usados, versão das regras e dos modelos, períodos de avaliação e critérios para manter ou retirar cada piloto. Usar PostgreSQL isolado se houver persistência nova e validar tela em celular e desktop.

Antes da implementação, registrar baseline dos testes focados. Depois de cada entrega, repetir os testes afetados e anexar resultados à issue. Builds e testes de implementação pendentes. Ambiente observado com Bun 1.3.4 e Docker Compose v5.5.1. O `dotnet` precisa ser disponibilizado no shell ou em ambiente de build equivalente.

## Dependências e liberação

As regras determinísticas podem avançar em paralelo com o assistente e a importação de prints. A detecção estatística depende de histórico comparável. A projeção líquida depende de dados contratuais e fiscais suficientes por lote. O piloto de ML.NET depende de bases rotuladas, séries avaliáveis e métricas registradas. Reaproveitar os serviços existentes na tela e no assistente, com liberação independente de cada capacidade. Ao implementar, sincronizar issue, ADR quando aplicável, histórias, plano canônico, evidências e índices.
