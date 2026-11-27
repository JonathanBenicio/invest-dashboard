# AS-05 · Prints dentro do assistente

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#82](https://github.com/JonathanBenicio/invest-dashboard/issues/82)
- Page: [abrir plano](https://chatgpt.com/space/page_f31dc99d50e48191b2fb00c7f8537442)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: TD-02, AS-03 e IA-02
- Agentes: invest-api, invest-frontend, invest-test

## Objetivo e escopo

Prints dentro do assistente conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Entregar atalho /importar no primeiro recorte. Após TD-02, aceitar print em multipart no mesmo fluxo de prévia. Selecionar carteira com permissão de gestão e expor status e link autenticado.
- Em grupo consolidado pedir uma carteira explícita. Roteamento de visão é independente do chat. Guardar referência da prévia, sem cópia de imagem, e permitir novo envio após expiração.

## Critérios de aceite

- [ ] Testar arquivo válido e inválido, gestão, cancelamento, expiração e fallback.
- [ ] Acesso ao link é revalidado na abertura.
- [ ] Saldo permanece intacto até confirmação na importação.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
