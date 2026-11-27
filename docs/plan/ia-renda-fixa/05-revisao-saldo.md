# TD-02 · Revisão e registro de saldo

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#77](https://github.com/JonathanBenicio/invest-dashboard/issues/77)
- Page: [abrir plano](https://chatgpt.com/space/page_9e913433eaf08191a0d4568bb8c87954)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: TD-01, UX-01
- Agentes: invest-frontend, invest-backend, invest-api, invest-test

## Objetivo e escopo

Revisão e registro de saldo conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Adicionar fluxo na aba Tesouro de /importar. Permitir edição e escolha explícita de posição existente. Para rótulo genérico exigir verificação do saldo bruto. Se quantidade ausente, exigir confirmação de cobertura da posição. Divergência direciona ao extrato.
- Criar POST /{previaId}/confirmacao. Revalidar autor, gestão, versão, título, vencimento, posição aberta, compatibilidade de quantidade e data. Recusar observação anterior à base ativa ou avaliação mais recente e divergência no mesmo dia.
- Reusar avaliação por posição. Gravar proveniência e idempotência por item na mesma transação.

## Critérios de aceite

- [ ] Testar PostgreSQL com concorrência, rollback, reenvio, conflito de versão e outra conta.
- [ ] Confirmar modifica somente o saldo observado.
- [ ] Playwright cobre declaração, cancelamento, conflito e regressão do HTML.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.
