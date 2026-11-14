# Plano — importação de extrato do Tesouro Direto

> **Refinamento em 13/11/2026:** a visão mais recente do usuário inclui movimentações manuais conciliáveis, processamento assíncrono e avaliação por cotações públicas. Esses objetivos substituem a proposta anterior de bloquear movimentações para posições importadas; as demais regras abaixo são o baseline histórico e precisam ser reavaliadas. Consulte [o plano de descoberta e integração](tesouro-direto-importacao-e-dados-mercado-refinamento.md) antes de implementar ou utilizar estes critérios como aceite. As issues vinculadas ainda não foram reconciliadas com essa mudança.

- Estado: planejado
- Issue: [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53); etapas [#54](https://github.com/JonathanBenicio/invest-dashboard/issues/54), [#55](https://github.com/JonathanBenicio/invest-dashboard/issues/55), [#56](https://github.com/JonathanBenicio/invest-dashboard/issues/56), [#57](https://github.com/JonathanBenicio/invest-dashboard/issues/57) e [#58](https://github.com/JonathanBenicio/invest-dashboard/issues/58)
- ADR: pendente de proposta para identidade, persistência e conciliação.
- Story: pendente de definição específica; [US-IMPORT-001](../USER-STORIES.md) cobre importação de operações CSV e é referência parcial, não aceite para posições de extrato.
- Baseline: `develop`, `f85ae79`, em 11/11/2026; árvore sem alterações rastreadas ou não rastreadas antes desta edição. O plano anterior foi registrado na raiz no contexto de `84241c7`.
- Revisão do fluxo: branch `codex/homologacao-autenticacao-real`, `f85ae79`; este plano já existia como arquivo não rastreado e havia outras alterações preexistentes na árvore.
- Plano canônico: `docs/plan/tesouro-direto-extrato-plano.md`

## Objetivo, baseline e escopo

Importar posições e valores atuais de um extrato particular do Tesouro Direto em formato Excel XLSX para uma carteira selecionada, com prévia e conciliação. As duas amostras locais examinadas anteriormente são XLSX, com uma aba cada e dados de investimento, aplicação e situação em caso de resgate; sua interpretação detalhada ainda precisa de confirmação. A tela existente `/importar` ([importador atual](../../frontend/src/pages/tools/Import.tsx)) aceita CSV de operações e rejeita XLSX.

Na mesma tela, o usuário escolherá a importação do Tesouro Direto, selecionará a carteira de destino e enviará manualmente o XLSX. Antes de confirmar a gravação, poderá revisar posições, valores, divergências e erros na prévia. A importação de operações por CSV e seu modelo permanecerão disponíveis como fluxo separado.

Inclui inventário seguro, contrato, prévia e tratamento de posições e avaliações. A importação de histórico e a criação de transações a partir do extrato ficam fora da entrega inicial; o histórico existente será preservado. Não haverá conexão automática com o Tesouro Direto; não confundir este extrato particular com a base pública Tesouro Transparente. Dependências: amostras locais, fixtures sintéticas, mapeamento dos campos e identidade dos títulos. Não publicar ou versionar arquivos pessoais nem armazenar o original por padrão.

## Refinamento técnico

- **Inventário e identidade:** examinar localmente as amostras, documentar o layout e o significado dos campos, inclusive data de referência, valores, lotes e fórmulas, e produzir fixtures sintéticas. O mapeamento e a identidade estável de títulos permanecem pendentes de definição; propor ADR para identidade, persistência e conciliação antes da implementação.
- **Prévia e confirmação:** criar contrato próprio para leitura, prévia e confirmação de posições, separado da API de operações. A prévia deve apresentar carteira, data, posições e valores de origem, substituições, divergências, erros e posições antes importadas ausentes do arquivo; nada é gravado até a confirmação. Rejeitar arquivos inválidos e dados ambíguos com mensagens úteis.
- **Conciliação:** a confirmação substitui as posições correspondentes da carteira, sem criar compras ou vendas. Posições anteriormente importadas que não constem do novo arquivo permanecem inalteradas e são sinalizadas. Reimportação do mesmo conteúdo deve ser idempotente, sem duplicação. Persistir data de referência e proveniência mínima, sem armazenar o arquivo original por padrão.
- **Histórico e ledger:** operações anteriores permanecem consultáveis, mas não reconstroem posições controladas pelo extrato. Bloquear no servidor, inclusive em chamadas diretas à API, criação e alteração de operações que afetem essas posições; impedir que reconstruções do ledger sobrescrevam seus valores. A liberação posterior para operações exige regra própria e fica fora da primeira versão. Atualmente `TransacaoAppService` reconstrói posições com base no histórico, portanto essa integração precisa ser tratada explicitamente.
- **Integridade e acesso:** autorizar no servidor o acesso à carteira e ao grupo na prévia e confirmação; revalidar os dados e a versão da carteira antes de gravar. Se ela mudar após a prévia, solicitar nova revisão. Confirmar a importação em transação, evitando gravação parcial em caso de falha.

## Etapas e verificações

| Etapa/entrega | Dependências | Verificação observável | Estado e evidência |
|---|---|---|---|
| Inventariar layout e gerar fixtures sintéticas ([#54](https://github.com/JonathanBenicio/invest-dashboard/issues/54)) | Amostras locais | Seções, campos, tipos, datas e fórmulas documentados sem dados pessoais | Pendente |
| Definir contrato e conciliação ([#55](https://github.com/JonathanBenicio/invest-dashboard/issues/55)) | Inventário e decisões de domínio | Mapeamento e identidade definidos; contrato de prévia e confirmação, bloqueios, concorrência e ADR propostos | Pendente |
| Implementar leitura e prévia ([#56](https://github.com/JonathanBenicio/invest-dashboard/issues/56)) | Contrato definido | Tela existente permite selecionar Tesouro Direto, carteira e XLSX; prévia apresenta origem, data, valores, divergências e erros antes da confirmação | Pendente |
| Persistir e reconciliar ([#57](https://github.com/JonathanBenicio/invest-dashboard/issues/57)) | Decisão de domínio e prévia | Substituição confirmada, preservação de ausentes e histórico, bloqueios, isolamento, transação e repetição idempotente | Pendente |
| Validar ponta a ponta ([#58](https://github.com/JonathanBenicio/invest-dashboard/issues/58)) | Etapas anteriores | Fixtures, PostgreSQL e E2E para XLSX e regressão do CSV; comparação local sem publicar valores | Pendente |

## Critérios de aceite

- [ ] Na tela `/importar`, o usuário escolhe Tesouro Direto, seleciona a carteira e fornece um XLSX válido manualmente — evidência: pendente.
- [ ] Mapeamento de campos, data de referência e significado dos valores definidos; arquivos inválidos e dados desconhecidos ou ambíguos apresentam erros e não são importados silenciosamente — evidência: pendente.
- [ ] Usuário revisa a prévia e confirma antes de gravar posições e valores na carteira selecionada, sem criar operações de compra ou venda — evidência: pendente.
- [ ] Reimportação não duplica dados; divergências não sobrescrevem silenciosamente; isolamento entre usuários e grupos é preservado — evidência: pendente.
- [ ] Após confirmação, posições correspondentes são substituídas; posições importadas ausentes permanecem inalteradas e geram alerta — evidência: pendente.
- [ ] Operações anteriores permanecem consultáveis; criação ou alteração de operações que afetem posições controladas pelo extrato é bloqueada também pela API, e reconstruções do ledger preservam essas posições — evidência: pendente.
- [ ] Confirmação revalida autorização e versão da carteira, exige nova prévia em caso de mudança e grava a importação em transação — evidência: pendente.
- [ ] Totais e posições reconciliados com amostras locais e testes sintéticos, sem expor dados financeiros — evidência: pendente.
- [ ] A importação existente de operações CSV e o download do seu modelo continuam disponíveis — evidência: pendente.

## Validação

Ambiente e versões efetivos, comandos e SHA devem constar no relatório. Nenhum teste funcional foi executado nesta edição documental.

| Verificação | Comando/método e ambiente | Estado | Evidência/limitação |
|---|---|---|---|
| Leitura e reconciliação | Fixtures sintéticas, casos de data, decimal, subtotal, lotes, erro e duplicidade | não executado | Contrato e implementação pendentes. |
| Persistência e isolamento | Testes PostgreSQL de substituição, ausentes, idempotência, histórico, bloqueios, reconstrução, autorização, concorrência e rollback | não executado | Decisão de domínio e implementação pendentes. |
| Fluxo de usuário | E2E com XLSX válido e inválido, escolha de carteira, prévia, confirmação e repetição; regressão do CSV | não executado | Implementação pendente. |

## Riscos, gaps e pendências

| Item | Impacto | Mitigação/ação | Referência/estado |
|---|---|---|---|
| Valores de resgate, custo e avaliação | Semânticas distintas podem gerar posições incorretas | Confirmar campos e data; não inferir regras fiscais ou transações | Pendente |
| Dados pessoais e financeiros | Exposição de informações sensíveis | Análise local e fixtures inteiramente sintéticas | Pendente |
| Mapeamento e reconciliação | Contrato e atualizações incorretos | Definir campos do XLSX, identidade e conflitos antes de implementar | [#55](https://github.com/JonathanBenicio/invest-dashboard/issues/55), pendente |
| Reconstrução do ledger | Posições importadas podem ser sobrescritas por operações existentes ou futuras | Proteger posições controladas pelo extrato e bloquear operações que as afetem; testar o fluxo completo | [#57](https://github.com/JonathanBenicio/invest-dashboard/issues/57), pendente |

## Entrega e sincronização

- Implementação/PR/commits: pendentes.
- Story específica, ADR e relatório: pendentes.
- Issues [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53) a [#58](https://github.com/JonathanBenicio/invest-dashboard/issues/58): descrições alinhadas ao refinamento técnico; publicação do plano e atualização de seus links remotos permanecem pendentes.
- Índices: `docs/INDEX.md` e `conductor/tracks.md`; sincronização dos links remotos depende da publicação deste plano.
