# Tracks do Invest Dashboard

Índice de iniciativas acompanhadas. O conteúdo do plano permanece em `docs/plan/`; esta lista registra somente identificação, estado documental conhecido e rastreabilidade. Estado de implementação/validação só deve avançar com evidência atual. O inventário inicial abaixo contém planos históricos existentes e não afirma que estejam ativos ou aprovados.

## Iniciativas


| Identificador | Iniciativa (conforme arquivo) | Estado conhecido | Plano canônico | Observações |
|---|---|---|---|---|
| [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53) | Importação e conciliação do Tesouro Direto | em andamento | [Refinamento da importação](../docs/plan/tesouro-direto-importacao-e-dados-mercado-refinamento.md); [reimportação](../docs/plan/tesouro-reimportacao-snapshots.md); [validação inicial](../docs/plan/tesouro-teste-integracao-importacao-inicial.md); [Extrato detalhado](../docs/plan/tesouro-extrato-detalhado.md) | PR #62. Validação local cobre os dois HTMLs originais e a reimportação sintética. Decisão aprovada: divergência de instituição identificada bloqueia a importação e pede outra carteira. Validação manual pendente. |
| [#70](https://github.com/JonathanBenicio/invest-dashboard/issues/70) | Investimentos consolidados por grupo | implementação e validações focadas concluídas | [Plano](../docs/plan/investimentos-consolidados-por-grupo.md) | Integração PostgreSQL, Playwright, typecheck, lint e builds passaram. Conferência autenticada pendente. |
| A definir | Gestão de carteira | Documento existente; estado atual pendente de confirmação | [carteira.md](../docs/plan/carteira.md) | Plano preexistente; revisar requisitos e implementação antes de reativar. |
| A definir | Chat IA | Documento existente; estado atual pendente de confirmação | [chat-ia.md](../docs/plan/chat-ia.md) | Plano preexistente; não inferir adoção ou execução. |
| A definir | Plano de implementação da plataforma | Documento existente; estado atual pendente de confirmação | [plan.md](../docs/plan/plan.md) | Documento amplo/histórico; verificar premissas contra o código antes de usar. |
| A definir | Chat IA (OpenCode) | Documento existente; estado atual pendente de confirmação | [opencode/chat-ia.md](../docs/plan/opencode/chat-ia.md) | Documento preexistente. |
| A definir | Convenção de nomes (OpenCode) | Documento existente; estado atual pendente de confirmação | [opencode/naming-convention.md](../docs/plan/opencode/naming-convention.md) | Documento preexistente. |
| A definir | Normalização de models/DTOs (OpenCode) | Documento existente; estado atual pendente de confirmação | [opencode/normalização-models-dtos.md](../docs/plan/opencode/normalização-models-dtos.md) | Documento preexistente. |
| [#71](https://github.com/JonathanBenicio/invest-dashboard/issues/71) | IA e renda fixa | Em andamento | [Épico e subplanos](../docs/plan/ia-renda-fixa/epico.md) | EP-00 e UX-01 em execução. |

## Manutenção

Ao iniciar uma iniciativa, use um ID/Issue real quando houver; não invente número. Adicione a linha e o link para o plano em `docs/plan/` assim que criado. Mantenha estados simples e sustentados por evidência (`planejada`, `em andamento`, `concluída`, `bloqueada`, `arquivada`); detalhe critérios, etapas e resultados somente no plano. Ao concluir ou arquivar, preserve a referência e registre a evidência/status final. Para trabalho pontual sem track, declare N/A no PR ou plano aplicável.
