# ADR-0002: Avaliações de renda fixa vinculadas à posição

## Estado

Proposta — implementação validada localmente na branch do PR #44; decisão arquitetural permanece sujeita à revisão do PR.

## Contexto

O histórico `historical_prices` é vinculado globalmente ao ativo. Isso é apropriado para cotações de mercado, mas avaliações manuais de extrato também eram gravadas ali. Como ativos podem ser compartilhados entre carteiras, uma projeção ou o gráfico de uma carteira podia ler o valor informado por outra. Além disso, a posição refletia a avaliação enviada mais recentemente, enquanto a projeção selecionava a maior data de observação.

## Decisão proposta

- Persistir avaliações manuais em uma tabela própria vinculada a `asset_positions.id`, com data observada, preço unitário, quantidade observada e instante de registro.
- Considerar vigente a observação com maior data; em empate, usar o maior instante de registro e, por fim, o identificador como desempate determinístico.
- Usar a mesma observação autorizada da posição para o valor atual, projeção e gráfico. Manter no histórico global apenas preços de mercado.
- Migrar uma avaliação antiga somente quando o ativo pertencer a uma única posição e a data da avaliação coincidir exatamente com a data de uma compra da mesma posição. Manter registros sem associação inequívoca no histórico legado, sem exibi-los nem usá-los em projeções por posição; o usuário poderá registrar nova avaliação.
- Excluir avaliações vinculadas junto com a posição, usando FK com exclusão em cascata.

## Consequências

Avaliações deixam de vazar entre carteiras que compartilham ativos, e datas retroativas não substituem a observação vigente. Registros antigos ambíguos deixam de alimentar gráficos e projeções até nova avaliação; os dados brutos legados não são apagados. A aplicação e o histórico da posição precisarão consultar a nova tabela.

## Migração e rollback

A migração cria a tabela/indexes, copia apenas observações legadas com associação única e atualiza o preço atual da posição a partir da observação migrada mais recente. A reversão remove a tabela e preserva o histórico global existente; dados manuais criados após a migração não podem ser convertidos de volta ao escopo global sem perder o isolamento, portanto rollback requer restaurar a aplicação junto de backup/coorte compatível.

## Verificação

Testes de API/InMemory e PostgreSQL cobrem isolamento entre usuários, avaliação retroativa, histórico autorizado, migração com e sem associação única e persistência após reiniciar o host. A proposta não aprova homologação externa nem deploy.
