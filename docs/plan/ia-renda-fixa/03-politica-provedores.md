# IA-02 · Configuração, fallback e consumo

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#75](https://github.com/JonathanBenicio/invest-dashboard/issues/75)
- Page: [abrir plano](https://chatgpt.com/space/page_e2d8d87a2c1481919b64b7d0e563635c)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: IA-01, UX-01 para a tela
- Agentes: invest-backend, invest-api, invest-database, invest-frontend, invest-security

## Objetivo e escopo

Configuração, fallback e consumo conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Reusar Auth:AdminUserIds. Expor GET/PUT /api/v1/ia/configuracao e POST /api/v1/ia/configuracao/{provedor}/teste. Persistir configuração concorrente com versão. Exibir chave configurada ou ausente e permitir rotação e remoção explícita. Manter destinos de teste aprovados e chave criptografada com chave do servidor.
- Selecionar o primeiro provedor habilitado e apto para a capacidade. Em indisponibilidade ou erro transitório antes do primeiro conteúdo, permitir uma tentativa no alternativo. Entrada inválida, autorização negada ou cancelamento encerram o pedido. Após começar o texto, sinalizar falha no turno. Para imagem, concluir fallback antes de persistir.
- Reservar orçamento de nuvem de forma atômica antes da chamada. Se faltar orçamento, usar Ollama elegível. Registrar versão de configuração, tentativa, provedor, motivo, latência, tokens e custo. Configuração de nuvem aplica-se à instalação e é indicada na UI.

## Critérios de aceite

- [ ] Inverter ordem e desligar cada provedor em chat e imagem.
- [ ] Testar cota concorrente, segredos, admin de grupo, conflito de versão, e falha antes/depois da primeira saída.
- [ ] Um pedido produz no máximo um resultado persistido.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
