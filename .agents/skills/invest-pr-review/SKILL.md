---
name: invest-pr-review
description: Review an Invest Dashboard pull request or local change for correctness, security, regression risk, and meaningful test coverage.
---

# Revisão de mudança

Identifique objetivo, diff e arquivos afetados. Compare o comportamento com os contratos e consumidores, procurando regressões, problemas de autorização, integridade de dados, consultas ineficientes e lacunas de teste relevantes. Execute verificações proporcionais quando forem viáveis e seguras.

Reporte primeiro achados acionáveis com severidade, impacto, condição de ocorrência e localização; diferencie fatos observados de hipóteses e indique testes ou cenários não verificados. Não publique comentários nem altere o PR sem pedido do usuário.

Quando a descrição ou as evidências do PR fizerem parte do escopo da revisão, use [pr-template.md](../../../templates/pr-template.md) e, se houver relatório separado, [validation-report-template.md](../../../templates/validation-report-template.md) como referências. Ausência de um campo do modelo só é achado se tiver impacto concreto no trabalho revisado.

## Scan Codex Security solicitado pelo usuário

O scan Codex Security é complementar a esta revisão e ao agente `invest-security`; não o inicie em toda revisão comum.

- Para um PR, commit, branch ou diff local, use `codex-security:security-diff-scan` com o alvo exato.
- Para um repositório ou caminho completo solicitado, use `codex-security:security-scan`. Use `codex-security:deep-security-scan` somente quando o usuário pedir uma análise profunda ou exaustiva.
- Siga o ciclo de vida da skill Codex Security e não declare um scan concluído se as ferramentas necessárias estiverem indisponíveis. Nesse caso, informe a limitação e, se solicitado, faça uma revisão contextual separada com `invest-security`.
- Preserve o escopo autorizado, não inclua extratos financeiros ou segredos no contexto do scan e não publique achados, abra issues ou altere código sem pedido explícito.
