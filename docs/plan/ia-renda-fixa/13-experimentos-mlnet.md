# RF-03 · Experimentos e decisão de ML.NET

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#85](https://github.com/JonathanBenicio/invest-dashboard/issues/85)
- Page: [abrir plano](https://chatgpt.com/space/page_84ae208511508191a3449663fe75003a)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: RF-01 e RF-02
- Agentes: invest-backend, invest-test, invest-review

## Objetivo e escopo

Experimentos e decisão de ML.NET conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Avaliar classificador SDCA contra regras com instituições fora do treino. Comparar detecção de picos e regime com alertas. Avaliar SSA de indexadores e estudar proventos separadamente quando houver histórico suficiente.
- Registrar antes do treino métricas, críticas e cortes. Aceitar classificação superior sem piora crítica, detecção superior com falsos alertas equivalentes ou menores, e projeção com erro menor e cobertura adequada. Registrar versões e habilitar apenas pilotos aprovados como sugestões.

## Critérios de aceite

- [ ] Relatório reproduzível por experimento com referência, métricas e decisão.
- [ ] Sem ganho, permanece a regra determinística.
- [ ] Modelo não autoriza avaliação nem calcula imposto.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
