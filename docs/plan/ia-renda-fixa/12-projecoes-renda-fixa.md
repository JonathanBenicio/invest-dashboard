# RF-02 · Projeções determinísticas

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#84](https://github.com/JonathanBenicio/invest-dashboard/issues/84)
- Page: [abrir plano](https://chatgpt.com/space/page_7330983eea7881919f41c7e7275900c8)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: RF-01 e regras oficiais verificadas
- Agentes: invest-backend, invest-database, invest-frontend, invest-test

## Objetivo e escopo

Projeções determinísticas conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Mapear taxas, convenções, fluxos, data, quantidade e dados de lotes. Implementar cálculo determinístico com decimal e arredondamento documentado. Ordenar prefixado, CDI/Selic, IPCA + taxa e Tesouro com aplicações individuais, conforme dados.
- Evoluir contrato aditivamente com valor/data observados, estimativas bruta e líquida opcional, método, fonte e premissas datadas. Diferenciar marcação a mercado e vencimento. Aplicar IR/IOF apenas com insumo e regra vigente verificada.

## Critérios de aceite

- [ ] Validar exemplos oficiais e fixtures de carência, cupom, calendário, resgate e arredondamento.
- [ ] Explicar premissas ausentes, sem preencher por inferência.
- [ ] Tela e ferramenta do assistente consomem o mesmo contrato.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
