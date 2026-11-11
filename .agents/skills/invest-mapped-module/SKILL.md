---
name: invest-mapped-module
description: Create or change a frontend feature that maps API or Supabase data into a shared UI representation in Invest Dashboard.
---

# Módulo com mapeamento compartilhado

Inspecione o contrato real, consumidores e padrões em `frontend/src/api`, `store`, `hooks` e `lib`. Defina tipos de entrada e saída adequados, sem presumir que todos os payloads venham do Supabase.

Centralize a transformação reutilizada em `frontend/src/lib/<entidade>-mapper.ts` ou junto da funcionalidade existente. Trate campos opcionais de acordo com o contrato; não substitua valores financeiros desconhecidos por números inventados. Derive rótulos e status no mapeador quando forem compartilhados.

Adapte os consumidores que efetivamente existem (store, query hook ou outro), sem criar uma store e um hook por obrigação. Teste a transformação quando houver lógica significativa, incluindo valores ausentes e casos de borda pertinentes. Preserve os tipos de domínio e valide o frontend conforme o escopo.

Se a tarefa incluir documentação de contrato HTTP ou SignalR, consulte [endpoint-template.md](../../../templates/endpoint-template.md) e confirme os campos no código. Para cenários BDD solicitados ou necessários, adapte [bdd-template.feature](../../../templates/bdd-template.feature) ao domínio real; não copie seus exemplos de agentes e MCP.
