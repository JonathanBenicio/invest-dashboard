# language: pt
Funcionalidade: Gestão do Ciclo de Vida da Carteira
  Para manter a organização do meu patrimônio
  Como um investidor
  Eu quero poder criar, listar e gerenciar múltiplas carteiras de investimento

  Cenário: Criar a primeira carteira de investimentos com sucesso
    Dado que o usuário está autenticado no sistema
    E o usuário não possui nenhuma carteira cadastrada
    Quando o usuário cria uma carteira com o nome "Aposentadoria" e saldo inicial de "0.00"
    Então o sistema deve salvar a nova carteira
    E a carteira deve ser automaticamente marcada como "IsDefault" (Principal)
    E a mensagem "Carteira criada com sucesso" deve ser exibida

  Cenário: Resumo consolidado de uma carteira
    Dado que o usuário possui uma carteira "Principal"
    E a carteira tem as seguintes posições:
      | Ativo | Quantidade | Preço Médio | Preço Atual |
      | PETR4 | 100        | 30.00       | 35.00       |
      | MXRF11| 10         | 10.00       | 10.50       |
    Quando o usuário acessa o resumo da carteira
    Então o sistema deve exibir o patrimônio total de "3605.00"
    E calcular a alocação de ativos separando "Ações" e "Fundos Imobiliários"

  Cenário: Exclusão em cascata da carteira
    Dado que o usuário possui a carteira "Trade Curto Prazo"
    E esta carteira contém transações e posições ativas
    Quando o usuário confirma a exclusão da carteira
    Então a carteira deve ser removida da base de dados
    E todas as transações, posições e metas associadas a esta carteira devem ser excluídas (exclusão em cascata)
    E a listagem de carteiras deve ser atualizada