# Plano — investimentos consolidados por grupo

- Estado: implementação e validações focadas concluídas no checkout; publicação no PR e conferência autenticada pendentes.
- Issue: [#70 — consolidar investimentos por grupo, titular e carteira](https://github.com/JonathanBenicio/invest-dashboard/issues/70). Atualizar com link do plano publicado após push.
- ADR: [0005 — consulta consolidada de investimentos](../adrs/0005-consulta-consolidada-investimentos.md).
- Story: [US-PORT-003 — consolidar investimentos por grupo](../USER-STORIES.md#us-port-003--consolidar-investimentos-por-grupo).
- Baseline: branch `codex/tesouro-previa-e-cotacoes`, commit `d7fe081`, com alterações locais preexistentes de Tesouro e carteiras.
- Plano canônico: `docs/plan/investimentos-consolidados-por-grupo.md`.

## Objetivo, baseline e escopo

Na baseline, as telas de Renda Fixa e Ações listavam posições por carteira e paginam as linhas individuais. Renda Fixa já tem filtro de grupo. O detalhe comum abre por ID de posição em `/investimento/$id`. O objetivo é abrir uma linha consolidada e continuar no detalhe do mesmo investimento, podendo alternar nos dois sentidos entre o consolidado e uma carteira.

Inclui filtros por grupo, titular e carteira, consulta consolidada, participação de cada carteira no total e navegação com contexto. O detalhe individual mantém movimentações, extrato oficial do Tesouro, avaliações e operações identificadas pela posição. O resumo de grupo apresenta as posições que o usuário pode consultar. Histórico consolidado de movimentações, fiscal, projeções e patrimônio fica fora desta entrega.

## Refinamento da interação

| Alternativa | Benefício | Custo |
|---|---|---|
| Três seletores sempre visíveis e independentes | Acesso em um clique a cada filtro | Ocupa espaço e facilita combinações que não existem, sobretudo no celular |
| Grupo primeiro, titular como refinamento e carteira para escolher uma posição | Mantém claro quando o total representa todo o grupo, um titular ou uma carteira | Exige definir como limpar seleções dependentes |
| Grupo, carteira e depois titular | Ajuda quem procura uma conta conhecida | Depois de escolher a carteira, o titular raramente oferece um recorte adicional. Buscar o total de um titular exige voltar e limpar a carteira |
| Somente grupo e carteira | Interface mais curta | Um titular com várias carteiras precisa consultar cada uma separadamente |

**Recomendação:** usar seletores dependentes. Na lista, exibir **Grupo**, **Titular** e **Carteira** nessa ordem. Titular e carteira começam em “Todos os titulares” e “Todas as carteiras”. Em telas estreitas, manter Grupo visível e reunir os filtros opcionais em **Filtros**, com indicação dos filtros ativos. Os controles têm rótulos, funcionam por teclado e explicam por que uma opção indisponível está desabilitada.

- Grupo controla o universo de titulares e carteiras. Em “Todos os grupos”, a lista continua separando os totais por grupo. Para filtrar por titular, selecionar um grupo primeiro, já que o cadastro de titulares pertence ao grupo.
- Titular restringe as carteiras do grupo no seletor. Se tiver várias carteiras, “Todas as carteiras” soma as posições daquele titular. É possível selecionar uma carteira diretamente, deixando titular em “Todos”. Selecionar uma carteira restringe a visualização àquela carteira. Ao mudar grupo ou titular, limpar escolhas incompatíveis e voltar à primeira página.
- Usar IDs, não nomes, para os filtros. Para carteiras sem grupo, manter linhas individuais, sem criar um grupo fictício. Em grupamentos com carteiras sem titular cadastrado, exibir “Sem titular” no detalhamento e garantir sua inclusão em “Todos os titulares”.
- Em toda visão filtrada, mostrar o escopo junto ao total. Exemplo: **Família · Pessoa A · 2 carteiras**. No grupo, explicar que o total usa as carteiras acessíveis, sem listar posições ou titulares de carteiras privadas sem acesso.

| Entrada no detalhe | Estado inicial | Ações disponíveis |
|---|---|---|
| Linha da lista com todas as carteiras do grupo | **Consolidado do grupo**, com titular selecionado mantido, se houver | Seletor de titular e lista de contribuições por carteira. **Ver por carteira** ou clique numa contribuição abre a posição individual |
| Linha da lista filtrada por carteira ou detalhe aberto pela tela de uma carteira | **Por carteira**, com carteira ativa identificada | Trocar para outra carteira acessível com o mesmo investimento ou **Ver consolidado do grupo**. A volta ao consolidado limpa a carteira e mantém o filtro de titular compatível, com opção de limpar esse filtro |
| Carteira sem grupo | Detalhe individual | Operações e dados da própria carteira |

Manter na URL o grupo, filtros relevantes, instrumento e visão, para suportar recarga, links compartilháveis e o botão Voltar. O detalhe consolidado fica associado ao mesmo grupo e instrumento ao trocar a visão. Para ver o mesmo ativo em outro grupo, usar a lista com o filtro de grupo. Proposta de navegação do detalhe consolidado na ADR. Um selecionador sem opções mostra “Nenhuma posição neste filtro” com opção de limpar filtros ou voltar. Tratar carregamento, erro com nova tentativa e perda de acesso sem reaproveitar dados privados de um contexto anterior.

Exemplo sintético: o Grupo Família tem Tesouro Prefixado 2032 em duas carteiras. A lista mostra uma linha com o total. O detalhe consolidado mostra a contribuição de cada carteira. Ao escolher a carteira da Pessoa A, aparece a visão individual e a ação para voltar ao consolidado do mesmo título.

## Etapas e verificações

| Etapa | Dependência | Resultado verificável | Estado |
|---|---|---|---|
| Confirmar regras de identidade, visões e valores | Story e ADR propostas | Registrar o comportamento de filtros, estados de posição e valores ausentes | Identidade canônica e escopo de leitura aplicados; demais limites registrados na ADR |
| Registrar baseline antes da implementação | Suite e ambiente de teste do projeto | Executar verificações existentes de autorização, listagem paginada e detalhe individual. Registrar resultados e resolver impedimentos prévios | 8 testes de integração existentes do controller passaram em fixture InMemory |
| Implementar leitura consolidada na API | Autorização, identidade e métricas aprovadas | Agregar após autorização e filtros, ordenar e paginar consolidados, devolver contribuições autorizadas no detalhe | Implementada; ver validação abaixo |
| Implementar filtros e linhas agrupadas nas duas telas | Contrato de leitura | Seletores dependentes, total correto e filtros na URL | Implementados localmente |
| Implementar navegação de detalhe nos dois sentidos | Contrato de detalhe e rotas | Preservar instrumento e contexto, direcionar operações à carteira escolhida | Implementada localmente |
| Validar integração, navegador e regressões | Implementação | Autorização, paginação, somas, troca de visão e manutenção do detalhe existente | Integração focal, Playwright, build, typecheck e lint passaram; conferência autenticada manual não executada |

## Critérios de aceite

- [x] Mesmo investimento em duas carteiras acessíveis do grupo ocupa uma linha. A contribuição de cada carteira explica a soma inteira, mesmo quando as posições cairiam em páginas diferentes. Coberto por integração HTTP.
- [x] O mesmo investimento em outro grupo fica separado. Carteiras sem grupo permanecem individuais. Vencimentos diferentes no mesmo ano ficam em linhas distintas e compras do mesmo ticker em anos diferentes permanecem na mesma linha de ações. Casos cobertos por integração.
- [x] Titular restringe a visão às suas carteiras. Carteira restringe a uma posição. Filtros incompatíveis são limpos e a abrangência atualizada. Coberto por integração e E2E.
- [x] Abrir pela linha consolidada mantém o agrupamento no detalhe. Abrir pela carteira inicia no detalhe individual. A troca preserva ativo e contexto. Coberto por E2E.
- [x] Operações continuam vinculadas a uma posição individual, com autorização existente; linhas consolidadas não são destinos de escrita.
- [x] Carteiras sem acesso não contribuem para valores, contagens ou opções. Link direto não amplia acesso. Coberto por integração HTTP e revisão de segurança.

## Validação

| Verificação | Método e ambiente | Estado |
|---|---|---|
| Comportamento atual e escopo | Leitura de páginas, DTOs, controller e política de visibilidade no checkout | Revisão documental |
| Identidade, filtros, valores e acesso | Integração HTTP em PostgreSQL isolado e InMemory | 7 testes da classe `InvestimentosConsolidadosTests` passaram. Seis usam InMemory para grupos, carteiras sem grupo, paginação, filtros, acesso e datas de vencimento/compra; um usa PostgreSQL para datas distintas de bases do Tesouro |
| Navegação e visual | Playwright com dados sintéticos | 11/11 cenários passaram em Chromium; o E2E também verifica entrada direta por carteira sem grupo. O cenário consolidado repetido após essa alteração passou 1/1. Larguras móveis e desktop definidas no spec. Conferência manual autenticada não executada |
| Regressões | Build .NET, tipos/lint/build frontend e suítes focadas | Filtro de integração Tesouro/consolidação passou 49/49, seguido da classe consolidada ampliada 7/7. 105 unitários passaram. Build API, typecheck e build Vite passaram; lint sem erros, com 10 avisos existentes |

## Riscos, gaps e pendências

- A consulta agrupa sobre todas as posições autorizadas antes de ordenar e paginar, e busca bases ativas por carteira. Isso pode crescer com o número de carteiras. Não há medição de volume ou impacto em produção; fica como risco futuro, não como defeito confirmado.
- A opção “Todos os grupos” mantém linhas por grupo e escopo fixo no detalhe. Identidade canônica, filtros de situação e datas conhecidas/diferentes/indisponíveis foram validados. Para renda fixa sem fonte comprovada, a data fica indisponível. Cadastros distintos permanecem separados. A referência pública do Tesouro continua como consulta separada com cache temporário.

## Entrega e sincronização

Plano, história e ADR registrados e indexados no repositório. A Issue #70 acompanha a entrega. Código e documentação permanecem no checkout até a publicação na branch do PR #62. Após o push, atualizar a Issue #70 com o link publicado do plano e confirmar que o PR contém o diff consolidado.
