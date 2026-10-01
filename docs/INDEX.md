# Índice da documentação

Este índice reúne o material existente sem substituir documentos históricos. Situação ou conteúdo de planos e histórias deve ser confirmado em suas fontes; a presença no índice não significa aprovação ou implementação.

## Processo e contribuição

- [Workflow de documentação e implementação](../conductor/workflow.md)
- [Tracks e iniciativas](../conductor/tracks.md)
- [Índice Conductor](../conductor/index.md)
- [Templates](../templates/README.md)
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

**Pendência editorial:** o registro de stack em `docs/0002-monorepo-tech-stack.md` e `docs/adrs/0001-core-tech-stack.md` se sobrepõe e ambos se declaram aceitos; além disso, o identificador `0001` é usado para decisões diferentes. Os caminhos são listados explicitamente para desambiguar a navegação. Nenhum registro foi renumerado, movido ou declarado substituído; resolver status e eventual consolidação exige decisão dos responsáveis.

## Planos existentes

- [Plano de implementação da plataforma](plan/plan.md) (preexistente; confirmar premissas antes de reutilizar)
- [Gestão de carteira](plan/carteira.md)
- [Fechamento dos gaps das telas que usavam mocks](plan/mock-era-parity.md)
- [Chat IA](plan/chat-ia.md)
- [Planos OpenCode](plan/opencode/)

## Relatórios de validação

Ainda não há relatórios de validação separados. Quando uma validação exigir um documento próprio, crie o relatório em uma pasta `validation/` e adicione-o a este índice usando [templates/validation-report-template.md](../templates/validation-report-template.md). A pasta não é criada enquanto estiver vazia.

## Guias existentes

- [README da documentação (histórico)](README.md)
- [Template documental legado](TEMPLATE.md)
