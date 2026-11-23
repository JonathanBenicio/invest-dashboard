# Plano — Extrato detalhado do Tesouro Direto

- Estado: planejado.
- Issue: [#69](https://github.com/JonathanBenicio/invest-dashboard/issues/69). Contexto na [iniciativa #53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: avaliar se a consulta entre lotes exige decisão arquitetural.
- História proposta, sem ID: como investidor, quero consultar o extrato oficial nos detalhes do título para entender vendas, compras e cobranças.
- Baseline: branch `codex/tesouro-previa-e-cotacoes`, commit `d68df4f`, com alterações locais preexistentes na importação, nos testes e nas instruções.
- Plano canônico: `docs/plan/tesouro-extrato-detalhado.md`.

## Objetivo, baseline e escopo

A tela [InvestmentDetails.tsx](../../frontend/src/pages/investments/InvestmentDetails.tsx) mostra a posição e as transações da carteira. Eventos oficiais do HTML confirmado ficam separados e a consulta atual é por lote. Adicionar a seção **Extrato oficial do Tesouro Direto** nos detalhes do título, mantendo a visão de saldo e as movimentações da carteira.

| Data | Tipo | Valor |
|---|---|---:|
| Data da venda | Venda — valor bruto do resgate | R$ 1.000,00 |
| Data da cobrança | Cobrança de taxa — débito | R$ 2,00 |

Exemplo sintético. A seção mostra data, natureza e tipo, valor em BRL e origem do extrato. Proposta para informações adicionais: quantidade, preço unitário e percentual anual quando presentes, em detalhes da linha. Compras mantêm a distinção entre taxa anual informada e não informada. Uma cobrança conserva o sentido de débito e só recebe identificação de B3 ou instituição se a fonte informar isso explicitamente.

O escopo é consultar o extrato confirmado que serve de base ao saldo atual. A seção não oferece histórico de extratos nem seletor de lotes anteriores. Vendas e cobranças permanecem linhas independentes. Ficam fora do escopo associação automática, cálculo de valor após taxas ou líquido, recálculo de saldo, criação de transações e exclusão de registros já gravados.

### O que mudou na última importação

Decisão de produto aprovada: manter os lotes completos para auditoria e mostrar, nos detalhes, o extrato ativo e um resumo de diferenças em relação à base imediatamente anterior. A primeira importação exibe apenas o extrato ativo. O resumo compara dois lotes confirmados do mesmo título e carteira, com suas datas de referência.

Compare campos observáveis e suas contagens. Uma linha já presente com a mesma contagem fica fora do resumo. Uma venda ou cobrança adicional aparece como observada no novo extrato, em linhas independentes. Uma linha ausente é descrita como “não apareceu no novo extrato”. Quando não houver identidade suficiente, indique a ambiguidade em vez de presumir que um evento foi editado ou cancelado. Mostre a variação da quantidade entre as bases e sinalize quando o lote anterior estiver indisponível. Consulte a [ADR de reimportação](../adrs/0004-tesouro-reimportacao-snapshots.md) para preservar os critérios de comparação e auditoria.

## Etapas e verificações

| Etapa | Dependências | Verificação | Estado |
|---|---|---|---|
| Executar as validações de baseline disponíveis | SDKs do projeto | Registrar resultados e impedimentos antes da implementação | Pendente |
| Definir consulta paginada por carteira e título | Eventos oficiais, lotes confirmados e autorização | Validar contrato, ordenação estável e indexação; reaproveitar o endpoint por lote se ele atender à política aprovada | Pendente |
| Implementar leitura e teste integrado da API | Contrato aprovado e dados sintéticos | Consultar eventos do título sem incluir outra carteira, título ou lote pendente | Pendente |
| Implementar resumo da última importação | Bases confirmadas e regra de comparação da ADR | Expor novos registros observados, ausências, ambiguidades e variação de quantidade sem duplicar lançamentos | Pendente |
| Integrar seção nos detalhes | API e padrão de tabela do frontend | Mostrar valores, origem e estados de carregamento, vazio, erro e paginação | Pendente |
| Executar validações finais e registrar evidências | Implementação completa | Integração, build e Playwright nos tamanhos pertinentes | Pendente |

## Critérios de aceite

- [ ] Apenas eventos oficiais do lote confirmado da base ativa da carteira e do título consultados são exibidos, com paginação estável. Sem base ativa, apresentar estado vazio.
- [ ] Venda é identificada como valor bruto do resgate, e a cobrança aparece como débito próprio. O percentual anual tem apresentação distinta de valores monetários.
- [ ] Dados ausentes são apresentados conforme sua classificação. Nenhuma taxa ou valor líquido é inferido.
- [ ] Com duas bases confirmadas, o resumo omite linhas repetidas e distingue novas observações, ausências e ambiguidades. Na primeira importação, o extrato ativo aparece sem resumo.
- [ ] O saldo e as transações da carteira permanecem consistentes após a consulta. Um vínculo de auditoria existente pode identificar um evento sem virar novo lançamento.
- [ ] Interface responsiva com leitura por teclado, incluindo lista vazia, erro recuperável e tabelas largas com rolagem interna.

## Validação

| Verificação | Método e ambiente | Estado |
|---|---|---|
| Baseline de persistência | Validação prévia reportada pelo agente de testes: filtro `TesouroImportPersistenceTests` com PostgreSQL isolado. Um teste passou, sem ignorados. Não cobre o endpoint HTTP de eventos | Passou |
| Baseline de frontend | ESLint em `InvestmentDetails.tsx` em cópia temporária restaurada pelo lockfile, conforme relato do agente de testes. O checkout local apresenta dependências desalinhadas | Passou |
| Integração da API | .NET 10 e PostgreSQL isolado quando necessário; testar autorização, isolamento, lotes pendentes, paginação e ausência de efeitos sobre a carteira | Não executado |
| Frontend | ESLint, TypeScript e build com dependências do lockfile | Não executado |
| Navegador | Playwright com fixture sintética, celular e desktop, incluindo 360, 1280, 1366 e 1920 px, reflow em 320 px e transição 767/768 px quando aplicável | Não executado |

## Riscos, gaps e pendências

| Item | Impacto | Ação |
|---|---|---|
| Reimportações podem repetir movimentos em lotes distintos | Misturar lotes pode sugerir uma segunda venda ou cobrança | Decisão de produto: exibir só o lote da base ativa. Se uma nova importação substituir a base, a seção passa a mostrar o novo lote. |
| O endpoint atual é por lote | A tela poderá precisar de uma consulta por posição | Depois da decisão de produto, verificar se basta localizar o lote autorizado ou se é necessário um novo endpoint. Registrar decisão técnica e ADR se aplicável. |
| Eventos antigos podem não conter todos os campos interpretados | Risco de exibir informação incorreta | Preservar valores existentes e sinalizar ausência. Validar uma fixture antiga. |

## Entrega e sincronização

- Implementação, evidências e PR específicos: pendentes.
- Índice documental atualizado. Rastreabilidade no GitHub pela issue #69.
