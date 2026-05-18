# 1. Adoção de Architecture Decision Records (ADR)

* **Status:** Aceito
* **Data:** 2026-05-18

## Contexto e Definição do Problema

À medida que o projeto do Invest Dashboard cresce, decisões arquiteturais importantes sobre ferramentas, bibliotecas e infraestrutura são tomadas e, muitas vezes, esquecidas no histórico de conversas ou PRs. É necessário um método para registrar por que uma escolha técnica específica foi feita.

## Decisão

Decidimos adotar os Architecture Decision Records (ADRs) usando uma formatação em Markdown simplificada baseada no modelo proposto por Michael Nygard. Todos os ADRs ficarão versionados na pasta `docs/adr/`.

## Consequências

### Positivas
* **Histórico documentado:** Novos desenvolvedores e agentes IA poderão entender o "porquê" por trás da estrutura do projeto de forma rápida.
* **Alinhamento:** Facilita revisões assíncronas de mudanças de alto impacto.

### Negativas / Trade-offs
* **Manutenção extra:** Requer disciplina do time para escrever o documento sempre que houver uma decisão impactante.