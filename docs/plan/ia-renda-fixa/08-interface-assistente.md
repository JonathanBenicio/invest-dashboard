# AS-03 · Interface do assistente

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#80](https://github.com/JonathanBenicio/invest-dashboard/issues/80)
- Page: [abrir plano](https://chatgpt.com/space/page_aaac71bf156481919f010996a528ae18)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: UX-01, AS-01 e núcleo de AS-02
- Agentes: invest-frontend, invest-test

## Objetivo e escopo

Interface do assistente conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Adicionar /assistente em TanStack Router com atalho pela carteira e menu. Reusar tema e componentes. Usar TanStack Query para persistência remota e estado local para o stream ativo.
- Implementar histórico, seletor, composer, indicação do provedor, cancelamento e erros. Sanitizar Markdown e renderizar cartões de resultados tipados. Reconciliar abort do fetch com o estado persistido.
- Seguir direção escolhida em UX-01 e validar integração com navegação e layout reais.

## Critérios de aceite

- [ ] Testar abertura, envio, streaming, interrupção e reabertura.
- [ ] Verificar foco, teclado, tema escuro, tabelas longas, 320 px e desktop.
- [ ] Validar base de GitHub Pages e Capacitor conforme a superfície.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
