---
name: invest-feature-discovery
description: Explore a proposed Invest Dashboard feature or architecture decision, including backend-only, data, interface, or full-stack work, when scope and trade-offs need discovery.
---

# Descoberta de funcionalidade

Use para conceber uma funcionalidade ou avaliar alternativas que tenham decisões ou dependências relevantes. Investigue primeiro os arquivos, contratos, consumidores e restrições relacionados ao pedido. Diferencie fatos observados, hipóteses e dados ainda não disponíveis; não invente requisitos, fontes de dados ou evidências.

- Para backend ou dados, comece pelo domínio, contratos, persistência, integrações e efeitos observáveis pertinentes. Não proponha nem exija concepção de tela quando não houver interface no escopo.
- Para interface, considere usuário, ações e dados necessários; use [invest-screen-concept](../invest-screen-concept/SKILL.md) se a tarefa pedir concepção visual ou de interação.
- Para full-stack, ordene trabalho e validação pelas dependências reais entre domínio, API, persistência e consumidores; analise partes independentes em paralelo quando a sessão permitir.

Compare alternativas apenas quando esclarecer uma decisão, com custos, riscos e critério de escolha. Identifique critérios verificáveis e perguntas que realmente bloqueiam uma decisão; prossiga com suposições explícitas quando puderem ser resolvidas depois. Não exija número fixo de perguntas, artefatos ou aprovação para seguir o trabalho autorizado.

Para um plano ou decisão arquitetural solicitado ou pertinente, consulte respectivamente [plan-template.md](../../../templates/plan-template.md) ou [adr-template.md](../../../templates/adr-template.md) e `conductor/workflow.md`. Se o pedido for apenas de exploração ou plano, apresente a proposta sem implementar; se incluir implementação, prossiga conforme o escopo autorizado.
