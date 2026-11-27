# AS-01 · Conversas, histórico e streaming

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#78](https://github.com/JonathanBenicio/invest-dashboard/issues/78)
- Page: [abrir plano](https://chatgpt.com/space/page_a399189001b48191a8bca3d12be985e4)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: IA-01
- Agentes: invest-backend, invest-database, invest-api

## Objetivo e escopo

Conversas, histórico e streaming conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Criar POST/GET /api/v1/ia/conversas, GET/DELETE /{id} e POST /{id}/mensagens com SSE. Persistir dono, escopo, mensagem, turno, tentativa, prazo e identificador idempotente. Garantir um turno ativo por conversa.
- Persistir o turno antes da inferência e resposta final antes da conclusão. Reenvio devolve estado ou resultado já persistido. Cancelamento e desconexão encerram inferência. Recuperar turno abandonado pelo prazo persistido, sem repetição implícita.
- Emitir turno, provedor, delta, ferramenta, concluido e erro. Permitir erro terminal após início da resposta. Usar fetch autenticado e desabilitar buffering do proxy só na rota SSE.

## Critérios de aceite

- [ ] Texto final exibido coincide com histórico.
- [ ] Testar reenvio, concorrência, desconexão, falha e flush pelo proxy.
- [ ] Provar exclusão pelo dono e recuperação após interrupção.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
