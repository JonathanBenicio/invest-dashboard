# ADR-0007 — Avaliação de posição do Tesouro por print

- Data: 2026-11-27
- Estado da decisão: proposta
- Implementação: não iniciada
- Validação: não executada
- Issue: [#77](https://github.com/JonathanBenicio/invest-dashboard/issues/77)
- História: [US-IA-004](../USER-STORIES.md#us-ia-004)
- Plano: [subplano](../plan/ia-renda-fixa/05-revisao-saldo.md)

## Contexto e problema

Um print pode ter saldo ambíguo, quantidade ausente e períodos diferentes para saldo e rentabilidade. A revisão HTML tem semântica de conciliação.

## Decisão proposta

Persistir prévia estruturada sem a imagem. A confirmação humana grava, sobre posição existente, apenas o saldo bruto observado, com data e proveniência. Revalidar permissão, versão, compatibilidade e idempotência na transação.

## Alternativas, consequências e migração

Atualização automática reduziria cliques e poderia converter leitura ambígua em valor gravado. Reaproveitar a avaliação existente preserva a conciliação de snapshots e permite reversão por desativação do fluxo.

## Implementação, validação e gaps

Ver verificações e critérios do subplano. A revisão da ADR e os testes de execução estão pendentes.
