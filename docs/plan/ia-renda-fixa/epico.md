# Épico de IA e renda fixa no Invest Dashboard

- Estado: em andamento
- Issue: [#71](https://github.com/JonathanBenicio/invest-dashboard/issues/71)
- Page: [abrir plano](https://chatgpt.com/space/page_3cca0ccf380c8191b6a02e59b06b7f53)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- ADRs: [assistente e provedores](../../adrs/0006-assistente-e-provedores-ia.md), [avaliação por print](../../adrs/0007-avaliacao-tesouro-por-print.md), [projeções](../../adrs/0008-projecoes-renda-fixa.md)
- Fontes: [Assistente](https://chatgpt.com/space/page_1277f245c874819180161c37edbd36b3) · [Prints](https://chatgpt.com/space/page_57fcb9aa019c8191bd64f5de3eef76fc) · [Renda fixa](https://chatgpt.com/space/page_8cb984bcffc4819195829d28e3ecec9f)
- Referências no repositório: [exports históricos das Pages](referencias/README.md)

## Objetivo e escopo

Entregar assistente interno com streaming e histórico, extração de prints de Tesouro para revisão humana e evolução determinística da renda fixa com experimentos ML.NET avaliados separadamente. A API ASP.NET Core usa orquestração na Application, MEAI na Infrastructure, PostgreSQL e React/Vite com SSE autenticado. Os provedores são Ollama e OpenRouter com paridade de chat/ferramentas e imagem/esquema. O administrador define prioridade e ativação. OpenRouter habilitado abrange a instalação, incluindo fallback e dados de grupo já autorizados ao usuário. Chaves diretas ficam para uma evolução posterior.

## Etapas e verificações

| Subplano | Depende de | Responsáveis |
|---|---|---|
| [EP-00 · Rastreabilidade e baseline](00-rastreabilidade.md) | — | Agente principal, invest-msbuild, invest-test |
| [UX-01 · Levantamento e desenho dos fluxos](01-descoberta-ux.md) | EP-00 | invest-frontend, invest-test |
| [IA-01 · Adaptadores, capacidades e Compose](02-provedores-ia.md) | EP-00 | invest-backend, invest-test |
| [IA-02 · Configuração, fallback e consumo](03-politica-provedores.md) | IA-01, UX-01 para a tela | invest-backend, invest-api, invest-database, invest-frontend, invest-security |
| [TD-01 · Extração e API de prévias](04-extracao-prints.md) | IA-01, IA-02 para nuvem | invest-backend, invest-api, invest-database, invest-test |
| [TD-02 · Revisão e registro de saldo](05-revisao-saldo.md) | TD-01, UX-01 | invest-frontend, invest-backend, invest-api, invest-test |
| [AS-01 · Conversas, histórico e streaming](06-conversas-streaming.md) | IA-01 | invest-backend, invest-database, invest-api |
| [AS-02 · Ferramentas de carteira e análises](07-ferramentas-financeiras.md) | AS-01, RF-02 para projeções avançadas | invest-backend, invest-api, invest-test |
| [AS-03 · Interface do assistente](08-interface-assistente.md) | UX-01, AS-01 e núcleo de AS-02 | invest-frontend, invest-test |
| [AS-04 · Carteiras compartilhadas e grupos](09-assistente-grupos.md) | AS-02, AS-03 e IA-02 | invest-backend, invest-security, invest-test |
| [AS-05 · Prints dentro do assistente](10-anexos-assistente.md) | TD-02, AS-03 e IA-02 | invest-api, invest-frontend, invest-test |
| [RF-01 · Classificação e alertas por regras](11-classificacao-alertas.md) | EP-00, UX-01 para apresentação | invest-backend, invest-frontend, invest-test |
| [RF-02 · Projeções determinísticas](12-projecoes-renda-fixa.md) | RF-01 e regras oficiais verificadas | invest-backend, invest-database, invest-frontend, invest-test |
| [RF-03 · Experimentos e decisão de ML.NET](13-experimentos-mlnet.md) | RF-01 e RF-02 | invest-backend, invest-test, invest-review |
| [QA-01 · Homologação e liberação](14-homologacao.md) | Por capacidade | invest-test, invest-review, invest-security, agente principal |

EP-00 concluiu a rastreabilidade e a baseline. UX-01 concluiu a auditoria e o handoff da experiência híbrida, validada em protótipo com dados sintéticos. As entregas estão no [PR #87](https://github.com/JonathanBenicio/invest-dashboard/pull/87). IA-01 e o núcleo determinístico de RF-01 podem avançar em paralelo. Liberar primeiro print local, depois chat local, nuvem e fallback, grupos e anexos. Renda fixa segue em trilha própria. A direção híbrida aprovada em UX-01 orienta a implementação visual. Verificar cada capacidade como descrito em QA-01.

## Critérios de aceite e validação

- [ ] Chat com resposta persistida igual ao histórico, ferramentas limitadas ao escopo e isolamento do grupo.
- [ ] Print com revisão e confirmação do saldo bruto de posição existente. Extrato permanece o fluxo para criar posições e conciliar movimentos.
- [ ] Ollama e OpenRouter aprovados por capacidade e fallback medido, com limites e telemetria.
- [ ] Classificação, alerta e projeção explicáveis. Modelos estatísticos entram mediante ganho medido.
- [ ] Issues, Pages, PRs e evidências ligados a partir dos subplanos.

Ver [baseline](../../validation/ia-renda-fixa-baseline-2026-11-27.md) e [QA-01](14-homologacao.md). Product Design, ImageGen e Playwright MCP dão suporte à descoberta e revisão visual. Playwright do repo sustenta a regressão executável. Pages, GitHub CLI e API REST de subissues sustentam a publicação.

## Riscos, gaps e entrega

Registrar modelos homologados no IA-01 e regras oficiais aplicáveis no RF-02. O [handoff de UX-01 no repositório](ux/auditoria.md) é a referência para a experiência híbrida, com as propostas, a escolha, os wireframes e a validação do protótipo. Habilitar provedores, grupos e anexos separadamente. Atualizar Issue, Markdown, Pages, índice, histórias e track ao disponibilizar cada resultado.
