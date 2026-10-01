# Plano futuro — baixa prioridade

Atualizado em 29/09/2026. Este backlog não integra o plano ativo de acompanhamento de posições. Sua implementação começará somente depois de uma nova priorização e da aprovação das regras de negócio.

## Fluxo de caixa, dividendos e proventos

**Estado:** futuro, baixa prioridade. O endpoint atual de proventos retorna uma lista vazia e não representa uma funcionalidade concluída.

**Escopo a decidir:** depósitos e saques; eventos declarados, previstos, confirmados e pagos; dividendos/JCP; cupons, resgates e vencimentos; impostos e taxas; frações/bonificações; reinvestimentos; fontes dos eventos e conciliação com as operações e posições do usuário.

**Quando priorizado:** aprovar histórias e o modelo de evento e livro-caixa; definir fonte, identificação e deduplicação; implementar o ledger auditável no backend; expor filtros, totais e calendário no frontend; validar com eventos reais versionados, falhas de fonte e reprocessamento.

**Critérios de aceite propostos:** cada lançamento individual informa origem, ativo, data, valor e status; a confirmação de pagamento não duplica saldo; valores públicos agregados do Tesouro Transparente não são atribuídos ao usuário nem lançados no seu caixa.

As especificações acima são preliminares. As decisões de evento, fonte, imposto, reinvestimento e conciliação serão fechadas quando esta fase receber prioridade.
