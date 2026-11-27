# ADR-0006 — Assistente interno e política de provedores de IA

- Data: 2026-11-27
- Estado da decisão: proposta
- Implementação: não iniciada
- Validação: não executada
- Issue: [#78](https://github.com/JonathanBenicio/invest-dashboard/issues/78)
- História: [US-IA-001](../USER-STORIES.md#us-ia-001)
- Plano: [subplano](../plan/ia-renda-fixa/06-conversas-streaming.md)

## Contexto e problema

A arquitetura anterior descrevia Agent-System externo e SignalR. O épico requer paridade entre Ollama e OpenRouter e controle no dashboard.

## Decisão proposta

Orquestrar na Application e integrar por MEAI na Infrastructure. Entregar streaming por SSE autenticado e persistir o turno no PostgreSQL. Encaminhar por modelo e capacidade, com fallback antes da primeira saída e orçamento de nuvem.

## Alternativas, consequências e migração

Serviço externo facilita separar implantação, mas acrescenta dependência operacional. Orquestração interna reusa identidade e serviços de carteira. SSE usa a conexão HTTP por turno e requer desabilitar buffering na rota.

## Implementação, validação e gaps

Ver verificações e critérios do subplano. A revisão da ADR e os testes de execução estão pendentes.
