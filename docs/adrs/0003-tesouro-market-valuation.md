# ADR-0003 — cotação pública na avaliação de posições do Tesouro Direto

- Data: 2026-11-18
- Estado da decisão: proposta
- Implementação: parcial, apenas como rascunho local na branch `codex/tesouro-avaliacao-liquida`; nenhum commit ou validação
- Issue: [#64 — Tesouro Direto: avaliar posição com cotação pública e líquido estimado](https://github.com/JonathanBenicio/invest-dashboard/issues/64)
- Story: [US-PORT-003](../USER-STORIES.md#us-port-003--avaliação-atual-de-posições-do-tesouro-direto)
- Plano: [avaliação atual de posições do Tesouro Direto](../plan/avaliacao-liquida-tesouro-direto.md)

> Esta é uma proposta técnica para revisão. A direção do produto aprovada pelo usuário é usar PU base × quantidade como valor bruto de referência e manter tributos/taxas do extrato datados. O modelo de persistência/consulta dos valores agregados ainda precisa de aprovação.

## Contexto e problema

O repositório consulta o CSV histórico oficial do Tesouro Transparente, mas essa cotação ainda não é associada automaticamente às posições nem aplicada aos totais da carteira. A posição de renda fixa contém preço registrado, enquanto o extrato HTML fornece uma observação privada com data própria. Usar um dado como se fosse o outro pode apresentar um valor desatualizado ou substituir indevidamente o histórico/custo.

A issue #64 requer usar a cotação pública para calcular a posição e os totais, exibir fonte e data e manter o ledger inalterado. Também requer não apresentar valor líquido quando custos atuais, lotes ou eventos necessários estiverem ausentes.

## Decisão proposta

- Tratar a cotação do Tesouro Transparente como referência de mercado compartilhada, associada por tipo de título e vencimento.
- Calcular uma avaliação de leitura a partir do PU base público e da quantidade atual da posição, com origem e data explícitas.
- Usar essa avaliação nos valores atuais da posição e nos agregados da carteira sem gravar uma transação, alterar custo de aquisição, substituir a base do extrato ou sobrescrever avaliações privadas.
- Manter impostos e taxas do HTML como observações do extrato na data em que foram informados. Só disponibilizar líquido estimado quando todos os componentes forem recalculáveis para a data da cotação; caso contrário, mostrar o motivo da indisponibilidade.
- Reutilizar o cache compartilhado já usado pelo provider do CSV. Não consultar uma vez por posição se a carteira puder ser calculada a partir de um catálogo/série já carregado.
- Deixar persistência diária de cotação, histórico comparativo e integração com CDI para plano posterior próprio.

## Alternativas e trade-offs

| Alternativa | Benefícios | Custos/riscos | Motivo da avaliação |
|---|---|---|---|
| Atualizar `PosicaoInvestimento.CurrentPrice` durante um GET | Reaproveita diretamente os agregados atuais | GET teria efeito persistente, concorrência e fonte externa mudariam estado de posição sem evento explícito | Rejeitada na proposta |
| Gravar cotação pública como avaliação manual/valor do extrato | Reaproveita fluxo já existente de avaliações | Mistura proveniência, pode substituir dado privado e dificulta auditoria | Rejeitada na proposta |
| Usar cotação como read model e preservar ledger/extrato | Separa fonte de mercado e dados pessoais; não altera operações | Exige ajustar consultas de posição e totais, tratar indisponibilidade/defasagem e cache | Recomendada para revisão |
| Manter apenas o preço gravado pelo último extrato | Menor mudança | O valor fica desatualizado quando houver nova cotação pública | Não atende à direção do usuário |

## Consequências e migração

- Camadas: provider de mercado existente continua responsável pela leitura do CSV; Application resolve o título/posição autorizada e calcula os valores derivados; WebAPI mantém contratos versionados; frontend identifica origem e data.
- Dados: sem alteração de schema ou ledger nesta proposta. O histórico persistido de preços e a comparação de desempenho continuam no plano separado.
- Autorização: a consulta da posição deve começar pela carteira acessível ao usuário; os preços públicos não podem contornar o isolamento da posição.
- Operação: a cotação pode estar indisponível ou defasada. A UI deve indicar data e não chamar o valor de atual sem referência. Os totais precisam reutilizar dados em cache e não multiplicar chamadas externas por posição.
- Líquido: tributos e taxas não serão persistidos como verdade atual. IR/IOF exigem lotes/datas/custos completos e custos de custódia/instituição vigentes. Cupom não reconciliado bloqueia o líquido.
- Migração/rollback: N/A enquanto a implementação permanecer como leitura derivada e não persistir avaliações públicas. Se a persistência mudar, reabrir esta ADR e planejar migração/rollback antes do schema.

## Implementação

- Estado e escopo implementado: rascunho local de endpoint e cartão de detalhe; sem aplicação nos agregados da carteira, sem testes, sem commit e sem validação.
- Referências/PR/commits: branch separada local `codex/tesouro-avaliacao-liquida`; PR ainda não criado.
- Restante: revisar e aceitar/rejeitar proposta; completar posição e agregados; corrigir cálculos/estados fiscais; testar autorização, datas, cache e mutações.

## Validação e gaps

- Critérios de validação: cotação correta por título/vencimento; cálculo bruto por quantidade atual em posição e totais; nenhum efeito no ledger; origem/data explícitas; falha e cache defasado visíveis; isolamento multiusuário.
- Execuções e evidências reais: não executadas. O host WSL não tem `dotnet` instalado; a branch não foi compilada nem testada.
- Limitações e gaps: taxas do extrato são históricas; cupons ainda podem não estar no ledger; fórmula atual de custos e comportamento de carteira não estão aprovados nem validados.
