# Índice da documentação

Este índice reúne o material existente sem substituir documentos históricos. Situação ou conteúdo de planos e histórias deve ser confirmado em suas fontes; a presença no índice não significa aprovação ou implementação.

## Processo e contribuição

- [Workflow de documentação e implementação](../conductor/workflow.md)
- [Tracks e iniciativas](../conductor/tracks.md)
- [Índice Conductor](../conductor/index.md)
- [Templates](../templates/README.md)
- [Configuração e migração Codex](codex-migration.md)
- [Auditoria do legado de agentes](codex-legacy-audit.md)
- [Revisão das skills candidatas](codex-skills-review.md)
- [README do repositório](../README.md)

## Produto e requisitos registrados

- [Catálogo de User Stories](USER-STORIES.md)
- [Modelo de domínio](DOMAIN-MODEL.md)
- [Cenários BDD: gestão de carteiras](01-gestao-carteiras.feature)
- [Cenários BDD: regras fiscais](02-regras-fiscais-ir.feature)
- [Feature: autenticação](features/authentication.feature)
- [Feature: carteira](features/portfolio.feature)

## Arquitetura e decisões

- [Diagrama de arquitetura](diagrams/architecture.md)
- [Registro histórico 0001: adoção de ADRs](0001-adoption-of-adrs.md)
- [Registro histórico 0002: stack monorepo](0002-monorepo-tech-stack.md)
- [Registro histórico 0001: stack e padrão arquitetural](adrs/0001-core-tech-stack.md)
- [ADRs existentes](adrs/) — localização canônica atual para novos ADRs; mantida para evitar movimentação desnecessária.
- [Proposta: avaliações de renda fixa por posição](adrs/0002-position-scoped-valuations.md) — implementação validada localmente; status ainda proposto até revisão.
- [ADR-0004: revisão auditável de divergências entre snapshots do Tesouro](adrs/0004-tesouro-reimportacao-snapshots.md) — decisão aceita; implementação e validações focadas no checkout.

**Pendência editorial:** o registro de stack em `docs/0002-monorepo-tech-stack.md` e `docs/adrs/0001-core-tech-stack.md` se sobrepõe e ambos se declaram aceitos; além disso, o identificador `0001` é usado para decisões diferentes. Os caminhos são listados explicitamente para desambiguar a navegação. Nenhum registro foi renumerado, movido ou declarado substituído; resolver status e eventual consolidação exige decisão dos responsáveis.

- [ADR-0005: consulta consolidada de investimentos](adrs/0005-consulta-consolidada-investimentos.md) — adotada, implementada e validada em testes focados.

## Planos existentes

- [Investimentos consolidados por grupo](plan/investimentos-consolidados-por-grupo.md) — integração PostgreSQL e Playwright focados aprovados; conferência autenticada e publicação no PR #62 pendentes.

- [Plano de implementação da plataforma](plan/plan.md) (preexistente; confirmar premissas antes de reutilizar)
- [Importação de extrato do Tesouro Direto](plan/tesouro-direto-extrato-plano.md) — iniciativa #53, em refinamento.
- [Extrato detalhado do Tesouro Direto](plan/tesouro-extrato-detalhado.md) — API e UI implementadas; leitura compartilhada e E2E responsivo validados.
- [Fluxo guiado e teste de integração da importação inicial do Tesouro Direto](plan/tesouro-teste-integracao-importacao-inicial.md) — integração, calendário mensal e fluxo guiado focados passaram; conferência autenticada manual pendente.
- [Refinamento da importação e dos dados de mercado do Tesouro Direto](plan/tesouro-direto-importacao-e-dados-mercado-refinamento.md) — regras de importação implementadas; avaliação de mercado permanece em trabalho futuro.
- [Reimportação auditável de snapshots do Tesouro Direto](plan/tesouro-reimportacao-snapshots.md) — Issues #55/#57/#58; inclui referência do HTML, revisão de divergências e encerramento comprovado por venda integral.
- [Gestão de carteira](plan/carteira.md)
- [Fechamento dos gaps das telas que usavam mocks](plan/mock-era-parity.md)
- [Correções da revisão do PR #44](plan/pr-44-review-fixes.md) — planejado; isolamento de extratos, avaliação cronológica e indicadores com cotação atual.
- [Chat IA](plan/chat-ia.md)
- [Planos OpenCode](plan/opencode/)

## Relatórios de validação

- [Fechamento da importação e do agrupamento em 26/11/2026](validation/fechamento-importacao-agrupamento-2026-11-26.md)
- [Importação e reimportação do Tesouro Direto em 25 e 26/11/2026](validation/tesouro-importacao-2026-11-25.md)
- [Correções da revisão do PR #44](validation/pr-44-review-fixes.md)

## Guias existentes

- [README da documentação (histórico)](README.md)
- [Template documental legado](TEMPLATE.md)
