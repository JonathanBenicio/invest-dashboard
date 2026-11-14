---
name: invest-pr-review
description: Review an Invest Dashboard pull request or local change for correctness, security, regression risk, and meaningful test coverage.
---

# Revisão de mudança

Identifique objetivo, diff e arquivos afetados. Compare o comportamento com os contratos e consumidores, procurando regressões, problemas de autorização, integridade de dados, consultas ineficientes e lacunas de teste relevantes. Execute verificações proporcionais quando forem viáveis e seguras.

Reporte primeiro achados acionáveis com severidade, impacto, condição de ocorrência e localização; diferencie fatos observados de hipóteses e indique testes ou cenários não verificados. Não publique comentários nem altere o PR sem pedido do usuário.

Quando a descrição ou as evidências do PR fizerem parte do escopo da revisão, use [pr-template.md](../../../templates/pr-template.md) e, se houver relatório separado, [validation-report-template.md](../../../templates/validation-report-template.md) como referências. Ausência de um campo do modelo só é achado se tiver impacto concreto no trabalho revisado.
