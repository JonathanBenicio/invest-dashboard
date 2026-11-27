# RF-01 · Classificação e alertas por regras

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#83](https://github.com/JonathanBenicio/invest-dashboard/issues/83)
- Page: [abrir plano](https://chatgpt.com/space/page_2157cc8a28f08191883f9b6ff4b73115)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: EP-00, UX-01 para apresentação
- Agentes: invest-backend, invest-frontend, invest-test

## Objetivo e escopo

Classificação e alertas por regras conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Normalizar C# para Tesouro, CDB, LCI e LCA. Cobrir prefixado, percentual do CDI, CDI + spread, Selic e IPCA + taxa. Preservar texto original, evidência, taxa, unidade e vencimento. Separar classificação de tributação.
- Criar alertas explicáveis para escala, quantidade, data e fonte incompatível. Considerar movimentação, cupom e marcação a mercado antes de comparar. Preservar o dado observado.

## Critérios de aceite

- [ ] Fixtures de descrições ambíguas e ausência de taxa.
- [ ] Alertar escala incorreta e aceitar diferença explicada por movimentação.
- [ ] Registrar baseline de acertos e falsos alertas para RF-03.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
