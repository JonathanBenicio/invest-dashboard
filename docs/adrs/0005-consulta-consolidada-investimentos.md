# ADR-0005 — consulta consolidada de investimentos

- Data: 2026-11-26.
- Estado da decisão: adotada.
- Implementação: consulta autorizada, filtros por situação/titular/carteira/instituição, consolidação por identidade canônica e navegação lista/detalhe implementados.
- Validação: 49 integrações focadas, 105 unitários relacionados, 11 E2E, typecheck, lint e builds passaram em 26/11/2026. Comandos e limites estão no plano.
- Issue: [#70](https://github.com/JonathanBenicio/invest-dashboard/issues/70).
- Story: [US-PORT-003](../USER-STORIES.md#us-port-003--consolidar-investimentos-por-grupo).
- Plano: [Investimentos consolidados por grupo](../plan/investimentos-consolidados-por-grupo.md).

## Contexto e problema

A listagem atual de investimentos filtra e pagina posições individuais. A carteira é a unidade de autorização e registro das movimentações. O detalhe atual usa o ID de uma posição. Renda Fixa já oferece filtro por grupo, enquanto Ações ainda não tem o equivalente. O frontend de Ações atualiza a avaliação exibida com cotações disponíveis. Um total consolidado precisa combinar as posições acessíveis de todo o recorte e usar o mesmo critério de avaliação na lista e no detalhe.

## Decisão

Criar um contrato de leitura consolidada separado, sem alterar o formato das consultas individuais. No servidor, aplicar autorização e filtros antes de agrupar. Ordenar e paginar as linhas resultantes com desempate estável. O total da paginação conta as linhas consolidadas. A consulta de detalhe usa a mesma chave e regras do resumo e retorna os totais e a participação das carteiras acessíveis. Se precisar paginar contribuições no detalhe, calcular os totais sobre todas as contribuições autorizadas antes de paginar.

| Universo | Identidade para agrupar |
|---|---|
| Renda fixa com grupo | Grupo e identidade confiável do instrumento, associada a código, subtipo e data completa de vencimento. Ano é rótulo. Antes de unir registros distintos, comprovar equivalência de emissor e da emissão quando aplicável. |
| Ações com grupo | Grupo, código normalizado e subtipo, respeitando o identificador do instrumento e mercado quando necessário. Ano da compra fica nas movimentações. |
| Carteira sem grupo | Preservar a identidade da posição individual. |
| Identidade incompleta | Manter a posição individual visível. Unir somente quando houver elementos suficientes para demonstrar que se trata do mesmo instrumento. |

Usar de preferência o ID canônico do ativo, compartilhado pelas posições de cada carteira. O catálogo tem ticker único, mas isso não comprova que posições manuais e importadas com tickers diferentes sejam sempre identificáveis como o mesmo título. Cadastros com IDs distintos permanecem separados até que exista correspondência verificável. Nome ou ano coincidentes, isoladamente, não comprovam equivalência. Carteira e titular são filtros sobre as contribuições. O ID do titular vale dentro de seu grupo. Nomes não identificam instrumentos, grupos ou titulares.

Usar o controle de acesso já aplicado às carteiras como origem das linhas e opções. Filtrar carteiras privadas antes de calcular totais, contagens, opções, ordenação ou paginação. Validar grupo, titular e carteira no mesmo escopo autorizado a cada consulta. A visão consolidada não cria entidades de domínio nem destinos de escrita. Compra, venda e edição da posição, assim como exclusão, continuam em contratos individuais com suas regras de gestão.

A API usa `GET /api/v1/investments/consolidated` para listar e `GET /api/v1/investments/consolidated/{grupoId}/{ativoId}` para detalhar. A interface reutiliza `/investimento/$id` com a visão e os filtros na URL. A API aplica a autorização em cada leitura.

### Valores do resumo

- Somar quantidade somente para unidades comparáveis do mesmo instrumento. Somar valor investido e valor atual para valores comparáveis na mesma moeda. Separar posições incompatíveis no contrato.
- Quando os valores investido e atual forem comparáveis e completos, calcular resultado pela diferença dos totais. Calcular o percentual sobre o custo consolidado positivo. Usar indisponível quando faltar insumo. Preço médio, se exposto e aplicável, usa custo total dividido pela quantidade total positiva.
- Evitar somar percentuais, rentabilidades contratadas, preços unitários, taxas ou projeções de carteiras diferentes. Em Ações, compatibilizar fonte, data e atualização das cotações. Em renda fixa, preservar a data de referência de cada carteira e indicar no resumo se houver datas distintas, quando as datas de referência das avaliações forem diferentes.
- Diferenciar posição ativa, vencida e encerrada. A visão padrão mostra posições com saldo. Um filtro explícito permite consultar posições encerradas, sem acrescentar saldo ou custo delas ao consolidado padrão.

## Alternativas e trade-offs

| Alternativa | Benefício | Limite |
|---|---|---|
| Agrupar somente as linhas da página recebida | Mudança rápida na tela | Totais e paginação parciais |
| Buscar todas as páginas no navegador e agrupar | Reutiliza API de posição | Mais tráfego, risco de leituras inconsistentes e duplicação de regra entre telas |
| Leitura consolidada no servidor | Totais completos, autorização aplicada antes da soma e regra comum para lista e detalhe | Exige consulta, contrato e cobertura próprios |
| Materializar uma posição consolidada | Pode acelerar leituras volumosas | Introduz sincronização e deriva entre posições e totais sem evidência de necessidade |

## Consequências e migração

Sem migração de dados prevista para a primeira versão. A implementação deve avaliar o volume e a forma de consulta antes de decidir otimizações. A arquitetura mantém uma visão de leitura derivada de posições e preserva a unidade de autoria e auditoria por carteira. O detalhe individual continua resolvendo extrato, avaliações e transações pela posição. A visão consolidada mostra o saldo e sua composição. Não está prevista uma trilha fiscal ou extrato unificado entre titulares.

## Implementação

A consulta usa grupo e ID canônico do ativo, aplica autorização antes da soma e mantém as operações individuais. Lista e detalhe usam os valores persistidos. O histórico público do Tesouro aparece separadamente e não altera a posição. A entrega usa a branch do PR #62.

## Validação e gaps

Testes HTTP em InMemory cobrem agregação/paginação, carteiras sem grupo, grupos/titulares e encerradas. PostgreSQL isolado cobre datas diferentes entre bases do mesmo título e leitura compartilhada do Extrato. Playwright/MSW cobre navegação, filtros e responsividade. Não foi feita conferência autenticada manual. A cotação pública não é usada como valor pessoal.
