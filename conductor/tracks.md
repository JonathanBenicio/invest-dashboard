# Tracks do Invest Dashboard

Índice de iniciativas acompanhadas. O conteúdo do plano permanece em `docs/plan/`; esta lista registra somente identificação, estado documental conhecido e rastreabilidade. Estado de implementação/validação só deve avançar com evidência atual. O inventário inicial abaixo contém planos históricos existentes e não afirma que estejam ativos ou aprovados.

## Iniciativas

| Identificador | Iniciativa (conforme arquivo) | Estado conhecido | Plano canônico | Observações |
|---|---|---|---|---|
| [#48](https://github.com/JonathanBenicio/invest-dashboard/issues/48) | Homologação de autenticação real | bloqueada | [autenticacao-real-plano.md](../docs/plan/autenticacao-real-plano.md) | Testes determinísticos em InMemory/PostgreSQL passam; live exige contas/recursos locais e sessão autenticada no Dashboard para URL Configuration. |
| [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53) | Importação de extrato do Tesouro Direto | planejada | [tesouro-direto-extrato-plano.md](../docs/plan/tesouro-direto-extrato-plano.md) | Posições e valores atuais; regras e formato pendentes. |
| A definir | Gestão de carteira | Documento existente; estado atual pendente de confirmação | [carteira.md](../docs/plan/carteira.md) | Plano preexistente; revisar requisitos e implementação antes de reativar. |
| A definir | Chat IA | Documento existente; estado atual pendente de confirmação | [chat-ia.md](../docs/plan/chat-ia.md) | Plano preexistente; não inferir adoção ou execução. |
| A definir | Plano de implementação da plataforma | Documento existente; estado atual pendente de confirmação | [plan.md](../docs/plan/plan.md) | Documento amplo/histórico; verificar premissas contra o código antes de usar. |
| A definir | Chat IA (OpenCode) | Documento existente; estado atual pendente de confirmação | [opencode/chat-ia.md](../docs/plan/opencode/chat-ia.md) | Documento preexistente. |
| A definir | Convenção de nomes (OpenCode) | Documento existente; estado atual pendente de confirmação | [opencode/naming-convention.md](../docs/plan/opencode/naming-convention.md) | Documento preexistente. |
| A definir | Normalização de models/DTOs (OpenCode) | Documento existente; estado atual pendente de confirmação | [opencode/normalização-models-dtos.md](../docs/plan/opencode/normalização-models-dtos.md) | Documento preexistente. |

## Manutenção

Ao iniciar uma iniciativa, use um ID/Issue real quando houver; não invente número. Adicione a linha e o link para o plano em `docs/plan/` assim que criado. Mantenha estados simples e sustentados por evidência (`planejada`, `em andamento`, `concluída`, `bloqueada`, `arquivada`); detalhe critérios, etapas e resultados somente no plano. Ao concluir ou arquivar, preserve a referência e registre a evidência/status final. Para trabalho pontual sem track, declare N/A no PR ou plano aplicável.
