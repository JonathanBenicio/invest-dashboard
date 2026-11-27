# AS-04 · Carteiras compartilhadas e grupos

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#81](https://github.com/JonathanBenicio/invest-dashboard/issues/81)
- Page: [abrir plano](https://chatgpt.com/space/page_e91d21c01b8081919b3d90c095351296)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: AS-02, AS-03 e IA-02
- Agentes: invest-backend, invest-security, invest-test

## Objetivo e escopo

Carteiras compartilhadas e grupos conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Persistir escopo explícito carteira própria, compartilhada ou grupo. Reusar associação ativa, papel, visibilidade e titularidade. Limitar o consolidado às carteiras acessíveis.
- Registrar fontes de cada resultado, inclusive agregado. Validar novamente antes de devolver ou reutilizar conteúdo. Se uma fonte perder acesso, bloquear leitura e continuação e manter exclusão pelo dono.
- Aplicar as mesmas regras para ambos os provedores e minimizar campos enviados.

## Critérios de aceite

- [ ] Cobrir grupo distinto, membro inativo, carteira privada, troca de papel e revogação posterior.
- [ ] Total coincide apenas com as carteiras visíveis.
- [ ] Histórico antigo não contorna a autorização atual.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
