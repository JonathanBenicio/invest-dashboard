# Pages de referência do épico de IA e renda fixa

Estas cinco exportações preservam o histórico das ideias e dos planos preparados nas Pages fornecidas. O [épico e seus subplanos](../epico.md) definem o escopo atual. Comandos, trechos de código, preços e modelos das exportações registram as propostas da época e requerem validação antes de qualquer uso.

| Exportação | Conteúdo |
|---|---|
| [Rascunho](rascunho.md) | Pesquisa inicial sobre modelos, importação por imagem, ações no ChatGPT e ML.NET. |
| [Ideia projeto](ideia-projeto.md) | Consolidado da arquitetura inicial de extração, provedores e integração externa. |
| [Assistente IA](assistente-ia.md) | Motivação do assistente interno e referências de arquitetura. |
| [Assistente de investimentos dentro do dashboard com Ollama e OpenRouter](assistente-ollama-openrouter.md) | Plano anterior do chat, persistência, ferramentas e uso dos provedores. |
| [Renda fixa com classificação, alertas e projeções avaliadas com ML.NET](renda-fixa-mlnet.md) | Plano anterior de evolução determinística e avaliação de modelos. |

## Entrega de UX-01

O [handoff de UX-01](../ux/auditoria.md) reúne as propostas visuais, a escolha da experiência híbrida e a validação. O [protótipo versionado](../ux/prototipo/README.md) inclui código, instruções e capturas.

## Decisões consolidadas

- O assistente roda dentro do Invest Dashboard com Ollama e OpenRouter. Custom GPT, Actions e MCP ficaram nas referências de desenho. O roteamento vigente usa prioridade e fallback configurados pelo administrador. [Provedores](../02-provedores-ia.md) · [Política de provedores](../03-politica-provedores.md) · [Conversas e streaming](../06-conversas-streaming.md)
- O print gera uma prévia. A confirmação manual registra saldo bruto de uma posição existente. O extrato HTML sustenta a criação e a conciliação de posições e movimentos. [Extração](../04-extracao-prints.md) · [Revisão e saldo](../05-revisao-saldo.md)
- Renda fixa começa com regras e cálculos determinísticos. Cada experimento com ML.NET precisa demonstrar ganho antes da liberação. [Classificação e alertas](../11-classificacao-alertas.md) · [Projeções](../12-projecoes-renda-fixa.md) · [Experimentos](../13-experimentos-mlnet.md)
