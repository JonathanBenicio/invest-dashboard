# Índice da documentação

Este índice reúne o material existente sem substituir documentos históricos. Situação ou conteúdo de planos e histórias deve ser confirmado em suas fontes; a presença no índice não significa aprovação ou implementação.

## Processo e contribuição

- [Workflow de documentação e implementação](../conductor/workflow.md)
- [Tracks e iniciativas](../conductor/tracks.md)
- [Índice Conductor](../conductor/index.md)
- [Templates](../templates/README.md)
- [Configuração e migração Codex](codex/codex-migration.md) — configurações e observações da migração; confirme o ambiente atual.
- [Auditoria do legado de agentes](codex/codex-legacy-audit.md) — auditoria de 11/11/2026.
- [Revisão das skills candidatas](codex/codex-skills-review.md) — revisão de 11/11/2026.
- [README do repositório](../README.md)

## Produto e requisitos registrados

- [Catálogo de User Stories](USER-STORIES.md)
- [Modelo de domínio](DOMAIN-MODEL.md) — revisão estática do checkout em 13/11/2026.
- [Contratos da API em português brasileiro](API-CONTRACTS-PT-BR.md) — consulte o código para confirmar o contrato vigente.
- [Cenários BDD: gestão de carteiras](features/01-gestao-carteiras.feature)
- [Cenários BDD: regras fiscais](features/02-regras-fiscais-ir.feature)
- [Feature: autenticação](features/authentication.feature)
- [Feature: carteira](features/portfolio.feature)

## Arquitetura e decisões

- [Diagrama de arquitetura](diagrams/architecture.md) — revisão estática do checkout em 13/11/2026.
- [Registro histórico 0001: adoção de ADRs](0001-adoption-of-adrs.md)
- [Registro histórico 0002: stack monorepo](0002-monorepo-tech-stack.md)
- [Registro histórico 0001: stack e padrão arquitetural](adrs/0001-core-tech-stack.md)
- [ADRs existentes](adrs/) — localização canônica atual para novos ADRs; mantida para evitar movimentação desnecessária.
- [Proposta: avaliações de renda fixa por posição](adrs/0002-position-scoped-valuations.md) — implementação validada localmente; status ainda proposto até revisão.

**Pendência editorial:** o registro de stack em `docs/0002-monorepo-tech-stack.md` e `docs/adrs/0001-core-tech-stack.md` se sobrepõe e ambos se declaram aceitos; além disso, o identificador `0001` é usado para decisões diferentes. Os caminhos são listados explicitamente para desambiguar a navegação. Nenhum registro foi renumerado, movido ou declarado substituído; resolver status e eventual consolidação exige decisão dos responsáveis.

## Planos existentes

- [Homologação de autenticação real](plan/autenticacao-real-plano.md) — bloqueado por preparação de contas/Dashboard; [issue #48](https://github.com/JonathanBenicio/invest-dashboard/issues/48).
- [Importação de extrato do Tesouro Direto](plan/tesouro-direto-extrato-plano.md) — planejado; [issue #53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- [Refinamento da importação e dos dados de mercado do Tesouro Direto](plan/tesouro-direto-importacao-e-dados-mercado-refinamento.md) — descoberta, dependências e decisões pendentes.

- [Plano de implementação da plataforma](plan/plan.md) (preexistente; confirmar premissas antes de reutilizar)
- [Plano de backend .NET e DDD](plan/dotnet-backend-ddd.md) — planejamento original; estado e premissas a confirmar.
- [Padronização de contratos e DTOs](plan/api-contracts-ptbr.md) — registro do plano; conferir estado atual no código e nos contratos.
- [Gestão de carteira](plan/carteira.md)
- [Fechamento dos gaps das telas que usavam mocks](plan/mock-era-parity.md)
- [Correções da revisão do PR #44](plan/pr-44-review-fixes.md) — registro de correções e validação de 05/10/2026.
- [Concluir o plano de posições](plan/complete-active-plan.md) — registro de acompanhamento e evidências históricas.
- [Gaps funcionais](plan/functional-gaps.md) — roadmap e pendências registradas.
- [Integração com dados reais](plan/real-data-integration.md) — histórico de integração e referências aos próximos passos.
- [Importação de CSVs de corretoras](plan/broker-csv-import-plan.md) — planejamento de adaptadores específicos.
- [Homologação de autenticação Supabase](plan/supabase-auth-homologation.md) — instruções de preparação e validação.
- [Backlog futuro de baixa prioridade](plan/future-low-priority.md) — itens sujeitos a priorização e definição de regras.
- [Chat IA](plan/chat-ia.md) — plano preexistente; não confundir com a versão OpenCode abaixo.
- Planos OpenCode, com premissas a confirmar: [Chat IA](plan/opencode/chat-ia.md), [convenção de nomes](plan/opencode/naming-convention.md) e [normalização de models e DTOs](plan/opencode/normalização-models-dtos.md).

## Relatórios de validação

- [Auditoria aprofundada da documentação e API](validation/auditoria-aprofundada.md) — resultados, decisões e limites em 13/11/2026.
- [Inventário de operações HTTP e OpenAPI](validation/contratos-api-inventario.md) — operações e schemas extraídos em host isolado; não substitui a verificação dos serviços e testes.
- [Comparação da documentação com o código](validation/comparacao-documentacao-codigo.md) — revisão estática, divergências e limites em 13/11/2026.
- [Homologação de autenticação real](validation/autenticacao-real-homologacao.md) — testes locais passaram; Supabase live e URL Configuration aguardam preparação/autenticação.
- [Correções da revisão do PR #44](validation/pr-44-review-fixes.md)

## Guias existentes

- [Apresentação da documentação](README.md)
- [Template documental legado](TEMPLATE.md)

## Requisitos históricos

- [Visão inicial do produto](historico/base.md) — requisitos registrados; não estabelecem comportamento atual.
- [Solicitação original de integração](historico/ORIGINAL_REQUEST.md) — pedido de 04/06/2026; não comprova implementação ou validação.
