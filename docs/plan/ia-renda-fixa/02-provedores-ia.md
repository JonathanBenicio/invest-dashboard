# IA-01 · Adaptadores, capacidades e Compose

- Estado: planejado
- Épico: [mapa e dependências](epico.md)
- Issue: [#74](https://github.com/JonathanBenicio/invest-dashboard/issues/74)
- Page: [abrir plano](https://chatgpt.com/space/page_ea9bcf55c340819194d79266d13e2df0)
- Baseline: 27/11/2026 · `codex/tesouro-previa-e-cotacoes` · `d683cfe` · `.codex-attachments/` preexistente.
- Dependências: EP-00
- Agentes: invest-backend, invest-test

## Objetivo e escopo

Adaptadores, capacidades e Compose conforme o [épico](epico.md). O recorte e o resultado verificável estão definidos nas etapas e critérios abaixo.

## Etapas e verificações

- Adicionar Ollama como perfil opcional em rede interna e volume persistente. Fixar versões com evidência de homologação e manter o health check da API independente. Medir startup frio/quente em WSL.
- Criar portas Application para chat e imagem. Implementar adaptadores MEAI `IChatClient` locais e de OpenRouter em Infrastructure. Verificar por modelo chat com tools, SSE e cancelamento e, separadamente, visão com esquema estruturado. Começar a medição de visão local com Qwen2.5-VL.
- Adaptar registro Ollama do base-ia, SSE e contabilidade do Agent-System e referência de modelo de conversa do portfolio-management.

## Critérios de aceite

- [ ] Ambos passam a mesma suíte de contratos com cenários positivos e negativos.
- [ ] API e HTML operam com Ollama parado.
- [ ] Relatório por modelo, capacidade, duração, falha e cancelamento.

## Validação, riscos e entrega

Evidências de implementação pendentes. Aplicar os controles e os testes descritos nesta página e em [QA-01](14-homologacao.md). Registrar resultados, dependências por capacidade e links de entrega na issue e na Page.

## Referências de implementação

- Adaptar o registro de Ollama e a declaração de ferramentas do [base-ia · 26573cc](https://github.com/JonathanBenicio/base-ia/blob/26573ccacab88b6caf29a8a0e7ff55d649fc9d73/agent-Invest/src/AgentInvest.API/Program.cs#L131). Usar a identidade autenticada da aplicação nas ferramentas.
- Adaptar do Agent-System `20ab9e8` a [escrita de eventos SSE](https://github.com/JonathanBenicio/Agent-System/blob/20ab9e8531c15ab1200f3f4642fb1bec944f4cef/src/AgenticSystem.Api/Helpers/SseWriter.cs#L11) e o [controle de consumo mesmo após cancelamento](https://github.com/JonathanBenicio/Agent-System/blob/20ab9e8531c15ab1200f3f4642fb1bec944f4cef/src/AgenticSystem.Infrastructure/LLM/TenantQuotaChatClient.cs#L66). Exigir identidade e escopo antes de consultar ou transmitir dados.
- Usar do [portfolio-management · 18ad6d1](https://github.com/JonathanBenicio/portfolio-management/blob/18ad6d158c433ee1cd87ba9dd077d7fbb62b96cd/PortfolioAPI/src/Portfolio.Infrastructure/Repositories/ChatRepository.cs#L17) a referência de conversa e mensagem. Transmitir e persistir a mesma resposta no backend.
