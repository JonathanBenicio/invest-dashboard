# Plano — importação e conciliação do Tesouro Direto

- Estado: em andamento, com prévia e confirmação transacional HTML permitem criar posição inicial quando os campos necessários são reconhecidos. XLSX foi adiado pelo usuário.
- Iniciativa: [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53). Etapas: [#54](https://github.com/JonathanBenicio/invest-dashboard/issues/54), [#55](https://github.com/JonathanBenicio/invest-dashboard/issues/55), [#56](https://github.com/JonathanBenicio/invest-dashboard/issues/56), [#57](https://github.com/JonathanBenicio/invest-dashboard/issues/57) e [#58](https://github.com/JonathanBenicio/invest-dashboard/issues/58).
- PR: [#62](https://github.com/JonathanBenicio/invest-dashboard/pull/62), em revisão.
- ADR: pendente para criação de posições, identidade de eventos, reimportação de snapshots e avaliação pública.
- Plano detalhado: [refinamento da importação e dados de mercado](tesouro-direto-importacao-e-dados-mercado-refinamento.md).
- Reimportação de snapshots: [plano auditável para comparação e revisão de divergências](tesouro-reimportacao-snapshots.md), ligado às Issues #55, #57 e #58.

## Objetivo

Permitir que o usuário crie ou atualize uma carteira com seu extrato do Tesouro Direto, examine posições e movimentações, revise divergências e use o extrato como fonte oficial do histórico. A importação existente de operações CSV continua disponível. Cotações públicas, Selic, IPCA e CDI são fontes separadas e devem exibir origem e data.

## Estado da entrega

| Área | Estado |
|---|---|
| Leitura de HTML | Implementada para o layout reconhecido. A cobertura total do arquivo não é presumida. XLSX não está habilitado para importação. |
| Prévia e histórico de lotes | Implementados com carteira, autor, data de referência e retomada da prévia. O arquivo original não é persistido. |
| Conciliação | Implementada para HTML, título e vencimento correspondentes a uma posição existente. Sugestões não são aceitas automaticamente. |
| Confirmação | Transacional e idempotente para o mesmo lote. Registra eventos oficiais, associações revisadas e uma base temporal, e reconstrói a posição com operações posteriores. |
| Criação inicial a partir de HTML | Implementada para dados essenciais reconhecidos. Testes sintéticos incluem duas aplicações com taxas diferentes; validação com arquivos reais permanece pendente. |
| XLSX | Adiado pelo usuário, fora do escopo atual. |
| Novos títulos sem posição cadastrada | O HTML pode criar uma posição para o título e vencimento identificados; taxas são preservadas nas linhas do extrato e não atribuídas como taxa única do título. |
| Avaliação pela cotação pública | Consulta histórica do Tesouro está disponível, mas associação e atualização das posições não são automáticas. |
| Comparação de desempenho e estimativas | Pendente de metodologia para aportes, retiradas, impostos e períodos incompletos. |

## Regras vigentes

- O extrato é a fonte oficial e completa do histórico do usuário. A confirmação exige leitura estruturada e revisão dos eventos reconhecidos.
- A confirmação exige autorização para a carteira, posição compatível, versão atual da carteira e revisões vigentes. A gravação de eventos e base ocorre na mesma transação PostgreSQL.
- Operações manuais posteriores à data-base são reaplicadas sobre a base. Operações ainda não conciliadas no intervalo que a base substituirá impedem a confirmação.
- Cada lote mantém seus eventos oficiais e vínculos para auditoria. A repetição do mesmo lote devolve a confirmação existente.
- Posições ausentes de uma reimportação não são removidas automaticamente. Diferenças precisam ser revistas.
- O PU público, o valor do extrato e os dados fiscais do extrato são apresentados com suas datas e proveniência distintas.

## Pendências e validação

Validar reimportações em cenários adicionais e na interface, tratar operações manuais sem correspondência e revisão de ausência integral da posição, manter XLSX adiado e implementar avaliação por cotação em etapa posterior ao PR #62. A projeção para Tesouro fica indisponível até haver cálculo por aplicação com dados conciliados.

A branch `codex/tesouro-previa-e-cotacoes` foi validada pela CI do PR #62. A confirmação local acrescentou testes com PostgreSQL de concorrência e repetição. A validação mais recente passou com 154 testes unitários e 101 de integração, além de duas verificações externas ignoradas. Os seis testes Playwright direcionados, TypeScript e ESLint direcionado passaram com respostas sintéticas.

Em 18/11/2026, testes direcionados no SDK .NET 10.0.401 e PostgreSQL 15 isolado passaram: seis testes unitários de criação e quatro integrações de confirmação/isolamento. O teste HTML cria uma posição com duas taxas de aplicação, mantendo os valores no lote; dois usuários com o mesmo ativo não recebem taxa legada compartilhada nem projeção baseada nela. A validação real e de interface continua pendente.
