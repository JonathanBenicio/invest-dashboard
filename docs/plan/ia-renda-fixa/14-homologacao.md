# QA-01 · Homologação e liberação

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#86](https://github.com/JonathanBenicio/invest-dashboard/issues/86)
- Page: [abrir plano](https://chatgpt.com/space/page_f2e45075c92081919c2b2981041858ee)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: Por capacidade
- Agentes: invest-test, invest-review, invest-security, agente principal

## Objetivo e escopo

Homologação e liberação conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Validar flags e queda de cada provedor nos fluxos de chat e print. Comparar clientes controlados e reais com dados sintéticos. Medir correção, latência fria/quente, custo, tokens e erros.
- Verificar PostgreSQL isolado, autorização de grupos, orçamento, ausência de avaliação antes do aceite e regressão da importação HTML. Rodar Playwright responsivo e design-qa após implementação de cada tela.
- Liberar primeiro print local, depois assistente local, OpenRouter/fallback, grupos e anexos. Renda fixa segue trilha independente.

## Critérios de aceite

- [ ] PR de cada capacidade tem build, teste de integração HTTP e critérios vinculados.
- [ ] Evidências sanitizadas registram comandos, ambiente e SHA.
- [ ] Flags independentes e rollback operacional exercitados.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
