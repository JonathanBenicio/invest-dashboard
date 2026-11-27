# ADR-0008 — Projeções de renda fixa por produto e lote

- Data: 2026-11-27
- Estado da decisão: proposta
- Implementação: não iniciada
- Validação: não executada
- Issue: [#84](https://github.com/JonathanBenicio/invest-dashboard/issues/84)
- História: [US-IA-005](../USER-STORIES.md#us-ia-005)
- Plano: [subplano](../plan/ia-renda-fixa/12-projecoes-renda-fixa.md)

## Contexto e problema

O saldo observado, a marcação a mercado e a estimativa no vencimento têm naturezas distintas. Dados contratuais e fiscais podem faltar.

## Decisão proposta

Calcular com decimal em C# por produto/lote, informar fonte e premissas datadas e disponibilizar líquido quando as condições e regras verificadas permitirem. Avaliar ML.NET offline e liberar sugestões apenas mediante ganho medido.

## Alternativas, consequências e migração

Projeções sem premissas ampliariam cobertura aparente e prejudicariam comparabilidade. Evoluir o contrato aditivamente e preservar valores observados.

## Implementação, validação e gaps

Ver verificações e critérios do subplano. A revisão da ADR e os testes de execução estão pendentes.
