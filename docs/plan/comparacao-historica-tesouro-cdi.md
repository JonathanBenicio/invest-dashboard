# Plano — comparação histórica de posições com CDI e outros ativos

- Estado: planejado; fora do escopo do PR #62.
- Issue: [#65 — Tesouro Direto: comparar histórico da posição com CDI e outros ativos](https://github.com/JonathanBenicio/invest-dashboard/issues/65), relacionada à iniciativa [#53](https://github.com/JonathanBenicio/invest-dashboard/issues/53).
- ADR: pendente até definir método de retorno, tratamento de fluxos e base bruta/líquida.
- Story: [US-COMP-002](../USER-STORIES.md#us-comp-002--comparação-histórica-de-posição-com-cdi-e-outros-ativos), proposta.
- Baseline: branch `codex/tesouro-avaliacao-liquida`, HEAD `f70fbb3`; branch de planejamento separada de `codex/tesouro-previa-e-cotacoes` e do PR #62.
- Plano canônico: `docs/plan/comparacao-historica-tesouro-cdi.md`.

## Objetivo, baseline e escopo

- Problema observado: a análise atual mostra CDI do BCB SGS 12 normalizado com uma série da carteira, mas a normalização isolada não demonstra retorno ajustado por aportes, retiradas, vendas ou cupons. O CSV do Tesouro Transparente contém histórico de preços, não o histórico pessoal completo da posição.
- Resultado esperado: definir e depois implementar uma comparação histórica compreensível e reproduzível entre posição/carteira, CDI e outros ativos, com método, fontes, período coberto e lacunas explícitos.
- Incluído: escolher retorno e normalização; alinhar calendários e frequência; determinar o papel de aportes, retiradas, vendas parciais, cupons, tributos e taxas; escolher fontes e critérios de cobertura para o título, CDI e ativos comparados; definir como séries incompletas aparecem sem inventar pontos.
- Excluído: implementar a tela/gráfico neste plano; estimar retornos futuros; interpolar datas sem observação; importar XLSX. XLSX permanece adiado pelo usuário.
- Dependências: avaliação histórica por posição; qualidade do ledger e eventos HTML; [plano de avaliação atual e custos](avaliacao-liquida-tesouro-direto.md); histórico oficial de preços do Tesouro Transparente; CDI diário do BCB SGS série 12; fonte consistente para cada outro ativo comparado.

## Direção de produto e metodologia a decidir

- Comparar séries sob o mesmo período, moeda BRL, frequência, base de normalização e política de datas; mostrar origem e última observação de cada série.
- Determinar se o indicador principal será retorno ponderado pelo tempo (TWR), retorno ponderado por fluxo de caixa (IRR/XIRR ou método equivalente) ou ambos. A decisão depende da disponibilidade de avaliação nas datas dos fluxos e do objetivo do gráfico; não escolher pela conveniência do dado existente.
- Decidir se a comparação inicial será bruta ou líquida. Um retorno líquido só poderá ser comparado a referência com custos/impostos tratados de modo coerente; usar taxas antigas de extrato como encargos atuais é proibido.
- Usar CDI diário da série SGS 12 com composição diária conforme seu valor e datas observadas. Alinhar dias da série do título com o índice sem criar dados para feriados, indisponibilidade ou período anterior à cobertura.
- Para posições de Tesouro, reconstruir quantidade histórica com compras/vendas/cuponagens disponíveis e associar PU do dia ao título/vencimento. Se a posição histórica não puder ser reconstruída, exibir a cobertura parcial e não afirmar retorno pessoal completo.
- Outros investimentos só entram quando houver preço e tratamento de proventos/ajustes compatíveis. Cotação sem ajuste de proventos não deve ser comparada a uma série de retorno total sem declarar a diferença.

## Etapas e verificações

| Etapa/entrega | Dependências | Verificação observável | Estado e evidência |
|---|---|---|---|
| [ ] Inventariar séries e cobertura disponível | Provider Tesouro CSV, BCB SGS 12, APIs de ativos e ledger | Matriz de fonte, frequência, unidade, início/fim, lacunas, ajustes/proventos e licença/atribuição | Pendente |
| [ ] Comparar métodos para fluxos | Movimentações, posições históricas, custos/cuponagens | Exemplos sintéticos calculados por TWR e fluxo de caixa, com pré-condições e limitações de cada método | Pendente |
| [ ] Definir bruta versus líquida e calendário | Plano #64 e fontes fiscais | Regra documentada para custos, impostos, datas úteis/civis, alinhamento e normalização | Pendente; ADR pode ser necessária |
| [ ] Refinar contrato de consulta e visualização | Método escolhido e Story US-COMP-002 | Contrato descreve período, fonte, método, valor-base, retornos, cobertura e lacunas | Pendente |
| [ ] Implementar a comparação depois da aprovação metodológica | Contratos e fontes aprovados | Testes sintéticos para sem fluxos, aportes, retiradas, vendas parciais, cupons, feriados e série incompleta | Não iniciado |
| [ ] Validar com dados observados e privacidade | Implementação e autorização por carteira | Testes API/UI e conferência independente de casos reproduzíveis, sem expor dados pessoais | Não iniciado |

## Critérios de aceite

- [ ] O método e a fórmula de retorno são declarados, incluindo tratamento de fluxos, taxas, tributos e cupons; evidência: ADR/matriz metodológica revisada.
- [ ] CDI e ativo comparado usam o mesmo período, frequência, moeda e base; evidência: caso sintético reproduzível.
- [ ] As observações preservam fonte e data, e dias sem dados permanecem lacunas; evidência: teste com série incompleta/feriados.
- [ ] O retorno de posição só é apresentado como pessoal quando a quantidade histórica e os fluxos necessários estiverem reconciliados; caso contrário, a cobertura parcial e a limitação aparecem claramente.
- [ ] Preço bruto não é comparado como retorno líquido sem declarar e harmonizar custos/tributos; evidência: casos sintéticos antes/depois de custos.
- [ ] XLSX não é implementado nem usado como requisito desta comparação; evidência: exclusão explícita do escopo.

## Validação

Este documento registra planejamento; nenhuma implementação, cálculo ou teste da comparação foi executado. Ambiente e versões serão registrados antes da implementação. As fontes e limites precisam ser revalidados na fase de refinamento.

| Verificação | Comando/método e ambiente | Estado | Evidência/limitação |
|---|---|---|---|
| Inventário de séries e eventos | Revisão dos contratos e código existentes | não executado | O plano atual registra CDI SGS 12 e CSV do Tesouro; cobertura por período e ativo precisa de levantamento específico. |
| Implementação/testes | Não executados | não executado | Não há método aprovado nem código desta comparação. |

## Riscos, gaps e pendências

| Item | Impacto | Mitigação/ação | Referência/estado |
|---|---|---|---|
| Usar valor normalizado sem ajustar fluxos | Comparação pode atribuir a aportes um ganho que veio do mercado | Comparar TWR e retorno ponderado por fluxo com exemplos e escolher método | Issue #65 aberta |
| Séries públicas não refletem titularidade/quantidade da posição | Cotação não prova retorno pessoal | Reconstruir custódia pelo ledger e marcar início/cobertura | Dependente das issues #53, #58 e #64 |
| CDI e título têm calendários/frequências diferentes | Desalinhamento ou valor inventado em feriados | Usar datas observadas comuns ou regras de agregação declaradas, sem interpolação oculta | Pendente |
| Preço não ajustado por cupons/proventos | Retorno do título/ativo fica subestimado ou incompatível | Incluir fluxos de renda ou rotular preço como cotação sem retorno total | Pendente |
| Impostos e taxas têm datas e regras diferentes | Comparação líquida enganosa | Definir bruto/líquido e usar componentes contemporâneos conforme Issue #64 | Pendente |
| Dados incompletos de HTML/ledger | Período longo pode parecer completo sem ser | Mostrar intervalo coberto e lacunas; não preencher artificialmente | Pendente |

## Entrega e sincronização

- Implementação/PR/commits: nenhum; este plano está fora do PR #62 e não implementa tela/gráfico.
- Relatório/evidências: N/A nesta etapa de planejamento.
- Limitações restantes: método, fontes para outros ativos, tratamento dos fluxos, proventos e impostos precisam de refinamento antes de implementar.
- Índices: ligar à Issue #65, Story US-COMP-002, `docs/INDEX.md` e `conductor/tracks.md`; atualizar a Issue #53 para apontar este plano.

## Fontes candidatas

- [BCB SGS — série 12, CDI diário](https://dadosabertos.bcb.gov.br/dataset/12-taxa-de-juros---cdi).
- [Tesouro Transparente — taxas e preços ofertados do Tesouro Direto](https://www.tesourotransparente.gov.br/ckan/dataset/taxas-dos-titulos-ofertados-pelo-tesouro-direto).
- [Plano de avaliação atual do Tesouro Direto — Issue #64](avaliacao-liquida-tesouro-direto.md).
